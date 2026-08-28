<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Database;

class Connection extends DriverManager
{
    public static function __callStatic($name, $arguments)
    {
        return call_user_func_array(array(static::getConnection(), $name), $arguments);
    }
}

?>