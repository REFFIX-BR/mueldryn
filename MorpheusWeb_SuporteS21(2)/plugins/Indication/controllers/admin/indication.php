<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/indication/goals", function () {
    $goals = Connection::fetchAll("select * from mw_indication_goals");
    View::display("Indication.goals/index", array("goals" => $goals));
})->name("indication.goals");
Route::map("/indication/goals/add", function () {
    if (Request::isPost()) {
        $post = Input::post();
        $validation = new Morpheus\Core\Validation($post, array("goal" => Morpheus\Core\Validation::REQUIRED, "coin" => Morpheus\Core\Validation::REQUIRED, "amount" => Morpheus\Core\Validation::REQUIRED));
        if ($validation->isValid()) {
            try {
                Connection::insert("mw_indication_goals", array("goal" => $post["goal"], "coin" => $post["coin"], "amount" => $post["amount"]));
                return success(__("Indication goal has been saved"), array("redirect" => "/admin/indication/goals"));
            } catch (Exception $ex) {
                return error($ex);
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("Indication.goals/add");
})->via("GET", "POST")->name("indication.goals.add");
Route::map("/indication/goals/edit/:id", function ($id = NULL) {
    $goal = Connection::fetchAssoc("select * from mw_indication_goals where id = ?", array($id));
    if (Request::isPost()) {
        $post = Input::post();
        $validation = new Morpheus\Core\Validation($post, array("goal" => Morpheus\Core\Validation::REQUIRED, "coin" => Morpheus\Core\Validation::REQUIRED, "amount" => Morpheus\Core\Validation::REQUIRED));
        if ($validation->isValid()) {
            try {
                Connection::update("mw_indication_goals", array("goal" => $post["goal"], "coin" => $post["coin"], "amount" => $post["amount"]), array("id" => $id));
                return success(__("Indication goal has been updated"), array("redirect" => "/admin/indication/goals/edit/" . $id));
            } catch (Exception $ex) {
                return error($ex);
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("Indication.goals/edit", array("goal" => $goal));
})->via("GET", "POST")->name("indication.goals.edit");
Route::get("/indication/goals/delete/:id", function ($id) {
    try {
        Connection::delete("mw_indication_goals", array("id" => $id));
        return success(__("Indication goal has been deleted"), array("redirect" => "/admin/indication/goals"));
    } catch (Exception $ex) {
        return error($ex, array("redirect" => "/admin/indication/goals"));
    }
})->name("indication.goals.delete");

?>