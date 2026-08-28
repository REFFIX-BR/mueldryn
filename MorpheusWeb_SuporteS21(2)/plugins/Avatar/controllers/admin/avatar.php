<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::map("/configs/avatar", function () {
    if (Request::isPost()) {
        try {
            Morpheus\Core\Config::save(array("avatar.use_type" => Input::post("use_type"), "avatar.filesize" => Input::post("filesize"), "avatar.allow_gif" => Input::post("allow_gif") == 1));
            return success(__("The settings has ben saved"), array("redirect" => "/admin/configs/avatar"));
        } catch (Morpheus\Database\Exception $ex) {
            return error($ex);
        }
    }
    View::display("Avatar.config");
})->via("GET", "POST");

?>