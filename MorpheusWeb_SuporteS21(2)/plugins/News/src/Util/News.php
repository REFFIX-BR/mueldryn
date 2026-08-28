<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace News\Util;

class News
{
    public static function newsType($type, $col = true, $span = true)
    {
        $types = config("news.types");
        if (isset($types[$type])) {
            return ($span ? "<span style=\"color:" . $types[$type]["color"] . "\">" : "") . ($col ? "[" : "") . $types[$type]["name"] . ($col ? "]" : "") . ($span ? "</span>" : "");
        }
        return "";
    }
}

?>