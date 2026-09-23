#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=load-env.sh
source "$ROOT_DIR/scripts/load-env.sh"
load_dotenv "$ROOT_DIR/.env"

if [ -z "${NODE_AUTH_TOKEN:-}" ]; then
  echo "Erro: NODE_AUTH_TOKEN não definido (necessário para build do frontend)."
  echo ""
  echo "  cp .env.example .env"
  echo "  # edite .env e preencha NODE_AUTH_TOKEN=ghp_xxxxxxxx"
  echo ""
  exit 1
fi

echo "==> Subindo stack com Docker Compose..."
cd "$ROOT_DIR"
docker compose up --build -d

echo "==> Aguardando serviços..."
sleep 15

echo "==> Health check da API..."
curl -sf "http://localhost:5080/health" | head -c 200 || echo "(API ainda iniciando — tente novamente em alguns segundos)"
echo ""

echo "==> Frontend:  http://localhost:4200"
echo "==> API:       http://localhost:5080/swagger"
echo "==> Postgres:  localhost:5433 (host) — banco gestao_ic_dev"
