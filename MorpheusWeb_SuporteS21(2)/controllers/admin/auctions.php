<?php

Route::get("/auctions", function () {
    // Lógica para listar os leilões principais
})->name("auctions.index");

Route::map("/auctions/edit/:id", function ($id) {
    $auction = Connection::fetchAssoc("SELECT * FROM auctions WHERE id = ?", array($id));
    
    // Se o leilão ainda não existir no banco, criamos um evento padrão automaticamente
    if (!$auction) {
        Connection::insert("auctions", array(
            "start_date" => date('Y-m-d H:i:s'),
            "end_date" => date('Y-m-d H:i:s', strtotime('+7 days')),
            "active" => 1
        ));
        $auction = Connection::fetchAssoc("SELECT * FROM auctions WHERE id = ?", array($id));
    }

    if (Request::isPost()) {
        try {
            Connection::update("auctions", array(
                "start_date" => Input::post("start_date"),
                "end_date" => Input::post("end_date"),
                "active" => Input::post("active") == 1 ? 1 : 0
            ), array("id" => $id));
            return success("Datas do leilão salvas com sucesso!", array("redirect" => "/admin/auctions/edit/" . $id));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    
    View::display("../../../plugins/Auction/views/admin/form", array("auction" => $auction));
})->via("GET", "POST")->name("auctions.edit");

// Esta rota atende '/auctions/items' usada pela view que você já possui
Route::get("/auctions/items", function () {
    // Criando as tabelas dinamicamente (Compatível com SQL Server e MySQL)
    $schema = Connection::getSchemaManager();
    
    if (!$schema->tablesExist("auctions")) {
        $table = new \Doctrine\DBAL\Schema\Table("auctions");
        $table->addColumn("id", "integer", array("autoincrement" => true));
        $table->addColumn("start_date", "datetime");
        $table->addColumn("end_date", "datetime");
        $table->addColumn("active", "boolean", array("default" => 1, "notnull" => false));
        $table->setPrimaryKey(array("id"));
        $schema->createTable($table);
    }

    if (!$schema->tablesExist("auction_items")) {
        $table = new \Doctrine\DBAL\Schema\Table("auction_items");
        $table->addColumn("id", "integer", array("autoincrement" => true));
        $table->addColumn("auction_id", "integer");
        $table->addColumn("item", "string", array("length" => 255));
        $table->addColumn("winner", "string", array("length" => 50, "notnull" => false));
        $table->addColumn("winner_date", "datetime", array("notnull" => false));
        $table->addColumn("delivery", "boolean", array("default" => 0, "notnull" => false));
        $table->addColumn("active", "boolean", array("default" => 1, "notnull" => false));
        $table->addColumn("can_delivery", "boolean", array("default" => 0, "notnull" => false));
        $table->addColumn("initial_bid", "integer", array("default" => 0, "notnull" => false));
        $table->addColumn("end_date", "datetime", array("notnull" => false));
        $table->addColumn("image", "string", array("length" => 255, "notnull" => false));
        $table->addColumn("coin", "string", array("length" => 50, "notnull" => false));
        $table->setPrimaryKey(array("id"));
        $table->addIndex(array("auction_id"));
        $schema->createTable($table);
    } else {
        // Se a tabela já existir, adicionamos as colunas novas caso faltem
        $columns = $schema->listTableColumns("auction_items");
        $tableDiff = new \Doctrine\DBAL\Schema\TableDiff("auction_items");
        $changed = false;
        
        if (!array_key_exists('end_date', $columns) && !array_key_exists('END_DATE', $columns)) {
            $tableDiff->addedColumns["end_date"] = new \Doctrine\DBAL\Schema\Column("end_date", \Doctrine\DBAL\Types\Type::getType("datetime"), array("notnull" => false));
            $changed = true;
        }
        if (!array_key_exists('image', $columns) && !array_key_exists('IMAGE', $columns)) {
            $tableDiff->addedColumns["image"] = new \Doctrine\DBAL\Schema\Column("image", \Doctrine\DBAL\Types\Type::getType("string"), array("length" => 255, "notnull" => false));
            $changed = true;
        }
        if (!array_key_exists('coin', $columns) && !array_key_exists('COIN', $columns)) {
            $tableDiff->addedColumns["coin"] = new \Doctrine\DBAL\Schema\Column("coin", \Doctrine\DBAL\Types\Type::getType("string"), array("length" => 50, "notnull" => false));
            $changed = true;
        }
        if ($changed) {
            $schema->alterTable($tableDiff);
        }
    }

    $auctionId = Input::get("auction_id");
    if (!$auctionId) {
        $auctionId = 1;
    }
    
    $auction = array(
        "id" => $auctionId
    );
    
    // Buscando os itens reais no banco de dados do MuWeb
    $items = Connection::fetchAll("SELECT * FROM auction_items WHERE auction_id = ?", array($auctionId));
    
    // Chama a view injetando as variáveis
    View::display("../../../plugins/Auction/views/admin/items/index", array("auction" => $auction, "items" => $items));
})->name("auctions.items");

// Rotas adicionais de acordo com os botões da sua View
Route::map("/auctions/items/add", function () {
    $auctionId = Input::get("auction_id");
    if (!$auctionId) {
        $auctionId = 1;
    }

    if (Request::isPost()) {
        try {
            Connection::insert("auction_items", array(
                "auction_id" => $auctionId,
                "item" => Input::post("item"),
                "image" => Input::post("image"),
                "initial_bid" => (int) Input::post("initial_bid"),
                "coin" => Input::post("coin"),
                "end_date" => Input::post("end_date"),
                "active" => Input::post("active") == 1 ? 1 : 0
            ));
            return success("Item adicionado com sucesso!", array("redirect" => "/admin/auctions/items?auction_id=" . $auctionId));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    
    View::display("../../../plugins/Auction/views/admin/items/form", array("action" => "add", "item" => null, "auction_id" => $auctionId));
})->via("GET", "POST")->name("auctions.items.add");

Route::map("/auctions/items/edit/:id", function ($id) {
    $item = Connection::fetchAssoc("SELECT * FROM auction_items WHERE id = ?", array($id));
    if (!$item) return App::notFound();

    if (Request::isPost()) {
        try {
            Connection::update("auction_items", array(
                "item" => Input::post("item"),
                "image" => Input::post("image"),
                "initial_bid" => (int) Input::post("initial_bid"),
                "coin" => Input::post("coin"),
                "end_date" => Input::post("end_date"),
                "active" => Input::post("active") == 1 ? 1 : 0
            ), array("id" => $id));
            return success("Item atualizado com sucesso!", array("redirect" => "/admin/auctions/items?auction_id=" . $item["auction_id"]));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    
    View::display("../../../plugins/Auction/views/admin/items/form", array("action" => "edit", "item" => $item, "auction_id" => $item["auction_id"]));
})->via("GET", "POST")->name("auctions.items.edit");

Route::get("/auctions/items/delete/:id", function ($id) {
    $item = Connection::fetchAssoc("SELECT * FROM auction_items WHERE id = ?", array($id));
    try {
        Connection::delete("auction_items", array("id" => $id));
        return success("Item excluído com sucesso!", array("redirect" => "/admin/auctions/items?auction_id=" . $item["auction_id"]));
    } catch (Exception $ex) {
        return error($ex->getMessage());
    }
})->name("auctions.items.delete");

Route::get("/auctions/items/delivery/:id", function ($id) {
    $item = Connection::fetchAssoc("SELECT * FROM auction_items WHERE id = ?", array($id));
    if (!$item) return App::notFound();

    if ($item['delivery'] == 1) {
        return error("Este item já foi entregue!");
    }

    if (empty($item['winner'])) {
        return error("Não há vencedor para este item.");
    }

    try {
        Connection::transactional(function () use ($item) {
            $account = new \Morpheus\Account\Account($item['winner']);
            
            if (!$account->exists()) {
                throw new Exception("Conta do vencedor não encontrada.");
            }
            
            if ($account->isConnected()) {
                throw new Exception("O jogador precisa estar offline para receber o item.");
            }

            $warehouse = $account->getWarehouse();
            $warehouse->load();
            
            $itemObj = new \Morpheus\Item\Item(str_pad($item['item'], 32, '0'));
            $itemObj->parse();
            
            if (!$itemObj->isHexEmpty()) {
                $added = $warehouse->addItem($itemObj);
                if (!$added) {
                    throw new Exception("Não há espaço disponível no baú do vencedor.");
                }
                $warehouse->update();
            } else {
                throw new Exception("O código Hex do item é inválido.");
            }
            
            Connection::update("auction_items", array("delivery" => 1, "can_delivery" => 0), array("id" => $item['id']));
        });

        return success("Item entregue ao baú do vencedor com sucesso!", array("redirect" => "/admin/auctions/items?auction_id=" . $item["auction_id"]));
    } catch (Exception $ex) {
        return error($ex->getMessage());
    }
})->name("auctions.items.delivery");

Route::map("/auctions/settings", function () {
    if (Request::isPost()) {
        try {
            $playerAuctions = Input::post("player_auctions") == 1 ? 1 : 0;
            $maxPlayerAuctions = (int) Input::post("max_player_auctions");
            $minDuration = (int) Input::post("min_duration");
            $maxDuration = (int) Input::post("max_duration");
            
            $currentConfig = config();
            $currentConfig['auction.player_auctions'] = $playerAuctions;
            $currentConfig['auction.max_player_auctions'] = $maxPlayerAuctions;
            $currentConfig['auction.min_duration'] = $minDuration;
            $currentConfig['auction.max_duration'] = $maxDuration;
            
            \Morpheus\Core\Config::save($currentConfig);
            
            return success("Configurações salvas com sucesso!", array("redirect" => "/admin/auctions/settings"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    
    View::display("../../../plugins/Auction/views/admin/settings");
})->via("GET", "POST")->name("auctions.settings");

Route::get("/auctions/player", function () {
    $schema = Connection::getSchemaManager();
    
    if (!$schema->tablesExist("player_auctions")) {
        $table = new \Doctrine\DBAL\Schema\Table("player_auctions");
        $table->addColumn("id", "integer", array("autoincrement" => true));
        $table->addColumn("seller", "string", array("length" => 50));
        $table->addColumn("item_hex", "string", array("length" => 2000));
        $table->addColumn("item_name", "string", array("length" => 255));
        $table->addColumn("start_price", "integer");
        $table->addColumn("current_price", "integer", array("default" => 0));
        $table->addColumn("current_bidder", "string", array("length" => 50, "notnull" => false));
        $table->addColumn("coin", "string", array("length" => 50));
        $table->addColumn("start_date", "datetime");
        $table->addColumn("end_date", "datetime");
        $table->addColumn("status", "string", array("length" => 20, "default" => "active"));
        $table->addColumn("winner", "string", array("length" => 50, "notnull" => false));
        $table->addColumn("delivered", "boolean", array("default" => 0, "notnull" => false));
        $table->setPrimaryKey(array("id"));
        $table->addIndex(array("seller"));
        $table->addIndex(array("status"));
        $schema->createTable($table);
    }
    
    $auctions = Connection::fetchAll("SELECT pa.*, m.memb_name as seller_name, m2.memb_name as winner_name 
        FROM player_auctions pa 
        LEFT JOIN MEMB_INFO m ON pa.seller = m.memb___id 
        LEFT JOIN MEMB_INFO m2 ON pa.winner = m2.memb___id
        ORDER BY pa.id DESC");
    
    View::display("../../../plugins/Auction/views/admin/player_auctions", array("auctions" => $auctions));
})->name("auctions.player");

Route::get("/auctions/player/delivery/:id", function ($id) {
    $auction = Connection::fetchAssoc("SELECT * FROM player_auctions WHERE id = ?", array($id));
    if (!$auction) return App::notFound();
    
    if ($auction['delivered'] == 1) {
        return error("Este item já foi entregue!");
    }
    
    if (empty($auction['winner'])) {
        return error("Não há vencedor para este item.");
    }
    
    if ($auction['status'] !== 'completed' && strtotime($auction['end_date']) > time()) {
        return error("O leilão ainda não foi encerrado.");
    }
    
    try {
        Connection::transactional(function () use ($auction) {
            if ($auction['status'] === 'active') {
                Connection::update("player_auctions", array("status" => "completed"), array("id" => $auction['id']));
            }
            
            $account = new \Morpheus\Account\Account($auction['winner']);
            
            if (!$account->exists()) {
                throw new Exception("Conta do vencedor não encontrada.");
            }
            
            if ($account->isConnected()) {
                throw new Exception("O jogador precisa estar offline para receber o item.");
            }
            
            $warehouse = $account->getWarehouse();
            $warehouse->load();
            
            $itemObj = new \Morpheus\Item\Item($auction['item_hex']);
            $itemObj->parse();
            
            if (!$itemObj->isHexEmpty()) {
                $added = $warehouse->addItem($itemObj);
                if (!$added) {
                    throw new Exception("Não há espaço disponível no baú do vencedor.");
                }
                $warehouse->update();
            } else {
                throw new Exception("O código Hex do item é inválido.");
            }
            
            $seller = new \Morpheus\Account\Account($auction['seller']);
            if ($seller->exists()) {
                if ($auction['coin'] === 'credits') {
                    $seller->addCredit($auction['current_price']);
                    $seller->update();
                } else {
                    $coinConfig = config("coins", array());
                    if (!empty($coinConfig) && isset($coinConfig[$auction['coin']])) {
                        $coinData = $coinConfig[$auction['coin']];
                        $cTable = $coinData["table"];
                        $cColumn = $coinData["column"];
                        $cKey = $coinData["foreign_key"];
                        
                        Connection::executeUpdate("UPDATE {$cTable} SET {$cColumn} = {$cColumn} + ? WHERE {$cKey} = ?", array($auction['current_price'], $auction['seller']));
                    }
                }
            }
            
            Connection::update("player_auctions", array("delivered" => 1), array("id" => $auction['id']));
        });
        
        return success("Item entregue ao vencedor e pagamento enviado ao vendedor!", array("redirect" => "/admin/auctions/player"));
    } catch (Exception $ex) {
        return error($ex->getMessage());
    }
})->name("auctions.player.delivery");

Route::get("/auctions/player/cancel/:id", function ($id) {
    $auction = Connection::fetchAssoc("SELECT * FROM player_auctions WHERE id = ?", array($id));
    if (!$auction) return App::notFound();
    
    if ($auction['status'] !== 'active') {
        return error("Este leilão não pode ser cancelado.");
    }
    
    try {
        Connection::transactional(function () use ($auction) {
            $seller = new \Morpheus\Account\Account($auction['seller']);
            if ($seller->exists()) {
                $warehouse = $seller->getWarehouse();
                $warehouse->load();
                
                $itemObj = new \Morpheus\Item\Item($auction['item_hex']);
                $itemObj->parse();
                
                if (!$itemObj->isHexEmpty()) {
                    $added = $warehouse->addItem($itemObj);
                    if (!$added) {
                        throw new Exception("Não há espaço disponível no baú do vendedor para devolver o item.");
                    }
                    $warehouse->update();
                }
            }
            
            if (!empty($auction['current_bidder'])) {
                if ($auction['coin'] === 'credits') {
                    $bidder = new \Morpheus\Account\Account($auction['current_bidder']);
                    if ($bidder->exists()) {
                        $bidder->addCredit($auction['current_price']);
                        $bidder->update();
                    }
                } else {
                    $coinConfig = config("coins", array());
                    if (!empty($coinConfig) && isset($coinConfig[$auction['coin']])) {
                        $coinData = $coinConfig[$auction['coin']];
                        $cTable = $coinData["table"];
                        $cColumn = $coinData["column"];
                        $cKey = $coinData["foreign_key"];
                        
                        Connection::executeUpdate("UPDATE {$cTable} SET {$cColumn} = {$cColumn} + ? WHERE {$cKey} = ?", array($auction['current_price'], $auction['current_bidder']));
                    }
                }
            }
            
            Connection::update("player_auctions", array("status" => "cancelled"), array("id" => $auction['id']));
        });
        
        return success("Leilão cancelado! Item devolvido ao vendedor e lance devolvido ao comprador.", array("redirect" => "/admin/auctions/player"));
    } catch (Exception $ex) {
        return error($ex->getMessage());
    }
})->name("auctions.player.cancel");