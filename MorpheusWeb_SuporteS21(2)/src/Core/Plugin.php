<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Core;

class Plugin
{
    private $_plugins = array();
    private $_routes = array();
    private static $_instance = NULL;
    public function __construct()
    {
        $this->_plugins = config("plugins", array());
    }
    public static function getInstance()
    {
        if (static::$_instance === null) {
            static::$_instance = new self();
        }
        return static::$_instance;
    }
    public function getPlugins()
    {
        return $this->_plugins;
    }
    public function getRoutes()
    {
        return $this->_routes;
    }
    public function getPluginsInfo()
    {
        $plugins = array();
        $updates = static::checkUpdates();
        foreach (new \DirectoryIterator(PLUGIN_PATH) as $file) {
            if ($file->isDir() && !in_array($file->getFilename(), array(".", ".."))) {
                $temp = (require $file->getPathname() . DS . "info.php");
                $temp["active"] = in_array($temp["name"], (array) config("plugins", array()));
                $temp["update"] = isset($updates[$temp["name"]]) ? $updates[$temp["name"]] : false;
                $plugins[] = $temp;
            }
        }
        return $plugins;
    }
    public static function checkUpdates($force = false)
    {
        $key = "pnu";
        $cache = Cache::factory("Output", "File", array("lifetime" => 86400, "automatic_serialization" => true));
        $result = $cache->load($key);
        if ($force || $result === false) {
            $result = array();
            $update = new \VisualAppeal\AutoUpdate(TMP_PATH, ROOT . DS);
            foreach (new \DirectoryIterator(PLUGIN_PATH) as $file) {
                if ($file->isDir() && !in_array($file->getFilename(), array(".", ".."))) {
                    $info = (require $file->getPathname() . DS . "info.php");
                    $update->setCurrentVersion($info["version"]);
                    $update->setUpdateUrl(UPDATE_SERVER_URL . "/plugins/" . $info["name"] . "/");
                    $update->setUpdateFile("update.ini");
                    $update->checkUpdate();
                    if ($update->newVersionAvailable()) {
                        $result[$info["name"]] = $update->getLatestVersion()->getVersion();
                    }
                }
            }
            $cache->save($result, $key);
        }
        return $result;
    }
    public static function exists($plugin)
    {
        $instance = self::getInstance();
        $plugins = $instance->getPluginsInfo();
        foreach ($plugins as $p) {
            if ($p["name"] == $plugin) {
                return true;
            }
        }
        return false;
    }
    public static function getPluginInfo($plugin)
    {
        $data = array();
        $file = PLUGIN_PATH . $plugin . DS . "info.php";
        if (file_exists($file)) {
            $data = (require $file);
        }
        return $data;
    }
    public static function translates(\Zend_Translate $translate)
    {
        $instance = static::getInstance();
        foreach ($instance->getPlugins() as $plugin) {
            if (is_dir(PLUGIN_PATH . $plugin)) {
                try {
                    $translate->addTranslation(array("content" => PLUGIN_PATH . $plugin . DS . "languages" . DS));
                } catch (\Exception $ex) {
                }
            }
        }
    }
    public static function routes()
    {
        $instance = static::getInstance();
        $cache = array();
        foreach ($instance->getPlugins() as $plugin) {
            if (is_dir(PLUGIN_PATH . $plugin)) {
                if (file_exists(PLUGIN_PATH . $plugin . DS . "bootstrap.php")) {
                    require_once PLUGIN_PATH . $plugin . DS . "bootstrap.php";
                }
                $controllers = PLUGIN_PATH . $plugin . DS . "controllers" . DS . (in_admin() ? "admin" . DS : "") . "*.php";
                foreach (glob($controllers) as $controller) {
                    require_once $controller;
                }
                $instance->_routes[$plugin] = array();
                foreach (app()->router()->getRoutes() as $route) {
                    if (!in_array($route->getPattern(), $cache)) {
                        $cache[] = $route->getPattern();
                        $instance->_routes[$plugin][] = $route->getPattern();
                    }
                }
            }
        }
    }
    public static function isActive($plugin)
    {
        $instance = static::getInstance();
        $plugins = $instance->getPlugins();
        return in_array($plugin, $plugins);
    }
    public static function activate($plugin)
    {
        $instance = static::getInstance();
        $plugins = $instance->getPlugins();
        $plugins[] = $plugin;
        $info = self::getPluginInfo($plugin);
        if (!empty($info)) {
            if (isset($info["services"])) {
                foreach ($info["services"] as $service => $data) {
                    $s = \Morpheus\Account\Service::getService($service);
                    if ($s === null) {
                        \Morpheus\Account\Service::newService($data + array("service" => $service));
                    } else {
                        \Morpheus\Account\Service::activate($service);
                    }
                }
            }
            if (isset($info["depends"])) {
                foreach ($info["depends"] as $depend) {
                    if (!static::isActive($depend)) {
                        throw new \Exception(__("Plugin %s depends of the plugin %s be active", array($plugin, $depend)));
                    }
                }
            }
        }
        Config::save("plugins", $plugins);
    }
    public static function deactivate($plugin)
    {
        $instance = static::getInstance();
        $plugins = $instance->getPlugins();
        $plugins = array_diff($plugins, array($plugin));
        Config::save("plugins", $plugins);
        $info = self::getPluginInfo($plugin);
        if (!empty($info) && isset($info["services"])) {
            foreach ($info["services"] as $service => $data) {
                $s = \Morpheus\Account\Service::getService($service);
                if ($s !== null) {
                    \Morpheus\Account\Service::deactivate($service);
                }
            }
        }
    }
    public static function notify($name)
    {
        $args = func_get_args();
        array_shift($args);
        app()->applyHook($name, ...$args);
    }
    public static function autoload($loader)
    {
        $instance = static::getInstance();
        foreach ($instance->getPlugins() as $plugin) {
            $loader->addPsr4($plugin . "\\", PLUGIN_PATH . $plugin . DS . "src", true);
        }
    }
}

?>