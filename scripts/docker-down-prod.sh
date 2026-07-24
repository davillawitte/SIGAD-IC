#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${1:-$ROOT_DIR/.env.prod}"

cd "$ROOT_DIR"
if [ -f "$ENV_FILE" ]; then
  docker compose -f docker-compose.prod.yml --env-file "$ENV_FILE" down
else
  docker compose -f docker-compose.prod.yml down
fi
echo "==> Stack de produção parada."
