<?php
// Tema SESI Indústria — filho do Boost. Aplica o protótipo Figma (curso interno)
// à página de curso do Moodle: sidebar escura com progresso e índice, banner e
// identidade visual SESI.
defined('MOODLE_INTERNAL') || die();

$plugin->component = 'theme_sesi';
$plugin->version   = 2026102400;
$plugin->requires  = 2024042200;          // Moodle 4.4
$plugin->release   = '1.0.0';
$plugin->maturity  = MATURITY_STABLE;
$plugin->dependencies = ['theme_boost' => 2024042200];
