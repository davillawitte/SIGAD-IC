#!/usr/bin/env bash
# Backup do Postgres via docker exec (dev ou prod).
# Uso:
#   ./scripts/backup-postgres.sh              # detecta container em execução
#   ./scripts/backup-postgres.sh prod         # força sigad-ic-postgres / gestao_ic
#   ./scripts/backup-postgres.sh dev          # força template-postgres / gestao_ic_dev

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MODE="${1:-auto}"
STAMP="$(date +%Y%m%d_%H%M%S)"
OUT_DIR="${ROOT_DIR}/backups"
mkdir -p "$OUT_DIR"

resolve_target() {
  case "$MODE" in
    prod)
      CONTAINER="sigad-ic-postgres"
      DB_NAME="${DB_NAME:-gestao_ic}"
      DB_USER="${DB_USER:-postgres}"
      ;;
    dev)
      CONTAINER="template-postgres"
      DB_NAME="gestao_ic_dev"
      DB_USER="postgres"
      ;;
    auto)
      if docker ps --format '{{.Names}}' | grep -qx 'sigad-ic-postgres'; then
        CONTAINER="sigad-ic-postgres"
        DB_NAME="${DB_NAME:-gestao_ic}"
        DB_USER="${DB_USER:-postgres}"
      elif docker ps --format '{{.Names}}' | grep -qx 'template-postgres'; then
        CONTAINER="template-postgres"
        DB_NAME="gestao_ic_dev"
        DB_USER="postgres"
      else
        echo "Erro: nenhum container Postgres do SIGAD-IC está em execução."
        echo "Suba o ambiente (dev ou prod) e tente novamente."
        exit 1
      fi
      ;;
    *)
      echo "Uso: $0 [auto|dev|prod]"
      exit 1
      ;;
  esac
}

resolve_target

if ! docker ps --format '{{.Names}}' | grep -qx "$CONTAINER"; then
  echo "Erro: container '$CONTAINER' não está em execução."
  exit 1
fi

# Carrega .env.prod se existir (senha/usuário de produção).
if [ -f "$ROOT_DIR/.env.prod" ]; then
  # shellcheck source=load-env.sh
  source "$ROOT_DIR/scripts/load-env.sh"
  load_dotenv "$ROOT_DIR/.env.prod"
  if [ "$CONTAINER" = "sigad-ic-postgres" ]; then
    DB_NAME="${DB_NAME:-gestao_ic}"
    DB_USER="${DB_USER:-postgres}"
  fi
fi

FILE_BASE="gestao_ic_${STAMP}"
DUMP_CUSTOM="${OUT_DIR}/${FILE_BASE}.dump"
DUMP_SQL="${OUT_DIR}/${FILE_BASE}.sql"

echo "==> Backup de '${DB_NAME}' em container '${CONTAINER}'..."
# Stream via stdout (evita bugs de path docker cp no Git Bash/Windows).
MSYS_NO_PATHCONV=1 docker exec -e PGPASSWORD="${DB_PASSWORD:-}" "$CONTAINER" \
  pg_dump -U "$DB_USER" -d "$DB_NAME" -Fc > "$DUMP_CUSTOM"

MSYS_NO_PATHCONV=1 docker exec -e PGPASSWORD="${DB_PASSWORD:-}" "$CONTAINER" \
  pg_dump -U "$DB_USER" -d "$DB_NAME" --no-owner --no-acl > "$DUMP_SQL"

echo "==> Backup concluído:"
echo "    $DUMP_CUSTOM  (pg_restore -Fc)"
echo "    $DUMP_SQL     (psql / texto)"
ls -lh "$DUMP_CUSTOM" "$DUMP_SQL"
