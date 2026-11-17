#!/usr/bin/env bash
# Richtet einen frischen Debian- oder Ubuntu-Server ein: Docker, Firewall,
# automatische Sicherheitsupdates und SSH nur per Schlüssel.
# Aufruf: sudo bash setup-server.sh
# Mehrfaches Ausführen ist unbedenklich.
set -euo pipefail

log() { printf '\n==> %s\n' "$*"; }
die() { printf '\nFEHLER: %s\n' "$*" >&2; exit 1; }

# --- Vorprüfungen: hier wird noch nichts verändert ---------------------------

[ "$(id -u)" -eq 0 ] || die "Bitte mit sudo oder als root ausführen."

. /etc/os-release
case "${ID:-}" in
  debian|ubuntu) ;;
  *) die "Dieses Skript unterstützt nur Debian und Ubuntu (gefunden: ${ID:-unbekannt})." ;;
esac

# Ohne hinterlegten Schlüssel würde das Abschalten des Passwort-Logins aussperren.
login_user="${SUDO_USER:-root}"
login_home="$(getent passwd "$login_user" | cut -d: -f6)"
auth_keys="$login_home/.ssh/authorized_keys"
grep -Eq '^[^#]*(ssh-|ecdsa-|sk-)' "$auth_keys" 2>/dev/null \
  || die "Kein SSH-Schlüssel in $auth_keys gefunden. Erst 'ssh-copy-id' ausführen."

ram_mb="$(awk '/MemTotal/ {print int($2 / 1024)}' /proc/meminfo)"
if [ "$ram_mb" -lt 3500 ]; then
  printf 'WARNUNG: Nur %s MB RAM vorhanden, geplant sind etwa 4 GB.\n' "$ram_mb"
fi

export DEBIAN_FRONTEND=noninteractive

log "System aktualisieren"
apt-get update -y
apt-get -y -o Dpkg::Options::=--force-confold upgrade
apt-get install -y ca-certificates curl ufw unattended-upgrades

# --- Docker ------------------------------------------------------------------

log "Docker und Docker Compose installieren"
install -m 0755 -d /etc/apt/keyrings
curl -fsSL "https://download.docker.com/linux/$ID/gpg" -o /etc/apt/keyrings/docker.asc
chmod a+r /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/$ID $VERSION_CODENAME stable" \
  > /etc/apt/sources.list.d/docker.list
apt-get update -y
apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

log "Docker-Autostart aktivieren"
systemctl enable --now docker containerd

# --- Firewall ----------------------------------------------------------------

# Ubuntu startet SSH per Socket, dann fehlt dieses Verzeichnis für 'sshd -T'.
mkdir -p /run/sshd

log "Firewall einrichten"
ufw default deny incoming
ufw default allow outgoing
# Vor dem Aktivieren freigeben, damit die laufende Verbindung bestehen bleibt.
for port in $(sshd -T | awk '$1 == "port" {print $2}'); do
  ufw allow "$port/tcp" comment 'SSH'
done
ufw allow 80/tcp comment 'HTTP'
ufw allow 443/tcp comment 'HTTPS'
ufw --force enable

# --- Automatische Sicherheitsupdates -----------------------------------------

log "Automatische Sicherheitsupdates aktivieren"
cat > /etc/apt/apt.conf.d/20auto-upgrades <<'EOF'
APT::Periodic::Update-Package-Lists "1";
APT::Periodic::Unattended-Upgrade "1";
EOF
systemctl enable --now unattended-upgrades

# --- SSH ---------------------------------------------------------------------

log "SSH auf Schlüssel-Anmeldung umstellen"
# In sshd gilt der erste Wert. Der Präfix 00 schlägt daher z. B. 50-cloud-init.conf.
cat > /etc/ssh/sshd_config.d/00-hardening.conf <<'EOF'
PubkeyAuthentication yes
PasswordAuthentication no
KbdInteractiveAuthentication no
PermitRootLogin prohibit-password
EOF
sshd -t || die "SSH-Konfiguration ungültig, Dienst wurde nicht neu geladen."
systemctl reload ssh || systemctl restart ssh

[ "$(sshd -T | awk '$1 == "passwordauthentication" {print $2}')" = "no" ] \
  || die "Passwort-Login ist noch aktiv. Prüfe /etc/ssh/sshd_config."

log "Fertig. Stand:"
ufw status verbose
systemctl is-enabled docker unattended-upgrades
docker compose version
printf '\nTeste jetzt in einem ZWEITEN Terminal den SSH-Login, bevor du dieses schließt.\n'
