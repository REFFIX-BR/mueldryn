<?php

Route::get("/achievements", function () {
    $schema = Connection::getSchemaManager();
    if (!$schema->tablesExist("mw_achievements")) {
        $table = new \Doctrine\DBAL\Schema\Table("mw_achievements");
        $table->addColumn("id", "integer", array("autoincrement" => true));
        $table->addColumn("name", "string", array("length" => 255));
        $table->addColumn("description", "text", array("notnull" => false));
        $table->addColumn("required_amount", "integer", array("default" => 1));
        $table->addColumn("active", "boolean", array("default" => 1));
        $table->addColumn("requirements", "text", array("notnull" => false));
        $table->addColumn("rewards", "text", array("notnull" => false));
        $table->setPrimaryKey(array("id"));
        $schema->createTable($table);
    }

    $achievements = Connection::fetchAll("SELECT * FROM mw_achievements ORDER BY id DESC");
    View::plugin("Achievements", "admin/index", array("achievements" => $achievements));
})->name("achievements.index");

Route::map("/achievements/add", function () {
    if (Request::isPost()) {
        try {
            $requirements = Input::post("requirements", array());
            $rewards = Input::post("rewards", array());
            
            Connection::insert("mw_achievements", array(
                "name" => Input::post("name"),
                "description" => Input::post("description"),
                "required_amount" => (int) Input::post("required_amount"),
                "active" => Input::post("active") == 1 ? 1 : 0,
                "requirements" => json_encode($requirements),
                "rewards" => json_encode($rewards)
            ));
            return success("Conquista criada com sucesso!", array("redirect" => "/admin/achievements"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    View::plugin("Achievements", "admin/form", array("action" => "add", "achievement" => null));
})->via("GET", "POST")->name("achievements.add");

Route::map("/achievements/edit/:id", function ($id) {
    $achievement = Connection::fetchAssoc("SELECT * FROM mw_achievements WHERE id = ?", array($id));
    if (!$achievement) return App::notFound();
    
    if (Request::isPost()) {
        try {
            Connection::update("mw_achievements", array(
                "name" => Input::post("name"),
                "description" => Input::post("description"),
                "required_amount" => (int) Input::post("required_amount"),
                "active" => Input::post("active") == 1 ? 1 : 0,
                "requirements" => json_encode(Input::post("requirements", array())),
                "rewards" => json_encode(Input::post("rewards", array()))
            ), array("id" => $id));
            return success("Conquista atualizada com sucesso!", array("redirect" => "/admin/achievements"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    $achievement["requirements"] = json_decode($achievement["requirements"], true) ?: array();
    $achievement["rewards"] = json_decode($achievement["rewards"], true) ?: array();
    View::plugin("Achievements", "admin/form", array("action" => "edit", "achievement" => $achievement));
})->via("GET", "POST")->name("achievements.edit");

Route::get("/achievements/delete/:id", function ($id) {
    try {
        Connection::delete("mw_achievements", array("id" => $id));
        return success("Conquista excluída com sucesso!", array("redirect" => "/admin/achievements"));
    } catch (Exception $ex) {
        return error($ex->getMessage());
    }
})->name("achievements.delete");