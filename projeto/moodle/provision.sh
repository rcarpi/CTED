#!/bin/bash
# Habilita Web Services REST no Moodle local, ativa o serviço "moodle_mobile_app"
# (que expõe core_course_get_courses_by_field / core_course_get_categories) e
# gera um token permanente para o admin — sem depender de curl.
#
# Uso (após o Moodle terminar a instalação inicial):
#   docker compose exec moodle bash /opt/sesi/provision.sh
#
# IMPORTANTE: `docker compose exec` entra como root. Scripts CLI do Moodle rodando
# como root criam pastas em /bitnami/moodledata (cache, localcache) que o Apache
# (usuário `daemon`) não consegue usar -> "Invalid permissions detected when
# trying to create a directory". Por isso todo PHP aqui roda como `daemon` e as
# permissões são corrigidas ao final.
set -euo pipefail

MOODLE_DIR=/bitnami/moodle
DATA_DIR=/bitnami/moodledata
WEB_USER=daemon

run_php() {
  if [[ "$(id -u)" == "0" ]]; then
    if command -v gosu >/dev/null 2>&1; then gosu "$WEB_USER" php "$@";
    else su -s /bin/bash "$WEB_USER" -c "php $(printf '%q ' "$@")"; fi
  else
    php "$@"
  fi
}

echo ">> Habilitando web services + protocolo REST + serviço mobile"
run_php "$MOODLE_DIR/admin/cli/cfg.php" --name=enablewebservices --set=1
run_php "$MOODLE_DIR/admin/cli/cfg.php" --name=webserviceprotocols --set=rest
run_php "$MOODLE_DIR/admin/cli/cfg.php" --name=enablemobilewebservice --set=1

echo ">> Garantindo serviço moodle_mobile_app ativo e gerando token"
cat > /tmp/cc-token.php <<'PHP'
<?php
define('CLI_SCRIPT', true);
require('/bitnami/moodle/config.php');
require_once($CFG->libdir . '/externallib.php');

$service = $DB->get_record('external_services', ['shortname' => 'moodle_mobile_app'], '*', MUST_EXIST);
if (!$service->enabled) {
    $service->enabled = 1;
    $DB->update_record('external_services', $service);
    echo "   moodle_mobile_app ativado\n";
} else {
    echo "   moodle_mobile_app já estava ativo\n";
}

$username = getenv('MOODLE_USERNAME') ?: 'admin';
$user = $DB->get_record('user', ['username' => $username, 'deleted' => 0], '*', MUST_EXIST);
$context = context_system::instance();

// Reaproveita token permanente existente; senão cria um novo.
$existing = $DB->get_record('external_tokens', [
    'userid' => $user->id, 'externalserviceid' => $service->id, 'tokentype' => EXTERNAL_TOKEN_PERMANENT,
]);
if ($existing) {
    $token = $existing->token;
} elseif (class_exists('\core_external\util')) {            // Moodle >= 4.2
    $token = \core_external\util::generate_token(EXTERNAL_TOKEN_PERMANENT, $service, $user->id, $context);
} else {                                                     // Moodle < 4.2
    $token = external_generate_token(EXTERNAL_TOKEN_PERMANENT, $service, $user->id, $context);
}
echo "TOKEN={$token}\n";
PHP
chown "$WEB_USER" /tmp/cc-token.php 2>/dev/null || true
TOKEN_OUT=$(MOODLE_USERNAME="${MOODLE_USERNAME:-admin}" run_php /tmp/cc-token.php)
echo "$TOKEN_OUT" | grep -v '^TOKEN=' || true
TOKEN=$(echo "$TOKEN_OUT" | sed -n 's/^TOKEN=//p')
rm -f /tmp/cc-token.php

echo ">> Limpando caches"
run_php "$MOODLE_DIR/admin/cli/purge_caches.php" >/dev/null

if [[ "$(id -u)" == "0" ]]; then
  echo ">> Corrigindo permissões de $DATA_DIR para $WEB_USER"
  chown -R "$WEB_USER":"$WEB_USER" "$DATA_DIR"
fi

echo
echo "================================================================"
echo "MOODLE_WS_TOKEN=${TOKEN}"
echo "================================================================"
echo "Copie a linha acima para o arquivo .env e reinicie a API:"
echo "   docker compose up -d api"
echo "Teste: http://localhost:5000/api/moodle/status"
