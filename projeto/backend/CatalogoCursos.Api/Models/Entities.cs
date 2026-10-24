namespace CatalogoCursos.Api.Models;

public class Categoria
{
    public int Id { get; set; }
    public string Nome { get; set; } = default!;
    public string Slug { get; set; } = default!;
    /// <summary>Nome do ícone (arquivo SVG no front) — opcional.</summary>
    public string? Icone { get; set; }
    public int? MoodleId { get; set; }
    public List<Curso> Cursos { get; set; } = new();
}

public class Modalidade
{
    public int Id { get; set; }
    public string Nome { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public List<Curso> Cursos { get; set; } = new();
}

public class StatusCurso
{
    public int Id { get; set; }
    public string Nome { get; set; } = default!;
    public string Slug { get; set; } = default!;
    /// <summary>Cor sugerida para o selo (hex) — o front pode ignorar.</summary>
    public string? Cor { get; set; }
    public List<Curso> Cursos { get; set; } = new();
}

public class Curso
{
    public int Id { get; set; }
    public string NomeCompleto { get; set; } = default!;
    public string NomeBreve { get; set; } = default!;
    public string Descricao { get; set; } = default!;
    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = default!;
    public int ModalidadeId { get; set; }
    public Modalidade Modalidade { get; set; } = default!;
    public int StatusId { get; set; }
    public StatusCurso Status { get; set; } = default!;
    public int CargaHorariaHoras { get; set; }
    public string PublicoAlvo { get; set; } = default!;
    public string ImagemUrl { get; set; } = default!;
    public DateOnly? DataDisponibilizacao { get; set; }
    /// <summary>Id do curso no Moodle quando sincronizado (null = cadastrado só no catálogo).</summary>
    public int? MoodleId { get; set; }
    public string? MoodleUrl { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    public List<Modulo> Modulos { get; set; } = new();
}

public class Modulo
{
    public int Id { get; set; }
    public int CursoId { get; set; }
    public Curso Curso { get; set; } = default!;
    public int Ordem { get; set; }
    public string Titulo { get; set; } = default!;
    public string Descricao { get; set; } = default!;
    public int CargaHorariaHoras { get; set; }
}

/// <summary>Depoimento de aluno (seção "Quem fez recomenda"). CursoId nulo = depoimento geral da plataforma.</summary>
public class Depoimento
{
    public int Id { get; set; }
    public string Nome { get; set; } = default!;
    public string Cargo { get; set; } = default!;
    public string Texto { get; set; } = default!;
    /// <summary>Nota de 1 a 5.</summary>
    public int Nota { get; set; } = 5;
    public int? CursoId { get; set; }
    public Curso? Curso { get; set; }
    public int Ordem { get; set; }
}

/// <summary>Indicador exibido na home (seção "Impacto transformador").</summary>
public class Indicador
{
    public int Id { get; set; }
    public string Valor { get; set; } = default!;   // ex.: "25.000+"
    public string Rotulo { get; set; } = default!;  // ex.: "Profissionais capacitados"
    public int Ordem { get; set; }
}
