#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "==> Subindo stack com Docker Compose..."
docker compose -f "$ROOT_DIR/docker/docker-compose.yml" up --build -d

echo "==> Aguardando serviços..."
sleep 15

echo "==> Health check da API..."
curl -sf "http://localhost:5080/health" | head -c 200 || echo "(API ainda iniciando — tente novamente em alguns segundos)"
echo ""

echo "==> Frontend:  http://localhost:4200"
echo "==> API:       http://localhost:5080/swagger"
echo "==> Postgres:  localhost:5433 (host) — banco gestao_ic_dev"
