using CatalogoCursos.Api.Models;

namespace CatalogoCursos.Api.Dtos;

public record RefDto(int Id, string Nome, string Slug);
public record CategoriaDto(int Id, string Nome, string Slug, string? Icone, int QuantidadeCursos);
public record StatusDto(int Id, string Nome, string Slug, string? Cor);

/// <summary>Item da listagem (sem módulos).</summary>
public record CursoResumoDto(
    int Id,
    string NomeCompleto,
    string NomeBreve,
    string Descricao,
    RefDto Categoria,
    RefDto Modalidade,
    StatusDto Status,
    int CargaHorariaHoras,
    string PublicoAlvo,
    string ImagemUrl,
    DateOnly? DataDisponibilizacao,
    bool OrigemMoodle);

public record ModuloDto(int Ordem, string Titulo, string Descricao, int CargaHorariaHoras);

/// <summary>Detalhe completo do curso.</summary>
public record CursoDetalheDto(
    int Id,
    string NomeCompleto,
    string NomeBreve,
    string Descricao,
    RefDto Categoria,
    RefDto Modalidade,
    StatusDto Status,
    int CargaHorariaHoras,
    string PublicoAlvo,
    string ImagemUrl,
    DateOnly? DataDisponibilizacao,
    bool OrigemMoodle,
    int? MoodleId,
    string? MoodleUrl,
    IReadOnlyList<ModuloDto> Modulos);

public record DepoimentoDto(int Id, string Nome, string Cargo, string Texto, int Nota, int? CursoId);
public record IndicadorDto(int Id, string Valor, string Rotulo);

public record PaginaDto<T>(IReadOnlyList<T> Itens, int Total, int Pagina, int TamanhoPagina, int TotalPaginas);

public record CursoFiltro(string? Busca, string? Categoria, string? Modalidade, string? Status, int Pagina = 1, int TamanhoPagina = 12, string? Ordenar = null);

public static class Mapeamento
{
    public static RefDto ToRef(this Categoria c) => new(c.Id, c.Nome, c.Slug);
    public static RefDto ToRef(this Modalidade m) => new(m.Id, m.Nome, m.Slug);
    public static StatusDto ToDto(this StatusCurso s) => new(s.Id, s.Nome, s.Slug, s.Cor);

    public static CursoResumoDto ToResumo(this Curso c) => new(
        c.Id, c.NomeCompleto, c.NomeBreve, c.Descricao,
        c.Categoria.ToRef(), c.Modalidade.ToRef(), c.Status.ToDto(),
        c.CargaHorariaHoras, c.PublicoAlvo, c.ImagemUrl, c.DataDisponibilizacao,
        c.MoodleId.HasValue);

    public static CursoDetalheDto ToDetalhe(this Curso c) => new(
        c.Id, c.NomeCompleto, c.NomeBreve, c.Descricao,
        c.Categoria.ToRef(), c.Modalidade.ToRef(), c.Status.ToDto(),
        c.CargaHorariaHoras, c.PublicoAlvo, c.ImagemUrl, c.DataDisponibilizacao,
        c.MoodleId.HasValue, c.MoodleId, c.MoodleUrl,
        c.Modulos.OrderBy(m => m.Ordem).Select(m => new ModuloDto(m.Ordem, m.Titulo, m.Descricao, m.CargaHorariaHoras)).ToList());
}
