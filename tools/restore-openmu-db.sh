#!/usr/bin/env bash
# Restaura openmu-local.dump no Postgres do stack postgres_postgres (Swarm).
# Uso: ./restore-openmu-db.sh /root/openmu-local.dump
set -euo pipefail

DUMP="${1:-/root/openmu-local.dump}"
DB_NAME="${DB_NAME:-openmu}"
PGSVC="${PGSVC:-postgres_postgres}"

if [[ ! -f "$DUMP" ]]; then
  echo "Arquivo nao encontrado: $DUMP"
  echo "Baixe antes (GitHub Release ou transfer.sh)."
  exit 1
fi

CID=$(docker ps -q -f "name=${PGSVC}")
if [[ -z "$CID" ]]; then
  echo "Container postgres nao encontrado (filtro: ${PGSVC})."
  docker ps | grep -i postgres || true
  exit 1
fi

echo "==> Postgres container: $CID"
echo "==> Criando database '$DB_NAME' (se nao existir)..."
docker exec "$CID" psql -U postgres -tc "SELECT 1 FROM pg_database WHERE datname = '$DB_NAME'" | grep -q 1 \
  || docker exec "$CID" psql -U postgres -c "CREATE DATABASE \"$DB_NAME\";"

echo "==> Restaurando dump..."
docker exec -i "$CID" pg_restore -U postgres -d "$DB_NAME" --clean --if-exists --no-owner --no-acl < "$DUMP"

echo "==> Contagens:"
docker exec "$CID" psql -U postgres -d "$DB_NAME" -c 'SELECT COUNT(*) AS accounts FROM data."Account";'
docker exec "$CID" psql -U postgres -d "$DB_NAME" -c 'SELECT COUNT(*) AS characters FROM data."Character";'
echo "OK."
