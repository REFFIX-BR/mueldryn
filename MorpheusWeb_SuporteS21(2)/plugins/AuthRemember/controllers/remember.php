<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

App::hook("accounts.login", function ($user, $post = array()) {
    if (isset($post["remember"]) && $post["remember"]) {
        $exists = Connection::fetchColumn("select 1 from mw_auth_tokens where account = ?", array($user->getUsername()));
        $time = strtotime("+30 day");
        $token = generate_token();
        if (!$exists) {
            Connection::insert("mw_auth_tokens", array("account" => $user->getUsername(), "token" => $token, "expires" => $time));
        } else {
            Connection::update("mw_auth_tokens", array("token" => $token, "expires" => $time), array("account" => $user->getUsername()));
        }
        setcookie("token", $token, $time);
    }
});
App::hook("accounts.logout", function ($user) {
    Connection::delete("mw_auth_tokens", array("account" => $user->getUsername()));
    App::deleteCookie("token");
});
App::hook("auth.before_validate", function () {
    $token = App::getCookie("token");
    if ($token && Session::get("Morpheus.username") === NULL) {
        $reg = Connection::fetchAssoc("select * from mw_auth_tokens where token = ?", array($token));
        if ($reg) {
            if ($reg["expires"] <= time()) {
                Connection::delete("mw_auth_tokens", array("account" => $reg["account"]));
                App::deleteCookie("token");
            } else {
                Session::set("Morpheus.username", $reg["account"]);
            }
        }
    }
});

?>