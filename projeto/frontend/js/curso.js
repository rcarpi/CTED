// Página de detalhe do curso (ref.: doc/figma/2.png).
(function () {
  const { esc, fmtData, fmtCarga, icones, depoimentoCard, carrossel } = window.ui;
  const cfg = window.APP_CONFIG;
  const root = document.getElementById('detalhe');
  const id = new URLSearchParams(location.search).get('id');

  function renderLoading() {
    root.setAttribute('aria-busy', 'true');
    root.innerHTML = `<div class="container"><div class="estado" role="status" style="margin-block:3rem"><div class="spinner" aria-hidden="true"></div><p>Carregando curso…</p></div></div>`;
  }

  function renderErro(err) {
    root.setAttribute('aria-busy', 'false');
    const naoEncontrado = err.status === 404;
    root.innerHTML = `
      <div class="container"><div class="estado ${naoEncontrado ? '' : 'estado--erro'}" role="alert" style="margin-block:3rem">
        <h2>${naoEncontrado ? 'Curso não encontrado' : 'Não foi possível carregar o curso'}</h2>
        <p>${esc(err.message)}</p>
        <a class="btn btn--primary" href="cursos.html">Voltar ao catálogo</a>
        ${naoEncontrado ? '' : '<button type="button" class="btn btn--outline" id="tentar-novamente">Tentar novamente</button>'}
      </div></div>`;
    document.getElementById('tentar-novamente')?.addEventListener('click', carregar);
  }

  function acordeaoItem(id, titulo, conteudo, aberto = false) {
    return `
      <div class="acordeao__item">
        <h3><button type="button" class="acordeao__btn" aria-expanded="${aberto}" aria-controls="painel-${id}" id="btn-${id}">
          <span>${titulo}</span>
          <img class="icone-mais" src="assets/icons/add_2.svg" alt=""><img class="icone-menos" src="assets/icons/remove.svg" alt="">
        </button></h3>
        <div class="acordeao__painel" id="painel-${id}" role="region" aria-labelledby="btn-${id}" ${aberto ? '' : 'hidden'}>${conteudo}</div>
      </div>`;
  }

  function render(c) {
    root.setAttribute('aria-busy', 'false');
    document.title = `${c.nomeCompleto} — SESI Indústria`;
    const data = fmtData(c.dataDisponibilizacao);
    // Matricule-se: cadastro no Moodle já apontando para a inscrição do curso (quando sincronizado).
    const matricula = `${cfg.moodleUrl}/theme/sesi/matricular.php${c.moodleId ? `?course=${c.moodleId}` : ''}`;
    const img = window.api.imagemUrl(c.imagemUrl);

    const modulosHtml = c.modulos.length === 0
      ? '<p>Conteúdo programático ainda não divulgado.</p>'
      : `<ol class="modulos">${c.modulos.map((m) => `
          <li><span class="modulos__num">${m.ordem}</span><div><strong>${esc(m.titulo)}</strong><p>${esc(m.descricao)}</p></div><span class="modulos__carga">${fmtCarga(m.cargaHorariaHoras)}</span></li>`).join('')}</ol>`;

    const aulasHtml = `
      <p>Modalidade <strong>${esc(c.modalidade.nome)}</strong>, com carga horária total de <strong>${fmtCarga(c.cargaHorariaHoras)}</strong>${c.modulos.length ? ` distribuída em ${c.modulos.length} módulos` : ''}.
      As aulas e atividades ficam disponíveis no ambiente virtual de aprendizagem; a avaliação é feita por atividades ao final de cada módulo e o certificado SESI é emitido após a conclusão.</p>
      <p><strong>Público-alvo:</strong> ${esc(c.publicoAlvo)}.</p>`;

    const faqHtml = `
      <dl class="faq">
        <dt>Quando posso começar?</dt><dd>${data ? `O curso está disponível a partir de ${data}.` : 'A data de disponibilização será divulgada em breve.'} Status atual: ${esc(c.status.nome)}.</dd>
        <dt>Como faço a matrícula?</dt><dd>Clique em “Matricule-se”: você cria sua conta no ambiente virtual, confirma o e-mail e é levado direto à inscrição no curso.</dd>
        <dt>Recebo certificado?</dt><dd>Sim. O certificado SESI é emitido após a conclusão de todas as atividades do curso.</dd>
      </dl>`;

    root.innerHTML = `
      <section class="container detalhe-hero" aria-labelledby="titulo-curso">
        <div class="detalhe-hero__info">
          <span class="chip">${c.categoria.icone ? `<img src="assets/icons/${esc(c.categoria.icone)}" alt="">` : ''}${esc(c.categoria.nome)}</span>
          <h1 id="titulo-curso">${esc(c.nomeCompleto)}</h1>
          <ul class="detalhe-hero__meta">
            ${data ? `<li>${icones.calendario}<span>Início do curso: <strong>${data}</strong></span></li>` : ''}
            <li>${icones.certificado}<span>Certificação: <strong>SESI</strong></span></li>
            <li>${icones.modalidade}<span>Modalidade: <strong>${esc(c.modalidade.nome)}</strong> · Carga horária: <strong>${fmtCarga(c.cargaHorariaHoras)}</strong></span></li>
          </ul>
          <span class="selo" style="--selo-cor:${esc(c.status.cor || '')}">${esc(c.status.nome)}</span>
          <a class="btn btn--primary" href="${esc(matricula)}" target="_blank" rel="noopener">Matricule-se <img class="icon" src="assets/icons/arrow-navigation-1.svg" alt=""></a>
        </div>
        <div class="midia">
          <img src="assets/img/video-thumb.jpg" alt="Vídeo de apresentação do curso ${esc(c.nomeBreve)}" width="1600" height="900" onerror="this.onerror=null;this.src='${esc(img)}'">
          <button type="button" class="midia__play" aria-label="Assistir vídeo de apresentação (em breve)">${icones.play}</button>
        </div>
      </section>

      <section class="container" aria-label="Benefícios">
        <div class="beneficios beneficios--4">
          <div class="beneficio"><img src="assets/icons/01-certificacao-bege.svg" alt=""><h3>Certificação SESI</h3><p>Cursos com a melhor certificação do mercado industrial reconhecidos pelo SESI.</p></div>
          <div class="beneficio"><img src="assets/icons/02-professores-bege.svg" alt=""><h3>Professores especialistas</h3><p>Contamos com professores reconhecidos por sua excelência no mercado.</p></div>
          <div class="beneficio"><img src="assets/icons/03-suporte-bege.svg" alt=""><h3>Suporte contínuo</h3><p>Você não está sozinho! Acompanhamento online durante todo o curso.</p></div>
          <div class="beneficio"><img src="assets/icons/04-flexibilidade-bege.svg" alt=""><h3>Flexibilidade</h3><p>Turmas presenciais e online com horários que se adaptam à sua rotina.</p></div>
        </div>
      </section>

      <section class="container detalhe-layout" aria-labelledby="sobre">
        <div class="detalhe-layout__texto">
          <h2 id="sobre" class="secao__titulo">Sobre o curso</h2>
          <p>${esc(c.descricao)}</p>
          <dl class="ficha">
            <div><dt>Nome breve</dt><dd>${esc(c.nomeBreve)}</dd></div>
            <div><dt>Público-alvo</dt><dd>${esc(c.publicoAlvo)}</dd></div>
            ${data ? `<div><dt>Disponível a partir de</dt><dd>${data}</dd></div>` : ''}
            ${c.origemMoodle ? '<div><dt>Origem</dt><dd>Sincronizado do Moodle</dd></div>' : ''}
          </dl>
        </div>
        <div class="acordeao">
          ${acordeaoItem('modulos', 'Módulos', modulosHtml, true)}
          ${acordeaoItem('aulas', 'Aulas online e avaliações', aulasHtml)}
          ${acordeaoItem('faq', 'Dúvidas frequentes', faqHtml)}
        </div>
      </section>

      <section class="depoimentos depoimentos--bege" aria-labelledby="dep-titulo">
        <div class="container">
          <span class="sobretitulo">Depoimentos</span>
          <h2 class="secao__titulo" id="dep-titulo">Quem fez recomenda</h2>
          <div class="carrossel" id="depoimentos" aria-live="polite"></div>
          <div class="carrossel__rodape"><span></span>
            <div class="setas">
              <button type="button" class="seta" id="dep-antes" aria-label="Depoimentos anteriores"><img src="assets/icons/arrow-navigation.svg" alt=""></button>
              <button type="button" class="seta" id="dep-depois" aria-label="Próximos depoimentos"><img src="assets/icons/arrow-navigation-1.svg" alt=""></button>
            </div>
          </div>
        </div>
      </section>

      <section class="cta cta--escuro cta--centro" aria-labelledby="ajuda-titulo">
        <div class="container">
          <span class="sobretitulo">Precisa de ajuda para decidir?</span>
          <h2 id="ajuda-titulo">Fale com a gente</h2>
          <a class="btn btn--invert" href="mailto:contato@sesindustria.com.br?subject=${encodeURIComponent('Dúvida sobre o curso ' + c.nomeCompleto)}">Mandar uma mensagem <img class="icon" src="assets/icons/arrow-navigation-1.svg" alt=""></a>
        </div>
      </section>`;

    root.querySelectorAll('.acordeao__btn').forEach((btn) => btn.addEventListener('click', () => {
      const aberto = btn.getAttribute('aria-expanded') === 'true';
      btn.setAttribute('aria-expanded', String(!aberto));
      document.getElementById(btn.getAttribute('aria-controls')).hidden = aberto;
    }));

    // Depoimentos do curso (fallback: gerais) — falha não derruba a página.
    const track = root.querySelector('#depoimentos');
    window.api.listarDepoimentos(c.id)
      .then((lista) => { track.innerHTML = lista.map(depoimentoCard).join(''); })
      .catch(() => { track.closest('section').hidden = true; })
      .finally(() => carrossel(track, root.querySelector('#dep-antes'), root.querySelector('#dep-depois')));
  }

  async function carregar() {
    if (!id) return renderErro(new window.api.ApiError('Nenhum curso informado na URL.', 404));
    renderLoading();
    try { render(await window.api.obterCurso(id)); }
    catch (e) { console.error(e); renderErro(e instanceof window.api.ApiError ? e : new window.api.ApiError('Erro inesperado.', 0)); }
  }

  carregar();
})();
