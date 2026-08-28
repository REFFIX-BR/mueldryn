<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Util;

class Date
{
    public static function date($date, $format = NULL)
    {
        if (empty($date)) {
            return "";
        }
        if ($format === null) {
            $format = \Zend_Locale_Format::getDateFormat();
        }
        return (new \Zend_Date($date))->toString($format);
    }
    public static function dateTime($date, $format = NULL, $seconds = false)
    {
        if (empty($date)) {
            return "";
        }
        if ($format === null) {
            $format = \Zend_Locale_Format::getDateTimeFormat();
        }
        if (!$seconds) {
            $format = str_replace(":ss", "", $format);
        }
        return (new \Zend_Date($date))->toString($format);
    }
    public static function toISO($date, $format = NULL)
    {
        if ($format === null) {
            $format = \Zend_Locale_Format::getDateTimeFormat();
        }
        return (new \Zend_Date($date, $format))->toString(\Zend_Date::ISO_8601);
    }
}

?>