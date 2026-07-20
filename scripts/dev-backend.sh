#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

cd "$ROOT_DIR/backend"
dotnet restore TemplateSistema.sln
dotnet run --project src/Api/TemplateSistema.Api.csproj
