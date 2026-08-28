#!/usr/bin/env bash
# Cria o banco OpenMU no Postgres (stack postgres_postgres) e restaura o dump local.
#
# Uso na VPS:
#   chmod +x tools/setup-openmu-postgres.sh
#   ./tools/setup-openmu-postgres.sh /root/openmu-local.dump
#
# Variaveis opcionais:
#   PGSVC=postgres_postgres   nome do servico/container postgres
#   DB_NAME=openmu
#   DB_USER=openmu             usuario dedicado (criado se nao existir)
#   DB_PASSWORD=MuEldryn2026!  senha do usuario openmu (padrao; mude!)
#   SKIP_USER=1                usa so postgres (nao cria usuario openmu)
set -euo pipefail

DUMP="${1:-/root/openmu-local.dump}"
DB_NAME="${DB_NAME:-openmu}"
DB_USER="${DB_USER:-openmu}"
DB_PASSWORD="${DB_PASSWORD:-MuEldryn2026!}"
PGSVC="${PGSVC:-postgres_postgres}"
SKIP_USER="${SKIP_USER:-0}"

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

log()  { echo -e "${GREEN}==>${NC} $*"; }
fail() { echo -e "${RED}ERRO:${NC} $*" >&2; exit 1; }

[[ -f "$DUMP" ]] || fail "Dump nao encontrado: $DUMP"

CID=$(docker ps -q -f "name=${PGSVC}")
[[ -n "$CID" ]] || fail "Container Postgres nao encontrado (filtro: ${PGSVC}). Rode: docker ps | grep postgres"

log "Postgres container: $CID"

# Senha do superuser postgres (stack)
POSTGRES_SUPER_PW=$(docker service inspect "$PGSVC" --format '{{range .Spec.TaskTemplate.ContainerSpec.Env}}{{println .}}{{end}}' 2>/dev/null \
  | grep '^POSTGRES_PASSWORD=' | cut -d= -f2- || true)

if [[ -z "$POSTGRES_SUPER_PW" ]]; then
  POSTGRES_SUPER_PW=$(docker inspect "$CID" --format '{{range .Config.Env}}{{println .}}{{end}}' \
    | grep '^POSTGRES_PASSWORD=' | cut -d= -f2- || true)
fi

psql() {
  docker exec -e PGPASSWORD="${POSTGRES_SUPER_PW}" "$CID" psql -U postgres -v ON_ERROR_STOP=1 "$@"
}

log "1/4 — Criando database '$DB_NAME' (UTF8)..."
psql -tc "SELECT 1 FROM pg_database WHERE datname = '${DB_NAME}'" | grep -q 1 \
  && log "   Database '$DB_NAME' ja existe (ok)" \
  || psql -c "CREATE DATABASE \"${DB_NAME}\"
       WITH ENCODING 'UTF8'
            LC_COLLATE 'en_US.utf8'
            LC_CTYPE 'en_US.utf8'
            TEMPLATE template0;"

if [[ "$SKIP_USER" != "1" ]]; then
  log "2/4 — Usuario dedicado '$DB_USER'..."
  psql -tc "SELECT 1 FROM pg_roles WHERE rolname = '${DB_USER}'" | grep -q 1 \
    && psql -c "ALTER USER \"${DB_USER}\" WITH PASSWORD '${DB_PASSWORD}';" \
    || psql -c "CREATE USER \"${DB_USER}\" WITH PASSWORD '${DB_PASSWORD}';"
  psql -c "GRANT ALL PRIVILEGES ON DATABASE \"${DB_NAME}\" TO \"${DB_USER}\";"
  psql -c "ALTER DATABASE \"${DB_NAME}\" OWNER TO \"${DB_USER}\";" 2>/dev/null || true
else
  log "2/4 — Pulando usuario dedicado (SKIP_USER=1)"
fi

log "3/4 — Restaurando dump (contas, chars, itens, joias)..."
if head -c 5 "$DUMP" | grep -q '^PGDMP'; then
  docker exec -i -e PGPASSWORD="${POSTGRES_SUPER_PW}" "$CID" \
    pg_restore -U postgres -d "$DB_NAME" --clean --if-exists --no-owner --no-acl < "$DUMP"
elif [[ "$DUMP" == *.sql ]]; then
  docker exec -i -e PGPASSWORD="${POSTGRES_SUPER_PW}" "$CID" \
    psql -U postgres -d "$DB_NAME" < "$DUMP"
else
  fail "Dump invalido (corrompido no upload Windows?). Regenere com Export-LocalDatabases.ps1 e reenvie."
fi

log "4/4 — Permissoes e verificacao..."
if [[ "$SKIP_USER" != "1" ]]; then
  psql -d "$DB_NAME" -c "GRANT ALL ON SCHEMA public TO \"${DB_USER}\";"
  psql -d "$DB_NAME" -c "GRANT ALL ON ALL TABLES IN SCHEMA public TO \"${DB_USER}\";"
  psql -d "$DB_NAME" -c "GRANT ALL ON ALL SEQUENCES IN SCHEMA public TO \"${DB_USER}\";"
  for schema in data config admin panel; do
    psql -d "$DB_NAME" -tc "SELECT 1 FROM information_schema.schemata WHERE schema_name = '${schema}'" | grep -q 1 \
      && psql -d "$DB_NAME" -c "GRANT ALL ON SCHEMA \"${schema}\" TO \"${DB_USER}\";
                              GRANT ALL ON ALL TABLES IN SCHEMA \"${schema}\" TO \"${DB_USER}\";
                              GRANT ALL ON ALL SEQUENCES IN SCHEMA \"${schema}\" TO \"${DB_USER}\";" \
      || true
  done
fi

ACCOUNTS=$(psql -d "$DB_NAME" -tAc 'SELECT COUNT(*) FROM data."Account";' 2>/dev/null || echo "?")
CHARS=$(psql -d "$DB_NAME" -tAc 'SELECT COUNT(*) FROM data."Character";' 2>/dev/null || echo "?")
ITEMS=$(psql -d "$DB_NAME" -tAc 'SELECT COUNT(*) FROM data."Item";' 2>/dev/null || echo "?")

echo ""
echo "============================================"
echo " Banco OpenMU pronto: ${DB_NAME}"
echo " Contas:      ${ACCOUNTS}  (esperado: 22)"
echo " Personagens: ${CHARS}  (esperado: 78)"
echo " Itens:       ${ITEMS}  (esperado: ~4615)"
echo "============================================"
echo ""
echo "Coloque no ~/mueldryn/deploy/swarm-vps/.env :"
echo ""
if [[ "$SKIP_USER" != "1" ]]; then
  echo "  DB_HOST=postgres_postgres"
  echo "  DB_ADMIN_USER=${DB_USER}"
  echo "  DB_ADMIN_PW=${DB_PASSWORD}"
else
  echo "  DB_HOST=postgres_postgres"
  echo "  DB_ADMIN_USER=postgres"
  echo "  DB_ADMIN_PW=<POSTGRES_PASSWORD do stack>"
fi
echo "  Database__AssumeExternallyProvisioned=true  (ja no compose)"
echo ""
echo "Depois: cd ~/mueldryn/deploy/swarm-vps && ./deploy.sh"
echo ""
