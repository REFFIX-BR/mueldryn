<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::map("/items/find", function () {
    if (Request::post()) {
        $serial = Input::post("serial");
        $account = Morpheus\Item\Finder::inWarehouse($serial);
        return View::display("items/result-find", array("account" => $account, "serial" => $serial));
    }
    View::display("items/find");
})->via("GET", "POST")->name("items.find");

?>