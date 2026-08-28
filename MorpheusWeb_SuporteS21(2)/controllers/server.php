<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/info", function () {
    $services = array();
    foreach (Morpheus\Account\Service::getAllServices() as $service) {
        if ((strpos($service["service"], "account") !== false || strpos($service["service"], "character") !== false) && !empty($service["parent_id"]) && $service["active"]) {
            $services[] = $service;
        }
    }
    View::display("server/info", array("services" => $services));
});
Route::get("/coins", function () {
    View::display("server/coins");
});
Route::get("/players-online", function () {
    View::display("server/players-online");
});

?>