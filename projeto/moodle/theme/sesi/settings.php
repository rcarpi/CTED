<?php
defined('MOODLE_INTERNAL') || die();

if ($ADMIN->fulltree) {
    $settings = new theme_boost_admin_settingspage_tabs('themesettingsesi', get_string('configtitle', 'theme_sesi'));
    $page = new admin_settingpage('theme_sesi_general', get_string('generalsettings', 'theme_boost'));

    $page->add(new admin_setting_configtext('theme_sesi/catalogurl',
        get_string('catalogurl', 'theme_sesi'), get_string('catalogurl_desc', 'theme_sesi'),
        'http://localhost:3000', PARAM_URL));

    $page->add(new admin_setting_configcheckbox('theme_sesi/redirectfrontpage',
        get_string('redirectfrontpage', 'theme_sesi'), get_string('redirectfrontpage_desc', 'theme_sesi'), 1));

    $page->add(new admin_setting_configcheckbox('theme_sesi/hidenavbarstudents',
        get_string('hidenavbarstudents', 'theme_sesi'), get_string('hidenavbarstudents_desc', 'theme_sesi'), 1));

    $settings->add($page);
}
