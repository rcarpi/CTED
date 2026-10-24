// Header e rodapé compartilhados (evita duplicar markup nas 3 páginas).
// Uso: <header class="header" data-layout="header" data-variante="transparente"></header>
//      <footer class="footer" data-layout="footer"></footer>
(function () {
  const pagina = location.pathname.split('/').pop() || 'index.html';
  const cfg = window.APP_CONFIG || {};
  const moodle = cfg.moodleUrl || 'http://localhost:8080';

  const nav = [
    { href: 'index.html', rotulo: 'Início' },
    { href: 'cursos.html', rotulo: 'Cursos' },
    { href: 'index.html#sobre', rotulo: 'Sobre' },
  ];

  function header(el) {
    const transparente = el.dataset.variante === 'transparente';
    if (transparente) el.classList.add('header--transparente');
    const logo = transparente ? 'logo-sesi-industria-white.svg' : 'logo-sesi-industria-primary.svg';
    el.innerHTML = `
      <div class="container header__inner">
        <a class="header__logo" href="index.html" aria-label="SESI Indústria — página inicial">
          <img src="assets/icons/${logo}" alt="SESI Indústria" width="188" height="19">
        </a>
        <nav class="nav" aria-label="Principal">
          <button type="button" class="nav__toggle" aria-expanded="false" aria-controls="menu">Menu</button>
          <ul class="nav__list" id="menu">
            ${nav.map((n) => `<li><a class="nav__link" href="${n.href}" ${n.href === pagina || (pagina === 'curso.html' && n.href === 'cursos.html') ? 'aria-current="page"' : ''}>${n.rotulo}</a></li>`).join('')}
          </ul>
          <a class="btn btn--outline" href="${moodle}/login/index.php" target="_blank" rel="noopener">Área do aluno</a>
        </nav>
      </div>`;
    const toggle = el.querySelector('.nav__toggle');
    const list = el.querySelector('.nav__list');
    toggle.addEventListener('click', () => {
      const aberto = list.classList.toggle('is-open');
      toggle.setAttribute('aria-expanded', String(aberto));
    });
  }

  function footer(el) {
    el.innerHTML = `
      <div class="container">
        <div class="footer__grid">
          <div class="footer__marca">
            <a class="footer__logo" href="index.html"><img src="assets/icons/logo-sesi-industria-white.svg" alt="SESI Indústria" width="188" height="19"></a>
            <p>contato@sesindustria.com.br</p>
            <p>Av. Paulista, 1313</p>
          </div>
          <nav aria-labelledby="f-nav"><h3 id="f-nav">Navegação</h3>
            <ul><li><a href="index.html">Início</a></li><li><a href="index.html#beneficios">Certificação</a></li><li><a href="cursos.html">Cursos</a></li><li><a href="index.html#sobre">Sobre</a></li><li><a href="#">Blog</a></li></ul>
          </nav>
          <nav aria-labelledby="f-pop"><h3 id="f-pop">Cursos populares</h3>
            <ul id="footer-cursos"><li><a href="cursos.html">Ver catálogo</a></li></ul>
          </nav>
          <nav aria-labelledby="f-sup"><h3 id="f-sup">Suporte</h3>
            <ul><li><a href="#">Termos de uso</a></li><li><a href="#">Política de privacidade</a></li><li><a href="#">Trabalhe conosco</a></li><li><a href="#">Fale conosco</a></li><li><a href="#">Central de ajuda</a></li></ul>
          </nav>
        </div>
        <div class="footer__base">
          <p><img class="logo-sesi" src="assets/icons/logo-sesi.svg" alt="SESI"> Todos os direitos reservados. Dados fictícios para avaliação técnica.</p>
          <ul class="footer__social" aria-label="Redes sociais">
            <li><a href="#" aria-label="LinkedIn"><img src="assets/icons/icon-linkedIn-24px.svg" alt=""></a></li>
            <li><a href="#" aria-label="Instagram"><img src="assets/icons/icon-instagram-24px.svg" alt=""></a></li>
            <li><a href="#" aria-label="Facebook"><img src="assets/icons/icon-facebook-24px.svg" alt=""></a></li>
          </ul>
        </div>
      </div>`;
    // "Cursos populares" vem da API (primeiros 6 por nome); falha silenciosa mantém o link do catálogo.
    if (window.api) {
      window.api.listarCursos({ tamanhoPagina: 6 }).then((p) => {
        const ul = el.querySelector('#footer-cursos');
        if (p.itens?.length) ul.innerHTML = p.itens.map((c) => `<li><a href="curso.html?id=${c.id}">${window.ui.esc(c.nomeCompleto)}</a></li>`).join('');
      }).catch(() => {});
    }
  }

  document.querySelectorAll('[data-layout="header"]').forEach(header);
  document.querySelectorAll('[data-layout="footer"]').forEach(footer);
})();
