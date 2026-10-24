// Configuração do front-end.
// A URL da API pode ser sobrescrita sem rebuild:
//   1) <meta name="api-base" content="https://sua-api/api">  no HTML, ou
//   2) window.API_BASE antes deste script, ou
//   3) query string ?api=https://sua-api/api  (útil para testes).
(function () {
  const meta = document.querySelector('meta[name="api-base"]')?.content;
  const qs = new URLSearchParams(location.search).get('api');
  // Padrão: mesma origem (/api). No Docker o nginx faz proxy para a API;
  // em produção use a meta tag api-base ou um proxy equivalente.
  // Para abrir o HTML direto (ex.: Live Server na porta 5500) use ?api=http://localhost:5000/api
  const padrao = '/api';
  window.APP_CONFIG = {
    apiBase: (qs || window.API_BASE || meta || padrao).replace(/\/+$/, ''),
    tamanhoPagina: 12,
    // Caminho base das imagens relativas retornadas pela API (ex.: "img/cursos/x.svg")
    imagemBase: 'assets/',
    imagemPadrao: 'assets/img/cursos/moodle-padrao.svg',
    // Link "Área do aluno" (Moodle). Sobrescreva com <meta name="moodle-url" content="...">
    moodleUrl: document.querySelector('meta[name="moodle-url"]')?.content || 'http://localhost:8080/login/index.php',
  };
})();
