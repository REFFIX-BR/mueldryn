<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Core;

class Cache extends \Zend_Cache
{
    public static function factory($frontend, $backend, $frontendOptions = array(), $backendOptions = array(), $customFrontendNaming = false, $customBackendNaming = false, $autoload = false)
    {
        return parent::factory($frontend, $backend, $frontendOptions, array("cache_dir" => CACHE_PATH, "file_name_prefix" => "morpheus") + $backendOptions, $customFrontendNaming, $customBackendNaming, $autoload);
    }
}

?>