# Catálogo de Cursos — CTED (Moodle + C#/.NET + MySQL)

Aplicação web para consulta, pesquisa e filtragem de um catálogo de cursos de um
ambiente educacional Moodle. Composta por um front-end estático (HTML/CSS/JS),
uma API em C#/.NET 8, um banco MySQL 8 e um Moodle local (Docker) usado como
fonte opcional de cursos via Web Services REST.

> Todos os dados de cursos são **fictícios**.

## Sumário

1. [Objetivo](#objetivo)
2. [Tecnologias](#tecnologias)
3. [Arquitetura](#arquitetura)
4. [Estrutura de pastas](#estrutura-de-pastas)
5. [Execução local (Docker)](#execução-local-docker)
6. [Configuração do banco](#configuração-do-banco)
7. [Integração com o Moodle](#integração-com-o-moodle)
8. [Endpoints](#endpoints)
9. [Modelo de dados](#modelo-de-dados)
10. [Responsividade](#estratégia-de-responsividade)
11. [Acessibilidade](#decisões-de-acessibilidade)
12. [Publicação](#processo-de-publicação)
13. [Limitações conhecidas](#limitações-conhecidas)

## Objetivo

Permitir que o usuário liste os cursos por meio de uma API, pesquise pelo nome,
filtre por categoria, modalidade e status (combinando pesquisa e filtros), veja
a quantidade de resultados, limpe os filtros, abra os detalhes de um curso e
receba feedback claro de carregamento, lista vazia e erro — em qualquer tamanho
de tela.

## Tecnologias

| Camada | Tecnologia |
| --- | --- |
| Front-end | HTML5, CSS3 (custom properties), JavaScript ES2020 (sem framework), servido por nginx — 3 páginas: home, catálogo e detalhe |
| Back-end | C# / .NET 8 Minimal API, Entity Framework Core 8, Pomelo.EntityFrameworkCore.MySql, Swashbuckle (Swagger) |
| Banco | MySQL 8.0 (open source) — bancos `catalogo` (API) e `moodle` (Moodle) no mesmo servidor |
| AVA | Moodle 4.4 (imagem `bitnamilegacy/moodle:4.4`) — integração via Web Services REST + tema `theme_sesi` (filho do Boost) |
| E-mail (dev) | Mailpit — captura os e-mails do Moodle (confirmação de cadastro) |
| Infra | Docker Compose |

## Arquitetura

```
┌────────────────┐  fetch /api/*   ┌──────────────────┐  EF Core   ┌─────────────┐
│  Front (nginx) │ ──────────────► │  API C#/.NET 8   │ ─────────► │  MySQL 8    │
│  :3000         │  (proxy nginx)  │  :5000           │            │  catalogo   │
└────────────────┘                 └────────┬─────────┘            ├─────────────┤
                                            │ REST (wstoken)       │  moodle     │
                                            ▼                      └──────▲──────┘
                                   ┌──────────────────┐                   │
                                   │  Moodle 4.4      │ ──────────────────┘
                                   │  :8080           │
                                   └──────────────────┘
```

* O **banco próprio** (`catalogo`) é a fonte de verdade da API: schema criado
  pelo EF Core e seed com 8 cursos, 4 categorias, 3 modalidades e 4 status.
* O **Moodle** é um diferencial: a API consome `core_course_get_categories` e
  `core_course_get_courses_by_field` e faz *upsert* dos cursos no catálogo
  (`POST /api/moodle/sync`). A aplicação funciona sem o Moodle — o que permite
  publicá-la em hospedagem gratuita.
* O **front** chama a API pela mesma origem (`/api`, proxy no nginx) — sem CORS
  em produção; a API também aceita CORS para desenvolvimento sem nginx.

## Estrutura de pastas

```
projeto/
├─ docker-compose.yml        # mysql, moodle, api, frontend (+ adminer opcional)
├─ .env.example              # variáveis (copie para .env)
├─ backend/
│  └─ CatalogoCursos.Api/
│     ├─ Program.cs          # DI, CORS, Swagger, migração/seed na inicialização
│     ├─ Data/               # DbContext (mapeamento) e DbSeeder (dados fictícios)
│     ├─ Models/             # Entidades
│     ├─ Dtos/               # Contratos da API e mapeamentos
│     ├─ Endpoints/          # /api/cursos, /api/filtros..., /api/moodle/*
│     ├─ Moodle/             # MoodleClient (REST), MoodleSyncService, opções
│     └─ Dockerfile
├─ frontend/
│  ├─ index.html             # home (Figma 1.png): hero, principais cursos, impacto, depoimentos, CTA
│  ├─ cursos.html            # catálogo: busca, filtros combináveis, contagem, estados, paginação
│  ├─ curso.html             # detalhe (Figma 2.png): hero, benefícios, sobre + acordeão, depoimentos
│  ├─ css/tokens.css         # TOKENS de customização (cores, fontes, espaçamentos)
│  ├─ css/style.css          # componentes
│  ├─ js/{config,api,ui,layout,home,catalogo,curso}.js
│  ├─ assets/{icons,fonts,img}  # kit SESI Indústria (SVGs, Open Sans) + capas SVG
│  └─ nginx.conf             # estático + proxy /api -> api:8080
├─ db/
│  ├─ init/01-databases.sh   # cria bancos/usuários no 1º start do MySQL
│  └─ schema.sql             # DDL do banco catalogo (documentação / recriação manual)
├─ moodle/
│  ├─ provision.sh           # habilita web services REST + gera token
│  ├─ seed-courses.php       # cria categorias/cursos fictícios no Moodle
│  ├─ apply-theme.sh         # instala e ativa o tema sesi
│  ├─ diag.php               # diagnóstico de permissões/diretórios
│  └─ theme/sesi/            # tema Moodle (filho do Boost) — página do curso (Figma 3.png)
├─ README.md
└─ USO_DE_IA.md
```

## Execução local (Docker)

Pré-requisitos: apenas Docker 24+ e Docker Compose v2 (nada mais no host).

```bash
cp .env.example .env            # ajuste senhas se quiser
docker compose up -d --build    # 1ª vez: baixa imagens e compila a API
docker compose logs -f api      # aguarde "Seed verificado/aplicado."
```

| Serviço | URL |
| --- | --- |
| Front-end | http://localhost:3000 (ou `FRONTEND_PORT=80` no `.env` para http://localhost/). **Não use a mesma porta do Moodle (8080)** |
| API + Swagger | http://localhost:5000/swagger |
| Moodle | http://localhost:8080 (admin / senha do `.env`) — a 1ª instalação leva alguns minutos |
| Mailpit (e-mails do Moodle, ex.: confirmação de cadastro) | http://localhost:8025 |
| Adminer (opcional) | `docker compose --profile tools up -d adminer` → http://localhost:8081 |

**Tudo roda dentro do Docker — não é necessário .NET, Node ou MySQL no host.**
Para comandos pontuais do SDK (compilar, gerar migrations, gerar SQL) existe o
serviço `sdk` (perfil `dev`) e o atalho `backend/dotnet.sh`:

```bash
./backend/dotnet.sh build                        # compila a API no container SDK
./backend/dotnet.sh ef migrations add Inicial    # cria migration (instala dotnet-ef na 1ª vez)
./backend/dotnet.sh ef database update           # aplica no MySQL do compose
./backend/dotnet.sh ef migrations script -o /src/migrations.sql   # SQL em backend/migrations.sql
```

Após alterar o código da API, recompile a imagem: `docker compose up -d --build api`.
Alterações no front (`frontend/`) são refletidas ao recarregar a página (volume montado no nginx).

## Configuração do banco

* `db/init/01-databases.sh` roda automaticamente no primeiro start do MySQL e
  cria os bancos `catalogo` e `moodle` com seus usuários (valores do `.env`).
* A API, ao iniciar, cria o schema (EF Core `EnsureCreated`, ou `Migrate` se
  houver migrations na pasta `Migrations/`) e aplica o seed quando a tabela
  `cursos` está vazia (`Data/DbSeeder.cs`).
* `db/schema.sql` contém o DDL equivalente para criação manual.
* Para gerar migrations (opcional, via container SDK):
  `./backend/dotnet.sh ef migrations add Inicial` — a API passa a usar `Migrate()`
  automaticamente quando a pasta `Migrations/` existir.
* Para resetar: `docker compose down -v` (apaga volumes) e suba novamente.

## Integração com o Moodle

1. Aguarde o Moodle terminar a instalação (`docker compose logs -f moodle`).
2. Habilite os web services e gere um token (o script roda o PHP como usuário
   `daemon` e corrige permissões de `moodledata`):
   `docker compose exec moodle bash /opt/sesi/provision.sh`
3. (Opcional) crie cursos fictícios no Moodle (com autoinscrição):
   `docker compose exec moodle bash /opt/sesi/seed.sh`
4. Coloque o token impresso em `MOODLE_WS_TOKEN` no `.env` e `docker compose up -d api`.
   Sem token, a API tenta obtê-lo via `login/token.php` com usuário/senha do admin
   (apenas para uso local).

> Nunca rode scripts CLI do Moodle como root (`docker compose exec moodle php ...`):
> eles criam pastas de cache em `/bitnami/moodledata` que o Apache (`daemon`) não
> consegue usar, gerando "Invalid permissions detected when trying to create a
> directory". Correção: `docker compose exec moodle chown -R daemon:daemon /bitnami/moodledata`.
5. Teste: `GET /api/moodle/status`, `GET /api/moodle/cursos`, `POST /api/moodle/sync`.

Mapeamento Moodle → catálogo (ver `Moodle/MoodleSyncService.cs`): campos
personalizados de curso `modalidade`, `carga_horaria` e `publico_alvo` têm
prioridade; na ausência, são extraídos do resumo (`Modalidade: X`,
`Carga horária: 40h`, `Público-alvo: Y`); o status deriva de
`visible`/`startdate`/`enddate`.

## Tema Moodle (curso interno — Figma 3.png)

O tema `theme_sesi` (pasta `moodle/theme/sesi`, montada em `/opt/sesi/theme/sesi` e
copiada para `/bitnami/moodle/theme/sesi` pelo `apply-theme.sh`) é um tema filho
do Boost. Ele sobrescreve apenas o
layout `drawers` para a página de curso:

* sidebar escura com seta de retorno ao catálogo, rótulo "Certificação", nome do
  curso, barra de progresso (conclusão de atividades) e caixa de busca que filtra
  o índice do curso;
* banner com a imagem do curso (ou imagem padrão do kit) e logo SESI no topo do
  conteúdo;
* cores, fonte Open Sans e tipografia da marca;
* **home única**: a frontpage do Moodle redireciona visitantes não logados para
  o catálogo (`catalogurl`, preenchido pelo `apply-theme.sh` com
  `http://localhost:FRONTEND_PORT`); usuários logados caem no Painel;
* **fluxo de matrícula**: "Área do aluno" abre o login do Moodle; "Matricule-se"
  abre `theme/sesi/matricular.php?course=<id>`, que leva ao **formulário de
  inscrição do curso** (`enrol/index.php`, método "self" criado pelo
  `seed-courses.php`). Visitante não logado passa pelo login/**autocadastro**
  (`registerauth=email`; e-mail de confirmação no Mailpit, http://localhost:8025)
  e volta ao formulário. O `seed-courses.php` cria no Moodle os mesmos 8 cursos
  do catálogo (mesmo nome breve) e o `POST /api/moodle/sync` **vincula** cada um
  ao curso local (grava `MoodleId`) sem duplicar; cursos sem vínculo levam ao
  cadastro e depois ao Painel;
* **modo aluno**: usuários sem permissão de edição veem a página sem a barra
  superior, como no protótipo (nome e "Sair" ficam na sidebar). Professores e
  administradores mantêm a navbar e o modo de edição. Configurável em
  *Administração do site → Aparência → Temas → SESI Indústria*.

Aplicar: `docker compose exec moodle bash /opt/sesi/apply-theme.sh` (também após
alterar SCSS/templates, para limpar caches). O log do upgrade deve listar
`theme_sesi`; se não listar, verifique as permissões abaixo.

> **Permissões dos arquivos do projeto:** os volumes são *bind mounts*. Para não
> depender das permissões do host, o tema é copiado para dentro do volume pelo
> `apply-theme.sh`, os scripts PHP são copiados para `/tmp` antes de rodar como
> `daemon` e o nginx roda com `user root`. Ainda assim, `chmod -R a+rX .` na raiz
> do projeto é uma boa prática. Para ver o progresso, habilite
"Acompanhamento de conclusão" no curso e nas atividades.

## Endpoints

Base: `/api`. Documentação interativa em `/swagger`.

| Método | Rota | Descrição |
| --- | --- | --- |
| GET | `/cursos` | Lista paginada. Query: `busca`, `categoria`, `modalidade`, `status` (slug ou id; vários separados por vírgula), `ordenar` (`nome`, `nome-desc`, `data`, `data-desc`, `carga`, `carga-desc`), `pagina`, `tamanhoPagina` (1–100) |
| GET | `/cursos/{id}` | Detalhe com módulos. 404 (Problem Details) se não existir |
| GET | `/categorias` | Categorias com quantidade de cursos |
| GET | `/modalidades` | Modalidades |
| GET | `/status` | Status (com cor sugerida) |
| GET | `/filtros` | Categorias + modalidades + status em uma chamada |
| GET | `/indicadores` | Números da seção "Impacto transformador" (home) |
| GET | `/depoimentos?cursoId=` | Depoimentos do curso; sem específicos, retorna os gerais |
| GET | `/moodle/status` | Diagnóstico da conexão com o Moodle |
| GET | `/moodle/cursos` | Cursos lidos direto do Moodle (sem gravar) |
| POST | `/moodle/sync` | Importa categorias/cursos do Moodle (upsert por `MoodleId`; vincula por nome breve quando o curso já existe no catálogo) |
| GET | `/health` *(sem prefixo)* | Verifica conexão com o banco |

Exemplo: `GET /api/cursos?busca=mat&categoria=matematica&status=em-breve,inscricoes-abertas&pagina=1`

```json
{ "itens": [ { "id": 3, "nomeCompleto": "...", "nomeBreve": "MAT-FAB", "descricao": "...",
    "categoria": { "id": 2, "nome": "Matemática", "slug": "matematica" },
    "modalidade": { "id": 2, "nome": "Presencial", "slug": "presencial" },
    "status": { "id": 1, "nome": "Inscrições abertas", "slug": "inscricoes-abertas", "cor": "#1B7F3B" },
    "cargaHorariaHoras": 32, "publicoAlvo": "...", "imagemUrl": "img/cursos/matematica-fabrica.svg",
    "dataDisponibilizacao": "2026-11-10", "origemMoodle": false } ],
  "total": 1, "pagina": 1, "tamanhoPagina": 12, "totalPaginas": 1 }
```

## Modelo de dados

```
categorias (Id, Nome, Slug*, Icone, MoodleId*)
modalidades (Id, Nome, Slug*)
status (Id, Nome, Slug*, Cor)
cursos (Id, NomeCompleto, NomeBreve*, Descricao, CategoriaId→categorias, ModalidadeId→modalidades,
        StatusId→status, CargaHorariaHoras, PublicoAlvo, ImagemUrl, DataDisponibilizacao,
        MoodleId*, MoodleUrl, CriadoEm, AtualizadoEm)
modulos (Id, CursoId→cursos [cascade], Ordem, Titulo, Descricao, CargaHorariaHoras)  UNIQUE(CursoId, Ordem)
depoimentos (Id, Nome, Cargo, Texto, Nota, CursoId→cursos [set null], Ordem)
indicadores (Id, Valor, Rotulo, Ordem)
* = índice único
```

## Estratégia de responsividade

* Layout *mobile-first* com `clamp()` para a tipografia e grid fluido
  (1 → 2 → 3 colunas em 600px / 1024px); a página de detalhe passa de 1 para 2
  colunas em 900px.
* Menu colapsado em `<768px`; barra de ação fixa (`position: sticky`) no
  detalhe, reduzida ao botão no mobile.
* Imagens com `aspect-ratio`, `object-fit` e `loading="lazy"`; PNGs do kit
  convertidos para JPEG otimizado (3,3 MB → 160 KB).
* Sem rolagem horizontal verificada em 390px.

## Decisões de acessibilidade

* HTML semântico (`header/nav/main/section/article/footer`), *skip link*,
  rótulos visíveis em todos os campos, `role="search"`.
* Estados anunciados por `aria-live` (quantidade de resultados), `aria-busy`
  durante o carregamento, `role="alert"` para erro e `role="status"` para vazio.
* Filtros de categoria são botões com `aria-pressed`; acordeão de módulos com
  `aria-expanded`/`aria-controls`; menu com `aria-expanded`.
* Foco visível (`:focus-visible`), alvos de toque ≥ 44px, `prefers-reduced-motion`.
* Ícones decorativos com `alt=""`/`aria-hidden`; contraste de texto ≥ 4.5:1
  (vermelho da marca usado apenas em elementos grandes/decorativos).
* Estado de filtros refletido na URL (compartilhável e recuperável no *refresh*).

## Processo de publicação

Opção gratuita sem custo para o SESI-SP (a aplicação roda sem o Moodle):

| Componente | Plataforma gratuita | Observações |
| --- | --- | --- |
| API | Render (Web Service, Docker) ou Fly.io | Usa `backend/CatalogoCursos.Api/Dockerfile`; variáveis `ConnectionStrings__Catalogo` e `Cors__AllowedOrigins__0` |
| MySQL | Aiven (free), Clever Cloud (free) ou TiDB Serverless (compatível MySQL) | Copie a string de conexão para a API |
| Front | GitHub Pages / Netlify / Vercel | Publique a pasta `frontend/`; defina `<meta name="api-base" content="https://SUA-API/api">` em `index.html` e `curso.html` |

Passos resumidos: (1) criar o MySQL gratuito e obter a string de conexão;
(2) criar o Web Service na Render apontando para o repositório e o Dockerfile
da API, com as variáveis acima; (3) publicar `frontend/` no GitHub Pages
ajustando a meta tag `api-base` para a URL da API; (4) em `Cors__AllowedOrigins__0`
da API, colocar a URL do front.

> Hospedagens gratuitas podem hibernar: a primeira requisição pode levar
> ~30–60 s. O front exibe o estado de carregamento nesse intervalo.

## Limitações conhecidas

* Tema Moodle: a página de curso segue o protótipo; as demais páginas do Moodle
  (login, painel, administração) ficam com o Boost padrão nas cores da marca.
  O progresso depende do acompanhamento de conclusão estar habilitado.
* O conteúdo dos acordeões "Aulas online e avaliações" e "Dúvidas frequentes" no
  detalhe é gerado a partir dos dados do curso (modalidade, carga, status), não
  cadastrado por curso.
* Sem migrations versionadas (o schema é criado por `EnsureCreated`); para
  evoluções de schema, gerar migrations com `dotnet ef`.
* O vídeo de apresentação do curso (protótipo) é apenas uma *thumb* com botão
  de play; não há player.
* A tela do "player" do curso (segundo print do Figma) pertence ao AVA (Moodle)
  e não foi reproduzida; o botão **Matricule-se** leva ao curso no Moodle quando
  o curso foi sincronizado e fica desabilitado nos cursos cadastrados só no
  catálogo.
* O Moodle local não é publicado (não há hospedagem gratuita viável); a
  sincronização funciona apenas no ambiente Docker.
* Sem gerenciamento (CRUD) de cursos, conforme o escopo.
* Diferenças em relação ao Figma devem ser listadas aqui após comparação com o
  protótipo completo (apenas as telas de detalhe e player foram fornecidas como
  imagem).
