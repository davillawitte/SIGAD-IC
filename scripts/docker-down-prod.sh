#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${1:-$ROOT_DIR/.env.prod}"

cd "$ROOT_DIR"

echo "==> Parando nginx de borda (SIGAD)..."
if [ -f "$ENV_FILE" ]; then
  docker compose -f docker-compose.edge.yml --env-file "$ENV_FILE" down || true
else
  docker compose -f docker-compose.edge.yml down || true
fi

echo "==> Parando stack SIGAD-IC..."
if [ -f "$ENV_FILE" ]; then
  docker compose -f docker-compose.prod.yml --env-file "$ENV_FILE" down
else
  docker compose -f docker-compose.prod.yml down
fi

echo "==> Stacks SIGAD paradas."
echo "    A rede 'edge' foi mantida (external) — não afeta o SCFA."
echo "    Para removê-la só se ninguém mais usar: docker network rm edge"
