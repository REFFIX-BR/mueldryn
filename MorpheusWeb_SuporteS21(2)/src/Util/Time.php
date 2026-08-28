<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Util;

class Time
{
    public static function relative($dt, $precision = 2, $after = "ago")
    {
        $times = array(365 * 24 * 60 * 60 => array("year", "years"), 30 * 24 * 60 * 60 => array("month", "months"), 7 * 24 * 60 * 60 => array("week", "weeks"), 24 * 60 * 60 => array("day", "days"), 60 * 60 => array("hour", "hours"), 60 => array("minute", "minutes"), 1 => array("second", "seconds"));
        $passed = (new \DateTime())->getTimestamp() - ($dt instanceof \DateTimeInterface ? $dt->getTimestamp() : strtotime($dt));
        if ($passed < 5) {
            $output = "5 " . __("seconds") . " " . __($after);
        } else {
            $output = array();
            $exit = 0;
            foreach ($times as $period => $name) {
                if ($precision <= $exit || 0 < $exit && $period < 60) {
                    break;
                }
                $result = floor($passed / $period);
                if (0 < $result) {
                    $output[] = $result . " " . __($result == 1 ? $name[0] : $name[1]);
                    $passed -= $result * $period;
                    $exit++;
                } else {
                    if (0 < $exit) {
                        $exit++;
                    }
                }
            }
            $output = implode(" " . __("and") . " ", $output) . " " . __($after);
        }
        return $output;
    }
}

?>