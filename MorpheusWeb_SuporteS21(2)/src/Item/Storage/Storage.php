<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Item\Storage;

class Storage
{
    public static function factory($storage, $file)
    {
        $class = "\\Morpheus\\Item\\Storage\\" . $storage;
        if (class_exists($class)) {
            $factory = new $class($file);
            return $factory;
        }
        throw new \Exception("Storage Class \"" . $class . "\" not found");
    }
}

?>