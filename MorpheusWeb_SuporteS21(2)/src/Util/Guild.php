<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Util;

class Guild
{
    public static function logoUrl($mark, $size = 64)
    {
        return base() . "/resources/logo.php?mark=" . bintohex($mark) . "&size=" . $size;
    }
}

?>