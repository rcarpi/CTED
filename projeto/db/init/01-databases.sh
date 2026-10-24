#!/bin/bash
# Executado automaticamente pelo entrypoint do MySQL na PRIMEIRA inicialização
# (volume vazio). Cria os dois bancos e seus usuários a partir das variáveis
# de ambiente definidas no docker-compose.yml.
set -euo pipefail

mysql -uroot -p"${MYSQL_ROOT_PASSWORD}" <<SQL
CREATE DATABASE IF NOT EXISTS \`${MOODLE_DB_NAME}\`
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS '${MOODLE_DB_USER}'@'%' IDENTIFIED BY '${MOODLE_DB_PASSWORD}';
GRANT ALL PRIVILEGES ON \`${MOODLE_DB_NAME}\`.* TO '${MOODLE_DB_USER}'@'%';

CREATE DATABASE IF NOT EXISTS \`${CATALOGO_DB_NAME}\`
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS '${CATALOGO_DB_USER}'@'%' IDENTIFIED BY '${CATALOGO_DB_PASSWORD}';
GRANT ALL PRIVILEGES ON \`${CATALOGO_DB_NAME}\`.* TO '${CATALOGO_DB_USER}'@'%';

FLUSH PRIVILEGES;
SQL

echo ">> Bancos '${MOODLE_DB_NAME}' e '${CATALOGO_DB_NAME}' criados."
