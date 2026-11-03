#!/usr/bin/env bash
# Holt neue Images aus der GitHub Container Registry und startet sie. Der Timer aus
# install-auto-deploy.sh ruft das jede Minute auf, ohne neues Image passiert nichts.
# Ablauf: Backup, Migrationen, Container tauschen, prüfen, bei Fehler zurück zum alten Image.
# Logs: journalctl -u equipressure-deploy
set -euo pipefail

cd "$(dirname "$(readlink -f "$0")")"

# Ein Deployment kann länger als eine Minute dauern.
exec 9> .deploy.lock
flock -n 9 || exit 0

REPO=syp-equipressure/equipressure
KEEP_BACKUPS=14
FAILED_FILE=.deploy-failed

log() { printf '%s\n' "$*"; }

domain() { grep "^$1=" .env | cut -d= -f2- | tr -d "\"'"; }

image_name() { echo "ghcr.io/syp-equipressure/equipressure-$1:latest"; }

wanted_image() { docker image inspect -f '{{.Id}}' "$(image_name "$1")"; }

running_image() {
  local container
  container="$(docker compose ps -q "$1")"
  [ -z "$container" ] || docker inspect -f '{{.Image}}' "$container"
}

revision() {
  docker image inspect -f '{{index .Config.Labels "org.opencontainers.image.revision"}}' "$(image_name "$1")"
}

backup_database() {
  mkdir -p backups
  local file="backups/app-$(date +%Y%m%d-%H%M%S).dump"
  (umask 077; docker compose exec -T postgres pg_dump -U postgres -Fc app > "$file")
  ls -1t backups/*.dump | tail -n +$((KEEP_BACKUPS + 1)) | xargs -r rm --
  log "Backup: $file"
}

# schema.sql und stammdaten.sql dürfen mehrfach laufen, sie stammen vom Stand des API-Images.
apply_sql() {
  curl -fsSL "https://raw.githubusercontent.com/$REPO/$1/server-setup/database/$2" \
    | sed '1s/^\xEF\xBB\xBF//' \
    | docker compose exec -T postgres psql -q -U app -d app -v ON_ERROR_STOP=1 > /dev/null
}

# Die API hat kein /health. Eine Antwort ohne Anmeldung (401) zeigt, dass sie läuft.
is_healthy() {
  local url code
  case "$1" in
    api) url="https://$(domain API_DOMAIN)/api/breeds" ;;
    sattler) url="https://$(domain APP_DOMAIN)/" ;;
  esac
  for _ in $(seq 1 18); do
    code="$(curl -s -o /dev/null -m 5 -w '%{http_code}' "$url" || true)"
    case "$code" in 200|401|403) return 0 ;; esac
    sleep 5
  done
  log "$1 antwortet nicht, zuletzt HTTP $code von $url"
  return 1
}

docker compose pull -q api sattler

changed=()
declare -A previous
for service in api sattler; do
  wanted="$(wanted_image "$service")"
  previous[$service]="$(running_image "$service")"
  [ "$wanted" != "${previous[$service]}" ] || continue
  # Ein fehlgeschlagenes Image wird nicht jede Minute neu versucht.
  grep -qxF "$wanted" "$FAILED_FILE" 2>/dev/null && continue
  changed+=("$service")
done

[ "${#changed[@]}" -gt 0 ] || exit 0
log "Neue Images: ${changed[*]}"

if [[ " ${changed[*]} " == *" api "* ]]; then
  rev="$(revision api)"
  [ -n "$rev" ] || { log "API-Image hat keine Revision, Abbruch"; exit 1; }
  backup_database
  apply_sql "$rev" schema.sql
  apply_sql "$rev" stammdaten.sql
fi

docker compose up -d --no-deps "${changed[@]}"

for service in "${changed[@]}"; do
  rev="$(revision "$service")"
  if is_healthy "$service"; then
    log "$service läuft auf ${rev:0:8}"
    continue
  fi
  log "$service ${rev:0:8} ist fehlgeschlagen, zurück zum vorherigen Image"
  wanted_image "$service" >> "$FAILED_FILE"
  if [ -n "${previous[$service]}" ]; then
    docker tag "${previous[$service]}" "$(image_name "$service")"
    docker compose up -d --no-deps "$service"
  fi
  exit 1
done
