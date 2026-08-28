<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Util;

class Gens
{
    public static function getGraduationFromContribution($contribution, $position)
    {
        $graduation = 0;
        if (0 <= $contribution && $contribution <= 999) {
            $graduation = 14;
        } else {
            if (1000 <= $contribution && $contribution <= 4999) {
                $graduation = 13;
            } else {
                if (5000 <= $contribution && $contribution <= 14999) {
                    $graduation = 12;
                } else {
                    if (15000 <= $contribution && $contribution <= 49999) {
                        $graduation = 11;
                    } else {
                        if (50000 <= $contribution && $contribution <= 99999) {
                            $graduation = 10;
                        } else {
                            $graduation = 9;
                        }
                    }
                }
            }
        }
        if (100000 < $contribution) {
            if ($position == 1) {
                $graduation = 1;
            } else {
                if (2 <= $position && $position <= 5) {
                    $graduation = 2;
                } else {
                    if (6 <= $position && $position <= 10) {
                        $graduation = 3;
                    } else {
                        if (11 <= $position && $position <= 30) {
                            $graduation = 4;
                        } else {
                            if (31 <= $position && $position <= 50) {
                                $graduation = 5;
                            } else {
                                if (51 <= $position && $position <= 100) {
                                    $graduation = 6;
                                } else {
                                    if (101 <= $position && $position <= 200) {
                                        $graduation = 7;
                                    } else {
                                        if (201 <= $position && $position <= 300) {
                                            $graduation = 8;
                                        } else {
                                            if (301 <= $position) {
                                                $graduation = 9;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return $graduation;
    }
    public static function graduationName($graduation)
    {
        $name = "-";
        switch ($graduation) {
            case 1:
                $name = __("Grand Duke");
                break;
            case 2:
                $name = __("Duke");
                break;
            case 3:
                $name = __("Marquis");
                break;
            case 4:
                $name = __("Count");
                break;
            case 5:
                $name = __("Viscount");
                break;
            case 6:
                $name = __("Baron");
                break;
            case 7:
                $name = __("Knight Commander");
                break;
            case 8:
                $name = __("Superior Knight");
                break;
            case 9:
                $name = __("Graduation Knight");
                break;
            case 10:
                $name = __("Guard Perfect");
                break;
            case 11:
                $name = __("Graduation Officer");
                break;
            case 12:
                $name = __("Graduation Lieutenant");
                break;
            case 13:
                $name = __("Graduation Sergeant");
                break;
            case 14:
                $name = __("Graduation Private");
                break;
        }
        return $name;
    }
    public static function familyName($family)
    {
        $name = "Duprian";
        if ($family == 2) {
            $name = "Vanert";
        }
        return $name;
    }
}

?>