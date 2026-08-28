<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/panel/character(/:character)", authenticate(), function ($character = "") {
    $character = character($character, user()->getUsername());
    View::service($character->exists() ? "characters/info" : "", "character", array("character" => $character));
});
Route::get("/panel/:panel", authenticate(), function ($panel) {
    View::service("panel/" . $panel, $panel);
});
Route::get("/panel", authenticate(), function () {
    View::display("panel/index");
});

?>