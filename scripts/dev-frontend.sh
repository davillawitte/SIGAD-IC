#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if [ -z "${NODE_AUTH_TOKEN:-}" ]; then
  echo "Aviso: NODE_AUTH_TOKEN não definido. Execute ./scripts/setup-frontend.sh primeiro."
fi

export PATH="${NVM_HOME:-$HOME/AppData/Roaming/nvm}/v24.18.0:$PATH"

cd "$ROOT_DIR/frontend"
npm start
