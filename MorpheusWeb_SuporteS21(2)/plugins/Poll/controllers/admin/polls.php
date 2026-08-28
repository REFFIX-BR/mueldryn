<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/polls", function () {
    $polls = Connection::fetchAll("select * from mw_polls order by id desc");
    View::display("Poll.polls/index", array("polls" => $polls));
})->name("poll");
Route::map("/polls/add", function () {
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("title" => array(Morpheus\Core\Validation::REQUIRED), "start_at" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                Connection::transactional(function () {
                    $start = Input::post("start_at");
                    $end = Input::post("end_at");
                    Connection::insert("mw_polls", array("title" => Input::post("title"), "start_date" => !empty($start) ? util("Date")->toISO($start) : NULL, "end_date" => !empty($end) ? util("Date")->toISO($end) : NULL, "active" => Input::post("active") == 1, "allow_multiple" => Input::post("multiple") == 1, "allow_ips" => Input::post("allow_ips") == 1, "only_logged_in" => Input::post("only_logged_in") == 1), array("string", "string", "string", "integer", "integer", "integer", "integer"));
                    $id = Connection::lastInsertId();
                    $answers = Input::post("answers");
                    foreach ($answers as $answer) {
                        $answer = trim($answer);
                        if (!empty($answer)) {
                            Connection::insert("mw_polls_answers", array("answer" => $answer, "poll_id" => $id), array("string", "integer"));
                        }
                    }
                });
                return success(__("Poll has been saved"), array("redirect" => "/admin/polls"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("Poll.polls/add");
})->via("GET", "POST")->name("poll.add");
Route::map("/polls/edit/:id", function ($id) {
    $poll = Connection::fetchAssoc("select * from mw_polls where id = ?", array($id));
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("title" => array(Morpheus\Core\Validation::REQUIRED), "start_at" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                $start = Input::post("start_at");
                $end = Input::post("end_at");
                Connection::update("mw_polls", array("title" => Input::post("title"), "start_date" => !empty($start) ? util("Date")->toISO($start) : NULL, "end_date" => !empty($end) ? util("Date")->toISO($end) : NULL, "active" => Input::post("active") == 1, "allow_multiple" => Input::post("multiple") == 1, "allow_ips" => Input::post("allow_ips") == 1, "only_logged_in" => Input::post("only_logged_in") == 1), array("id" => $id), array("string", "string", "string", "integer", "integer", "integer", "integer", "integer"));
                return success(__("Poll has been updated"), array("redirect" => "/admin/polls/edit/" . $id));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("Poll.polls/edit", array("poll" => $poll));
})->via("GET", "POST")->name("poll.edit");

?>