<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/", function () {
    $data = array();
    if (server()->hasCastleSiege()) {
        $data["siege"] = server()->getSiegeInfo();
    }
    View::display("home", $data);
});

?>