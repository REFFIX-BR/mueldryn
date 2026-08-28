<?php

Route::get("/marketchars", function () {
    $charsForSale = Connection::fetchAll("SELECT * FROM mw_market_chars ORDER BY id DESC");
    $coinsConfig = config("coins", []);
    
    $charsData = [];
    foreach ($charsForSale as $char) {
        $charObj = new \Morpheus\Character\Character($char['character_name']);
        if ($charObj->exists()) {
            $char['Class'] = $charObj->getClass();
            $charsData[] = $char;
        } else {
            $char['Class'] = null;
            $charsData[] = $char;
        }
    }
    
    View::display("marketchars/index", ["chars" => $charsData, "coins" => $coinsConfig]);
})->name("admin.marketchars");

Route::post("/marketchars/delete/:id", function ($id) {
    $listing = Connection::fetchAssoc("SELECT * FROM mw_market_chars WHERE id = ?", [$id]);
    
    if (!$listing) {
        return error("Anúncio não encontrado.");
    }
    
    try {
        Connection::transactional(function () use ($listing) {
            // Desbloqueia o personagem para voltar a ser usado
            Connection::update("Character", ["CtlCode" => 0], ["Name" => $listing['character_name'], "AccountID" => $listing['seller']]);
            // Remove do mercado
            Connection::delete("mw_market_chars", ["id" => $listing['id']]);
        });
        return success("Anúncio excluído e personagem devolvido ao vendedor com sucesso!", ["redirect" => "/admin/marketchars"]);
    } catch (Exception $ex) {
        return error("Erro ao excluir: " . $ex->getMessage());
    }
})->name("admin.marketchars.delete");
