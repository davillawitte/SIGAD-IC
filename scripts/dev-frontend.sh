#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=load-env.sh
source "$ROOT_DIR/scripts/load-env.sh"
load_dotenv "$ROOT_DIR/.env"

if [ -z "${NODE_AUTH_TOKEN:-}" ]; then
  echo "Aviso: NODE_AUTH_TOKEN não definido."
  echo "Configure em .env (cp .env.example .env) ou execute ./scripts/setup-frontend.sh."
fi

export PATH="${NVM_HOME:-$HOME/AppData/Roaming/nvm}/v24.18.0:$PATH"

cd "$ROOT_DIR/frontend"
npm start
