// Utilitários de interface compartilhados (listagem e detalhe).
(function () {
  const esc = (s) => String(s ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

  const fmtData = (iso) => {
    if (!iso) return null;
    const [a, m, d] = iso.split('-').map(Number);
    return new Date(a, m - 1, d).toLocaleDateString('pt-BR');
  };

  const fmtCarga = (h) => (h ? `${h}h` : 'A definir');

  // Ícones inline (Material Symbols, mesmo estilo dos SVGs do kit)
  const icones = {
    relogio: '<svg viewBox="0 0 20 20" aria-hidden="true"><path fill="currentColor" d="M10 18a8 8 0 1 1 0-16 8 8 0 0 1 0 16Zm0-1.5a6.5 6.5 0 1 0 0-13 6.5 6.5 0 0 0 0 13Zm.75-6.8 2.6 1.5-.75 1.3-3.35-1.95V5.5h1.5v4.2Z"/></svg>',
    calendario: '<svg viewBox="0 0 20 20" aria-hidden="true"><path fill="currentColor" d="M4.5 18A1.5 1.5 0 0 1 3 16.5v-11A1.5 1.5 0 0 1 4.5 4H6V2h1.5v2h5V2H14v2h1.5A1.5 1.5 0 0 1 17 5.5v11a1.5 1.5 0 0 1-1.5 1.5h-11Zm0-1.5h11V9h-11v7.5Zm0-9h11v-2h-11v2Z"/></svg>',
    modalidade: '<svg viewBox="0 0 20 20" aria-hidden="true"><path fill="currentColor" d="M3.5 15A1.5 1.5 0 0 1 2 13.5v-8A1.5 1.5 0 0 1 3.5 4h13A1.5 1.5 0 0 1 18 5.5v8a1.5 1.5 0 0 1-1.5 1.5h-13Zm0-1.5h13v-8h-13v8ZM6 18v-1.5h8V18H6Z"/></svg>',
    certificado: '<svg viewBox="0 0 20 20" aria-hidden="true"><path fill="currentColor" d="M10 2.5a4.5 4.5 0 0 1 2.9 7.94V18l-2.9-1.5L7.1 18v-7.56A4.5 4.5 0 0 1 10 2.5Zm0 1.5a3 3 0 1 0 0 6 3 3 0 0 0 0-6Zm-1.4 7.72v3.83l1.4-.72 1.4.72v-3.83a4.5 4.5 0 0 1-2.8 0Z"/></svg>',
    publico: '<svg viewBox="0 0 20 20" aria-hidden="true"><path fill="currentColor" d="M7 10a3 3 0 1 1 0-6 3 3 0 0 1 0 6Zm0-1.5a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3ZM1 16v-1.1c0-.6.3-1.1.8-1.4A9.7 9.7 0 0 1 7 12.3c1.9 0 3.6.4 5.2 1.2.5.3.8.8.8 1.4V16H1Zm1.5-1.5h9v-.2A8.3 8.3 0 0 0 7 13.8a8.3 8.3 0 0 0-4.5.5v.2ZM14 10a3 3 0 1 1 0-6 3 3 0 0 1 0 6Zm0-1.5a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3Zm.3 7.5v-1.6c0-.9-.3-1.6-.8-2.2 1.5.1 2.9.5 4.2 1.1.6.3.9.8.9 1.4V16h-4.3Z"/></svg>',
    play: '<svg viewBox="0 0 24 24" aria-hidden="true"><path fill="currentColor" d="M8 5v14l11-7L8 5Z"/></svg>',
  };

  const estrelas = (n) => `<div class="estrelas" aria-label="${n} de 5 estrelas">${Array.from({ length: 5 }, (_, i) =>
    `<img src="assets/icons/star.svg" alt="" ${i >= n ? 'style="opacity:.3"' : ''}>`).join('')}</div>`;

  // Card vermelho de curso (home e catálogo) — mesmo markup nas duas páginas.
  const cardCurso = (c, { mostrarStatus = false } = {}) => {
    const data = fmtData(c.dataDisponibilizacao);
    return `
      <article class="card-curso">
        <div class="card-curso__midia">
          <img src="${esc(window.api.imagemUrl(c.imagemUrl))}" alt="" loading="lazy" width="400" height="225"
               onerror="this.onerror=null;this.src='${window.APP_CONFIG.imagemPadrao}'">
          <span class="chip card-curso__chip">${c.categoria.icone ? `<img src="assets/icons/${esc(c.categoria.icone)}" alt="">` : ''}${esc(c.categoria.nome)}</span>
          ${mostrarStatus ? `<span class="selo card-curso__selo" style="--selo-cor:${esc(c.status.cor || '')}">${esc(c.status.nome)}</span>` : ''}
        </div>
        <div class="card-curso__body">
          <h3 class="card-curso__titulo"><a href="curso.html?id=${c.id}">${esc(c.nomeCompleto)}</a></h3>
          <p class="card-curso__desc">${esc(c.descricao)}</p>
          <ul class="card-curso__meta" aria-label="Informações do curso">
            <li>${icones.modalidade}<span>${esc(c.modalidade.nome)}</span></li>
            <li>${icones.relogio}<span>${fmtCarga(c.cargaHorariaHoras)}</span></li>
            ${data ? `<li>${icones.calendario}<span>${data}</span></li>` : ''}
          </ul>
        </div>
      </article>`;
  };

  const depoimentoCard = (d) => `
      <article class="depoimento">
        <p class="depoimento__texto">“${esc(d.texto)}”</p>
        <div class="depoimento__autor"><strong>${esc(d.nome)}</strong><span>${esc(d.cargo)}</span>${estrelas(d.nota)}</div>
      </article>`;

  // Setas de carrossel: rolam uma "página" do container.
  function carrossel(track, antes, depois) {
    const passo = () => track.clientWidth * 0.9;
    antes?.addEventListener('click', () => track.scrollBy({ left: -passo(), behavior: 'smooth' }));
    depois?.addEventListener('click', () => track.scrollBy({ left: passo(), behavior: 'smooth' }));
    const atualizar = () => {
      if (antes) antes.disabled = track.scrollLeft <= 2;
      if (depois) depois.disabled = track.scrollLeft + track.clientWidth >= track.scrollWidth - 2;
    };
    track.addEventListener('scroll', atualizar, { passive: true });
    window.addEventListener('resize', atualizar);
    atualizar();
  }

  window.ui = { esc, fmtData, fmtCarga, icones, estrelas, cardCurso, depoimentoCard, carrossel };
})();
