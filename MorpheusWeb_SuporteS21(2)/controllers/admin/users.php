<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::map("/login", function () {
    if (Request::isPost()) {
        if (Morpheus\Admin\Auth::login(Input::post("username"), Input::post("password"))) {
            App::redirect("/admin");
        } else {
            View::set("error", __("User and/or password are incorrect"));
        }
    }
    View::display("users/login", array("layout" => "login"));
})->via("GET", "POST");
Route::get("/logout", function () {
    Morpheus\Admin\Auth::logout();
    App::redirect("/admin/login");
});
Route::get("/users", function () {
    $users = Connection::fetchAll("SELECT u.*,\n        g.name AS grupo\n        FROM mw_users u\n        LEFT JOIN mw_user_groups g ON (g.id = u.group_id)\n    ");
    View::display("users/index", array("users" => $users));
})->name("users");
Route::map("/users/add", function () {
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED), "email" => array(__("Invalid email format") => array("rule" => "email", "allowEmpty" => true)), "username" => array(Morpheus\Core\Validation::REQUIRED), "password" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                Connection::insert("mw_users", array("name" => Input::post("name"), "email" => Input::post("email"), "username" => Input::post("username"), "password" => md5(Input::post("password")), "super_user" => Input::post("super_user") == 1, "group_id" => Input::post("group_id")), array("string", "string", "string", "string", "integer", "integer"));
                return success(__("User has been saved"), array("redirect" => "/admin/users"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    $groups = Connection::fetchAll("SELECT * FROM mw_user_groups");
    View::display("users/add", array("groups" => $groups));
})->via("GET", "POST")->name("users.add");
Route::map("/users/edit/:id", function ($id) {
    $user = Connection::fetchAssoc("SELECT *\n        FROM mw_users\n        WHERE id = ?\n    ", array($id));
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED), "email" => array(__("Invalid email format") => array("rule" => "email", "allowEmpty" => true)), "username" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                $password = Input::post("password");
                Connection::update("mw_users", array("name" => Input::post("name"), "email" => Input::post("email"), "username" => Input::post("username"), "password" => !empty($password) ? md5($password) : $user["password"], "super_user" => Input::post("super_user") == 1, "group_id" => Input::post("group_id")), array("id" => $id), array("string", "string", "string", "string", "integer", "integer", "integer"));
                return success(__("User has been updated"), array("redirect" => "/admin/users/edit/" . $id));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    $groups = Connection::fetchAll("SELECT * FROM mw_user_groups");
    View::display("users/edit", array("user" => $user, "groups" => $groups));
})->via("GET", "POST")->name("users.edit");
Route::get("/users/delete/:id", function ($id) {
    try {
        Connection::delete("mw_users", array("id" => $id));
        return success(__("User has been deleted"), array("redirect" => "/admin/users"));
    } catch (Exception $ex) {
        return error($ex, array("redirect" => "/admin/users"));
    }
})->name("users.delete");

?>