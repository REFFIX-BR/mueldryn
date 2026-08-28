<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Util;

class Coin
{
    public function name($coin)
    {
        if ($coin === 'credits') {
            return 'Credits';
        }
        $coins = config("coins", array());
        if (isset($coins[$coin])) {
            return $coins[$coin]["name"];
        }
        return "";
    }
}

?>