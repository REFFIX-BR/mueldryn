<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Database;

class DriverManager
{
    protected static $_connections = array();
    protected static $_defaultConnection = "default";
    public static function addConnection($conn = "default", $config = array())
    {
        if (isset($config["driver_class"])) {
            $config["driverClass"] = $config["driver_class"];
            unset($config["driver_class"]);
        }
        if (!isset(static::$_connections[$conn])) {
            $configuration = new \Doctrine\DBAL\Configuration();
            $logger = new \Doctrine\DBAL\Logging\DebugStack();
            $configuration->setSQLLogger($logger);
            $connection = \Doctrine\DBAL\DriverManager::getConnection($config, $configuration);
            try {
                $connection->exec("SET LANGUAGE us_english; SET DATEFORMAT ymd;");
            } catch (\Exception $e) {
            }
            static::$_connections[$conn] = $connection;
            registry("sql_logger", $logger);
        }
        return static::$_connections[$conn];
    }
    public static function getConnection($conn = NULL)
    {
        if ($conn === null) {
            return static::$_connections[static::$_defaultConnection];
        }
        if (!isset(static::$_connections[$conn])) {
            throw new \RuntimeException("Doen't exists configuration for connection '" . $conn . "'");
        }
        return static::$_connections[$conn];
    }
    public static function getConfig($key = false, $def = NULL)
    {
        return config("conn." . static::$_defaultConnection . ($key ? "." . $key : ""), $def);
    }
    public static function getTableWithDb($table = "")
    {
        $prefix = "";
        $config = Connection::getConfig();
        if (isset($config["me"]) && $config["me"]) {
            $prefix = "[" . $config["me"] . "].dbo.";
        }
        return $prefix . $table;
    }
    public static function get($conn = "default")
    {
        return static::getConnection($conn);
    }
    public static function getConnections()
    {
        return static::$_connections;
    }
    public static function setDefaultConnection($default)
    {
        return static::$_defaultConnection = $default;
    }
}

?>