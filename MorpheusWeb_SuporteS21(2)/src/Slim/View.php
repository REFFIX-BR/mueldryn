<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Slim;

class View extends \Slim\View
{
    private $_defaultDirectory = NULL;
    const DEFAULT_LAYOUT = "default";
    const LAYOUT_KEY = "layout";
    const YIELD_KEY = "content";
    public function remove($key)
    {
        $this->data->remove($key);
        return $this;
    }
    public function add($key, $value = NULL)
    {
        $data = $this->data->get($key);
        if ($data == null) {
            $data = array();
        }
        if (!is_array($value)) {
            $value = array($value);
        }
        $this->data->set($key, array_merge($data, $value));
        return $this;
    }
    public function assign($type)
    {
        $out = "";
        $data = $this->get($type);
        $data = $data === null ? array() : $data;
        switch ($type) {
            case "script":
                if (config("debug", false) && !in_admin()) {
                    $out .= registry("debugbar")->getJavascriptRenderer(base() . "/vendor/maximebf/debugbar/src/DebugBar/Resources")->renderHead();
                }
                foreach ($data as $script) {
                    $file = base() . "/templates/" . template() . "/assets/js/" . $script . ".js";
                    if (strpos($script, ".") !== false) {
                        $parts = explode(".", $script);
                        $name = join(".", array_slice($parts, 1));
                        $exists = \Morpheus\Core\Plugin::exists($parts[0]);
                        if ($exists) {
                            $file = base() . "/plugins/" . $parts[0] . "/assets/js/" . $name . ".js";
                        }
                    }
                    $out .= "<script type=\"text/javascript\" src=\"" . $file . "\"></script>";
                }
                break;
            case "css":
                foreach ($data as $style) {
                    $file = base() . "/templates/" . template() . "/assets/css/" . $style . ".css";
                    if (strpos($style, ".") !== false) {
                        $parts = explode(".", $style);
                        $name = join(".", array_slice($parts, 1));
                        $exists = \Morpheus\Core\Plugin::exists($parts[0]);
                        if ($exists) {
                            $file = base() . "/plugins/" . $parts[0] . "/assets/css/" . $name . ".css";
                        }
                    }
                    $out .= "<link rel=\"stylesheet\" href=\"" . $file . "\" />";
                }
                break;
            default:
                $out = join("", $data);
        }
        return $out;
    }
    public function partial($partial, $data = array())
    {
        $plugin = false;
        if (strpos($partial, ".") !== false) {
            list($plugin, $partial) = explode(".", $partial);
        }
        foreach ($data as $k => $v) {
            $this->data->set($k, $v);
        }
        extract($this->getData());
        if ($plugin) {
            $file = $this->getTemplatesDirectory() . "partials" . DS . $partial . ".phtml";
            if (file_exists($file)) {
                require $file;
            } else {
                require PLUGIN_PATH . DS . $plugin . DS . "views" . DS . "partials" . DS . $partial . ".phtml";
            }
        } else {
            require ($this->_defaultDirectory ? $this->_defaultDirectory : $this->getTemplatesDirectory()) . DS . "partials" . DS . $partial . ".phtml";
        }
    }
    public function render($template, $data = NULL)
    {
        if (!is_dir($this->templatesDirectory)) {
            throw new \Morpheus\Core\Exception("Template \"" . template() . "\" doesn't exists.");
        }
        return parent::render($template, $data);
    }
    public function fetch($template, $data = NULL)
    {
        $app = Application::getInstance();
        if ($this->_defaultDirectory === null) {
            $this->_defaultDirectory = $this->templatesDirectory;
        }
        $layout = $this->getLayout($data);
        if (strpos($template, ".") !== false) {
            $parts = explode(".", $template);
            $plugin = $parts[0];
            $active = \Morpheus\Core\Plugin::isActive($plugin);
            if ($active) {
                $template = join(".", array_slice($parts, 1));
                $file = $this->_defaultDirectory . DS . "plugins" . DS . $plugin . DS . $template . ".phtml";
                if (file_exists($file)) {
                    $this->templatesDirectory = $this->_defaultDirectory . DS . "plugins" . DS . $plugin . DS;
                } else {
                    $this->templatesDirectory = PLUGIN_PATH . $plugin . DS . "views" . DS . (in_admin() ? "admin" . DS : "");
                }
            }
        } else {
            $this->templatesDirectory = $this->_defaultDirectory;
        }
        $result = $this->render($template . ".phtml", $data);
        if (!$app->request()->isAjax() && is_string($layout)) {
            $this->templatesDirectory = $this->_defaultDirectory;
            $result = $this->renderLayout($layout, $result, $data);
        }
        return $result;
    }
    public function service($template, $name, $data = array())
    {
        $account = user();
        if ($account !== null) {
            $avaliables = \Morpheus\Util\Hash::nest($account->getAvaliableServices());
            $services = array();
            $service = \Morpheus\Account\Service::getService($name);
            if (empty($service["parent_id"])) {
                $root = $service;
            } else {
                $root = \Morpheus\Account\Service::getServiceById($service["parent_id"]);
            }
            foreach ($avaliables as $avaliable) {
                if ($avaliable["service"] === $root["service"]) {
                    $services = array();
                    foreach ($avaliable["children"] as $serv) {
                        $serv["current"] = $service["service"] === $serv["service"];
                        $services[] = $serv;
                    }
                }
            }
            $characters = array();
            if ($root["service"] === "character") {
                $characters = $account->getCharacters();
            }
            return View::display("panel/" . ($root["service"] === "character" ? "character" : "view"), array_merge(array("service" => empty($template) ? "" : View::fetch($template, array_merge(array("layout" => false, "service" => $service), $data)), "account" => user(), "characters" => $characters, "root" => $root, "title" => $root["name"], "services" => $services, "currentService" => $service), $data));
        }
    }
    public function getLayout($data = NULL)
    {
        $layout = null;
        if (is_array($data) && array_key_exists(self::LAYOUT_KEY, $data)) {
            $layout = $data[self::LAYOUT_KEY];
            unset($data[self::LAYOUT_KEY]);
        }
        if ($this->has(self::LAYOUT_KEY)) {
            $layout = $this->get(self::LAYOUT_KEY);
            $this->remove(self::LAYOUT_KEY);
        }
        if (is_null($layout)) {
            $app = Application::getInstance();
            $layout = $app->config(self::LAYOUT_KEY);
        }
        if (is_null($layout)) {
            $layout = self::DEFAULT_LAYOUT;
        }
        if ($layout == null) {
            return $layout;
        }
        return "layouts/" . $layout . ".phtml";
    }
    protected function renderLayout($layout, $yield, $data = NULL)
    {
        if (!is_array($data)) {
            $data = array();
        }
        $data[self::YIELD_KEY] = $yield;
        $currentTemplate = $this->templatesDirectory;
        $result = $this->render($layout, $data);
        $this->templatesDirectory = $currentTemplate;
        return $result;
    }
}

?>