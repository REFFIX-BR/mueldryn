<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Util;

class Ranking
{
    public static function position($rank)
    {
        if (0 < $rank) {
            return "<span class=\"rank-up\">&uarr;" . $rank . "</span>";
        }
        if ($rank < 0) {
            return "<span class=\"rank-down\">&darr;" . $rank * -1 . "</span>";
        }
        return "<span class=\"rank-stayed\">-</span>";
    }
}

?>