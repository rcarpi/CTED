using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace CatalogoCursos.Api.Data;

/// <summary>
/// Sem migrations, <c>EnsureCreated</c> não cria tabelas novas em um banco que já
/// possui tabelas. Este utilitário gera o script de criação do modelo atual e
/// executa apenas os comandos das tabelas que ainda não existem — suficiente para
/// o ambiente local/demo. Em produção, prefira migrations (dotnet ef).
/// </summary>
public static class SchemaUtil
{
    public static async Task<int> EnsureMissingTablesAsync(CatalogoDbContext db, ILogger log, CancellationToken ct = default)
    {
        var existentes = (await db.Database
                .SqlQueryRaw<string>("SELECT TABLE_NAME AS `Value` FROM information_schema.tables WHERE table_schema = DATABASE()")
                .ToListAsync(ct))
            .Select(t => t.ToLowerInvariant()).ToHashSet();

        var script = db.Database.GenerateCreateScript();
        var comandos = Regex.Split(script, @";\s*\r?\n").Select(c => c.Trim()).Where(c => c.Length > 0);

        var criadas = 0;
        foreach (var cmd in comandos)
        {
            var m = Regex.Match(cmd, @"CREATE\s+(?:UNIQUE\s+)?(?:TABLE|INDEX\s+`[^`]+`\s+ON)\s+`([^`]+)`", RegexOptions.IgnoreCase);
            if (!m.Success) continue;                       // ALTER DATABASE etc.
            var tabela = m.Groups[1].Value.ToLowerInvariant();
            if (existentes.Contains(tabela)) continue;      // já existia antes desta execução

            await db.Database.ExecuteSqlRawAsync(cmd, ct);
            if (cmd.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
            {
                criadas++;
                log.LogInformation("Tabela criada: {Tabela}", tabela);
            }
        }
        return criadas;
    }
}
