#!/usr/bin/env bash
# Executa comandos `dotnet` dentro do container SDK (serviço `sdk` do compose),
# sem precisar do .NET instalado no host.
#
#   ./backend/dotnet.sh build                       # compila a API
#   ./backend/dotnet.sh ef migrations add Inicial   # cria migration (instala dotnet-ef se faltar)
#   ./backend/dotnet.sh ef migrations script -o /src/migrations.sql   # gera SQL em backend/migrations.sql
#   ./backend/dotnet.sh --info
set -euo pipefail
cd "$(dirname "$0")/.."

if [[ "${1:-}" == "ef" ]]; then
  # dotnet-ef não vem no SDK; instala como tool global no volume dotnet_tools (1ª vez só).
  docker compose --profile dev run --rm --entrypoint bash sdk -lc \
    'export PATH="$PATH:/root/.dotnet/tools"; command -v dotnet-ef >/dev/null 2>&1 || dotnet tool install -g dotnet-ef --version 8.*; dotnet ef "$@"' -- "${@:2}"
else
  docker compose --profile dev run --rm sdk "$@"
fi
