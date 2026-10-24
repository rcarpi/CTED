<?php
/**
 * Cria categorias e cursos fictícios no Moodle local para demonstrar a
 * sincronização Moodle -> API C#.
 *
 * Uso (como usuário `daemon`, para não criar arquivos de cache como root):
 *   docker compose exec moodle bash /opt/sesi/seed.sh
 */
define('CLI_SCRIPT', true);
require('/bitnami/moodle/config.php');
require_once($CFG->dirroot . '/course/lib.php');
require_once($CFG->libdir . '/enrollib.php');
// core_course_category é autoloaded (Moodle >= 3.6).

/**
 * Cria/ativa o método de inscrição "self" (autoinscrição) no curso, sem chave,
 * para que o fluxo Matricule-se -> cadastro -> inscrição funcione.
 */
function habilitar_autoinscricao(stdClass $course): void {
    global $DB;
    $plugin = enrol_get_plugin('self');
    if (!$plugin) { echo "   plugin enrol_self indisponível\n"; return; }
    $instance = $DB->get_record('enrol', ['courseid' => $course->id, 'enrol' => 'self']);
    if (!$instance) {
        $id = $plugin->add_default_instance($course) ?: $plugin->add_instance($course);
        $instance = $DB->get_record('enrol', ['id' => $id]);
    }
    if ($instance && $instance->status != ENROL_INSTANCE_ENABLED) {
        $plugin->update_status($instance, ENROL_INSTANCE_ENABLED);
    }
    if ($instance && !empty($instance->password)) {
        $DB->set_field('enrol', 'password', '', ['id' => $instance->id]);
    }
    echo "   autoinscrição ativa em {$course->shortname}\n";
}

$categorias = [
    'Linguagens'           => 'linguagens',
    'Matemática'           => 'matematica',
    'Ciências da Natureza' => 'ciencias-natureza',
    'Sustentabilidade'     => 'sustentabilidade',
];

$catIds = [];
foreach ($categorias as $nome => $idnumber) {
    $existing = $DB->get_record('course_categories', ['idnumber' => $idnumber]);
    if ($existing) {
        $catIds[$nome] = $existing->id;
        echo "Categoria existente: {$nome}\n";
        continue;
    }
    $cat = core_course_category::create((object)['name' => $nome, 'idnumber' => $idnumber]);
    $catIds[$nome] = $cat->id;
    echo "Categoria criada: {$nome}\n";
}

// Mesmos cursos do seed da API (Data/DbSeeder.cs): nome breve igual => o sync
// vincula o curso do catálogo ao curso do Moodle em vez de duplicar.
$cursos = [
    ['Comunicação Técnica e Redação Industrial', 'COM-TEC', 'Linguagens', 'Desenvolva a escrita de relatórios, procedimentos operacionais e e-mails profissionais com clareza e objetividade.', 'EAD', 40, 'Técnicos, supervisores e analistas'],
    ['Inglês Instrumental para Manuais e Equipamentos', 'ING-INST', 'Linguagens', 'Leitura e interpretação de manuais, fichas técnicas e painéis de máquinas em inglês.', 'Híbrido', 60, 'Operadores e técnicos de manutenção'],
    ['Matemática Aplicada ao Chão de Fábrica', 'MAT-FAB', 'Matemática', 'Proporção, porcentagem, unidades de medida, gráficos e estatística para controle de qualidade.', 'Presencial', 32, 'Operadores de produção e auxiliares'],
    ['Análise de Dados com Planilhas para Indicadores de Produção', 'DADOS-PLAN', 'Matemática', 'Indicadores (OEE, refugo, produtividade) com planilhas: fórmulas, tabelas dinâmicas e dashboards.', 'EAD', 24, 'Analistas, assistentes de PCP e líderes de célula'],
    ['Química Básica e Manuseio Seguro de Produtos', 'QUI-SEG', 'Ciências da Natureza', 'Propriedades de substâncias, FISPQ/FDS, compatibilidade química e EPI adequado.', 'Presencial', 16, 'Colaboradores que manipulam produtos químicos'],
    ['Física dos Processos Industriais: Energia, Calor e Movimento', 'FIS-PROC', 'Ciências da Natureza', 'Energia, transferência de calor, pressão e movimento aplicados a motores, caldeiras e sistemas pneumáticos.', 'EAD', 36, 'Técnicos de manutenção e estudantes'],
    ['Gestão de Resíduos e Economia Circular na Indústria', 'RES-CIRC', 'Sustentabilidade', 'Classificação de resíduos, logística reversa, redução na fonte e economia circular.', 'Híbrido', 20, 'Equipes de meio ambiente, produção e facilities'],
    ['Eficiência Energética e Uso Racional da Água', 'EFIC-ENERG', 'Sustentabilidade', 'Diagnóstico de consumo, desperdícios, medidas de baixo custo e indicadores ambientais.', 'EAD', 12, 'Todos os colaboradores'],
];

foreach ($cursos as [$fullname, $shortname, $catNome, $summary, $modalidade, $carga, $publico]) {
    if ($DB->record_exists('course', ['shortname' => $shortname])) {
        echo "Curso existente: {$shortname}\n";
        continue;
    }
    $data = (object)[
        'fullname'  => $fullname,
        'shortname' => $shortname,
        'category'  => $catIds[$catNome],
        'summary'   => "<p>{$summary}</p><p><strong>Modalidade:</strong> {$modalidade}<br>"
                     . "<strong>Carga horária:</strong> {$carga}h<br>"
                     . "<strong>Público-alvo:</strong> {$publico}</p>",
        'summaryformat' => FORMAT_HTML,
        'format'    => 'topics',
        'visible'   => 1,
        'startdate' => time(),
    ];
    $course = create_course($data);
    echo "Curso criado: {$fullname}\n";
    habilitar_autoinscricao($course);
}

// Garante autoinscrição também em cursos já existentes.
foreach ($DB->get_records_select('course', 'id <> ?', [SITEID]) as $c) {
    habilitar_autoinscricao($c);
}
echo "Concluído.\n";
