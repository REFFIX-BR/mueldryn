<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Core;

class Config implements \Countable, \Iterator
{
    private $_index = NULL;
    private $_count = NULL;
    private $_data = array();
    private static $_instance = NULL;
    public static function getInstance()
    {
        if (static::$_instance === null) {
            static::$_instance = new self();
        }
        return static::$_instance;
    }
    public static function addFile($file)
    {
        if (is_array($file)) {
            foreach ($file as $f) {
                static::addFile($f);
            }
        } else {
            $file = CONFIGS_PATH . $file;
            if (!file_exists($file)) {
                throw new \RuntimeException("Configuration file '" . $file . "' doesn't not exists");
            }
            static::add(require $file);
        }
    }
    public static function addFiles($files)
    {
        static::addFile($files);
    }
    public static function add($data)
    {
        $instance = static::getInstance();
        $instance->_index = 0;
        $instance->_data = \Morpheus\Util\Hash::merge($instance->_data, $data);
        $instance->_count = count($instance->_data);
    }
    public static function load()
    {
        $instance = static::getInstance();
        try {
            $configs = \Morpheus\Database\Connection::fetchAll("SELECT * FROM mw_config");
            $data = array();
            foreach ($configs as $config) {
                if ($config["type"] === "array") {
                    $data[$config["config"]] = json_decode($config["body"], true);
                } else {
                    if ($config["type"] === "boolean") {
                        $data[$config["config"]] = (bool) $config["body"];
                    } else {
                        $data[$config["config"]] = html_entity_decode($config["body"]);
                    }
                }
            }
            $plain = array();
            $dotted = array();
            foreach ($data as $key => $value) {
                if (strpos($key, ".") === false) {
                    $plain[$key] = $value;
                } else {
                    $dotted[$key] = $value;
                }
            }
            uksort($dotted, function ($a, $b) {
                return substr_count($a, ".") - substr_count($b, ".");
            });
            $instance->add(\Morpheus\Util\Hash::expand(array_merge($plain, $dotted)));
        } catch (Exception $ex) {
        }
    }
    public static function save($key, $value = NULL)
    {
        if (is_array($key)) {
            foreach ($key as $k => $v) {
                static::save($k, $v);
            }
        } else {
            $type = "";
            if (is_array($value)) {
                $type = "array";
            } else {
                if (is_bool($value)) {
                    $type = "boolean";
                }
            }
            $result = \Morpheus\Database\Connection::fetchAssoc("SELECT * FROM mw_config WHERE config = :config", array("config" => $key));
            if (empty($result)) {
                \Morpheus\Database\Connection::insert("mw_config", array("config" => $key, "body" => is_array($value) ? json_encode($value) : $value, "type" => $type));
            } else {
                \Morpheus\Database\Connection::update("mw_config", array("body" => is_array($value) ? json_encode($value) : $value, "type" => $type), array("config" => $key));
            }
        }
    }
    public static function get($name = NULL, $default = NULL)
    {
        $instance = static::getInstance();
        if ($name === null) {
            return $instance->_data;
        }
        $data = \Morpheus\Util\Hash::get($instance->_data, $name);
        if ($data !== null) {
            return $data;
        }
        return $default;
    }
    public static function all()
    {
        $instance = static::getInstance();
        $configs = $instance->_data;
        unset($configs["conn"]);
        return $configs;
    }
    public function count()
    {
        return $this->_count;
    }
    public function current()
    {
        return current($this->_data);
    }
    public function key()
    {
        return key($this->_data);
    }
    public function next()
    {
        next($this->_data);
        $this->_index++;
    }
    public function rewind()
    {
        reset($this->_data);
        $this->_index = 0;
    }
    public function valid()
    {
        return $this->_index < $this->_count;
    }
}

?>