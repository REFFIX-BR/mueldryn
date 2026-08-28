<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::map("/webshop/config", function () {
    if (Request::isPost()) {
        try {
            Morpheus\Core\Config::save(array("webshop.blocked_recover_items" => Input::post("blocked")));
            return success(__("Settings has been updated"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    $blockeds = config("webshop.blocked_recover_items", array());
    View::display("WebShop.config", array("blockeds" => $blockeds ?: array()));
})->name("shops.config")->via("GET", "POST");

?>