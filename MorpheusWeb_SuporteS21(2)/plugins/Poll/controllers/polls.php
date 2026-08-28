<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

App::view()->add("script", "Poll.poll");
App::hook("poll.last-active-poll", function () {
    $poll = Connection::fetchAssoc("select * \r\n        from mw_polls\r\n        where active = 1\r\n        and convert(date, getdate()) >= start_date\r\n        and (end_date is null or convert(date, getdate()) <= end_date)\r\n        order by id desc\r\n    ");
    $voted = false;
    if (!empty($poll)) {
        $anwsers = Connection::fetchAll("select * \r\n            from mw_polls_answers\r\n            where poll_id = ?\r\n            order by sequence\r\n        ", array($poll["id"]), array("integer"));
        $total = 0;
        foreach ($anwsers as $i => &$answer) {
            $persons = Connection::fetchColumn("select count(1) \r\n                from mw_polls_answers_persons\r\n                where answer_id = ?\r\n            ", array($answer["id"]));
            $total += $persons;
            $answer["votes"] = $persons;
            if (!$poll["allow_ips"]) {
                if ($poll["only_logged_in"] && logged_in()) {
                    $exists = Connection::fetchAssoc("select * \r\n                        from mw_polls_answers_persons\r\n                        where answer_id = ? \r\n                        and ip = ?\r\n                        and account = ?\r\n                    ", array($answer["id"], env("REMOTE_ADDR"), user()->getUsername()), array("integer", "string", "string"));
                } else {
                    $exists = Connection::fetchAssoc("select * \r\n                        from mw_polls_answers_persons\r\n                        where answer_id = ? \r\n                        and ip = ?\r\n                    ", array($answer["id"], env("REMOTE_ADDR")), array("integer", "string"));
                }
                if (!$voted) {
                    $voted = !empty($exists);
                }
            }
        }
        $poll["total"] = $total;
        $poll["answers"] = $anwsers;
    }
    View::display("Poll.poll", array("layout" => false, "poll" => $poll, "voted" => $voted));
});
Route::post("/polls/vote/:poll", function ($id) {
    $poll = Connection::fetchAssoc("select * \r\n        from mw_polls \r\n        where id = ?\r\n        and active = 1\r\n        and convert(date, getdate()) >= start_date\r\n        and (end_date is null or convert(date, getdate()) <= end_date)\r\n    ", array($id), array("integer"));
    if (empty($poll)) {
        return error(__("Invalid poll"));
    }
    $answers = Input::post("answer");
    if (empty($answers)) {
        return error(__("Select your vote"));
    }
    if ($poll["only_logged_in"] && !logged_in()) {
        return error(__("You need logged in to vote"));
    }
    if (!is_array($answers)) {
        $answers = array($answers);
    }
    foreach ($answers as $answer) {
        $exists = Connection::fetchAssoc("select * \r\n        from mw_polls_answers_persons\r\n        where " . (logged_in() ? "account" : "ip") . " = ?\r\n        and answer_id = ?\r\n    ", array(logged_in() ? user()->getUsername() : env("REMOTE_ADDR"), $answer), array("string", "integer"));
        if ($poll["allow_ips"] || empty($exists)) {
            $data = array("answer_id" => $answer, "ip" => env("REMOTE_ADDR"));
            $types = array("integer", "string");
            if (logged_in()) {
                $data += array("account" => user()->getUsername());
                $types[] = "string";
            }
            Connection::insert("mw_polls_answers_persons", $data, $types);
        }
    }
    return success("Your vote was computed", array("allow_ips" => $poll["allow_ips"] == 1));
});
Route::get("/polls/result/:poll", function ($id) {
    $poll = Connection::fetchAssoc("select * \r\n        from mw_polls \r\n        where id = ?\r\n        and active = 1\r\n        and convert(date, getdate()) >= start_date\r\n        and (end_date is null or convert(date, getdate()) <= end_date)\r\n    ", array($id), array("integer"));
    if (!empty($poll)) {
        $answers = Connection::fetchAll("select * \r\n            from mw_polls_answers\r\n            where poll_id = ?\r\n            order by sequence\r\n        ", array($poll["id"]), array("integer"));
        $total = 0;
        foreach ($answers as $i => &$answer) {
            $persons = Connection::fetchColumn("select count(1) \r\n                from mw_polls_answers_persons\r\n                where answer_id = ?\r\n            ", array($answer["id"]));
            $total += $persons;
            $answer["votes"] = $persons;
        }
        $poll["total"] = $total;
        $poll["answers"] = $answers;
    }
    View::display("Poll.result", array("poll" => $poll));
});

?>