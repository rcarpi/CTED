<?php
defined('MOODLE_INTERNAL') || die();

require_once(__DIR__ . '/lib.php');

$THEME->name = 'sesi';
$THEME->parents = ['boost'];
$THEME->sheets = [];
$THEME->editor_sheets = [];
$THEME->editor_scss = ['editor'];
$THEME->usefallback = true;
$THEME->scss = function($theme) {
    return theme_sesi_get_main_scss_content($theme);
};

// Mesmo mapa de layouts do Boost 4.4; só `drawers.php` é sobrescrito neste tema
// (os demais arquivos são resolvidos no tema pai).
$THEME->layouts = [
    'base'           => ['file' => 'drawers.php', 'regions' => []],
    'standard'       => ['file' => 'drawers.php', 'regions' => ['side-pre'], 'defaultregion' => 'side-pre'],
    'course'         => ['file' => 'drawers.php', 'regions' => ['side-pre'], 'defaultregion' => 'side-pre', 'options' => ['langmenu' => true]],
    'coursecategory' => ['file' => 'drawers.php', 'regions' => ['side-pre'], 'defaultregion' => 'side-pre'],
    'incourse'       => ['file' => 'drawers.php', 'regions' => ['side-pre'], 'defaultregion' => 'side-pre'],
    'frontpage'      => ['file' => 'drawers.php', 'regions' => ['side-pre'], 'defaultregion' => 'side-pre', 'options' => ['nonavbar' => true]],
    'admin'          => ['file' => 'drawers.php', 'regions' => ['side-pre'], 'defaultregion' => 'side-pre'],
    'mycourses'      => ['file' => 'drawers.php', 'regions' => ['side-pre'], 'defaultregion' => 'side-pre', 'options' => ['nonavbar' => true]],
    'mydashboard'    => ['file' => 'drawers.php', 'regions' => ['side-pre'], 'defaultregion' => 'side-pre', 'options' => ['nonavbar' => true, 'langmenu' => true]],
    'mypublic'       => ['file' => 'drawers.php', 'regions' => ['side-pre'], 'defaultregion' => 'side-pre'],
    'login'          => ['file' => 'login.php', 'regions' => [], 'options' => ['langmenu' => true]],
    'popup'          => ['file' => 'columns1.php', 'regions' => [], 'options' => ['nofooter' => true, 'nonavbar' => true,
                         'activityheader' => ['notitle' => true, 'nocompletion' => true, 'nodescription' => true]]],
    'frametop'       => ['file' => 'columns1.php', 'regions' => [], 'options' => ['nofooter' => true, 'nocoursefooter' => true,
                         'activityheader' => ['nocompletion' => true]]],
    'embedded'       => ['file' => 'embedded.php', 'regions' => ['side-pre'], 'defaultregion' => 'side-pre'],
    'maintenance'    => ['file' => 'maintenance.php', 'regions' => []],
    'print'          => ['file' => 'columns1.php', 'regions' => [], 'options' => ['nofooter' => true, 'nonavbar' => false, 'noactivityheader' => true]],
    'redirect'       => ['file' => 'embedded.php', 'regions' => []],
    'report'         => ['file' => 'drawers.php', 'regions' => ['side-pre'], 'defaultregion' => 'side-pre'],
    'secure'         => ['file' => 'secure.php', 'regions' => ['side-pre'], 'defaultregion' => 'side-pre',
                         'options' => ['activityheader' => ['notitle' => false]]],
];

$THEME->enable_dock = false;
$THEME->yuicssmodules = [];
$THEME->rendererfactory = 'theme_overridden_renderer_factory';
$THEME->requiredblocks = '';
$THEME->addblockposition = BLOCK_ADDBLOCK_POSITION_FLATNAV;
$THEME->iconsystem = \core\output\icon_system::FONTAWESOME;
$THEME->haseditswitch = true;
$THEME->usescourseindex = true;
$THEME->activityheaderconfig = ['notitle' => true];
