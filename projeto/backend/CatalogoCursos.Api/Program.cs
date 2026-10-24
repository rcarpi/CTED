using System.Text.Json.Serialization;
using CatalogoCursos.Api.Data;
using CatalogoCursos.Api.Endpoints;
using CatalogoCursos.Api.Moodle;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------- serviços
var cs = builder.Configuration.GetConnectionString("Catalogo")
         ?? throw new InvalidOperationException("ConnectionStrings:Catalogo não configurada.");

builder.Services.AddDbContext<CatalogoDbContext>(o =>
    o.UseMySql(cs, new MySqlServerVersion(new Version(8, 0, 36)),
        my => my.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null)));

builder.Services.Configure<MoodleOptions>(builder.Configuration.GetSection(MoodleOptions.Section));
builder.Services.AddHttpClient<MoodleClient>((sp, http) =>
{
    var opt = builder.Configuration.GetSection(MoodleOptions.Section).Get<MoodleOptions>() ?? new MoodleOptions();
    if (!string.IsNullOrWhiteSpace(opt.BaseUrl)) http.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
    // O Moodle redireciona para $CFG->wwwroot quando o Host da requisição não bate.
    // Dentro do Docker chamamos http://moodle:8080, mas o wwwroot é http://localhost:8080;
    // enviar o Host público evita o redirect (que, seguido, cairia na própria API -> 404).
    if (!string.IsNullOrWhiteSpace(opt.PublicBaseUrl) && Uri.TryCreate(opt.PublicBaseUrl, UriKind.Absolute, out var pub))
        http.DefaultRequestHeaders.Host = pub.Authority;
    http.Timeout = TimeSpan.FromSeconds(30);
})
// Sem seguir redirects: um 3xx vira erro explícito em vez de um 404 enganoso.
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddScoped<MoodleSyncService>();

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var origens = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    if (origens.Length == 0) p.AllowAnyOrigin();
    else p.WithOrigins(origens).SetIsOriginAllowedToAllowWildcardSubdomains();
    p.AllowAnyHeader().AllowAnyMethod();
}));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "Catálogo de Cursos — API", Version = "v1",
        Description = "API de consulta do catálogo de cursos (CTED). Fonte: banco MySQL próprio, com importação opcional do Moodle." });
    var xml = Path.Combine(AppContext.BaseDirectory, "CatalogoCursos.Api.xml");
    if (File.Exists(xml)) o.IncludeXmlComments(xml);
});
builder.Services.AddProblemDetails();

var app = builder.Build();

// ----------------------------------------------------------------- pipeline
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(o => { o.RoutePrefix = "swagger"; o.DocumentTitle = "Catálogo de Cursos — API"; });

app.MapGet("/health", async Task<IResult> (CatalogoDbContext db, CancellationToken ct) =>
{
    var ok = await db.Database.CanConnectAsync(ct);
    return ok ? Results.Ok(new { status = "Healthy", banco = "ok" })
              : Results.Json(new { status = "Unhealthy", banco = "indisponivel" }, statusCode: 503);
}).ExcludeFromDescription();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapCatalogo();
app.MapMoodle();

// ----------------------------------------------------------------- migrações + seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    // Retentativa manual: o MySQL pode ainda estar subindo quando a API inicia.
    for (var tentativa = 1; ; tentativa++)
    {
        try
        {
            // Com migrations no projeto (dotnet ef migrations add ...) aplica-as;
            // sem migrations, cria o schema diretamente a partir do modelo.
            if (db.Database.GetMigrations().Any())
            {
                await db.Database.MigrateAsync();
                log.LogInformation("Migrations aplicadas.");
            }
            else
            {
                var criado = await db.Database.EnsureCreatedAsync();
                log.LogInformation(criado ? "Schema criado via EnsureCreated (sem migrations)." : "Schema já existia.");
                if (!criado)
                {
                    // Banco antigo + modelo novo: cria só as tabelas que faltam.
                    var novas = await SchemaUtil.EnsureMissingTablesAsync(db, log);
                    if (novas > 0) log.LogInformation("{N} tabela(s) nova(s) criada(s).", novas);
                }
            }
            await DbSeeder.SeedAsync(db);
            log.LogInformation("Seed verificado/aplicado.");
            break;
        }
        catch (Exception ex) when (tentativa < 40)
        {
            log.LogWarning("Banco indisponível (tentativa {T}): {Msg}", tentativa, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

app.Run();

public partial class Program { }
