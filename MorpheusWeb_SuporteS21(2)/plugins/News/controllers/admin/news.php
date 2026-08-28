<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Morpheus\Menu\Menu::getInstance("admin")->add(array("id" => "news", "title" => __("News"), "icon" => "newspaper-o"))->add(array("id" => "news.add", "title" => __("Add news"), "url" => "/admin/news/add"))->add(array("id" => "news.manage", "title" => __("Manage news"), "url" => "/admin/news"))->add(array("id" => "news.sep1", "title" => "-"))->add(array("id" => "news.config", "title" => __("Settings"), "url" => "/admin/news/config"));
Route::get("/news", function () {
    $news = Connection::fetchAll("SELECT\n        n.*,\n        u.name AS username\n        FROM mw_news n\n        JOIN mw_users u ON u.id = n.user_id\n        ORDER BY n.id DESC\n    ");
    View::display("News.index", array("news" => $news));
})->name("news");
Route::map("/news/add", function () {
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("title" => array(Morpheus\Core\Validation::REQUIRED), "body" => array(Morpheus\Core\Validation::REQUIRED), "type" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                $image = Input::file("image_cover");
                $upload["success"] = false;
                if (!empty($image)) {
                    $upload = upload(rework_upload_files($image)[0], array("dir" => "uploads" . DS . "news" . DS));
                }
                $news = new News\News();
                $news->setTitle(Input::post("title"))->setBody(Input::post("body"))->setLink(Input::post("link"))->setType(Input::post("type"))->setImageCover($upload["success"] ? $upload["filename"] : "")->setAllowedComments(Input::post("allow_comments") == 1);
                $news->insert();
                return success(__("The news has been saved"), array("redirect" => "/admin/news"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    } else {
        View::display("News.add");
    }
})->via("GET", "POST")->name("news.add");
Route::map("/news/edit/:id", function ($id) {
    $news = new News\News($id);
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("title" => array(Morpheus\Core\Validation::REQUIRED), "body" => array(Morpheus\Core\Validation::REQUIRED), "type" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                $image = Input::file("image_cover");
                $upload = array("success" => false);
                if (!empty($image)) {
                    $upload = upload(rework_upload_files($image)[0], array("dir" => "uploads" . DS . "news" . DS));
                }
                $news->setTitle(Input::post("title"))->setBody(Input::post("body"))->setLink(Input::post("link"))->setType(Input::post("type"))->setAllowedComments(Input::post("allow_comments") == 1);
                if ($upload["success"]) {
                    $news->setImageCover($upload["filename"]);
                }
                $news->update();
                return success(__("the news has been updated"), array("redirect" => "/admin/news/edit/" . $id));
            } catch (Exception $ex) {
                return error($ex);
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("News.edit", array("news" => $news));
})->via("GET", "POST")->name("news.edit");
Route::get("/news/delete/:id", function ($id) {
    try {
        $news = new News\News($id);
        $news->delete();
        return success(__("The news has been deleted"), array("redirect" => "/admin/news"));
    } catch (Exception $ex) {
        return error($ex, array("redirect" => "/news"));
    }
})->name("news.delete");
Route::map("/news/config", function () {
    if (Request::isPost()) {
        $types = array();
        $i = 1;
        foreach (Input::post("news_types") as $type) {
            if (!empty($type["name"]) && !empty($type["color"])) {
                $types[$i++] = array("name" => $type["name"], "color" => $type["color"]);
            }
        }
        try {
            Morpheus\Core\Config::save(array("news.limit" => Input::post("news_limit"), "news.types" => $types, "news.show_preview" => Input::post("show_preview") == 1, "news.allow_comments" => Input::post("allow_comments") == 1));
            return success(__("The settings has ben saved"), array("redirect" => "/admin/news/config"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    View::display("News.config");
})->via("GET", "POST")->name("news.config");

?>