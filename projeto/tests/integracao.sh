#!/usr/bin/env bash
# =============================================================================
# Teste de integração ponta a ponta (ambiente Docker local)
#   catálogo (API C#) <-> Moodle (Web Services REST) <-> fluxo de matrícula
#
# Uso: ./tests/integracao.sh            (com os containers no ar)
# Variáveis opcionais: API=http://localhost:5000  MOODLE=http://localhost:8080  FRONT=http://localhost
# Saída: PASS/FAIL por etapa; código de saída != 0 se alguma falhar.
# Requisitos no host: bash, curl, grep (sem jq).
# =============================================================================
set -u
API="${API:-http://localhost:5000}"
MOODLE="${MOODLE:-http://localhost:8080}"
FRONT="${FRONT:-http://localhost}"
falhas=0

pass() { printf '  \033[32mPASS\033[0m %s\n' "$1"; }
fail() { printf '  \033[31mFAIL\033[0m %s\n' "$1"; falhas=$((falhas+1)); }
check() { # check "descrição" "condição shell"
  if eval "$2" >/dev/null 2>&1; then pass "$1"; else fail "$1"; fi
}
campo() { grep -o "\"$2\":[^,}]*" <<<"$1" | head -1 | cut -d: -f2- | tr -d '"'; }

echo "1) API de catálogo (exigências do PDF)"
LISTA=$(curl -sf "$API/api/cursos?tamanhoPagina=100")
check "GET /api/cursos responde"                          '[[ -n "$LISTA" ]]'
TOTAL=$(campo "$LISTA" total)
check "catálogo tem >= 6 cursos (total=$TOTAL)"           '[[ "${TOTAL:-0}" -ge 6 ]]'
FILTROS=$(curl -sf "$API/api/filtros")
check "filtros: >= 3 categorias, >= 2 modalidades, >= 3 status" \
  '[[ $(grep -o "\"slug\"" <<<"$(grep -o "\"categorias\":\[[^]]*\]" <<<"$FILTROS")" | wc -l) -ge 3 && $(grep -o "\"slug\"" <<<"$(grep -o "\"modalidades\":\[[^]]*\]" <<<"$FILTROS")" | wc -l) -ge 2 && $(grep -o "\"slug\"" <<<"$(grep -o "\"status\":\[[^]]*\]" <<<"$FILTROS")" | wc -l) -ge 3 ]]'
BUSCA=$(curl -sf "$API/api/cursos?busca=mat&categoria=matematica")
check "busca + filtro combinados (busca=mat&categoria=matematica) retorna >= 1" '[[ "$(campo "$BUSCA" total)" -ge 1 ]]'
VAZIO=$(curl -sf "$API/api/cursos?busca=zzzznaoexiste")
check "busca sem resultado retorna total=0"               '[[ "$(campo "$VAZIO" total)" == "0" ]]'
check "GET /api/cursos/999999 retorna 404"                '[[ "$(curl -s -o /dev/null -w "%{http_code}" "$API/api/cursos/999999")" == "404" ]]'

echo "2) Integração com o Moodle (Web Services)"
STATUS=$(curl -sf "$API/api/moodle/status")
check "GET /api/moodle/status: conectado=true"            '[[ "$(campo "$STATUS" conectado)" == "true" ]]'
SYNC=$(curl -sf -X POST "$API/api/moodle/sync")
check "POST /api/moodle/sync sem erro (resposta: ${SYNC:0:120})" '[[ -n "$SYNC" && "$SYNC" != *"Falha"* ]]'

echo "3) Vínculo catálogo <-> Moodle"
C1=$(curl -sf "$API/api/cursos/1")
MID=$(campo "$C1" moodleId)
check "curso 1 do catálogo tem moodleId (=$MID)"           '[[ -n "$MID" && "$MID" != "null" ]]'

echo "4) Fluxo de matrícula (Matricule-se)"
if [[ -n "${MID:-}" && "$MID" != "null" ]]; then
  LOC=$(curl -s -o /dev/null -w '%{redirect_url}' "$MOODLE/theme/sesi/matricular.php?course=$MID")
  check "matricular.php?course=$MID redireciona para enrol/index.php?id=$MID (Location: $LOC)" \
    '[[ "$LOC" == *"/enrol/index.php?id=$MID"* ]]'
  CODE=$(curl -s -o /dev/null -w '%{http_code}' -L "$MOODLE/enrol/index.php?id=$MID")
  check "formulário de inscrição (deslogado) leva ao login do Moodle (HTTP $CODE)" '[[ "$CODE" == "200" ]]'
fi
LOC2=$(curl -s -o /dev/null -w '%{redirect_url}' "$MOODLE/theme/sesi/matricular.php")
check "matricular.php sem curso abre o autocadastro (Location: $LOC2)" '[[ "$LOC2" == *"/login/signup.php"* ]]'
SIGNUP=$(curl -s -o /dev/null -w '%{http_code}' "$MOODLE/login/signup.php")
check "login/signup.php acessível (autocadastro habilitado) HTTP $SIGNUP" '[[ "$SIGNUP" == "200" ]]'

echo "5) Home única e front"
LOC3=$(curl -s -o /dev/null -w '%{redirect_url}' "$MOODLE/")
check "frontpage do Moodle (deslogado) redireciona ao catálogo (Location: $LOC3)" '[[ -n "$LOC3" && "$LOC3" != "$MOODLE/"* ]]'
check "front responde em $FRONT (index.html)"              '[[ "$(curl -s -o /dev/null -w "%{http_code}" "$FRONT/index.html")" == "200" ]]'
check "front: proxy /api funciona"                         '[[ "$(curl -s -o /dev/null -w "%{http_code}" "$FRONT/api/filtros")" == "200" ]]'

echo
if [[ $falhas -eq 0 ]]; then echo "Resultado: todas as etapas passaram."; else echo "Resultado: $falhas etapa(s) falharam."; fi
exit $falhas
