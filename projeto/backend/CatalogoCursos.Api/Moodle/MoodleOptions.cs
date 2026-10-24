namespace CatalogoCursos.Api.Moodle;

public class MoodleOptions
{
    public const string Section = "Moodle";

    /// <summary>URL interna usada pela API para falar com o Moodle (ex.: http://moodle:8080 dentro do Docker).</summary>
    public string? BaseUrl { get; set; }

    /// <summary>URL pública do Moodle, usada para montar links clicáveis no front (ex.: http://localhost:8080).</summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>Token de Web Service. Se vazio, tenta obter via login/token.php com usuário/senha.</summary>
    public string? Token { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }

    /// <summary>Shortname do serviço externo (padrão: moodle_mobile_app).</summary>
    public string Service { get; set; } = "moodle_mobile_app";

    public bool Configurado => !string.IsNullOrWhiteSpace(BaseUrl)
        && (!string.IsNullOrWhiteSpace(Token) || (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password)));
}
