<?php

// Rotas de Eventos
Route::get("/events", function () {
    $schema = Connection::getSchemaManager();
    
    if (!$schema->tablesExist("mw_event_categories")) {
        $table = new \Doctrine\DBAL\Schema\Table("mw_event_categories");
        $table->addColumn("id", "integer", array("autoincrement" => true));
        $table->addColumn("name", "string", array("length" => 255));
        $table->addColumn("color", "string", array("length" => 50, "default" => "#3bafda", "notnull" => false));
        $table->setPrimaryKey(array("id"));
        $schema->createTable($table);
    } else {
        $columns = $schema->listTableColumns("mw_event_categories");
        if (!array_key_exists('color', $columns) && !array_key_exists('COLOR', $columns)) {
            $tableDiff = new \Doctrine\DBAL\Schema\TableDiff("mw_event_categories");
            $tableDiff->addedColumns["color"] = new \Doctrine\DBAL\Schema\Column("color", \Doctrine\DBAL\Types\Type::getType("string"), array("length" => 50, "default" => "#3bafda", "notnull" => false));
            $schema->alterTable($tableDiff);
        }
    }

    if (!$schema->tablesExist("mw_events")) {
        $table = new \Doctrine\DBAL\Schema\Table("mw_events");
        $table->addColumn("id", "integer", array("autoincrement" => true));
        $table->addColumn("category_id", "integer");
        $table->addColumn("name", "string", array("length" => 255));
        $table->addColumn("days", "string", array("length" => 255, "notnull" => false));
        $table->addColumn("times", "text", array("notnull" => false));
        $table->addColumn("active", "boolean", array("default" => 1));
        $table->setPrimaryKey(array("id"));
        $table->addForeignKeyConstraint($schema->listTableDetails("mw_event_categories"), array("category_id"), array("id"), array("onDelete" => "CASCADE"));
        $schema->createTable($table);
    }

    $events = Connection::fetchAll("SELECT e.*, c.name as category_name FROM mw_events e LEFT JOIN mw_event_categories c ON e.category_id = c.id ORDER BY e.id DESC");
    View::display("../../../plugins/Events/views/admin/events/index", array("events" => $events));
})->name("events.index");

Route::map("/events/add", function () {
    if (Request::isPost()) {
        try {
            Connection::insert("mw_events", array(
                "category_id" => Input::post("category_id"),
                "name" => Input::post("name"),
                "days" => is_array(Input::post("days")) ? json_encode(Input::post("days")) : '[]',
                "times" => Input::post("times"),
                "active" => Input::post("active") == 1 ? 1 : 0
            ));
            return success(__("Event has been added"), array("redirect" => "/admin/events"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    
    $categories = Connection::fetchAll("SELECT * FROM mw_event_categories ORDER BY name ASC");
    View::display("../../../plugins/Events/views/admin/events/form", array("action" => "add", "event" => null, "categories" => $categories));
})->via("GET", "POST")->name("events.add");

Route::map("/events/edit/:id", function ($id) {
    $event = Connection::fetchAssoc("SELECT * FROM mw_events WHERE id = ?", array($id));
    if (!$event) return App::notFound();

    if (Request::isPost()) {
        try {
            Connection::update("mw_events", array(
                "category_id" => Input::post("category_id"),
                "name" => Input::post("name"),
                "days" => is_array(Input::post("days")) ? json_encode(Input::post("days")) : '[]',
                "times" => Input::post("times"),
                "active" => Input::post("active") == 1 ? 1 : 0
            ), array("id" => $id));
            return success(__("Event has been updated"), array("redirect" => "/admin/events"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    
    $categories = Connection::fetchAll("SELECT * FROM mw_event_categories ORDER BY name ASC");
    View::display("../../../plugins/Events/views/admin/events/form", array("action" => "edit", "event" => $event, "categories" => $categories));
})->via("GET", "POST")->name("events.edit");

Route::get("/events/delete/:id", function ($id) {
    try {
        Connection::delete("mw_events", array("id" => $id));
        return success(__("Event has been deleted"), array("redirect" => "/admin/events"));
    } catch (Exception $ex) {
        return error($ex->getMessage());
    }
})->name("events.delete");

// Rotas de Categorias
Route::get("/events/categories", function () {
    $schema = Connection::getSchemaManager();
    
    if (!$schema->tablesExist("mw_event_categories")) {
        $table = new \Doctrine\DBAL\Schema\Table("mw_event_categories");
        $table->addColumn("id", "integer", array("autoincrement" => true));
        $table->addColumn("name", "string", array("length" => 255));
        $table->addColumn("color", "string", array("length" => 50, "default" => "#3bafda", "notnull" => false));
        $table->setPrimaryKey(array("id"));
        $schema->createTable($table);
    } else {
        $columns = $schema->listTableColumns("mw_event_categories");
        if (!array_key_exists('color', $columns) && !array_key_exists('COLOR', $columns)) {
            $tableDiff = new \Doctrine\DBAL\Schema\TableDiff("mw_event_categories");
            $tableDiff->addedColumns["color"] = new \Doctrine\DBAL\Schema\Column("color", \Doctrine\DBAL\Types\Type::getType("string"), array("length" => 50, "default" => "#3bafda", "notnull" => false));
            $schema->alterTable($tableDiff);
        }
    }

    $categories = Connection::fetchAll("SELECT * FROM mw_event_categories ORDER BY id DESC");
    View::display("../../../plugins/Events/views/admin/categories/index", array("categories" => $categories));
})->name("events.categories");

Route::map("/events/categories/add", function () {
    if (Request::isPost()) {
        try {
            Connection::insert("mw_event_categories", array("name" => Input::post("name"), "color" => Input::post("color")));
            return success("Categoria salva com sucesso!", array("redirect" => "/admin/events/categories"));
        } catch (Exception $ex) { return error($ex->getMessage()); }
    }
    View::display("../../../plugins/Events/views/admin/categories/form", array("action" => "add", "category" => null));
})->via("GET", "POST")->name("events.categories.add");

Route::map("/events/categories/edit/:id", function ($id) {
    $category = Connection::fetchAssoc("SELECT * FROM mw_event_categories WHERE id = ?", array($id));
    if (!$category) return App::notFound();
    if (Request::isPost()) {
        try {
            Connection::update("mw_event_categories", array("name" => Input::post("name"), "color" => Input::post("color")), array("id" => $id));
            return success("Categoria alterada com sucesso!", array("redirect" => "/admin/events/categories"));
        } catch (Exception $ex) { return error($ex->getMessage()); }
    }
    View::display("../../../plugins/Events/views/admin/categories/form", array("action" => "edit", "category" => $category));
})->via("GET", "POST")->name("events.categories.edit");

Route::get("/events/categories/delete/:id", function ($id) {
    try { Connection::delete("mw_event_categories", array("id" => $id)); return success("Categoria excluída com sucesso!", array("redirect" => "/admin/events/categories")); } catch (Exception $ex) { return error($ex->getMessage()); }
})->name("events.categories.delete");