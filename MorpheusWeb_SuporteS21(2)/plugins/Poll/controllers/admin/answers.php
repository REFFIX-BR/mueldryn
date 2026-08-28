<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/polls/answers", function () {
    $poll = Connection::fetchAssoc("select * from mw_polls where id = ?", array(Input::get("poll_id")), array("integer"));
    if (empty($poll)) {
        return App::notFound();
    }
    $answers = Connection::fetchAll("select * from mw_polls_answers where poll_id = ? order by sequence", array($poll["id"]), array("integer"));
    $total = 0;
    foreach ($answers as &$answer) {
        $persons = Connection::fetchColumn("select count(1) \r\n                from mw_polls_answers_persons\r\n                where answer_id = ?\r\n            ", array($answer["id"]));
        $total += $persons;
        $answer["votes"] = $persons;
    }
    View::display("Poll.answers/index", array("poll" => $poll, "answers" => $answers, "total" => $total));
})->name("poll.answers.index");
Route::map("/polls/answers/add", function () {
    $poll = Connection::fetchAssoc("select * from mw_polls where id = ?", array(Input::get("poll_id")), array("integer"));
    if (empty($poll)) {
        return App::notFound();
    }
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("answer" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                Connection::insert("mw_polls_answers", array("answer" => Input::post("answer"), "poll_id" => $poll["id"]), array("string", "integer"));
                return success(__("Answer has been saved"), array("redirect" => "/admin/polls/answers?poll_id=" . $poll["id"]));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("Poll.answers/add", array("poll" => $poll));
})->via("GET", "POST")->name("poll.answers.add");
Route::map("/polls/answers/edit/:id", function ($id) {
    $answer = Connection::fetchAssoc("select * from mw_polls_answers where id = ?", array($id), array("integer"));
    if (empty($answer)) {
        return App::notFound();
    }
    $poll = Connection::fetchAssoc("select * from mw_polls where id = ?", array($answer["poll_id"]), array("integer"));
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("answer" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                Connection::update("mw_polls_answers", array("answer" => Input::post("answer")), array("id" => $id), array("string"));
                return success(__("Answer has been updated"), array("redirect" => "/admin/polls/answers?poll_id=" . $poll["id"]));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("Poll.answers/edit", array("poll" => $poll, "answer" => $answer));
})->via("GET", "POST")->name("poll.answers.edit");
Route::get("/polls/answers/delete/:id", function ($id = NULL) {
    $answer = Connection::fetchAssoc("select * from mw_polls_answers where id = ?", array($id), array("integer"));
    try {
        Connection::transactional(function () use($id) {
            Connection::delete("mw_polls_answers", array("id" => $id));
            Connection::delete("mw_polls_answers_persons", array("answer_id" => $id));
        });
        return success(__("Answer has been deleted"), array("redirect" => "/admin/polls/answers?poll_id=" . $answer["poll_id"]));
    } catch (Exception $ex) {
        return error($ex->getMessage(), array("redirect" => "/admin/polls/answers?poll_id=" . $answer["poll_id"]));
    }
})->name("poll.answers.delete");
Route::post("/polls/answers/reorder", function () {
    $sequences = Input::post("sequence");
    foreach ($sequences as $sequence => $id) {
        Connection::update("mw_polls_answers", array("sequence" => $sequence + 1), array("id" => $id));
    }
});

?>