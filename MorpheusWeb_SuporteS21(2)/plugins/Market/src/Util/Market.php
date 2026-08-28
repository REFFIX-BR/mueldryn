<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Market\Util;

class Market
{
    public static function paymentName($payment, $coin)
    {
        $payments = array("coin" => __("Coin"), "zen" => __("Zen"), "jewels" => "Joias", "jc" => __("Jewel of Chaos"), "jb" => __("Jewel of Bless"), "js" => __("Jewel of Soul"), "jl" => __("Jewel of Life"), "jcr" => __("Jewel of Creation"));
        if (isset($payments[$payment])) {
            if ($payment === "coin") {
                $coins = config("coins", array());
                if (isset($coins[$coin])) {
                    return $coins[$coin]["name"];
                }
                return "";
            }
            return $payments[$payment];
        }
        return "";
    }
}

?>