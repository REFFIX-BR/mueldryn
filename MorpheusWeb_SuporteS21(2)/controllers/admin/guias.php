<?php

// ==============================
// ROTAS DE CATEGORIAS
// ==============================
Route::map("/guias/categories", function () {
    $schema = Connection::getSchemaManager();
    if (!$schema->tablesExist("mw_guides_categories")) {
        $table = new \Doctrine\DBAL\Schema\Table("mw_guides_categories");
        $table->addColumn("id", "integer", array("autoincrement" => true));
        $table->addColumn("name", "string", array("length" => 255));
        $table->addColumn("active", "boolean", array("default" => 1));
        $table->setPrimaryKey(array("id"));
        $schema->createTable($table);
    }

    if (Request::isPost()) {
        $name = Input::post("name");
        $active = Input::post("active") ? 1 : 0;
        
        Connection::insert("mw_guides_categories", [
            "name" => $name,
            "active" => $active
        ]);
        
        return success("Categoria criada com sucesso!", array("redirect" => "/admin/guias/categories"));
    }
    
    $categories = Connection::fetchAll("SELECT * FROM mw_guides_categories ORDER BY id DESC");
    View::display("../../../plugins/Guias/views/admin/categories", ["categories" => $categories]);
})->via("GET", "POST")->name("guias.categories");

Route::get("/guias/categories/toggle/:id", function ($id) {
    $category = Connection::fetchAssoc("SELECT * FROM mw_guides_categories WHERE id = ?", [$id]);
    if ($category) {
        $newStatus = $category['active'] ? 0 : 1;
        Connection::update("mw_guides_categories", ["active" => $newStatus], ["id" => $id]);
        return success("Status alterado com sucesso!", array("redirect" => "/admin/guias/categories"));
    }
    return error("Categoria não encontrada.");
})->name("guias.categories.toggle");

Route::get("/guias/categories/delete/:id", function ($id) {
    try {
        Connection::delete("mw_guides_categories", array("id" => $id));
        return success("Categoria excluída com sucesso!", array("redirect" => "/admin/guias/categories"));
    } catch (Exception $ex) {
        return error($ex->getMessage());
    }
})->name("guias.categories.delete");

Route::map("/guias/categories/edit/:id", function ($id) {
    $category = Connection::fetchAssoc("SELECT * FROM mw_guides_categories WHERE id = ?", array($id));
    if (!$category) return App::notFound();

    if (Request::isPost()) {
        try {
            Connection::update("mw_guides_categories", array(
                "name" => Input::post("name"),
                "active" => Input::post("active") ? 1 : 0
            ), array("id" => $id));
            return success("Categoria editada com sucesso!", array("redirect" => "/admin/guias/categories"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    View::display("../../../plugins/Guias/views/admin/categories_edit", array("category" => $category));
})->via("GET", "POST")->name("guias.categories.edit");

// ==============================
// ROTAS DE GUIAS
// ==============================
Route::map("/guias/guides", function () {
    $schema = Connection::getSchemaManager();
    if (!$schema->tablesExist("mw_guides")) {
        $table = new \Doctrine\DBAL\Schema\Table("mw_guides");
        $table->addColumn("id", "integer", array("autoincrement" => true));
        $table->addColumn("category_id", "integer");
        $table->addColumn("title", "string", array("length" => 255));
        $table->addColumn("content", "text");
        $table->addColumn("active", "boolean", array("default" => 1));
        $table->setPrimaryKey(array("id"));
        $schema->createTable($table);
    }

    if (Request::isPost()) {
        Connection::insert("mw_guides", [
            "title" => Input::post("title"),
            "category_id" => Input::post("category_id"),
            "content" => Input::post("content"),
            "active" => Input::post("active") ? 1 : 0
        ]);
        
        return success("Guia criado com sucesso!", array("redirect" => "/admin/guias/guides"));
    }
    
    $guides = Connection::fetchAll("
        SELECT g.*, c.name as category_name 
        FROM mw_guides g 
        LEFT JOIN mw_guides_categories c ON g.category_id = c.id 
        ORDER BY g.id DESC
    ");
    $categories = Connection::fetchAll("SELECT * FROM mw_guides_categories WHERE active = 1");
    
    View::display("../../../plugins/Guias/views/admin/guides", [
        "guides" => $guides,
        "categories" => $categories
    ]);
})->via("GET", "POST")->name("guias.guides");

Route::get("/guias/guides/toggle/:id", function ($id) {
    $guide = Connection::fetchAssoc("SELECT * FROM mw_guides WHERE id = ?", [$id]);
    if ($guide) {
        $newStatus = $guide['active'] ? 0 : 1;
        Connection::update("mw_guides", ["active" => $newStatus], ["id" => $id]);
        return success("Status alterado com sucesso!", array("redirect" => "/admin/guias/guides"));
    }
    return error("Guia não encontrado.");
})->name("guias.guides.toggle");

Route::get("/guias/guides/delete/:id", function ($id) {
    try {
        Connection::delete("mw_guides", array("id" => $id));
        return success("Guia excluído com sucesso!", array("redirect" => "/admin/guias/guides"));
    } catch (Exception $ex) {
        return error($ex->getMessage());
    }
})->name("guias.guides.delete");

Route::map("/guias/guides/edit/:id", function ($id) {
    $guide = Connection::fetchAssoc("SELECT * FROM mw_guides WHERE id = ?", array($id));
    if (!$guide) return App::notFound();

    if (Request::isPost()) {
        try {
            Connection::update("mw_guides", array(
                "title" => Input::post("title"),
                "category_id" => Input::post("category_id"),
                "content" => Input::post("content"),
                "active" => Input::post("active") ? 1 : 0
            ), array("id" => $id));
            return success("Guia editado com sucesso!", array("redirect" => "/admin/guias/guides"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    
    $categories = Connection::fetchAll("SELECT * FROM mw_guides_categories WHERE active = 1");
    View::display("../../../plugins/Guias/views/admin/guides_edit", array("guide" => $guide, "categories" => $categories));
})->via("GET", "POST")->name("guias.guides.edit");
