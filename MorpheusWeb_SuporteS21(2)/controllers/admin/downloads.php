<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/downloads", function () {
    $downloads = config("downloads", array());
    View::display("downloads/index", compact("downloads"));
})->name("downloads");
Route::map("/downloads/add", function () {
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED), "size" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            $downloads = config("downloads", array());
            $links = array();
            foreach (Input::post("links") as $link) {
                if (!empty($link["name"]) && !empty($link["link"])) {
                    $links[$link["name"]] = $link["link"];
                }
            }
            $downloads[] = array("name" => Input::post("name"), "size" => Input::post("size"), "desc" => Input::post("desc"), "links" => $links);
            try {
                Morpheus\Core\Config::save("downloads", $downloads);
                return success(__("Download has been saved"), array("redirect" => "/admin/downloads"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("downloads/add");
})->via("GET", "POST")->name("downloads.add");
Route::map("/downloads/edit/:id", function ($id) {
    $downloads = Morpheus\Core\Config::get("downloads", array());
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED), "size" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            $links = array();
            foreach (Input::post("links") as $link) {
                if (!empty($link["name"]) && !empty($link["link"])) {
                    $links[$link["name"]] = $link["link"];
                }
            }
            $download = array("name" => Input::post("name"), "size" => Input::post("size"), "desc" => Input::post("desc"), "links" => $links);
            $downloads[$id] = $download;
            try {
                Morpheus\Core\Config::save("downloads", $downloads);
                return success(__("Download has been updated"), array("redirect" => "/admin/downloads"));
            } catch (Exception $ex) {
                return error($ex);
            }
        } else {
            return error($validation->getErrors());
        }
    }
    $download = $downloads[$id];
    $download["id"] = $id;
    View::display("downloads/edit", array("download" => $download));
})->via("GET", "POST")->name("downloads.edit");
Route::get("/downloads/delete/:id", function ($id) {
    $downloads = config("downloads", array());
    unset($downloads[$id]);
    try {
        Morpheus\Core\Config::save("downloads", $downloads);
        return success(__("Download has been deleted"), array("redirect" => "/admin/downloads"));
    } catch (Exception $ex) {
        return error($ex);
    }
})->name("downloads.delete");

?>