<?php
defined('MOODLE_INTERNAL') || die();

/**
 * SCSS principal: variáveis SESI (pre) + preset padrão do Boost + ajustes (post).
 */
function theme_sesi_get_main_scss_content($theme) {
    global $CFG;
    $pre    = file_get_contents($CFG->dirroot . '/theme/sesi/scss/pre.scss');
    $preset = file_get_contents($CFG->dirroot . '/theme/boost/scss/preset/default.scss');
    $post   = file_get_contents($CFG->dirroot . '/theme/sesi/scss/post.scss');
    return $pre . "\n" . $preset . "\n" . $post;
}

/** URL do catálogo (front-end). */
function theme_sesi_catalog_url(): string {
    $url = trim((string)get_config('theme_sesi', 'catalogurl'));
    return $url !== '' ? $url : 'http://localhost:3000';
}

/**
 * Redirecionar a frontpage para o catálogo? (padrão: sim).
 * Nunca redireciona quando a URL do catálogo aponta para o próprio Moodle
 * (ex.: FRONTEND_PORT igual a MOODLE_PORT), o que causaria um loop.
 */
function theme_sesi_redirect_frontpage_enabled(): bool {
    global $CFG;
    $v = get_config('theme_sesi', 'redirectfrontpage');
    $ativo = ($v === false || $v === null) ? true : (bool)$v;
    if (!$ativo) {
        return false;
    }
    $catalogo = parse_url(theme_sesi_catalog_url());
    $wwwroot  = parse_url($CFG->wwwroot);
    $mesmohost = strcasecmp($catalogo['host'] ?? '', $wwwroot['host'] ?? '') === 0
        && ($catalogo['port'] ?? 80) == ($wwwroot['port'] ?? 80);
    if ($mesmohost && trim($catalogo['path'] ?? '/', '/') === trim($wwwroot['path'] ?? '/', '/')) {
        debugging('theme_sesi: catalogurl aponta para o próprio Moodle; redirect da frontpage ignorado.', DEBUG_DEVELOPER);
        return false;
    }
    return true;
}

/** Preferências herdadas do Boost (estado dos drawers). */
function theme_sesi_user_preferences(): array {
    return [
        'drawer-open-block' => ['type' => PARAM_BOOL, 'null' => NULL_NOT_ALLOWED, 'default' => false,
            'permissioncallback' => [core_user::class, 'is_current_user']],
        'drawer-open-index' => ['type' => PARAM_BOOL, 'null' => NULL_NOT_ALLOWED, 'default' => true,
            'permissioncallback' => [core_user::class, 'is_current_user']],
    ];
}

/**
 * Contexto extra para a página de curso (ref.: doc/figma/3.png).
 * Retorna null fora de páginas de curso.
 */
function theme_sesi_course_context(moodle_page $page, renderer_base $output): ?array {
    global $COURSE, $USER, $CFG;

    if (!in_array($page->pagelayout, ['course', 'incourse'], true) || empty($COURSE->id) || $COURSE->id == SITEID) {
        return null;
    }

    $context = context_course::instance($COURSE->id);
    $canedit = has_capability('moodle/course:update', $context);

    $progress = null;
    if (class_exists('\core_completion\progress')) {
        $progress = \core_completion\progress::get_course_progress_percentage($COURSE, $USER->id);
    }

    $banner = null;
    if (class_exists('\core_course\external\course_summary_exporter')) {
        $banner = \core_course\external\course_summary_exporter::get_course_image($COURSE);
    }
    if (empty($banner)) {
        $banner = $output->image_url('banner', 'theme_sesi')->out(false);
    }

    $catalogurl = theme_sesi_catalog_url();
    $hidenavbar = get_config('theme_sesi', 'hidenavbarstudents');
    $hidenavbar = ($hidenavbar === false || $hidenavbar === null) ? true : (bool)$hidenavbar;

    return [
        'coursename'  => format_string($COURSE->fullname, true, ['context' => $context]),
        'progress'    => $progress === null ? 0 : (int)round($progress),
        'hasprogress' => $progress !== null,
        'bannerurl'   => $banner,
        'logourl'     => $output->image_url('logo-sesi', 'theme_sesi')->out(false),
        'backurl'     => $catalogurl,
        'username'    => fullname($USER),
        'logouturl'   => (new moodle_url('/login/logout.php', ['sesskey' => sesskey()]))->out(false),
        'aluno'       => !$canedit && $hidenavbar,   // esconde navbar e usa layout do protótipo
    ];
}
