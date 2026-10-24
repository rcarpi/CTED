// Home: principais cursos (carrossel), indicadores e depoimentos — tudo via API.
(function () {
  const { esc, cardCurso, depoimentoCard, carrossel } = window.ui;
  const cfg = window.APP_CONFIG;

  const erro = (msg, tentar) => `
    <div class="estado estado--erro" role="alert" style="flex:1 1 100%">
      <p style="margin:0">${esc(msg)}</p>
      ${tentar ? '<button type="button" class="btn btn--primary" data-tentar>Tentar novamente</button>' : ''}
    </div>`;

  async function principais() {
    const track = document.getElementById('principais');
    track.setAttribute('aria-busy', 'true');
    track.innerHTML = Array.from({ length: 4 }, () => '<div class="skeleton" aria-hidden="true"><div class="skeleton__img"></div><div class="skeleton__body"><div class="skeleton__linha skeleton__linha--curta"></div><div class="skeleton__linha"></div></div></div>').join('');
    try {
      const p = await window.api.listarCursos({ tamanhoPagina: 8, ordenar: 'data-desc' });
      track.innerHTML = p.itens.length ? p.itens.map((c) => cardCurso(c)).join('') : '<p class="estado" style="flex:1 1 100%">Nenhum curso cadastrado ainda.</p>';
    } catch (e) {
      track.innerHTML = erro(`Não foi possível carregar os cursos. ${e.message}`, true);
      track.querySelector('[data-tentar]')?.addEventListener('click', principais);
    } finally {
      track.setAttribute('aria-busy', 'false');
      carrossel(track, document.getElementById('principais-antes'), document.getElementById('principais-depois'));
    }
  }

  async function indicadores() {
    const ul = document.getElementById('indicadores');
    try {
      const lista = await window.api.listarIndicadores();
      ul.innerHTML = lista.map((i) => `<li><span class="impacto__valor">${esc(i.valor)}</span><span class="impacto__rotulo">${esc(i.rotulo)}</span></li>`).join('');
    } catch (e) {
      ul.innerHTML = `<li><span class="texto-secundario">Indicadores indisponíveis no momento.</span></li>`;
    }
  }

  async function depoimentos() {
    const track = document.getElementById('depoimentos');
    try {
      const lista = await window.api.listarDepoimentos();
      track.innerHTML = lista.map(depoimentoCard).join('');
    } catch (e) {
      track.innerHTML = erro('Depoimentos indisponíveis no momento.');
    } finally {
      carrossel(track, document.getElementById('dep-antes'), document.getElementById('dep-depois'));
    }
  }

  // CTA "Matricule-se" da home: cadastro no Moodle (sem curso específico).
  document.querySelectorAll('[data-matricular]').forEach((a) => {
    a.href = `${cfg.moodleUrl}/theme/sesi/matricular.php`; a.target = '_blank'; a.rel = 'noopener';
  });

  principais(); indicadores(); depoimentos();
})();
