using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace CatalogoCursos.Api.Moodle;

/// <summary>
/// Cliente HTTP para os Web Services REST do Moodle
/// (https://docs.moodle.org/dev/Web_service_API_functions).
/// Funções usadas (todas disponíveis no serviço moodle_mobile_app):
///   - core_webservice_get_site_info
///   - core_course_get_categories
///   - core_course_get_courses_by_field
/// </summary>
public class MoodleClient(HttpClient http, IOptions<MoodleOptions> options, ILogger<MoodleClient> logger)
{
    private readonly MoodleOptions _opt = options.Value;
    private string? _tokenCache;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public bool Configurado => _opt.Configurado;

    // ---------------------------------------------------------------- token
    public async Task<string> ObterTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_opt.Token)) return _opt.Token;
        if (_tokenCache is not null) return _tokenCache;

        // Fallback (apenas ambiente local): login/token.php com usuário/senha.
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = _opt.Username!,
            ["password"] = _opt.Password!,
            ["service"] = _opt.Service,
        });
        using var resp = await http.PostAsync("/login/token.php", form, ct);
        await GarantirSucessoAsync(resp, "login/token.php", ct);
        var body = await resp.Content.ReadFromJsonAsync<TokenResponse>(Json, ct)
                   ?? throw new MoodleException("Resposta vazia de login/token.php");
        if (body.Error is not null) throw new MoodleException($"login/token.php: {body.Error} ({body.ErrorCode})");
        if (string.IsNullOrEmpty(body.Token)) throw new MoodleException("login/token.php não retornou token");
        _tokenCache = body.Token;
        logger.LogInformation("Token do Moodle obtido via login/token.php para o serviço {Service}", _opt.Service);
        return _tokenCache;
    }

    // ---------------------------------------------------------------- chamadas
    public Task<SiteInfo> GetSiteInfoAsync(CancellationToken ct) =>
        CallAsync<SiteInfo>("core_webservice_get_site_info", new(), ct);

    public Task<List<MoodleCategory>> GetCategoriesAsync(CancellationToken ct) =>
        CallAsync<List<MoodleCategory>>("core_course_get_categories", new(), ct);

    public async Task<List<MoodleCourse>> GetCoursesAsync(CancellationToken ct)
    {
        // Sem "field" retorna todos os cursos visíveis para o usuário do token.
        var r = await CallAsync<CoursesByFieldResponse>("core_course_get_courses_by_field", new(), ct);
        // O site principal (id 1, format=site) nunca é um curso de catálogo.
        return r.Courses.Where(c => c.Format != "site").ToList();
    }

    private async Task<T> CallAsync<T>(string wsfunction, Dictionary<string, string> args, CancellationToken ct)
    {
        var token = await ObterTokenAsync(ct);
        var form = new Dictionary<string, string>(args)
        {
            ["wstoken"] = token,
            ["wsfunction"] = wsfunction,
            ["moodlewsrestformat"] = "json",
        };
        using var resp = await http.PostAsync("/webservice/rest/server.php", new FormUrlEncodedContent(form), ct);
        await GarantirSucessoAsync(resp, wsfunction, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);

        // O Moodle devolve HTTP 200 mesmo em erro; o corpo vira {"exception":...}.
        if (raw.TrimStart().StartsWith("{\"exception\"", StringComparison.Ordinal))
        {
            var err = JsonSerializer.Deserialize<MoodleError>(raw, Json);
            throw new MoodleException($"{wsfunction}: {err?.Message} [{err?.ErrorCode}]");
        }
        return JsonSerializer.Deserialize<T>(raw, Json)
               ?? throw new MoodleException($"{wsfunction}: resposta vazia");
    }

    /// <summary>Transforma redirects e erros HTTP em mensagens acionáveis.</summary>
    private static async Task GarantirSucessoAsync(HttpResponseMessage resp, string contexto, CancellationToken ct)
    {
        var code = (int)resp.StatusCode;
        if (code is >= 300 and < 400)
            throw new MoodleException($"{contexto}: o Moodle redirecionou para '{resp.Headers.Location}'. " +
                "O Host da requisição não corresponde a $CFG->wwwroot. Ajuste Moodle__PublicBaseUrl para o mesmo valor do wwwroot " +
                "(ex.: http://localhost:8080) ou configure o Moodle para aceitar o host interno.");
        if (!resp.IsSuccessStatusCode)
        {
            var corpo = await resp.Content.ReadAsStringAsync(ct);
            var trecho = corpo.Length > 300 ? corpo[..300] + "…" : corpo;
            throw new MoodleException($"{contexto}: HTTP {code} ({resp.ReasonPhrase}) em {resp.RequestMessage?.RequestUri}. {trecho}");
        }
    }

    // ---------------------------------------------------------------- modelos
    private record TokenResponse(string? Token, string? Error, string? ErrorCode);
    private record MoodleError(string? Exception, string? ErrorCode, string? Message);
    private record CoursesByFieldResponse(List<MoodleCourse> Courses);

    public record SiteInfo(string SiteName, string SiteUrl, string Username, string Release, int UserId);

    public record MoodleCategory(int Id, string Name, string? IdNumber, string? Description, int Parent, int CourseCount, int Visible);

    public record MoodleCourse(
        int Id,
        string FullName,
        string ShortName,
        string? DisplayName,
        string? Summary,
        int? CategoryId,
        string? CategoryName,
        string? Format,
        int? Visible,
        long? StartDate,
        long? EndDate,
        long? TimeModified,
        string? CourseImage,
        List<MoodleCustomField>? CustomFields);

    /// <summary>Campos personalizados de curso (Administração > Cursos > Campos personalizados).</summary>
    public record MoodleCustomField(string Name, string ShortName, string Type, string? Value, string? ValueRaw);
}

public class MoodleException(string message) : Exception(message);
