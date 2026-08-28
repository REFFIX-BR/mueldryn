<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Util;

class Text
{
    public static function truncate($string, $limit, $break = ".", $pad = "...")
    {
        if (strlen($string) <= $limit) {
            return $string;
        }
        if (false !== ($breakpoint = strpos($string, $break, $limit)) && $breakpoint < strlen($string) - 1) {
            $string = substr($string, 0, $breakpoint) . $pad;
        }
        return $string;
    }
    public static function toList($list, $and = "and", $separator = ", ")
    {
        if (1 < count($list)) {
            return implode($separator, array_slice($list, null, -1)) . " " . $and . " " . array_pop($list);
        }
        return array_pop($list);
    }
    public static function nvl()
    {
        foreach (func_get_args() as $value) {
            if (!empty($value)) {
                return $value;
            }
        }
        return "";
    }
}

?>