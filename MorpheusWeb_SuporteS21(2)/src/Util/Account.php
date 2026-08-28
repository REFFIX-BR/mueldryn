<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Util;

class Account
{
    public static function vipName($code)
    {
        $vips = config("vip.types", array());
        if (!isset($vips[$code])) {
            return "";
        }
        $vip = $vips[$code];
        if (is_array($vip)) {
            return isset($vip["name"]) ? $vip["name"] : "";
        }
        return $vip;
    }
}

?>