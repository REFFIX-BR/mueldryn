<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::map("/exchange/config", function () {
    $account = user();
    if (Request::isPost()) {
        $post = Input::post("coins");
        $coins = array();
        foreach ($post as $id => $p) {
            if (isset($p["active"])) {
                $rates = array();
                foreach ($p["rates"] as $key => $value) {
                    if (!empty($value)) {
                        $rates[$key] = $value;
                    }
                }
                if (!empty($rates)) {
                    $coins[$id] = array("tax" => empty($p["tax"]) ? 0 : $p["tax"], "coin" => $id, "rates" => $rates);
                }
            }
        }
        try {
            Morpheus\Core\Config::save(array("exchange" => $coins));
            return success(__("The settings has been saved"), array("redirect" => "/admin/exchange/config"));
        } catch (Exception $ex) {
            return error($ex);
        }
    }
    return View::display("Exchange.config", array("coins" => config("coins", array()), "change" => config("exchange")));
})->via("GET", "POST")->name("exchange.config");

?>