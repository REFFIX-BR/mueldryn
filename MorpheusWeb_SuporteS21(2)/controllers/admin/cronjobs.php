<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/cronjobs", function () {
    $jobs = Connection::fetchAll("select * from mw_jobs");
    View::display("cronjobs/index", array("jobs" => $jobs));
})->name("configs.cronjobs");
Route::map("/cronjobs/add", function () {
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED), "minutes" => array(Morpheus\Core\Validation::REQUIRED), "hours" => array(Morpheus\Core\Validation::REQUIRED), "days" => array(Morpheus\Core\Validation::REQUIRED), "weeks" => array(Morpheus\Core\Validation::REQUIRED), "months" => array(Morpheus\Core\Validation::REQUIRED), "script_file" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                Connection::insert("mw_jobs", array("name" => Input::post("name"), "minutes" => Input::post("minutes"), "hours" => Input::post("hours"), "days" => Input::post("days"), "weeks" => Input::post("weeks"), "months" => Input::post("months"), "script_file" => Input::post("script"), "active" => false), array("string", "string", "string", "string", "string", "string", "string"));
                return success(__("Cronjob has been saved"), array("redirect" => "/admin/cronjobs"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("cronjobs/add", array("tasks" => gettasks()));
})->via("GET", "POST")->name("configs.cronjogs.add");
Route::map("/cronjobs/edit/:id", function ($id) {
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED), "minutes" => array(Morpheus\Core\Validation::REQUIRED), "hours" => array(Morpheus\Core\Validation::REQUIRED), "days" => array(Morpheus\Core\Validation::REQUIRED), "weeks" => array(Morpheus\Core\Validation::REQUIRED), "months" => array(Morpheus\Core\Validation::REQUIRED), "script_file" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                Connection::update("mw_jobs", array("name" => Input::post("name"), "minutes" => Input::post("minutes"), "hours" => Input::post("hours"), "days" => Input::post("days"), "weeks" => Input::post("weeks"), "months" => Input::post("months"), "script_file" => Input::post("script")), array("id" => $id), array("string", "string", "string", "string", "string", "string", "string"));
                return success(__("Cronjob has been updated"), array("redirect" => "/admin/cronjobs/edit/" . $id));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    $job = Connection::fetchAssoc("SELECT * FROM mw_jobs WHERE id = ?", array($id));
    View::display("cronjobs/edit", array("job" => $job, "tasks" => gettasks()));
})->via("GET", "POST")->name("configs.cronjogs.edit");
Route::get("/cronjobs/deactive/:job", function ($id) {
    $job = Connection::fetchAssoc("SELECT * FROM mw_jobs WHERE id = ?", array($id));
    try {
        Connection::update("mw_jobs", array("active" => 0), array("id" => $id));
        return success(__("CronJob has been deactivated"), array("redirect" => "/admin/cronjobs"));
    } catch (Exception $ex) {
        return error($ex->getMessage(), array("redirect" => "/admin/cronjobs"));
    }
})->name("configs.cronjobs.deactive");
Route::get("/cronjobs/active/:job", function ($id) {
    $job = Connection::fetchAssoc("SELECT * FROM mw_jobs WHERE id = ?", array($id));
    try {
        Connection::update("mw_jobs", array("active" => 1), array("id" => $id));
        return success(__("CronJob has been activated"), array("redirect" => "/admin/cronjobs"));
    } catch (Exception $ex) {
        return error($ex->getMessage(), array("redirect" => "/admin/cronjobs"));
    }
})->name("configs.cronjobs.active");
function getTasks()
{
    $tasks = array();
    foreach (new DirectoryIterator(ROOT . DS . "tasks") as $file) {
        if ($file->isFile()) {
            $task = str_replace(".php", "", $file->getFilename());
            $tasks["Default"][$task] = $task;
        }
    }
    foreach (Morpheus\Core\Plugin::getInstance()->getPlugins() as $plugin) {
        if (plugin_active($plugin)) {
            $path = PLUGIN_PATH . DS . $plugin . DS . "tasks";
            if (is_dir($path)) {
                foreach (new DirectoryIterator(PLUGIN_PATH . DS . $plugin . DS . "tasks") as $file) {
                    if ($file->isFile()) {
                        $task = str_replace(".php", "", $file->getFilename());
                        $tasks[$plugin][$plugin . "." . $task] = $task;
                    }
                }
            }
        }
    }
    return $tasks;
}

?>