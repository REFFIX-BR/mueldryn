<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::map("/hallfame/config", function () {
    if (Request::isPost()) {
        $post = Request::post("hallfame");
        $hallfame = array();
        foreach ($post as $id => $p) {
            if (isset($p["active"]) || isset($p["daily"]) || isset($p["weekly"]) || isset($p["monthly"])) {
                $hallfame[$id] = array("active" => isset($p["active"]), "name" => $p["name"], "daily" => isset($p["daily"]), "weekly" => isset($p["weekly"]), "monthly" => isset($p["monthly"]));
            }
        }
        try {
            Morpheus\Core\Config::save(array("hallfame.rankings" => $hallfame));
            Morpheus\Core\Config::save(array("hallfame.max_results" => Input::post("max_results")));
            return success(__("The settings has been saved"), array("redirect" => "/admin/hallfame/config"));
        } catch (Exception $ex) {
            return error($ex);
        }
    }
    $rankings = config("rankings", array());
    View::display("HallFame.config", array("rankings" => $rankings));
})->via("GET", "POST")->name("hallfame.config");

?>