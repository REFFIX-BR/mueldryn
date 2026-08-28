<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace Morpheus\Ranking;

class MasterResets extends Resets
{
    protected $_key = "master_resets";
    public function isValid()
    {
        return config("columns.master_resets", false);
    }
}

?>