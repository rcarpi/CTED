<?php
/**
 * Diagnóstico: descobre QUAL diretório o Moodle não consegue criar.
 * Uso: docker compose exec moodle bash -c 'cp /opt/sesi/diag.php /tmp/ && su -s /bin/bash daemon -c "php /tmp/diag.php"'
 */
define('CLI_SCRIPT', true);
define('ABORT_AFTER_CONFIG', true);          // carrega config.php sem executar o setup
require('/bitnami/moodle/config.php');

echo "Usuário efetivo : " . (function_exists('posix_geteuid') ? posix_geteuid() . ' (' . (posix_getpwuid(posix_geteuid())['name'] ?? '?') . ')' : getmyuid()) . "\n";
echo "umask           : " . sprintf('%04o', umask()) . "\n";
echo "dirroot         : {$CFG->dirroot}\n";
echo "dataroot        : {$CFG->dataroot}\n";
foreach (['tempdir', 'cachedir', 'localcachedir', 'backuptempdir', 'themedir', 'langotherroot', 'langlocalroot'] as $k) {
    if (isset($CFG->$k)) echo str_pad($k, 16) . ": {$CFG->$k}\n";
}
echo "directoryperms  : " . (isset($CFG->directorypermissions) ? sprintf('%04o', $CFG->directorypermissions) : '(padrão 02777)') . "\n\n";

// Testa escrita direta nos diretórios que o setup.php cria/usa.
$alvos = [
    $CFG->dataroot,
    $CFG->dataroot . '/temp', $CFG->dataroot . '/cache', $CFG->dataroot . '/localcache',
    $CFG->dataroot . '/muc', $CFG->dataroot . '/sessions', $CFG->dataroot . '/filedir',
    $CFG->dataroot . '/trashdir', $CFG->dataroot . '/lang', $CFG->dataroot . '/temp/backup',
];
foreach (['tempdir', 'cachedir', 'localcachedir', 'backuptempdir'] as $k) if (isset($CFG->$k)) $alvos[] = $CFG->$k;
foreach (array_unique($alvos) as $d) {
    $exists = is_dir($d);
    $ok = false; $err = '';
    if ($exists) {
        $t = $d . '/__diag_' . getmypid();
        $ok = @mkdir($t, 02777);
        if ($ok) @rmdir($t); else $err = error_get_last()['message'] ?? '';
    } else {
        $ok = @mkdir($d, 02777, true);
        if ($ok) @rmdir($d); else $err = error_get_last()['message'] ?? '';
    }
    $st = $exists ? @stat($d) : null;
    printf("%-45s %s  %s%s\n", $d, $ok ? 'OK ' : 'FALHA',
        $st ? sprintf('[%s %o uid=%d gid=%d]', $exists ? 'dir' : '---', $st['mode'] & 07777, $st['uid'], $st['gid']) : '[não existe]',
        $err ? "  <- $err" : '');
}

// Agora deixa o Moodle terminar o setup COM debug ligado para expor o debuginfo.
echo "\n>> Executando setup.php com debug para capturar o caminho exato...\n";
$CFG->debug = E_ALL;
$CFG->debugdisplay = 1;
define('ABORT_AFTER_CONFIG_CANCEL', true);
try {
    require($CFG->dirroot . '/lib/setup.php');
    echo "setup.php concluído sem erro.\n";
} catch (Throwable $e) {
    echo "EXCEÇÃO: " . get_class($e) . ": " . $e->getMessage() . "\n";
    if (property_exists($e, 'debuginfo')) echo "debuginfo: " . $e->debuginfo . "\n";
}
