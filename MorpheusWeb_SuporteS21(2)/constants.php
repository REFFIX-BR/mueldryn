<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

define("MORPHEUS_VERSION", "3.3.0");
define("MORPHEUS_WEBSITE", "http://morpheusmuweb.com/");
define("UPDATE_SERVER_URL", "http://morpheusmuweb.com/update/v3");
define("LICENSE_SERVER_URL", "http://morpheusmuweb.com/");
define("DS", DIRECTORY_SEPARATOR);
define("ROOT", __DIR__);
define("CONFIGS_PATH", ROOT . DS . "configs" . DS);
define("TEMPLATE_PATH", ROOT . DS . "templates" . DS);
define("PLUGIN_PATH", ROOT . DS . "plugins" . DS);
define("CONTROLLER_PATH", ROOT . DS . "controllers" . DS);
define("LANGUAGE_PATH", ROOT . DS . "languages" . DS);
define("TMP_PATH", ROOT . DS . "tmp" . DS);
define("LOGS_PATH", ROOT . DS . "logs" . DS);
define("CACHE_PATH", TMP_PATH . "cache" . DS);
define("DATETIME_ISO_FORMAT", "Y-m-d H:i:s");

?>