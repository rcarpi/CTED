using CatalogoCursos.Api.Moodle;

namespace CatalogoCursos.Api.Endpoints;

public static class MoodleEndpoints
{
    public static IEndpointRouteBuilder MapMoodle(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/moodle").WithTags("Moodle");

        // GET /api/moodle/status — verifica configuração e conectividade com o Moodle.
        g.MapGet("/status", async Task<IResult> (MoodleClient client, CancellationToken ct) =>
        {
            if (!client.Configurado)
                return Results.Ok(new { configurado = false, conectado = false, mensagem = "Moodle não configurado (Moodle__BaseUrl / Token ou usuário+senha)." });
            try
            {
                var info = await client.GetSiteInfoAsync(ct);
                return Results.Ok(new { configurado = true, conectado = true, site = info.SiteName, url = info.SiteUrl, versao = info.Release, usuario = info.Username });
            }
            catch (Exception ex)
            {
                return Results.Ok(new { configurado = true, conectado = false, mensagem = ex.Message });
            }
        }).WithSummary("Diagnóstico da integração com o Moodle.");

        // GET /api/moodle/cursos — leitura direta (sem persistir), útil para depuração.
        g.MapGet("/cursos", async Task<IResult> (MoodleClient client, CancellationToken ct) =>
        {
            if (!client.Configurado) return Results.Problem(statusCode: 503, title: "Moodle não configurado");
            try { return Results.Ok(await client.GetCoursesAsync(ct)); }
            catch (Exception ex) { return Results.Problem(statusCode: 502, title: "Falha ao consultar o Moodle", detail: ex.Message); }
        }).WithSummary("Lista os cursos diretamente do Moodle (sem gravar no catálogo).");

        // POST /api/moodle/sync — importa categorias/cursos do Moodle para o catálogo.
        g.MapPost("/sync", async Task<IResult> (MoodleClient client, MoodleSyncService sync, CancellationToken ct) =>
        {
            if (!client.Configurado) return Results.Problem(statusCode: 503, title: "Moodle não configurado");
            try { return Results.Ok(await sync.SincronizarAsync(ct)); }
            catch (Exception ex) { return Results.Problem(statusCode: 502, title: "Falha na sincronização", detail: ex.Message); }
        }).WithSummary("Sincroniza cursos do Moodle para o banco do catálogo (upsert por MoodleId).");

        return app;
    }
}
