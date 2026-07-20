#!/usr/bin/env bash
set -euo pipefail

if [ -z "${NODE_AUTH_TOKEN:-}" ]; then
  echo "Erro: NODE_AUTH_TOKEN não definido."
  echo ""
  echo "Crie um Personal Access Token em https://github.com/settings/tokens"
  echo "com escopo 'read:packages' e exporte:"
  echo ""
  echo "  export NODE_AUTH_TOKEN=ghp_xxxxxxxx"
  echo ""
  exit 1
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export PATH="${NVM_HOME:-$HOME/AppData/Roaming/nvm}/v24.18.0:$PATH"

cd "$ROOT_DIR/frontend"
cp -n .npmrc.example .npmrc 2>/dev/null || true
npm ci

echo "Dependências instaladas (incluindo @davillawitte/pci-design-system)."
