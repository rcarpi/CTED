using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using CatalogoCursos.Api.Data;
using CatalogoCursos.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CatalogoCursos.Api.Moodle;

public record SyncResultado(int CategoriasCriadas, int CursosCriados, int CursosAtualizados, int CursosVinculados, int CursosIgnorados, List<string> Avisos);

/// <summary>
/// Importa categorias e cursos do Moodle para o banco do catálogo (upsert por MoodleId).
/// Curso do Moodle cujo shortname coincide com o NomeBreve de um curso local é
/// VINCULADO (recebe MoodleId/MoodleUrl) sem sobrescrever os dados do catálogo.
///
/// Mapeamento Moodle -> Catálogo:
///   fullname/shortname/summary  -> NomeCompleto/NomeBreve/Descricao (HTML removido)
///   categoryid                  -> Categoria (criada se não existir)
///   customfields[modalidade]    -> Modalidade   (fallback: "Modalidade: X" no summary; senão EAD)
///   customfields[carga_horaria] -> CargaHoraria (fallback: "Carga horária: 40h" no summary; senão 0)
///   customfields[publico_alvo]  -> PublicoAlvo  (fallback: "Público-alvo: X" no summary)
///   visible/startdate/enddate   -> Status (em-breve | inscricoes-abertas | encerrado)
///   courseimage                 -> ImagemUrl (fallback: imagem padrão)
///   startdate                   -> DataDisponibilizacao
/// </summary>
public class MoodleSyncService(
    MoodleClient client,
    CatalogoDbContext db,
    IOptions<MoodleOptions> options,
    ILogger<MoodleSyncService> logger)
{
    private const string ImagemPadrao = "img/cursos/moodle-padrao.svg";

    public async Task<SyncResultado> SincronizarAsync(CancellationToken ct)
    {
        var avisos = new List<string>();
        var categoriasCriadas = 0; var criados = 0; var atualizados = 0; var ignorados = 0;

        var mcats = await client.GetCategoriesAsync(ct);
        var mcursos = await client.GetCoursesAsync(ct);

        // --- categorias
        var cats = await db.Categorias.ToListAsync(ct);
        foreach (var mc in mcats)
        {
            if (cats.Any(c => c.MoodleId == mc.Id)) continue;
            var slug = Slugify(mc.Name);
            var existente = cats.FirstOrDefault(c => c.Slug == slug);
            if (existente is not null) { existente.MoodleId = mc.Id; continue; }
            var nova = new Categoria { Nome = mc.Name, Slug = slug, MoodleId = mc.Id };
            db.Categorias.Add(nova); cats.Add(nova); categoriasCriadas++;
        }

        var modalidades = await db.Modalidades.ToListAsync(ct);
        var status = await db.Status.ToListAsync(ct);
        var cursos = await db.Cursos.Include(c => c.Modulos).Where(c => c.MoodleId != null).ToListAsync(ct);
        // Cursos cadastrados só no catálogo: candidatos a vínculo por nome breve (= shortname no Moodle).
        var locais = await db.Cursos.Where(c => c.MoodleId == null).ToListAsync(ct);
        var vinculados = 0;
        var agora = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var publicBase = (options.Value.PublicBaseUrl ?? options.Value.BaseUrl ?? "").TrimEnd('/');

        foreach (var mc in mcursos)
        {
            var categoria = cats.FirstOrDefault(c => c.MoodleId == mc.CategoryId);
            if (categoria is null)
            {
                avisos.Add($"Curso '{mc.ShortName}' (id {mc.Id}) ignorado: categoria {mc.CategoryId} não encontrada.");
                ignorados++; continue;
            }

            var summaryTexto = StripHtml(mc.Summary);
            var modalidadeNome = Campo(mc, "modalidade") ?? Extrair(summaryTexto, @"Modalidade:\s*([^\n]+?)(?:\s{2,}|\n|$)") ?? "EAD";
            var modalidade = modalidades.FirstOrDefault(m => string.Equals(m.Nome, modalidadeNome.Trim(), StringComparison.OrdinalIgnoreCase)
                                                        || m.Slug == Slugify(modalidadeNome));
            if (modalidade is null)
            {
                modalidade = new Modalidade { Nome = modalidadeNome.Trim(), Slug = Slugify(modalidadeNome) };
                db.Modalidades.Add(modalidade); modalidades.Add(modalidade);
            }

            var cargaTexto = Campo(mc, "carga_horaria") ?? Extrair(summaryTexto, @"Carga hor[aá]ria:\s*(\d+)");
            var carga = int.TryParse(Regex.Match(cargaTexto ?? "", @"\d+").Value, out var h) ? h : 0;

            var publico = Campo(mc, "publico_alvo") ?? Extrair(summaryTexto, @"P[úu]blico[- ]alvo:\s*([^\n]+?)(?:\s{2,}|\n|$)") ?? "Público geral";

            var statusSlug = (mc.Visible ?? 1) == 0 ? "encerrado"
                : mc.StartDate > agora ? "em-breve"
                : mc.EndDate is > 0 && mc.EndDate < agora ? "encerrado"
                : "inscricoes-abertas";
            var st = status.First(s => s.Slug == statusSlug);

            var descricao = LimparCamposDaDescricao(summaryTexto);
            if (string.IsNullOrWhiteSpace(descricao)) descricao = "Curso importado do Moodle sem descrição cadastrada.";

            var curso = cursos.FirstOrDefault(c => c.MoodleId == mc.Id);

            // Vínculo: curso local com o mesmo nome breve -> só grava MoodleId/URL e a
            // categoria do Moodle; mantém descrição, módulos e demais dados do catálogo.
            if (curso is null)
            {
                var local = locais.FirstOrDefault(c => string.Equals(c.NomeBreve, mc.ShortName, StringComparison.OrdinalIgnoreCase));
                if (local is not null)
                {
                    local.MoodleId = mc.Id;
                    local.MoodleUrl = $"{publicBase}/course/view.php?id={mc.Id}";
                    if (categoria.MoodleId is null) categoria.MoodleId = mc.CategoryId;
                    local.AtualizadoEm = DateTime.UtcNow;
                    locais.Remove(local); cursos.Add(local); vinculados++;
                    continue;
                }
            }

            if (curso is null)
            {
                curso = new Curso { MoodleId = mc.Id, CriadoEm = DateTime.UtcNow };
                db.Cursos.Add(curso); cursos.Add(curso); criados++;
            }
            else atualizados++;

            curso.NomeCompleto = Trunc(mc.FullName, 200);
            curso.NomeBreve = Trunc(mc.ShortName, 60);
            curso.Descricao = descricao;
            curso.Categoria = categoria;
            curso.Modalidade = modalidade;
            curso.Status = st;
            curso.CargaHorariaHoras = carga;
            curso.PublicoAlvo = Trunc(publico.Trim(), 300);
            curso.ImagemUrl = string.IsNullOrWhiteSpace(mc.CourseImage) ? ImagemPadrao : Trunc(mc.CourseImage, 500);
            curso.DataDisponibilizacao = mc.StartDate is > 0
                ? DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(mc.StartDate.Value).UtcDateTime)
                : null;
            curso.MoodleUrl = $"{publicBase}/course/view.php?id={mc.Id}";
            curso.AtualizadoEm = DateTime.UtcNow;
        }

        // NomeBreve é único: evita colisão com cursos ainda sem vínculo.
        var nomesLocais = locais.Select(c => c.NomeBreve).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var c in cursos.Where(c => c.MoodleId != null && nomesLocais.Contains(c.NomeBreve) && locais.All(l => l.Id != c.Id)))
        {
            c.NomeBreve = Trunc($"{c.NomeBreve}-M{c.MoodleId}", 60);
            avisos.Add($"NomeBreve do curso Moodle {c.MoodleId} renomeado para '{c.NomeBreve}' (colisão).");
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Sync Moodle: {Cat} categorias, {Novos} novos, {Upd} atualizados, {Vinc} vinculados, {Ign} ignorados",
            categoriasCriadas, criados, atualizados, vinculados, ignorados);
        return new SyncResultado(categoriasCriadas, criados, atualizados, vinculados, ignorados, avisos);
    }

    // ------------------------------------------------------------- helpers
    private static string? Campo(MoodleClient.MoodleCourse c, string shortname) =>
        c.CustomFields?.FirstOrDefault(f => f.ShortName.Equals(shortname, StringComparison.OrdinalIgnoreCase))?.Value is { Length: > 0 } v
            ? StripHtml(v) : null;

    private static string? Extrair(string texto, string pattern)
    {
        var m = Regex.Match(texto, pattern, RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static string LimparCamposDaDescricao(string texto) =>
        Regex.Replace(texto, @"(Modalidade|Carga hor[aá]ria|P[úu]blico[- ]alvo):\s*[^\n]+", "", RegexOptions.IgnoreCase).Trim();

    private static string StripHtml(string? html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        var t = Regex.Replace(html, @"<br\s*/?>|</p>|</div>|</li>", "\n", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, "<[^>]+>", "");
        t = WebUtility.HtmlDecode(t);
        return Regex.Replace(t, @"[ \t]+", " ").Replace("\r", "").Trim();
    }

    private static string Trunc(string s, int max) => s.Length <= max ? s : s[..max];

    public static string Slugify(string texto)
    {
        var norm = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in norm)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        var s = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        s = Regex.Replace(s, @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(s) ? "sem-nome" : s;
    }
}
