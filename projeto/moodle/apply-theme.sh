#!/bin/bash
# Instala/atualiza o tema "sesi" (montado em /bitnami/moodle/theme/sesi pelo
# docker-compose) e o define como tema padrão do site.
#
# Uso: docker compose exec moodle bash /opt/sesi/apply-theme.sh
# Repita após alterar arquivos do tema (SCSS/templates) para limpar caches.
set -euo pipefail
MOODLE_DIR=/bitnami/moodle
WEB_USER=daemon

run_php() {
  if [[ "$(id -u)" == "0" ]]; then
    if command -v gosu >/dev/null 2>&1; then gosu "$WEB_USER" php "$@";
    else su -s /bin/bash "$WEB_USER" -c "php $(printf '%q ' "$@")"; fi
  else php "$@"; fi
}

SRC=/opt/sesi/theme/sesi
[[ -f "$SRC/config.php" ]] || { echo "Fonte do tema não encontrada em $SRC (verifique o volume no docker-compose.yml)"; exit 1; }

echo ">> Copiando tema para $MOODLE_DIR/theme/sesi (dono $WEB_USER, legível)"
rm -rf "$MOODLE_DIR/theme/sesi"
cp -r "$SRC" "$MOODLE_DIR/theme/sesi"
chown -R "$WEB_USER":"$WEB_USER" "$MOODLE_DIR/theme/sesi"
chmod -R u=rwX,go=rX "$MOODLE_DIR/theme/sesi"

echo ">> Registrando plugin theme_sesi (upgrade)"
run_php "$MOODLE_DIR/admin/cli/upgrade.php" --non-interactive --allow-unstable

echo ">> Definindo tema padrão = sesi"
run_php "$MOODLE_DIR/admin/cli/cfg.php" --name=theme --set=sesi
run_php "$MOODLE_DIR/admin/cli/cfg.php" --name=themedesignermode --set=0
run_php "$MOODLE_DIR/admin/cli/cfg.php" --component=theme_sesi --name=catalogurl --set="${CATALOG_URL:-http://localhost:3000}"
run_php "$MOODLE_DIR/admin/cli/cfg.php" --component=theme_sesi --name=redirectfrontpage --set=1
run_php "$MOODLE_DIR/admin/cli/cfg.php" --component=theme_sesi --name=hidenavbarstudents --set=1

echo ">> Autocadastro por e-mail + SMTP (Mailpit) para confirmação"
run_php "$MOODLE_DIR/admin/cli/cfg.php" --name=auth --set=email
run_php "$MOODLE_DIR/admin/cli/cfg.php" --name=registerauth --set=email
run_php "$MOODLE_DIR/admin/cli/cfg.php" --name=smtphosts --set="${MOODLE_SMTP_HOST:-mailpit}:${MOODLE_SMTP_PORT_NUMBER:-1025}"
run_php "$MOODLE_DIR/admin/cli/cfg.php" --name=smtpsecure --set=""
run_php "$MOODLE_DIR/admin/cli/cfg.php" --name=noreplyaddress --set="noreply@sesi.local"
run_php "$MOODLE_DIR/admin/cli/cfg.php" --name=supportemail --set="suporte@sesi.local"
run_php "$MOODLE_DIR/admin/cli/cfg.php" --name=passwordpolicy --set=0      # ambiente de demonstração

echo ">> Limpando caches"
run_php "$MOODLE_DIR/admin/cli/purge_caches.php" >/dev/null

[[ "$(id -u)" == "0" ]] && chown -R "$WEB_USER":"$WEB_USER" /bitnami/moodledata
echo "Tema aplicado. Abra um curso em http://localhost:8080 (primeiro acesso compila o SCSS, ~10-20s)."
