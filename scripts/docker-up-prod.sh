#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=load-env.sh
source "$ROOT_DIR/scripts/load-env.sh"

ENV_FILE="${1:-$ROOT_DIR/.env.prod}"
if [ ! -f "$ENV_FILE" ]; then
  echo "Erro: arquivo de ambiente não encontrado: $ENV_FILE"
  echo ""
  echo "  cp .env.prod.example .env.prod"
  echo "  # edite .env.prod (DB_PASSWORD, JWT_KEY, NODE_AUTH_TOKEN)"
  echo "  ./scripts/docker-up-prod.sh"
  echo ""
  exit 1
fi

load_dotenv "$ENV_FILE"

missing=()
[ -z "${NODE_AUTH_TOKEN:-}" ] && missing+=("NODE_AUTH_TOKEN")
[ -z "${DB_PASSWORD:-}" ] && missing+=("DB_PASSWORD")
[ -z "${JWT_KEY:-}" ] && missing+=("JWT_KEY")
if [ "${#missing[@]}" -gt 0 ]; then
  echo "Erro: variáveis obrigatórias ausentes em $ENV_FILE: ${missing[*]}"
  exit 1
fi

if [ "${#JWT_KEY}" -lt 32 ]; then
  echo "Erro: JWT_KEY deve ter pelo menos 32 caracteres."
  exit 1
fi

APP_ORIGIN="${APP_ORIGIN:-http://10.9.233.98:8080}"
EDGE_HTTP_PORT="${EDGE_HTTP_PORT:-8080}"

echo "==> Garantindo rede Docker externa 'edge' (compartilhável com SCFA no futuro)..."
if ! docker network inspect edge >/dev/null 2>&1; then
  docker network create edge
  echo "    Rede 'edge' criada."
else
  echo "    Rede 'edge' já existe."
fi

echo "==> Subindo stack SIGAD-IC (sem publicar portas no host)..."
echo "    Origem pública: ${APP_ORIGIN}"
cd "$ROOT_DIR"
docker compose -f docker-compose.prod.yml --env-file "$ENV_FILE" up --build -d

echo "==> Subindo nginx de borda na porta ${EDGE_HTTP_PORT} (SCFA permanece na 80)..."
EDGE_HTTP_PORT="$EDGE_HTTP_PORT" docker compose -f docker-compose.edge.yml --env-file "$ENV_FILE" up -d

echo "==> Aguardando serviços..."
sleep 15

echo "==> Health check via edge nginx..."
if curl -sf "http://127.0.0.1:${EDGE_HTTP_PORT}/health" | head -c 300; then
  echo ""
  echo "==> OK"
else
  echo ""
  echo "(API ainda iniciando — tente /health em alguns segundos)"
fi
echo ""

echo "==> SIGAD-IC:  ${APP_ORIGIN}/"
echo "==> Health:    ${APP_ORIGIN}/health"
echo "==> SCFA:      http://10.9.233.98/  (intocado; porta 80)"
echo "==> Postgres e API do SIGAD apenas na rede Docker."
