#!/usr/bin/env bash
# Restaura dump OpenMU local no Postgres da VPS.
# Uso na VPS:
#   chmod +x import-openmu-vps.sh
#   ./import-openmu-vps.sh openmu-local.dump
set -euo pipefail

DUMP="${1:-openmu-local.dump}"
DB_NAME="${DB_NAME:-openmu}"
PGUSER="${PGUSER:-postgres}"
PGHOST="${PGHOST:-127.0.0.1}"

if [[ ! -f "$DUMP" ]]; then
  echo "Arquivo nao encontrado: $DUMP"
  exit 1
fi

echo "==> Criando database '$DB_NAME' (se nao existir)..."
sudo -u postgres psql -h "$PGHOST" -tc "SELECT 1 FROM pg_database WHERE datname = '$DB_NAME'" | grep -q 1 \
  || sudo -u postgres createdb -h "$PGHOST" "$DB_NAME"

echo "==> Restaurando dump (substitui dados existentes)..."
# --clean remove objetos antes de recriar; --if-exists evita erro se vazio
pg_restore -h "$PGHOST" -U "$PGUSER" -d "$DB_NAME" --clean --if-exists --no-owner --no-acl "$DUMP"

echo "==> Contagens pos-restore:"
sudo -u postgres psql -h "$PGHOST" -d "$DB_NAME" -c 'SELECT COUNT(*) AS accounts FROM data."Account";'
sudo -u postgres psql -h "$PGHOST" -d "$DB_NAME" -c 'SELECT COUNT(*) AS characters FROM data."Character";'

echo "OK. OpenMU pode subir com Database__AssumeExternallyProvisioned=true"
