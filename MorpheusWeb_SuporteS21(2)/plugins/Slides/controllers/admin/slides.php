<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/slides", function () {
    $slides = Connection::fetchAll("select * \r\n        from mw_slides\r\n    ");
    View::display("Slides.index", compact("slides"));
})->name("slides");
Route::map("/slides/add", function () {
    if (Request::isPost()) {
        $post = Input::post();
        $validation = new Morpheus\Core\Validation($post, array("image" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                $image = Input::file("image");
                $filename = NULL;
                if (!empty($image)) {
                    $upload = upload(rework_upload_files($image)[0], array("dir" => "uploads" . DS . "slides" . DS));
                    if (!$upload["success"]) {
                        return error($upload["error"]);
                    }
                    $filename = $upload["filename"];
                }
                Connection::insert("mw_slides", array("title" => $post["title"], "url" => $post["url"], "active" => isset($post["active"]) && $post["active"] == 1, "image" => $filename), array("string", "string", "boolean", "string"));
                return success(__("Slide has been saved"), array("redirect" => "/admin/slides"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("Slides.add");
})->via("GET", "POST")->name("slides.add");
Route::map("/slides/edit/:id", function ($id = NULL) {
    $slide = Connection::fetchAssoc("select *\r\n        from mw_slides\r\n        where id = ?\r\n    ", array($id));
    if (Request::isPost()) {
        $post = Input::post();
        $validation = new Morpheus\Core\Validation($post, array("image" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                $image = Input::file("image");
                $filename = $slide["image"];
                if (!empty($image)) {
                    $upload = upload(rework_upload_files($image)[0], array("dir" => "uploads" . DS . "slides" . DS));
                    if (!$upload["success"]) {
                        return error($upload["error"]);
                    }
                    $filename = $upload["filename"];
                }
                Connection::update("mw_slides", array("title" => $post["title"], "url" => $post["url"], "active" => isset($post["active"]) && $post["active"] == 1, "image" => $filename), array("id" => $id), array("string", "string", "boolean", "string", "integer"));
                return success(__("Slide has been updated"), array("redirect" => "/admin/slides"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("Slides.edit", array("slide" => $slide));
})->via("GET", "POST")->name("slides.edit");
Route::get("/slides/delete/:id", function ($id) {
    try {
        Connection::delete("mw_slides", array("id" => $id));
        return success(__("Slide has been deleted"), array("redirect" => "/admin/slides"));
    } catch (Exception $ex) {
        return error($ex, array("redirect" => "/slides"));
    }
})->name("slides.delete");

?>