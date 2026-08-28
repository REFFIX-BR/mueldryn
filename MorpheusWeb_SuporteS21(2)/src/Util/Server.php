<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Util;

class Server
{
    public static function status($online)
    {
        if ($online) {
            return "<span class=\"online\">Online</span>";
        }
        return "<span class=\"offline\">Offline</span>";
    }
}

?>