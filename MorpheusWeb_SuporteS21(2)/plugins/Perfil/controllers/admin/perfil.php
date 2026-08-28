<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::map("/perfil/config", function () {
    if (Request::isPost()) {
        try {
            Morpheus\Core\Config::save(array("perfil.show_equipments" => Input::post("show_equipments") == 1));
            return success(__("The settings has ben saved"), array("redirect" => "/admin/perfil/config"));
        } catch (Morpheus\Database\Exception $ex) {
            return error($ex);
        }
    }
    View::display("Perfil.config");
})->via("GET", "POST")->name("perfil.config");

?>