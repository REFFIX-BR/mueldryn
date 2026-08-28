<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

App::hook(array("home", "news.latest"), function () {
    $slides = (int) Connection::fetchColumn("SELECT COUNT(*) FROM mw_slides WHERE active = 1");
    $excludeId = null;
    if ($slides === 0) {
        $excludeId = Connection::fetchColumn("SELECT TOP 1 n.id FROM mw_news n ORDER BY n.type ASC, n.id DESC");
    }
    $sql = "SELECT TOP " . config("news.limit", 5) . "\n        n.*,\n        u.name AS username\n        FROM mw_news n\n        JOIN mw_users u\n        ON u.id = n.user_id\n    ";
    if ($excludeId) {
        $sql .= "WHERE n.id <> " . (int) $excludeId . "\n    ";
    }
    $sql .= "ORDER BY n.id DESC";
    $news = Connection::fetchAll($sql);
    View::display("News.home", array("layout" => false, "news" => $news));
}, 1);
App::hook(array("home", "news.featured"), function () {
    $slides = (int) Connection::fetchColumn("SELECT COUNT(*) FROM mw_slides WHERE active = 1");
    if ($slides > 0) {
        return;
    }
    $news = Connection::fetchAssoc("SELECT TOP 1\n        n.*,\n        u.name AS username\n        FROM mw_news n\n        JOIN mw_users u ON u.id = n.user_id\n        ORDER BY n.type ASC, n.id DESC\n    ");
    if (empty($news)) {
        return;
    }
    View::display("News.featured", array("layout" => false, "news" => $news));
}, 1);
Route::get("/news", function () {
    $news = Connection::fetchAll("SELECT\n        n.*,\n        u.name AS username\n        FROM mw_news n\n        JOIN mw_users u\n        ON u.id = n.user_id\n        ORDER BY n.id DESC\n    ");
    View::display("News.index", array("news" => $news));
});
Route::get("/news/:slug", function ($slug) {
    $news = new News\News();
    $news->readBySlug($slug);
    if (!$news->exists()) {
        App::notFound();
    } else {
        $news->incrementViews();
        if ($news->getLink() != NULL) {
            App::redirect($news->getLink());
        } else {
            View::display("News.view", array("news" => $news));
        }
    }
});
Route::post("/news/comments/add/:id", function ($id) {
    $news = new News\News($id);
    if (!$news->exists()) {
        App::notFound();
    } else {
        if (!logged_in()) {
            return error(__("You must be logged for comment this news"));
        }
        if (!$news->isAllowedComments()) {
            return error(__("Disabled comments"));
        }
        $validation = new Morpheus\Core\Validation(Input::post(), array("comment" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                $comment = new News\Comment();
                $comment->setAdmin(false)->setComment(Input::post("comment"))->setNewsId($news->getId())->setAuthor(user()->getUsername());
                $comment->insert();
                return success(__("Comment has been added to news"), array("redirect" => "/news/" . $news->getSlug()));
            } catch (Morpheus\Database\Exception $ex) {
                return error($ex);
            }
        } else {
            return error($validation->getErrors());
        }
    }
});

?>