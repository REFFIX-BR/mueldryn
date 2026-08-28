<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Util;

class Inflector
{
    public static function slug($string)
    {
        $characters = array("Á" => "a", "Ç" => "c", "É" => "e", "Í" => "i", "Ñ" => "n", "Ó" => "o", "Ú" => "u", "á" => "a", "ç" => "c", "é" => "e", "í" => "i", "ñ" => "n", "ó" => "o", "ú" => "u", "à" => "a", "è" => "e", "ì" => "i", "ò" => "o", "ù" => "u");
        $string = strtr($string, $characters);
        $string = strtolower(trim($string));
        $string = preg_replace("/[^a-z0-9-]/", "-", $string);
        $string = preg_replace("/-+/", "-", $string);
        if (substr($string, strlen($string) - 1, strlen($string)) === "-") {
            $string = substr($string, 0, strlen($string) - 1);
        }
        return strtolower($string);
    }
    public static function humanize($string)
    {
        return ucwords(str_replace(array("_", "-"), " ", $string));
    }
    public static function classify($string)
    {
        return static::camelize($string);
    }
    public static function camelize($string)
    {
        return str_replace(" ", "", static::humanize($string));
    }
    public static function delimit($string, $delimiter = "_")
    {
        return strtolower(preg_replace("/(?<=\\w)([A-Z])/", $delimiter . "\\1", $string));
    }
    public static function underscore($string)
    {
        return static::delimit(str_replace("-", "_", $string), "_");
    }
    public static function dasherize($string)
    {
        return static::delimit(str_replace("_", "-", $string), "-");
    }
}

?>