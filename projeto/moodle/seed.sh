#!/bin/bash
# Executa seed-courses.php como `daemon` (copia para /tmp para não depender das
# permissões do arquivo no host).
# Uso: docker compose exec moodle bash /opt/sesi/seed.sh
set -euo pipefail
cp /opt/sesi/seed-courses.php /tmp/seed-courses.php && chmod a+r /tmp/seed-courses.php
if command -v gosu >/dev/null 2>&1; then gosu daemon php /tmp/seed-courses.php;
else su -s /bin/bash daemon -c "php /tmp/seed-courses.php"; fi
rm -f /tmp/seed-courses.php
chown -R daemon:daemon /bitnami/moodledata
