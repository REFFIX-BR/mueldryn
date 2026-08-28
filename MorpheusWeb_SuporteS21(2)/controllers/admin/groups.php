<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/groups", function () {
    $groups = Connection::fetchAll("SELECT * FROM mw_user_groups");
    View::display("groups/index", array("groups" => $groups));
})->name("groups");
Route::map("/groups/add", function () {
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                $access = Input::post("access");
                Connection::insert("mw_user_groups", array("name" => Input::post("name"), "access" => empty($access) ? "" : join(",", $access)));
                return success(__("Group has been saved"), array("redirect" => "/admin/groups"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    $routes = array();
    $exists = array();
    foreach (App::router()->getNamedRoutes() as $name => $route) {
        if (empty($name)) {
            continue;
        }
        $r = explode("|", $name);
        if (!in_array($r[0], $exists)) {
            $routes[] = array("title" => isset($r[1]) ? $r[1] : $r[0], "route" => $r[0]);
            $exists[] = $r[0];
        }
    }
    View::display("groups/add", array("routes" => Morpheus\Util\Hash::sort($routes, "{n}.route", "asc")));
})->via("POST", "GET")->name("groups.add");
Route::map("/groups/edit/:id", function ($id) {
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                $access = Input::post("access");
                Connection::update("mw_user_groups", array("name" => Input::post("name"), "access" => empty($access) ? "" : join(",", $access)), array("id" => $id), array("string", "string", "integer"));
                return success(__("Group has been udpated"), array("redirect" => "/admin/groups/edit/" . $id));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    $routes = array();
    $exists = array();
    foreach (App::router()->getNamedRoutes() as $name => $route) {
        if (empty($name)) {
            continue;
        }
        $r = explode("|", $name);
        if (!in_array($r[0], $exists)) {
            $routes[] = array("title" => isset($r[1]) ? $r[1] : $r[0], "route" => $r[0]);
            $exists[] = $r[0];
        }
    }
    $group = Connection::fetchAssoc("SELECT * FROM mw_user_groups WHERE id = :id", array("id" => $id), array("integer"));
    $group["access"] = explode(",", $group["access"]);
    View::display("groups/edit", array("routes" => Morpheus\Util\Hash::sort($routes, "{n}.route", "asc"), "group" => $group));
})->via("GET", "POST")->name("groups.edit");
Route::get("/groups/delete/:id", function ($id) {
    try {
        Connection::delete("mw_user_groups", array("id" => $id));
        return success(__("Group has been deleted"), array("redirect" => "/admin/groups"));
    } catch (Exception $ex) {
        return error($ex, array("redirect" => "/admin/groups"));
    }
})->name("groups.delete");

?>