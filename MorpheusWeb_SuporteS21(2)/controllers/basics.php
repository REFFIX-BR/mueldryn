<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/maintenance", function () {
    if (config("maintenance", false) && !Morpheus\Admin\Auth::loggedIn()) {
        View::display("maintenance", array("layout" => false));
    } else {
        App::notFound();
    }
});
Route::get("/partial/:partial", function ($partial) {
    partial(str_replace("|", ".", $partial));
});
Route::get("/captcha(/:width(/:height))", function ($width = 200, $height = 70) {
    $captcha = new CoolCaptcha\Captcha();
    $captcha->width = $width;
    $captcha->height = $height;
    $text = $captcha->createImage();
    Session::set("captcha", $text);
});
Route::get("/guild/logo/:mark(/:size)", function ($mark, $size = 64) {
    $logo = new Morpheus\Guild\Logo();
    $logo->setMark($mark)->setSize($size)->toImage();
    header("Content-type: image/png");
});
Route::get("/lang/:locale", function ($locale) {
    Session::set("lang", $locale);
    App::redirect("/");
});
Route::get("/update/license", function () {
    $referer = isset($_SERVER["HTTP_REFERER"]) ? $_SERVER["HTTP_REFERER"] : "";
    if (!empty($referer)) {
        $paths = parse_url($referer);
        $paths2 = parse_url(MORPHEUS_WEBSITE);
        if ($paths["host"] === $paths2["host"]) {
            $license = new Morpheus\Core\License();
            $license->update();
        } else {
            App::notFound();
        }
    } else {
        App::notFound();
    }
});

?>