// Página de listagem: busca + filtros combináveis + estados + paginação.
(function () {
  const { esc } = window.ui;
  const cfg = window.APP_CONFIG;

  const el = {
    form: document.getElementById('form-filtros'),
    busca: document.getElementById('busca'),
    modalidade: document.getElementById('modalidade'),
    status: document.getElementById('status'),
    ordenar: document.getElementById('ordenar'),
    categorias: document.getElementById('categorias'),
    limpar: document.getElementById('limpar'),
    qtd: document.getElementById('qtd-resultados'),
    grid: document.getElementById('grid'),
    paginacao: document.getElementById('paginacao'),
    pagAnterior: document.getElementById('pag-anterior'),
    pagProxima: document.getElementById('pag-proxima'),
    pagInfo: document.getElementById('pag-info'),
    live: document.getElementById('live'),
  };

  const estado = { busca: '', categoria: '', modalidade: '', status: '', ordenar: '', pagina: 1 };
  let abort = null;
  let debounce = null;

  // ---------------------------------------------------------- URL <-> estado
  function lerUrl() {
    const p = new URLSearchParams(location.search);
    for (const k of Object.keys(estado)) if (p.has(k)) estado[k] = k === 'pagina' ? Number(p.get(k)) || 1 : p.get(k);
  }
  function gravarUrl() {
    const p = new URLSearchParams();
    for (const [k, v] of Object.entries(estado)) if (v && !(k === 'pagina' && v === 1)) p.set(k, v);
    const qs = p.toString();
    history.replaceState(null, '', qs ? `?${qs}` : location.pathname);
  }
  function sincronizarForm() {
    el.busca.value = estado.busca;
    el.modalidade.value = estado.modalidade;
    el.status.value = estado.status;
    el.ordenar.value = estado.ordenar;
    el.categorias.querySelectorAll('.categoria-btn').forEach((b) =>
      b.setAttribute('aria-pressed', String(b.dataset.slug === estado.categoria)));
    const temFiltro = estado.busca || estado.categoria || estado.modalidade || estado.status;
    el.limpar.hidden = !temFiltro;
  }

  // ---------------------------------------------------------- render
  function renderLoading() {
    el.qtd.innerHTML = 'Carregando cursos…';
    el.grid.setAttribute('aria-busy', 'true');
    el.grid.innerHTML = Array.from({ length: 8 }, () => `
      <div class="skeleton" aria-hidden="true">
        <div class="skeleton__img"></div>
        <div class="skeleton__body">
          <div class="skeleton__linha skeleton__linha--curta"></div>
          <div class="skeleton__linha"></div>
          <div class="skeleton__linha"></div>
        </div>
      </div>`).join('');
    el.paginacao.hidden = true;
  }

  function renderErro(err) {
    el.grid.setAttribute('aria-busy', 'false');
    el.qtd.textContent = '';
    el.grid.innerHTML = `
      <div class="estado estado--erro" role="alert">
        <h2>Não foi possível carregar os cursos</h2>
        <p>${esc(err.message)}${err.detalhe ? ` <small>(${esc(err.detalhe)})</small>` : ''}</p>
        <button type="button" class="btn btn--primary" id="tentar-novamente">Tentar novamente</button>
      </div>`;
    document.getElementById('tentar-novamente').addEventListener('click', carregar);
    el.paginacao.hidden = true;
  }

  function renderVazio() {
    el.grid.setAttribute('aria-busy', 'false');
    el.grid.innerHTML = `
      <div class="estado" role="status">
        <h2>Nenhum curso encontrado</h2>
        <p>Tente outro termo de pesquisa ou remova alguns filtros.</p>
        <button type="button" class="btn btn--outline" id="limpar-vazio">Limpar filtros</button>
      </div>`;
    document.getElementById('limpar-vazio').addEventListener('click', limparFiltros);
    el.paginacao.hidden = true;
  }

  const cardHtml = (c) => window.ui.cardCurso(c, { mostrarStatus: true });

  function renderLista(pagina) {
    el.grid.setAttribute('aria-busy', 'false');
    const n = pagina.total;
    el.qtd.innerHTML = `<strong>${n}</strong> ${n === 1 ? 'curso encontrado' : 'cursos encontrados'}`;
    el.live.textContent = `${n} ${n === 1 ? 'curso encontrado' : 'cursos encontrados'}`;
    if (n === 0) return renderVazio();
    el.grid.innerHTML = pagina.itens.map(cardHtml).join('');

    const mostrar = pagina.totalPaginas > 1;
    el.paginacao.hidden = !mostrar;
    if (mostrar) {
      el.pagInfo.textContent = `Página ${pagina.pagina} de ${pagina.totalPaginas}`;
      el.pagAnterior.disabled = pagina.pagina <= 1;
      el.pagProxima.disabled = pagina.pagina >= pagina.totalPaginas;
    }
  }

  // ---------------------------------------------------------- dados
  async function carregar() {
    if (abort) abort.abort();
    abort = new AbortController();
    renderLoading();
    gravarUrl();
    try {
      const pagina = await window.api.listarCursos({
        busca: estado.busca, categoria: estado.categoria, modalidade: estado.modalidade,
        status: estado.status, ordenar: estado.ordenar, pagina: estado.pagina, tamanhoPagina: cfg.tamanhoPagina,
      }, { signal: abort.signal });
      renderLista(pagina);
    } catch (e) {
      if (e.name === 'AbortError') return;
      console.error(e);
      renderErro(e instanceof window.api.ApiError ? e : new window.api.ApiError('Erro inesperado.', 0, e.message));
    }
  }

  async function carregarFiltros() {
    try {
      const f = await window.api.obterFiltros();
      el.modalidade.innerHTML = '<option value="">Todas</option>' + f.modalidades.map((m) => `<option value="${esc(m.slug)}">${esc(m.nome)}</option>`).join('');
      el.status.innerHTML = '<option value="">Todos</option>' + f.status.map((s) => `<option value="${esc(s.slug)}">${esc(s.nome)}</option>`).join('');
      el.categorias.innerHTML = f.categorias.map((c) => `
        <button type="button" class="categoria-btn" data-slug="${esc(c.slug)}" aria-pressed="false">
          ${c.icone ? `<img src="assets/icons/${esc(c.icone)}" alt="">` : ''}
          ${esc(c.nome)} <span class="categoria-btn__qtd">(${c.quantidadeCursos})</span>
        </button>`).join('');
      el.categorias.querySelectorAll('.categoria-btn').forEach((b) => b.addEventListener('click', () => {
        estado.categoria = estado.categoria === b.dataset.slug ? '' : b.dataset.slug;
        estado.pagina = 1; sincronizarForm(); carregar();
      }));
    } catch (e) {
      console.warn('Filtros indisponíveis:', e.message); // a listagem ainda mostra o erro principal
    }
    sincronizarForm();
  }

  function limparFiltros() {
    Object.assign(estado, { busca: '', categoria: '', modalidade: '', status: '', pagina: 1 });
    sincronizarForm(); carregar(); el.busca.focus();
  }

  // ---------------------------------------------------------- eventos
  el.form.addEventListener('submit', (ev) => { ev.preventDefault(); estado.busca = el.busca.value.trim(); estado.pagina = 1; carregar(); });
  el.busca.addEventListener('input', () => {
    clearTimeout(debounce);
    debounce = setTimeout(() => { estado.busca = el.busca.value.trim(); estado.pagina = 1; sincronizarForm(); carregar(); }, 350);
  });
  el.modalidade.addEventListener('change', () => { estado.modalidade = el.modalidade.value; estado.pagina = 1; sincronizarForm(); carregar(); });
  el.status.addEventListener('change', () => { estado.status = el.status.value; estado.pagina = 1; sincronizarForm(); carregar(); });
  el.ordenar.addEventListener('change', () => { estado.ordenar = el.ordenar.value; estado.pagina = 1; carregar(); });
  el.limpar.addEventListener('click', limparFiltros);
  el.pagAnterior.addEventListener('click', () => { estado.pagina--; carregar(); el.grid.scrollIntoView(); });
  el.pagProxima.addEventListener('click', () => { estado.pagina++; carregar(); el.grid.scrollIntoView(); });

  // ---------------------------------------------------------- init
  lerUrl();
  renderLoading();
  carregarFiltros().then(carregar);
})();
