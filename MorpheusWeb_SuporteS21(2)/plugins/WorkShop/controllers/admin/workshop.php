<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::map("/workshop/config", function () {
    $version = config("item.db_version", 3);
    if (Request::isPost()) {
        try {
            Morpheus\Core\Config::save(array("workshop.max_level" => Input::post("max_level"), "workshop.max_option" => Input::post("max_option"), "workshop.max_excellent" => Input::post("max_excellent"), "workshop.allow_ancient" => Input::post("allow_ancient"), "workshop.allow_refine" => Input::post("allow_refine"), "workshop.allow_harmony" => Input::post("allow_harmony"), "workshop.allow_sockets" => Input::post("allow_sockets"), "workshop.max_sockets" => Input::post("max_sockets"), "workshop.unique_sockets" => Input::post("unique_sockets"), "workshop.coin" => Input::post("coin"), "workshop.prices.level" => Input::post("price_level"), "workshop.prices.option" => Input::post("price_option"), "workshop.prices.skill" => Input::post("price_skill"), "workshop.prices.luck" => Input::post("price_luck"), "workshop.prices.excellent" => Input::post("price_excellent"), "workshop.prices.ancient" => Input::post("price_ancient"), "workshop.prices.harmony" => Input::post("price_harmony"), "workshop.prices.refine" => Input::post("price_refine"), "workshop.prices.socket" => Input::post("price_socket"), "workshop.blocked_items" => Input::post("blocked")));
            return success(__("Settings has been updated"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    $blockeds = config("workshop.blocked_items", array());
    View::display("WorkShop.config", array("version" => $version, "blockeds" => $blockeds ?: array()));
})->via("GET", "POST")->name("workshop.config");

?>