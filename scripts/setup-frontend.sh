#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=load-env.sh
source "$ROOT_DIR/scripts/load-env.sh"
load_dotenv "$ROOT_DIR/.env"

if [ -z "${NODE_AUTH_TOKEN:-}" ]; then
  echo "Erro: NODE_AUTH_TOKEN não definido."
  echo ""
  echo "Crie um Personal Access Token em https://github.com/settings/tokens"
  echo "com escopo 'read:packages' e configure uma vez:"
  echo ""
  echo "  cp .env.example .env"
  echo "  # edite .env e preencha NODE_AUTH_TOKEN=ghp_xxxxxxxx"
  echo ""
  echo "Ou exporte na sessão atual:"
  echo ""
  echo "  export NODE_AUTH_TOKEN=ghp_xxxxxxxx"
  echo ""
  exit 1
fi

export PATH="${NVM_HOME:-$HOME/AppData/Roaming/nvm}/v24.18.0:$PATH"

cd "$ROOT_DIR/frontend"
cp -n .npmrc.example .npmrc 2>/dev/null || true
npm ci

echo "Dependências instaladas (incluindo @davillawitte/pci-design-system)."
