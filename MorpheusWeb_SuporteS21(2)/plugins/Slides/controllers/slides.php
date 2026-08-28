<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

View::add("script", array("Slides.slippry", "Slides.slides"));
View::add("css", array("Slides.slippry"));
App::hook("slides", function ($options = array()) {
    $slides = Connection::fetchAll("select *\r\n        from mw_slides\r\n        where active = 1\r\n        order by sequence asc, id asc\r\n    ");
    View::display("Slides.slide", array("slides" => $slides, "layout" => false, "options" => $options));
});

?>