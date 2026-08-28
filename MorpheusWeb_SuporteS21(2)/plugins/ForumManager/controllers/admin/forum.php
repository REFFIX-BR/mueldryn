<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::map("/forum/config", function () {
    if (Request::isPost()) {
        try {
            Morpheus\Core\Config::save(array("forum.conn" => Input::post("forum_conn"), "forum.news.limit" => Input::post("forum_limit"), "forum.news.type" => Input::post("forum_type"), "forum.news.topic_url" => Input::post("forum_topic_url"), "forum.news.base_forum" => Input::post("forum_base"), "forum.auto_register" => Input::post("forum_auto_register")));
            return success(__("The settings has ben saved"), array("redirect" => "/admin/forum/config"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    $types = array("vbulletin" => "vBulletin", "ipb3" => "Invision Power Board v3", "ipb" => "Invision Power Board", "phpbb" => "phpBB", "mybb" => "mybb");
    View::display("ForumManager.config", array("types" => $types));
})->via("GET", "POST")->name("news.forum.config");

?>