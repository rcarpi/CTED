<?php
// This file is part of Moodle - http://moodle.org/
//
// Moodle is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Moodle is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Moodle.  If not, see <http://www.gnu.org/licenses/>.

/**
 * Ponto de entrada do botão "Matricule-se" do catálogo.
 *
 *   /theme/sesi/matricular.php?course=<id do curso no Moodle>
 *
 * - Com `course`: abre o formulário de inscrição do curso (enrol/index.php).
 *   Visitante não logado é levado ao login/cadastro pelo Moodle e volta ao formulário.
 * - Sem `course` (curso só no catálogo): cadastro e, depois, o Painel.
 *
 * @package   theme_sesi
 * @license   http://www.gnu.org/copyleft/gpl.html GNU GPL v3 or later
 */

require(__DIR__ . '/../../config.php');
require_once($CFG->libdir . '/authlib.php');

$courseid = optional_param('course', 0, PARAM_INT);

// Com curso: formulário de inscrição do curso. Se o visitante não estiver logado,
// o próprio enrol/index.php exige login (tela com "Criar uma conta") e volta
// para o formulário depois.
if ($courseid && $DB->record_exists('course', ['id' => $courseid, 'visible' => 1])) {
    redirect(new moodle_url('/enrol/index.php', ['id' => $courseid]));
}

// Sem curso (curso só no catálogo): cadastro e depois o Painel.
if (isloggedin() && !isguestuser()) {
    redirect(new moodle_url('/my/'));
}
$SESSION->wantsurl = (new moodle_url('/my/'))->out(false);
redirect(signup_is_enabled() ? new moodle_url('/login/signup.php') : new moodle_url('/login/index.php'));
