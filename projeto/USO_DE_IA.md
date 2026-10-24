# Registro do uso de Inteligência Artificial

## Ferramenta utilizada

Claude (Anthropic), em modo agente no aplicativo desktop (Cowork), com acesso à
pasta do projeto, ao zip de assets e a prints do protótipo Figma.

## Objetivo do uso

1. Gerar a estrutura inicial do projeto: `docker-compose.yml` (MySQL, Moodle,
   API, nginx), scripts de inicialização do banco e de provisionamento do Moodle.
2. Gerar o esqueleto da API C#/.NET 8 (entidades, DbContext, seed fictício,
   endpoints de listagem/filtros/detalhe, cliente REST do Moodle e serviço de
   sincronização).
3. Gerar o front-end estático (HTML/CSS/JS) com tokens de customização a partir
   dos SVGs/fontes do kit e do print do protótipo.
4. Escrever a primeira versão da documentação (README) e deste registro.

## Prompts relevantes

**Prompt inicial (aproveitado):** "com base nos documentos que estão na pasta criar 
uma estrutura em docker para utilizar o moodle local com c# de consumo de api, deixar 
a página para ser customizada conforme o pdf utilizando os arquivos que estão no zip".

**Decisões tomadas em diálogo com a IA (aproveitadas):**
* Banco MySQL compartilhado (dois bancos no mesmo servidor) em vez de PostgreSQL.
* Front estático sem framework, para publicação gratuita simples.
* Banco próprio como fonte de verdade + sincronização do Moodle como diferencial,
  para que a aplicação publicada não dependa de um Moodle hospedado.

**Descartado:**
* A IA propôs inicialmente usar `Microsoft.AspNetCore.OpenApi` (`WithOpenApi()`)
  e `AddDbContextCheck` — pacotes extras desnecessários; removidos para reduzir
  dependências (Swashbuckle e um `/health` manual bastam).
* A IA propôs filtrar por id com `CategoriaId.ToString()` dentro da consulta EF;
  foi substituído por separação explícita entre slugs e ids antes da consulta,
  evitando depender da tradução de `ToString()` para SQL.

**Segunda iteração (prints completos do Figma):** a IA refez a home, o catálogo
e o detalhe a partir de 1.png/2.png e gerou o tema Moodle `theme_sesi` a partir
do 3.png, copiando o layout `drawers` do Boost 4.4 (obtido do repositório
oficial) e acrescentando o contexto da sidebar. Descartado: a proposta inicial de
reproduzir também a home e o detalhe como tema Moodle, por enfraquecer o
requisito "front-end + API C#" do PDF.

## Como o conteúdo foi validado

* **Front-end:** testado com Playwright contra um *stub* da API (mesmo contrato
  JSON): listagem, busca + filtro combinados, contagem de resultados, estado
  vazio, estado de erro, limpar filtros, detalhe, acordeão, 404, menu mobile e
  ausência de rolagem horizontal em 390px — todos os cenários passaram.
* **Back-end:** revisão manual de cada arquivo (tipos de retorno das lambdas,
  pacotes NuGet e versões, mapeamento EF Core, nomes dos campos JSON do Moodle
  conforme a documentação das funções `core_course_get_courses_by_field` e
  `core_course_get_categories`). A compilação deve ser confirmada com
  `docker compose build api`, pois o ambiente onde o código foi gerado não tinha
  acesso ao NuGet.
* **Docker/Moodle:** variáveis da imagem `bitnami/moodle` conferidas com a
  documentação da imagem; o script `provision.sh` confere e, se necessário,
  força o `enabled` do serviço `moodle_mobile_app` diretamente na tabela
  `external_services`.