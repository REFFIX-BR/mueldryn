<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Util;

class Number
{
    public static function format($number, $decimals = 0, $sep = ".", $thousand = ".")
    {
        return number_format($number, $decimals, $sep, $thousand);
    }
    public static function toFloat($value)
    {
        return preg_replace("/^([0-9]+)(\\.([0-9]+))?,([0-9]+)\$/", "\$1\$3.\$4", $value);
    }
    public static function percent($one, $two)
    {
        if ($two < $one) {
            $porcent = 100;
        } else {
            if ($two == 0) {
                $porcent = 0;
            } else {
                $porcent = round($one * 100 / $two, 2);
            }
        }
        return $porcent;
    }
    public static function currency($value, array $options = array())
    {
        return (new \Zend_Currency(config("locale", "pt_BR")))->toCurrency($value, $options);
    }
}

?>