using CatalogoCursos.Api.Data;
using CatalogoCursos.Api.Dtos;
using CatalogoCursos.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatalogoCursos.Api.Endpoints;

public static class CatalogoEndpoints
{
    public static IEndpointRouteBuilder MapCatalogo(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api").WithTags("Catálogo");

        // ------------------------------------------------------------------
        // GET /api/cursos?busca=&categoria=&modalidade=&status=&pagina=&tamanhoPagina=&ordenar=
        // Filtros são combináveis. categoria/modalidade/status aceitam slug OU id,
        // e múltiplos valores separados por vírgula (ex.: status=em-breve,inscricoes-abertas).
        // ------------------------------------------------------------------
        g.MapGet("/cursos", async ([AsParameters] CursoFiltro f, CatalogoDbContext db, CancellationToken ct) =>
        {
            var pagina = Math.Max(1, f.Pagina);
            var tamanho = Math.Clamp(f.TamanhoPagina, 1, 100);

            var q = db.Cursos
                .AsNoTracking()
                .Include(c => c.Categoria)
                .Include(c => c.Modalidade)
                .Include(c => c.Status)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(f.Busca))
            {
                var termo = f.Busca.Trim();
                // MySQL com collation *_ci já é case-insensitive; LIKE cobre nome completo e breve.
                q = q.Where(c => EF.Functions.Like(c.NomeCompleto, $"%{termo}%")
                              || EF.Functions.Like(c.NomeBreve, $"%{termo}%"));
            }

            var (catSlugs, catIds) = Split(f.Categoria);
            if (catSlugs.Length + catIds.Length > 0)
                q = q.Where(c => catSlugs.Contains(c.Categoria.Slug) || catIds.Contains(c.CategoriaId));

            var (modSlugs, modIds) = Split(f.Modalidade);
            if (modSlugs.Length + modIds.Length > 0)
                q = q.Where(c => modSlugs.Contains(c.Modalidade.Slug) || modIds.Contains(c.ModalidadeId));

            var (stSlugs, stIds) = Split(f.Status);
            if (stSlugs.Length + stIds.Length > 0)
                q = q.Where(c => stSlugs.Contains(c.Status.Slug) || stIds.Contains(c.StatusId));

            q = (f.Ordenar?.ToLowerInvariant()) switch
            {
                "nome-desc" => q.OrderByDescending(c => c.NomeCompleto),
                "data" => q.OrderBy(c => c.DataDisponibilizacao == null).ThenBy(c => c.DataDisponibilizacao).ThenBy(c => c.NomeCompleto),
                "data-desc" => q.OrderBy(c => c.DataDisponibilizacao == null).ThenByDescending(c => c.DataDisponibilizacao).ThenBy(c => c.NomeCompleto),
                "carga" => q.OrderBy(c => c.CargaHorariaHoras).ThenBy(c => c.NomeCompleto),
                "carga-desc" => q.OrderByDescending(c => c.CargaHorariaHoras).ThenBy(c => c.NomeCompleto),
                _ => q.OrderBy(c => c.NomeCompleto),
            };

            var total = await q.CountAsync(ct);
            var itens = await q.Skip((pagina - 1) * tamanho).Take(tamanho).ToListAsync(ct);

            return Results.Ok(new PaginaDto<CursoResumoDto>(
                itens.Select(c => c.ToResumo()).ToList(),
                total, pagina, tamanho, (int)Math.Ceiling(total / (double)tamanho)));
        })
        .WithName("ListarCursos")
        .WithSummary("Lista cursos com busca por nome e filtros combináveis por categoria, modalidade e status.")
        .Produces<PaginaDto<CursoResumoDto>>();

        g.MapGet("/cursos/{id:int}", async Task<IResult> (int id, CatalogoDbContext db, CancellationToken ct) =>
        {
            var c = await db.Cursos.AsNoTracking()
                .Include(x => x.Categoria).Include(x => x.Modalidade).Include(x => x.Status).Include(x => x.Modulos)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            return c is null
                ? Results.Problem(statusCode: 404, title: "Curso não encontrado", detail: $"Não existe curso com id {id}.")
                : Results.Ok(c.ToDetalhe());
        })
        .WithName("ObterCurso")
        .WithSummary("Detalhe de um curso, incluindo módulos.")
        .Produces<CursoDetalheDto>().ProducesProblem(404);

