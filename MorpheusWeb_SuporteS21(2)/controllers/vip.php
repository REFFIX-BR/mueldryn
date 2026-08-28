<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/vip", function () {
    View::display("vip/index");
});
Route::get("/vip/advantages", function () {
    View::display("vip/advantages");
});
Route::get("/vip/payment", function () {
    View::display("vip/payment");
});

?>