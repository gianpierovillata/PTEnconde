#!/bin/sh
# =============================================================================
# Genera /usr/share/nginx/html/assets/config.json con las URLs del .env
# (FRONTEND_API_BASE_URL / FRONTEND_SIGNALR_URL) y arranca nginx.
# Así la MISMA imagen sirve para local y para los túneles de ngrok.
# =============================================================================
set -e

DESTINO="/usr/share/nginx/html/assets/config.json"
API_BASE_URL="${FRONTEND_API_BASE_URL:-http://localhost:5088}"
SIGNALR_URL="${FRONTEND_SIGNALR_URL:-${API_BASE_URL%/}/hubs/inbox}"

mkdir -p "$(dirname "$DESTINO")"
cat > "$DESTINO" <<EOF
{
  "apiBaseUrl": "${API_BASE_URL}",
  "signalRUrl": "${SIGNALR_URL}"
}
EOF

echo "[entrypoint] config.json → apiBaseUrl=${API_BASE_URL} signalRUrl=${SIGNALR_URL}"

exec nginx -g 'daemon off;'