        g.MapGet("/categorias", async (CatalogoDbContext db, CancellationToken ct) =>
            await db.Categorias.AsNoTracking().OrderBy(c => c.Nome)
                .Select(c => new CategoriaDto(c.Id, c.Nome, c.Slug, c.Icone, c.Cursos.Count))
                .ToListAsync(ct))
        .WithName("ListarCategorias").Produces<List<CategoriaDto>>();

        g.MapGet("/modalidades", async (CatalogoDbContext db, CancellationToken ct) =>
            await db.Modalidades.AsNoTracking().OrderBy(m => m.Nome).Select(m => new RefDto(m.Id, m.Nome, m.Slug)).ToListAsync(ct))
        .WithName("ListarModalidades").Produces<List<RefDto>>();

        g.MapGet("/status", async (CatalogoDbContext db, CancellationToken ct) =>
            await db.Status.AsNoTracking().OrderBy(s => s.Id).Select(s => new StatusDto(s.Id, s.Nome, s.Slug, s.Cor)).ToListAsync(ct))
        .WithName("ListarStatus").Produces<List<StatusDto>>();

        // Endpoint de conveniência: tudo que o front precisa para montar os filtros em 1 chamada.
        g.MapGet("/filtros", async (CatalogoDbContext db, CancellationToken ct) => Results.Ok(new
        {
            categorias = await db.Categorias.AsNoTracking().OrderBy(c => c.Nome)
                .Select(c => new CategoriaDto(c.Id, c.Nome, c.Slug, c.Icone, c.Cursos.Count)).ToListAsync(ct),
            modalidades = await db.Modalidades.AsNoTracking().OrderBy(m => m.Nome).Select(m => new RefDto(m.Id, m.Nome, m.Slug)).ToListAsync(ct),
            status = await db.Status.AsNoTracking().OrderBy(s => s.Id).Select(s => new StatusDto(s.Id, s.Nome, s.Slug, s.Cor)).ToListAsync(ct),
        }))
        .WithName("ObterFiltros");

        // GET /api/indicadores — números da seção "Impacto transformador" (home).
        g.MapGet("/indicadores", async (CatalogoDbContext db, CancellationToken ct) =>
            await db.Indicadores.AsNoTracking().OrderBy(i => i.Ordem).Select(i => new IndicadorDto(i.Id, i.Valor, i.Rotulo)).ToListAsync(ct))
        .WithName("ListarIndicadores").Produces<List<IndicadorDto>>();

        // GET /api/depoimentos?cursoId=3 — depoimentos do curso; sem os do curso, retorna os gerais (CursoId nulo).
        g.MapGet("/depoimentos", async (int? cursoId, CatalogoDbContext db, CancellationToken ct) =>
        {
            var q = db.Depoimentos.AsNoTracking();
            List<Depoimento> lista = new();
            if (cursoId is not null)
                lista = await q.Where(d => d.CursoId == cursoId).OrderBy(d => d.Ordem).ToListAsync(ct);
            if (lista.Count == 0)
                lista = await q.Where(d => d.CursoId == null).OrderBy(d => d.Ordem).ToListAsync(ct);
            return lista.Select(d => new DepoimentoDto(d.Id, d.Nome, d.Cargo, d.Texto, d.Nota, d.CursoId)).ToList();
        })
        .WithName("ListarDepoimentos").Produces<List<DepoimentoDto>>();

        return app;
    }

    /// <summary>Separa "a,b,3" em slugs (["a","b"]) e ids ([3]).</summary>
    private static (string[] Slugs, int[] Ids) Split(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return (Array.Empty<string>(), Array.Empty<int>());
        var partes = v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var ids = partes.Where(p => int.TryParse(p, out _)).Select(int.Parse).ToArray();
        var slugs = partes.Where(p => !int.TryParse(p, out _)).Select(p => p.ToLowerInvariant()).ToArray();
        return (slugs, ids);
    }
}
