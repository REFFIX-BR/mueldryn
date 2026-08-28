<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/market/plugins", function () {
    $plugins = array();
    $http = Morpheus\Network\Http::create("GET", MORPHEUS_WEBSITE . "plugins.json", array("query" => array("q" => Input::get("q"), "purchasable" => true)))->send();
    $response = $http->getResponse();
    if ($response["header"]["status"] === 200) {
        $data = json_decode($response["body"], true);
        $plugins = $data;
    }
    View::display("market/plugins", array("plugins" => $plugins));
});
Route::get("/market/templates", function () {
    $templates = array();
    $http = Morpheus\Network\Http::create("GET", MORPHEUS_WEBSITE . "themes.json", array("query" => array("q" => Input::get("q"))))->send();
    $response = $http->getResponse();
    if ($response["header"]["status"] === 200) {
        $data = json_decode($response["body"], true);
        $templates = $data;
    }
    View::display("market/templates", array("templates" => $templates));
});

?>