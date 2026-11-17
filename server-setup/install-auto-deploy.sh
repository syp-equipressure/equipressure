#!/usr/bin/env bash
# Richtet den Timer ein, der jede Minute deploy.sh aufruft.
# Aufruf im Ordner server-setup: sudo bash install-auto-deploy.sh
# Mehrfaches Ausführen ist unbedenklich.
set -euo pipefail

[ "$(id -u)" -eq 0 ] || { echo "Bitte mit sudo ausführen." >&2; exit 1; }

dir="$(dirname "$(readlink -f "$0")")"
chmod +x "$dir/deploy.sh"

cat > /etc/systemd/system/equipressure-deploy.service <<EOF
[Unit]
Description=EquiPressure: neue Images deployen
After=docker.service
Requires=docker.service

[Service]
Type=oneshot
ExecStart=$dir/deploy.sh
EOF

cat > /etc/systemd/system/equipressure-deploy.timer <<'EOF'
[Unit]
Description=EquiPressure: jede Minute nach neuen Images schauen

[Timer]
OnBootSec=2min
OnUnitActiveSec=1min

[Install]
WantedBy=timers.target
EOF

systemctl daemon-reload
systemctl enable --now equipressure-deploy.timer
systemctl list-timers equipressure-deploy.timer --no-pager
