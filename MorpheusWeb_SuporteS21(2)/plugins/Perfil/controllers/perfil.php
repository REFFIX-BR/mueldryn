<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

app()->view()->add("css", array("Perfil.perfil"));
Route::get("/perfil/character/:name", function ($name) {
    $character = new Morpheus\Character\Character($name);
    if ($character->exists()) {
        $character->getInventory()->load();
        View::display("Perfil.character", array("character" => $character));
    } else {
        App::notFound();
    }
});
Route::get("/perfil/guild/:name", function ($name) {
    $guild = new Morpheus\Guild\Guild($name);
    if ($guild->exists()) {
        View::display("Perfil.guild", array("guild" => $guild));
    } else {
        App::notFound();
    }
});

?>