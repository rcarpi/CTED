// Camada de acesso à API (fetch) — sem dependências.
(function () {
  const cfg = window.APP_CONFIG;

  class ApiError extends Error {
    constructor(mensagem, status, detalhe) { super(mensagem); this.status = status; this.detalhe = detalhe; }
  }

  async function get(caminho, params, { signal, timeoutMs = 15000 } = {}) {
    const url = new URL(cfg.apiBase + caminho, location.origin);
    Object.entries(params || {}).forEach(([k, v]) => {
      if (v !== undefined && v !== null && v !== '') url.searchParams.set(k, v);
    });

    const ctrl = new AbortController();
    const timer = setTimeout(() => ctrl.abort(), timeoutMs);
    if (signal) signal.addEventListener('abort', () => ctrl.abort(), { once: true });

    let resp;
    try {
      resp = await fetch(url, { signal: ctrl.signal, headers: { Accept: 'application/json' } });
    } catch (e) {
      if (e.name === 'AbortError' && !(signal && signal.aborted)) throw new ApiError('Tempo de resposta esgotado.', 0);
      if (e.name === 'AbortError') throw e;
      throw new ApiError('Não foi possível conectar à API.', 0, e.message);
    } finally { clearTimeout(timer); }

    if (!resp.ok) {
      let detalhe = '';
      try { const p = await resp.json(); detalhe = p.detail || p.title || ''; } catch { /* corpo não JSON */ }
      throw new ApiError(resp.status === 404 ? 'Recurso não encontrado.' : `Erro ${resp.status} ao consultar a API.`, resp.status, detalhe);
    }
    return resp.json();
  }

  window.api = {
    ApiError,
    listarCursos: (filtro, opts) => get('/cursos', filtro, opts),
    obterCurso: (id, opts) => get(`/cursos/${encodeURIComponent(id)}`, null, opts),
    obterFiltros: (opts) => get('/filtros', null, opts),
    listarDepoimentos: (cursoId, opts) => get('/depoimentos', cursoId ? { cursoId } : null, opts),
    listarIndicadores: (opts) => get('/indicadores', null, opts),
    imagemUrl(caminho) {
      if (!caminho) return cfg.imagemPadrao;
      if (/^(https?:)?\/\//.test(caminho) || caminho.startsWith('data:')) return caminho;
      return cfg.imagemBase + caminho.replace(/^\/+/, '');
    },
  };
})();
