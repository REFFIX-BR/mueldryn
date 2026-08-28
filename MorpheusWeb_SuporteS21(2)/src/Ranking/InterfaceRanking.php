<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Ranking;

interface InterfaceRanking
{
    public function getQuery();
    public function reset();
    public function isValid();
}

?>