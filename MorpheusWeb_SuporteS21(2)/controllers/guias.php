<?php

// Rota para listar as Categorias e Guias
Route::get("/guias", function () {
    // Pega as categorias ativas
    $categories = Connection::fetchAll("SELECT * FROM mw_guides_categories WHERE active = 1 ORDER BY name ASC");
    
    // Pega os guias ativos
    $guides = Connection::fetchAll("SELECT * FROM mw_guides WHERE active = 1 ORDER BY title ASC");

    View::display("guias", [
        "categories" => $categories,
        "guides" => $guides
    ]);
})->name("guias.index");

// Rota para ler um Guia Específico
Route::get("/guias/ler/:id", function ($id) {
    $guide = Connection::fetchAssoc("
        SELECT g.*, c.name as category_name 
        FROM mw_guides g 
        LEFT JOIN mw_guides_categories c ON g.category_id = c.id 
        WHERE g.id = ? AND g.active = 1
    ", array($id));

    if (!$guide) {
        return App::notFound();
    }

    View::display("guias-read", [
        "guide" => $guide
    ]);
})->name("guias.read");