using CatalogoCursos.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogoCursos.Api.Data;

/// <summary>
/// Popula o banco com dados FICTÍCIOS na primeira execução (idempotente:
/// só insere se a tabela de cursos estiver vazia).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(CatalogoDbContext db, CancellationToken ct = default)
    {
        if (!await db.Cursos.AnyAsync(ct)) await SeedCursosAsync(db, ct);
        if (!await db.Indicadores.AnyAsync(ct)) await SeedIndicadoresAsync(db, ct);
        if (!await db.Depoimentos.AnyAsync(ct)) await SeedDepoimentosAsync(db, ct);
    }

    private static async Task SeedIndicadoresAsync(CatalogoDbContext db, CancellationToken ct)
    {
        db.Indicadores.AddRange(
            new Indicador { Ordem = 1, Valor = "25.000+", Rotulo = "Profissionais capacitados" },
            new Indicador { Ordem = 2, Valor = "500+", Rotulo = "Empresas parceiras" },
            new Indicador { Ordem = 3, Valor = "4.8/5", Rotulo = "Avaliação média" },
            new Indicador { Ordem = 4, Valor = "2.5M+", Rotulo = "Horas de capacitação" });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedDepoimentosAsync(CatalogoDbContext db, CancellationToken ct)
    {
        // Depoimentos gerais (CursoId = null) aparecem na home e, como fallback, no detalhe de qualquer curso.
        var textos = new (string Nome, string Cargo, string Texto)[]
        {
            ("Lucas Pereira", "Analista na Empresa X", "A experiência do curso foi incrível! As aulas foram muito bem estruturadas e o cronograma foi cuidadosamente planejado, facilitando muito o aprendizado."),
            ("Mariana Costa", "Gestora na Empresa X", "Concluir o curso foi uma jornada enriquecedora. As aulas eram dinâmicas e o horário foi organizado para maximizar nosso tempo de estudo."),
            ("Thiago Almeida", "Técnico na Empresa X", "O curso superou minhas expectativas! As aulas eram bem elaboradas e o planejamento do horário ajudou a manter o foco e a produtividade."),
            ("Fernanda Silva", "Analista na Empresa X", "Finalizar o curso foi uma experiência motivadora. As aulas foram muito bem preparadas e o cronograma foi ideal para o aprendizado."),
            ("Gabriel Santos", "Analista na Empresa X", "A conclusão do curso foi uma etapa muito gratificante. As aulas eram bem organizadas e o horário foi pensado para facilitar nosso aprendizado."),
        };
        var ordem = 1;
        foreach (var t in textos)
            db.Depoimentos.Add(new Depoimento { Nome = t.Nome, Cargo = t.Cargo, Texto = t.Texto, Nota = 5, Ordem = ordem++ });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedCursosAsync(CatalogoDbContext db, CancellationToken ct)
    {

        var cat = new Dictionary<string, Categoria>
        {
            ["linguagens"] = new() { Nome = "Linguagens", Slug = "linguagens", Icone = "icon-linguagens-20px.svg" },
            ["matematica"] = new() { Nome = "Matemática", Slug = "matematica", Icone = "icon-matematica-20px.svg" },
            ["ciencias-natureza"] = new() { Nome = "Ciências da Natureza", Slug = "ciencias-natureza", Icone = "icon-ciencias-natureza-20px.svg" },
            ["sustentabilidade"] = new() { Nome = "Sustentabilidade", Slug = "sustentabilidade", Icone = "icon-sustentabilidade-20px.svg" },
        };
        var mod = new Dictionary<string, Modalidade>
        {
            ["ead"] = new() { Nome = "EAD", Slug = "ead" },
            ["presencial"] = new() { Nome = "Presencial", Slug = "presencial" },
            ["hibrido"] = new() { Nome = "Híbrido", Slug = "hibrido" },
        };
        var st = new Dictionary<string, StatusCurso>
        {
            ["abertas"] = new() { Nome = "Inscrições abertas", Slug = "inscricoes-abertas", Cor = "#1B7F3B" },
            ["andamento"] = new() { Nome = "Em andamento", Slug = "em-andamento", Cor = "#0C5BA5" },
            ["breve"] = new() { Nome = "Em breve", Slug = "em-breve", Cor = "#B86E00" },
            ["encerrado"] = new() { Nome = "Encerrado", Slug = "encerrado", Cor = "#6B6B6B" },
        };

        db.AddRange(cat.Values);
        db.AddRange(mod.Values);
        db.AddRange(st.Values);

        var cursos = new List<Curso>
        {
            new()
            {
                NomeCompleto = "Comunicação Técnica e Redação Industrial",
                NomeBreve = "COM-TEC",
                Descricao = "Desenvolva a escrita de relatórios, procedimentos operacionais e e-mails profissionais com clareza e objetividade. O curso trabalha estrutura textual, normas da ABNT aplicadas à indústria e revisão colaborativa.",
                Categoria = cat["linguagens"], Modalidade = mod["ead"], Status = st["abertas"],
                CargaHorariaHoras = 40, PublicoAlvo = "Técnicos, supervisores e analistas que produzem documentação técnica",
                ImagemUrl = "img/cursos/comunicacao-tecnica.svg", DataDisponibilizacao = new DateOnly(2026, 11, 3),
                Modulos =
                {
                    new() { Ordem = 1, Titulo = "Fundamentos da comunicação escrita", Descricao = "Coesão, coerência e adequação ao público.", CargaHorariaHoras = 8 },
                    new() { Ordem = 2, Titulo = "Relatórios técnicos", Descricao = "Estrutura, gráficos e linguagem objetiva.", CargaHorariaHoras = 12 },
                    new() { Ordem = 3, Titulo = "Procedimentos operacionais padrão", Descricao = "Redação de POPs e instruções de trabalho.", CargaHorariaHoras = 12 },
                    new() { Ordem = 4, Titulo = "Revisão e normalização", Descricao = "ABNT aplicada e revisão por pares.", CargaHorariaHoras = 8 },
                }
            },
            new()
            {
                NomeCompleto = "Inglês Instrumental para Manuais e Equipamentos",
                NomeBreve = "ING-INST",
                Descricao = "Leitura e interpretação de manuais, fichas técnicas e painéis de máquinas em inglês. Estratégias de leitura, vocabulário industrial e glossário colaborativo.",
                Categoria = cat["linguagens"], Modalidade = mod["hibrido"], Status = st["andamento"],
                CargaHorariaHoras = 60, PublicoAlvo = "Operadores e técnicos de manutenção",
                ImagemUrl = "img/cursos/ingles-instrumental.svg", DataDisponibilizacao = new DateOnly(2026, 9, 14),
                Modulos =
                {
                    new() { Ordem = 1, Titulo = "Estratégias de leitura", Descricao = "Skimming, scanning e cognatos.", CargaHorariaHoras = 15 },
                    new() { Ordem = 2, Titulo = "Vocabulário de máquinas e ferramentas", Descricao = "Termos recorrentes em manuais.", CargaHorariaHoras = 20 },
                    new() { Ordem = 3, Titulo = "Fichas técnicas e avisos de segurança", Descricao = "Leitura de safety labels e datasheets.", CargaHorariaHoras = 15 },
                    new() { Ordem = 4, Titulo = "Projeto final", Descricao = "Tradução comentada de um manual.", CargaHorariaHoras = 10 },
                }
            },
            new()
            {
                NomeCompleto = "Matemática Aplicada ao Chão de Fábrica",
                NomeBreve = "MAT-FAB",
                Descricao = "Proporção, porcentagem, unidades de medida, leitura de gráficos e noções de estatística para controle de qualidade. Exercícios baseados em situações reais de produção.",
                Categoria = cat["matematica"], Modalidade = mod["presencial"], Status = st["abertas"],
                CargaHorariaHoras = 32, PublicoAlvo = "Operadores de produção e auxiliares",
                ImagemUrl = "img/cursos/matematica-fabrica.svg", DataDisponibilizacao = new DateOnly(2026, 11, 10),
                Modulos =
                {
                    new() { Ordem = 1, Titulo = "Operações e unidades de medida", Descricao = "Conversões e instrumentos de medição.", CargaHorariaHoras = 8 },
                    new() { Ordem = 2, Titulo = "Razão, proporção e porcentagem", Descricao = "Rendimento, perdas e metas.", CargaHorariaHoras = 8 },
                    new() { Ordem = 3, Titulo = "Gráficos e indicadores", Descricao = "Leitura de cartas de controle.", CargaHorariaHoras = 8 },
                    new() { Ordem = 4, Titulo = "Estatística básica para qualidade", Descricao = "Média, desvio e amostragem.", CargaHorariaHoras = 8 },
                }
            },
            new()
            {
                NomeCompleto = "Análise de Dados com Planilhas para Indicadores de Produção",
                NomeBreve = "DADOS-PLAN",
                Descricao = "Construção de indicadores (OEE, refugo, produtividade) com planilhas eletrônicas: fórmulas, tabelas dinâmicas e dashboards. Inclui projeto aplicado com dados fictícios de uma linha de montagem.",
                Categoria = cat["matematica"], Modalidade = mod["ead"], Status = st["breve"],
                CargaHorariaHoras = 24, PublicoAlvo = "Analistas, assistentes de PCP e líderes de célula",
                ImagemUrl = "img/cursos/analise-dados.svg", DataDisponibilizacao = new DateOnly(2027, 2, 1),
                Modulos =
                {
                    new() { Ordem = 1, Titulo = "Fórmulas essenciais", Descricao = "Funções lógicas, de busca e de data.", CargaHorariaHoras = 8 },
                    new() { Ordem = 2, Titulo = "Tabelas dinâmicas", Descricao = "Consolidação de dados de produção.", CargaHorariaHoras = 8 },
                    new() { Ordem = 3, Titulo = "Dashboards de indicadores", Descricao = "Gráficos e painéis para a gestão.", CargaHorariaHoras = 8 },
                }
            },
            new()
            {
                NomeCompleto = "Química Básica e Manuseio Seguro de Produtos",
                NomeBreve = "QUI-SEG",
                Descricao = "Propriedades de substâncias, leitura de FISPQ/FDS, compatibilidade química e EPI adequado. Abordagem prática voltada à prevenção de acidentes em áreas industriais.",
                Categoria = cat["ciencias-natureza"], Modalidade = mod["presencial"], Status = st["encerrado"],
                CargaHorariaHoras = 16, PublicoAlvo = "Colaboradores que manipulam produtos químicos",
                ImagemUrl = "img/cursos/quimica-seguranca.svg", DataDisponibilizacao = new DateOnly(2026, 5, 5),
                Modulos =
                {
                    new() { Ordem = 1, Titulo = "Substâncias e misturas", Descricao = "Classificação e propriedades.", CargaHorariaHoras = 4 },
                    new() { Ordem = 2, Titulo = "FISPQ/FDS na prática", Descricao = "Leitura das 16 seções.", CargaHorariaHoras = 6 },
                    new() { Ordem = 3, Titulo = "Armazenamento e descarte", Descricao = "Compatibilidade e resíduos.", CargaHorariaHoras = 6 },
                }
            },
            new()
            {
                NomeCompleto = "Física dos Processos Industriais: Energia, Calor e Movimento",
                NomeBreve = "FIS-PROC",
                Descricao = "Conceitos de energia, transferência de calor, pressão e movimento aplicados a motores, caldeiras e sistemas pneumáticos. Experimentos virtuais e estudos de caso.",
                Categoria = cat["ciencias-natureza"], Modalidade = mod["ead"], Status = st["abertas"],
                CargaHorariaHoras = 36, PublicoAlvo = "Técnicos de manutenção e estudantes de cursos técnicos",
                ImagemUrl = "img/cursos/fisica-processos.svg", DataDisponibilizacao = new DateOnly(2026, 10, 20),
                Modulos =
                {
                    new() { Ordem = 1, Titulo = "Energia e trabalho", Descricao = "Formas de energia e eficiência.", CargaHorariaHoras = 12 },
                    new() { Ordem = 2, Titulo = "Calor e termodinâmica", Descricao = "Trocadores de calor e caldeiras.", CargaHorariaHoras = 12 },
                    new() { Ordem = 3, Titulo = "Pressão e fluidos", Descricao = "Sistemas hidráulicos e pneumáticos.", CargaHorariaHoras = 12 },
                }
            },
            new()
            {
                NomeCompleto = "Gestão de Resíduos e Economia Circular na Indústria",
                NomeBreve = "RES-CIRC",
                Descricao = "Classificação de resíduos (NBR 10004), logística reversa, redução na fonte e oportunidades de economia circular. Elaboração de um plano de gerenciamento de resíduos simplificado.",
                Categoria = cat["sustentabilidade"], Modalidade = mod["hibrido"], Status = st["breve"],
                CargaHorariaHoras = 20, PublicoAlvo = "Equipes de meio ambiente, produção e facilities",
                ImagemUrl = "img/cursos/residuos-circular.svg", DataDisponibilizacao = new DateOnly(2027, 1, 18),
                Modulos =
                {
                    new() { Ordem = 1, Titulo = "Classificação de resíduos", Descricao = "Perigosos, não inertes e inertes.", CargaHorariaHoras = 6 },
                    new() { Ordem = 2, Titulo = "Logística reversa", Descricao = "Obrigações legais e cadeias de retorno.", CargaHorariaHoras = 6 },
                    new() { Ordem = 3, Titulo = "Plano de gerenciamento", Descricao = "Construção do PGRS simplificado.", CargaHorariaHoras = 8 },
                }
            },
            new()
            {
                NomeCompleto = "Eficiência Energética e Uso Racional da Água",
                NomeBreve = "EFIC-ENERG",
                Descricao = "Diagnóstico de consumo, identificação de desperdícios, medidas de baixo custo e indicadores de desempenho ambiental. Casos de redução de consumo em plantas industriais.",
                Categoria = cat["sustentabilidade"], Modalidade = mod["ead"], Status = st["andamento"],
                CargaHorariaHoras = 12, PublicoAlvo = "Todos os colaboradores",
                ImagemUrl = "img/cursos/eficiencia-energetica.svg", DataDisponibilizacao = new DateOnly(2026, 8, 3),
                Modulos =
                {
                    new() { Ordem = 1, Titulo = "Diagnóstico de consumo", Descricao = "Leitura de contas e medições.", CargaHorariaHoras = 4 },
                    new() { Ordem = 2, Titulo = "Medidas de eficiência", Descricao = "Iluminação, motores e ar comprimido.", CargaHorariaHoras = 4 },
                    new() { Ordem = 3, Titulo = "Água: reúso e redução", Descricao = "Boas práticas e indicadores.", CargaHorariaHoras = 4 },
                }
            },
        };

        db.AddRange(cursos);
        await db.SaveChangesAsync(ct);
    }
}
