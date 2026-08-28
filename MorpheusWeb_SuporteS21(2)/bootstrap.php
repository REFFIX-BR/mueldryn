<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

$loader = (require ROOT . DS . "vendor" . DS . "autoload.php");
require ROOT . DS . "src" . DS . "basics.php";

if (!function_exists('randomw')) {
    function randomw($weights) {
        return array_rand($weights);
    }
}
Zend_Locale_Format::setOptions(array("disableCache" => true));
Morpheus\Core\Config::addFiles(array("app.php", "characters.php", "maps.php", "items.php", "database.php", "openmu.php"));
date_default_timezone_set(config("timezone", "America/Sao_Paulo"));
ini_set("error_log", LOGS_PATH . "/php/php-" . date("Y-m") . ".log");
error_reporting(E_ALL & ~E_NOTICE & ~E_WARNING & ~E_DEPRECATED & ~E_STRICT);
$debugstack = new Doctrine\DBAL\Logging\DebugStack();
foreach (config("conn", array()) as $name => $conf) {
    if (!isset($conf["active"]) || $conf["active"]) {
        try {
            $conn = Morpheus\Database\DriverManager::addConnection($name, $conf);
            $conn->getConfiguration()->setSQLLogger($debugstack);
            $conn->connect();
        } catch (Exception $e) {
            error_log("Database connection error [{$name}]: " . $e->getMessage());
            if (config("debug", false)) {
                throw $e;
            }
            die("Erro de conexÃ£o com o banco de dados. Por favor, tente novamente mais tarde.");
        }
    }
}
Morpheus\Core\Config::load();
$account = account("");
if (config("debug", false)) {
    $debugbar = new DebugBar\StandardDebugBar();
    $debugbar->addCollector(new DebugBar\Bridge\DoctrineCollector($debugstack));
    $debugbar->addCollector(new DebugBar\DataCollector\ConfigCollector(Morpheus\Core\Config::all()));
    registry("debugbar", $debugbar);
}
$locale = config("locale", "pt_BR");
$translate = new Zend_Translate(array("locale" => $locale, "adapter" => "Zend_Translate_Adapter_Array", "content" => LANGUAGE_PATH, "scan" => Zend_Translate::LOCALE_DIRECTORY, "disableNotices" => true));
Morpheus\Core\Plugin::autoload($loader);
Morpheus\Core\Plugin::translates($translate);
registry("translate", $translate);
$app = new Morpheus\Slim\Application(array("debug" => (bool) config("debug", false), "view" => new Morpheus\Slim\View(), "templates.path" => TEMPLATE_PATH . config("template", "default") . DS . "views"));

$app->notFound(function () use($app) {
    if (in_admin()) {
        if (admin_logged_in()) {
            $app->view()->display("404");
        } else {
            $app->redirect("/admin/login");
        }
    } else {
        $app->languageConfig();
        $app->view()->display("404");
    }
});
if (is_ajax()) {
    $app->error(function ($exception) use($app) {
        $app->response->status(500);
        $app->response->body("");
        $app->response->write(json_encode(array("error" => array("file" => $exception->getFile(), "line" => $exception->getLine(), "message" => $exception->getMessage(), "type" => "Exception"))));
        $app->stop();
    });
}
$app->hook("slim.before.dispatch", function () use($app) {
    $slimErrorHandler = set_error_handler(function($errno, $errstr, $errfile, $errline) use (&$slimErrorHandler) {
        if ($errno === E_RECOVERABLE_ERROR && strpos($errstr, '__toString()') !== false) {
            return true;
        }
        if (is_callable($slimErrorHandler)) {
            return call_user_func($slimErrorHandler, $errno, $errstr, $errfile, $errline);
        }
        return false;
    });
    if (is_dir(ROOT . DS . "install" . DS)) {
        app()->redirect("/install/index.php");
    }
    $license = new Morpheus\Core\License();
    if (!method_exists($license, "xhyt")) {
        return app()->redirect(MORPHEUS_WEBSITE . "license-illegal");
    }
    $license->validate();
    if (!config("fix_template", false) && !in_admin()) {
        $template = $app->request()->get("template");
        if (!$template) {
            $template = $app->session->get("template", config("template", "default"));
        } else {
            $app->session->set("template", $template);
        }
        $app->view()->setTemplatesDirectory(TEMPLATE_PATH . $template . DS . "views");
    }
});
$app->hook("slim.after", function () use($app) {
    if (config("debug", false) && !in_admin() && !is_ajax()) {
        echo registry("debugbar")->getJavascriptRenderer(base() . "/vendor/maximebf/debugbar/src/DebugBar/Resources")->render();
    }
});
require ROOT . DS . "dependencies.php";
if (in_admin()) {
    require ROOT . DS . "admin.php";
    $app->config(array("templates.path" => TEMPLATE_PATH . "admin" . DS . "views"));
    $app->group("/admin", function () use($app) {
        Morpheus\Core\Plugin::routes();
        foreach (glob(CONTROLLER_PATH . "admin" . DS . "*.php") as $controller) {
            require $controller;
        }
    });
} else {
    $app->hook("slim.before.dispatch", function () use($app) {
        if (config("maintenance", false)) {
            $uri = $app->request()->getResourceUri();
            if ($uri !== "/maintenance" && !admin_logged_in()) {
                return $app->redirect("/maintenance");
            }
        }
    });
    Morpheus\Core\Plugin::routes();
    foreach (glob(CONTROLLER_PATH . "*.php") as $controller) {
        require $controller;
    }
}
try {
    $translate->addTranslation(array("content" => $app->config("templates.path") . DS . ".." . DS . "languages" . DS));
} catch (Exception $ex) {
}
$app->run();

?>