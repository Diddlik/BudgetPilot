#!/usr/bin/env bash
# Deploy / update BudgetPilot on a Docker host (reverse proxy handled by Nginx Proxy Manager).
#
# Usage:
#   ./scripts/deploy.sh              # build + (re)start using docker-compose.npm.yml
#   ./scripts/deploy.sh --pull       # git pull first, then build + restart
#   COMPOSE_FILE=docker-compose.yml ./scripts/deploy.sh   # use a different compose file
set -euo pipefail

# Always run from the repository root (this script lives in scripts/).
cd "$(dirname "$0")/.."

COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.npm.yml}"

# docker compose (v2) or legacy docker-compose
if docker compose version >/dev/null 2>&1; then
  DC="docker compose"
elif command -v docker-compose >/dev/null 2>&1; then
  DC="docker-compose"
else
  echo "ERROR: docker compose is not installed." >&2
  exit 1
fi

# Require a configured .env (auth credentials + APP_PORT).
if [ ! -f .env ]; then
  echo "ERROR: .env not found. Create it first:" >&2
  echo "  cp .env.example .env   then edit BP_AUTH_EMAIL / BP_AUTH_PASSWORD / APP_PORT" >&2
  exit 1
fi

if [ "${1:-}" = "--pull" ]; then
  echo "==> git pull"
  git pull --ff-only
fi

echo "==> Building and starting ($COMPOSE_FILE)"
$DC -f "$COMPOSE_FILE" up -d --build

echo "==> Pruning old dangling images"
docker image prune -f >/dev/null 2>&1 || true

# Read APP_PORT from .env (default 8080) for the hint below.
APP_PORT="$(grep -E '^APP_PORT=' .env 2>/dev/null | tail -1 | cut -d= -f2)"
APP_PORT="${APP_PORT:-8080}"

echo "==> Status"
$DC -f "$COMPOSE_FILE" ps

cat <<EOF

Done. BudgetPilot is listening on host port ${APP_PORT}.
In Nginx Proxy Manager add a Proxy Host:
  - Forward Hostname/IP : <this server's LAN IP>   (or container name on a shared network)
  - Forward Port        : ${APP_PORT}
  - Scheme              : http   (NPM terminates TLS; the app trusts X-Forwarded-Proto)
  - Enable "Websockets Support"  (required for Blazor Server / SignalR)

Logs:  $DC -f $COMPOSE_FILE logs -f
Stop:  $DC -f $COMPOSE_FILE down
EOF
