<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

require "constants.php";
$loader = (require ROOT . DS . "vendor" . DS . "autoload.php");
require ROOT . DS . "src" . DS . "basics.php";
Morpheus\Core\Config::addFiles(array("database.php"));
date_default_timezone_set(config("timezone", "America/Sao_Paulo"));
$conn = Morpheus\Database\DriverManager::addConnection("default", config("conn.default"));
$conn->connect();
Morpheus\Core\Config::load();
$locale = config("locale", "pt_BR");
$translate = new Zend_Translate(array("locale" => $locale, "adapter" => "Zend_Translate_Adapter_Array", "content" => LANGUAGE_PATH, "scan" => Zend_Translate::LOCALE_DIRECTORY, "disableNotices" => true));
registry("translate", $translate);
$scheduler = new GO\Scheduler();
$scheduler->call(function () {
    Morpheus\Core\Plugin::checkUpdates(true);
})->daily(23, 59);
$jobs = $conn->fetchAll("select * from mw_jobs where active = 1");
foreach ($jobs as $job) {
    $scheduler->call(function () use($job, $conn) {
        $script = $job["script_file"];
        try {
            if (strpos($script, ".") !== false) {
                $parts = explode(".", $script);
                $plugin = $parts[0];
                $active = plugin_active($plugin);
                if ($active) {
                    $script = join(".", array_slice($parts, 1));
                    include PLUGIN_PATH . $plugin . DS . "tasks" . DS . $script . ".php";
                }
            } else {
                include ROOT . DS . "tasks" . DS . $script . ".php";
            }
            $conn->update("mw_jobs", array("last_execution" => (new DateTime())->format(DATETIME_ISO_FORMAT), "result" => "OK"), array("id" => $job["id"]));
            return true;
        } catch (Exception $ex) {
            $conn->update("mw_jobs", array("last_execution" => (new DateTime())->format(DATETIME_ISO_FORMAT), "result" => $ex->getMessage()), array("id" => $job["id"]));
            return false;
        }
    })->at($job["minutes"] . " " . $job["hours"] . " " . $job["days"] . " " . $job["months"] . " " . $job["weeks"])->output(LOGS_PATH . DS . "scheduler.log");
}
$scheduler->run();

?>