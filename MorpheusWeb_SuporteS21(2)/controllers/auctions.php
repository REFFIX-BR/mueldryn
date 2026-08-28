<?php

Route::map("/panel/auctions", function () {
    // Verifica se o jogador está logado antes de deixar acessar a página
    if (!logged_in()) {
        return App::redirect("/login");
    }

    // Busca o leilão ativo (considerando a data atual)
    $currentDate = date('Y-m-d H:i:s');
    $auction = Connection::fetchAssoc("SELECT TOP 1 * FROM auctions WHERE active = 1 AND start_date <= ? AND end_date >= ? ORDER BY id DESC", array($currentDate, $currentDate));
    
    $items = array();
    if ($auction) {
        // Se houver um leilão ativo, busca os itens vinculados a ele
        $items = Connection::fetchAll("SELECT i.*, m.memb_name as winner_name FROM auction_items i LEFT JOIN MEMB_INFO m ON i.winner = m.memb___id WHERE i.auction_id = ? AND i.active = 1", array($auction['id']));
    }

    // Passa os dados para a view padrão
    View::display("auctions", array("auction" => $auction, "items" => $items));
})->via("GET", "POST")->name("panel.auctions");

Route::map("/panel/auctions/bid/:id", function ($id) {
    if (!logged_in()) {
        return App::redirect("/login");
    }

    $item = Connection::fetchAssoc("SELECT * FROM auction_items WHERE id = ? AND active = 1", array($id));
    if (!$item) return App::notFound();

    if (!empty($item['end_date']) && strtotime($item['end_date']) < time()) {
        return error("Os lances para este item já foram encerrados.");
    }

    $currentDate = date('Y-m-d H:i:s');
    $auction = Connection::fetchAssoc("SELECT TOP 1 * FROM auctions WHERE id = ? AND active = 1 AND start_date <= ? AND end_date >= ?", array($item['auction_id'], $currentDate, $currentDate));
    
    if (!$auction) {
        return error("Este leilão não está mais ativo.");
    }
    
    // O usuário não pode dar lance se ele mesmo já for o vencedor atual
    if ($item['winner'] === user()->getUsername()) {
        return error("Você já é o maior licitante deste item.");
    }

    if (Request::isPost()) {
        $bidAmount = (int) Input::post("bid_amount");
        
        if ($bidAmount <= $item['initial_bid']) {
            return error("Seu lance deve ser maior que o lance atual de " . $item['initial_bid'] . ".");
        }

        try {
            $username = user()->getUsername();
            $coinType = $item['coin'];
            
            if (empty($coinType)) {
                return error("Erro interno: Tipo de moeda não configurado para este item.");
            }

            if ($coinType === 'credits') {
                $currentCoins = user()->getCredit();
                
                if ($currentCoins < $bidAmount) {
                    return error("Você não tem saldo suficiente para dar este lance.");
                }

                Connection::transactional(function () use ($item, $bidAmount, $username) {
                    if (!empty($item['winner'])) {
                        $previousBidder = new \Morpheus\Account\Account($item['winner']);
                        if ($previousBidder->exists()) {
                            $previousBidder->addCredit($item['initial_bid']);
                            $previousBidder->update();
                        }
                    }
                    
                    user()->addCredit(-$bidAmount);
                    user()->update();

                    Connection::update("auction_items", array(
                        "initial_bid" => $bidAmount,
                        "winner" => $username,
                        "winner_date" => date('Y-m-d H:i:s')
                    ), array("id" => $item['id']));
                });
            } else {
                $coinConfig = config("coins", array());
                
                if (empty($coinConfig) || !isset($coinConfig[$coinType])) {
                    return error("Erro interno: Moeda não encontrada na configuração.");
                }

                $coinData = $coinConfig[$coinType];
                $cTable = $coinData["table"];
                $cColumn = $coinData["column"];
                $cKey = $coinData["foreign_key"];

                $currentCoins = (int) Connection::fetchColumn("SELECT {$cColumn} FROM {$cTable} WHERE {$cKey} = ?", array($username));

                if ($currentCoins < $bidAmount) {
                    return error("Você não tem saldo suficiente para dar este lance.");
                }

                Connection::transactional(function () use ($item, $bidAmount, $cTable, $cColumn, $cKey, $username) {
                    if (!empty($item['winner'])) {
                        Connection::executeUpdate("UPDATE {$cTable} SET {$cColumn} = {$cColumn} + ? WHERE {$cKey} = ?", array($item['initial_bid'], $item['winner']));
                    }
                    
                    Connection::executeUpdate("UPDATE {$cTable} SET {$cColumn} = {$cColumn} - ? WHERE {$cKey} = ?", array($bidAmount, $username));

                    Connection::update("auction_items", array(
                        "initial_bid" => $bidAmount,
                        "winner" => $username,
                        "winner_date" => date('Y-m-d H:i:s')
                    ), array("id" => $item['id']));
                });
            }

            return success("Lance efetuado com sucesso!", array("redirect" => "/panel/auctions"));
        } catch (Exception $ex) {
            return error("Erro ao processar o lance: " . $ex->getMessage());
        }
    }

    // Se não for POST (Salvamento), exibe o formulário visual na tela
    View::display("auctions-bid", array("item" => $item));
})->via("GET", "POST")->name("panel.auctions.bid");

Route::map("/panel/auctions/create", function () {
    if (!logged_in()) {
        return App::redirect("/login");
    }

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

    $playerAuctionsEnabled = config("auction.player_auctions", false);
    if (!$playerAuctionsEnabled) {
        return error("O sistema de leilão de players está desativado.");
    }

    if (Request::isPost()) {
        try {
            $slot = Input::post("slot");
            $startPrice = (int) Input::post("start_price");
            $duration = (int) Input::post("duration");
            $coin = Input::post("coin");

            if ($startPrice < 1) {
                return error("O preço inicial deve ser maior que 0.");
            }

            if ($duration < 1 || $duration > 30) {
                return error("A duração deve ser entre 1 e 30 dias.");
            }

            $warehouse = user()->getWarehouse();
            $warehouse->load();
            
            $item = $warehouse->getItemBySlot($slot);
            if (!$item || $item->isHexEmpty()) {
                return error("Item não encontrado no baú.");
            }

            $activeAuctions = Connection::fetchColumn("SELECT COUNT(*) FROM player_auctions WHERE seller = ? AND status = 'active'", array(user()->getUsername()));
            $maxAuctions = config("auction.max_player_auctions", 3);
            if ($activeAuctions >= $maxAuctions) {
                return error("Você já possui " . $maxAuctions . " leilões ativos.");
            }

            Connection::transactional(function () use ($item, $startPrice, $duration, $coin, $warehouse) {
                $warehouse->removeItem($item);
                $warehouse->update();

                Connection::insert("player_auctions", array(
                    "seller" => user()->getUsername(),
                    "item_hex" => $item->getHex(),
                    "item_name" => $item->getName() ?: "Item",
                    "start_price" => $startPrice,
                    "current_price" => $startPrice,
                    "coin" => $coin,
                    "start_date" => date('Y-m-d H:i:s'),
                    "end_date" => date('Y-m-d H:i:s', strtotime("+{$duration} days")),
                    "status" => "active"
                ));
            });

            return success("Item anunciado no leilão com sucesso!", array("redirect" => "/panel/auctions/my"));
        } catch (Exception $ex) {
            return error("Erro ao criar leilão: " . $ex->getMessage());
        }
    }

    $warehouse = user()->getWarehouse();
    $warehouse->load();
    
    $coins = config("coins", array());
    View::display("auctions-create", array("warehouse" => $warehouse, "coins" => $coins));
})->via("GET", "POST")->name("panel.auctions.create");

Route::get("/panel/auctions/my", function () {
    if (!logged_in()) {
        return App::redirect("/login");
    }

    $myAuctions = Connection::fetchAll("SELECT * FROM player_auctions WHERE seller = ? ORDER BY id DESC", array(user()->getUsername()));
    View::display("auctions-my", array("auctions" => $myAuctions));
})->name("panel.auctions.my");

Route::map("/panel/auctions/player", function () {
    if (!logged_in()) {
        return App::redirect("/login");
    }

    $currentDate = date('Y-m-d H:i:s');
    $items = Connection::fetchAll("SELECT pa.*, m.memb_name as seller_name, m2.memb_name as bidder_name 
        FROM player_auctions pa 
        LEFT JOIN MEMB_INFO m ON pa.seller = m.memb___id 
        LEFT JOIN MEMB_INFO m2 ON pa.current_bidder = m2.memb___id
        WHERE pa.status = 'active' AND pa.end_date >= ? 
        ORDER BY pa.id DESC", array($currentDate));

    View::display("auctions-player", array("items" => $items));
})->via("GET", "POST")->name("panel.auctions.player");

Route::map("/panel/auctions/player/bid/:id", function ($id) {
    if (!logged_in()) {
        return App::redirect("/login");
    }

    $item = Connection::fetchAssoc("SELECT * FROM player_auctions WHERE id = ? AND status = 'active'", array($id));
    if (!$item) return App::notFound();

    if (strtotime($item['end_date']) < time()) {
        return error("Este leilão já foi encerrado.");
    }

    if ($item['seller'] === user()->getUsername()) {
        return error("Você não pode dar lance em seu próprio item.");
    }

    if ($item['current_bidder'] === user()->getUsername()) {
        return error("Você já é o maior licitante deste item.");
    }

    if (Request::isPost()) {
        $bidAmount = (int) Input::post("bid_amount");
        
        if ($bidAmount <= $item['current_price']) {
            return error("Seu lance deve ser maior que o lance atual de " . $item['current_price'] . ".");
        }

        try {
            $username = user()->getUsername();
            $coinType = $item['coin'];
            
            if ($coinType === 'credits') {
                $currentCoins = user()->getCredit();
                
                if ($currentCoins < $bidAmount) {
                    return error("Você não tem saldo suficiente para dar este lance.");
                }

                Connection::transactional(function () use ($item, $bidAmount, $username) {
                    if (!empty($item['current_bidder'])) {
                        $previousBidder = new \Morpheus\Account\Account($item['current_bidder']);
                        if ($previousBidder->exists()) {
                            $previousBidder->addCredit($item['current_price']);
                            $previousBidder->update();
                        }
                    }
                    
                    user()->addCredit(-$bidAmount);
                    user()->update();

                    Connection::update("player_auctions", array(
                        "current_price" => $bidAmount,
                        "current_bidder" => $username,
                        "winner" => $username
                    ), array("id" => $item['id']));
                });
            } else {
                $coinConfig = config("coins", array());
                
                if (empty($coinConfig) || !isset($coinConfig[$coinType])) {
                    return error("Erro interno: Moeda não encontrada na configuração.");
                }

                $coinData = $coinConfig[$coinType];
                $cTable = $coinData["table"];
                $cColumn = $coinData["column"];
                $cKey = $coinData["foreign_key"];

                $currentCoins = (int) Connection::fetchColumn("SELECT {$cColumn} FROM {$cTable} WHERE {$cKey} = ?", array($username));

                if ($currentCoins < $bidAmount) {
                    return error("Você não tem saldo suficiente para dar este lance.");
                }

                Connection::transactional(function () use ($item, $bidAmount, $cTable, $cColumn, $cKey, $username) {
                    if (!empty($item['current_bidder'])) {
                        Connection::executeUpdate("UPDATE {$cTable} SET {$cColumn} = {$cColumn} + ? WHERE {$cKey} = ?", array($item['current_price'], $item['current_bidder']));
                    }
                    
                    Connection::executeUpdate("UPDATE {$cTable} SET {$cColumn} = {$cColumn} - ? WHERE {$cKey} = ?", array($bidAmount, $username));

                    Connection::update("player_auctions", array(
                        "current_price" => $bidAmount,
                        "current_bidder" => $username,
                        "winner" => $username
                    ), array("id" => $item['id']));
                });
            }

            return success("Lance efetuado com sucesso!", array("redirect" => "/panel/auctions/player"));
        } catch (Exception $ex) {
            return error("Erro ao processar o lance: " . $ex->getMessage());
        }
    }

    View::display("auctions-player-bid", array("item" => $item));
})->via("GET", "POST")->name("panel.auctions.player.bid");

Route::get("/panel/auctions/cancel/:id", function ($id) {
    if (!logged_in()) {
        return App::redirect("/login");
    }

    $auction = Connection::fetchAssoc("SELECT * FROM player_auctions WHERE id = ? AND seller = ?", array($id, user()->getUsername()));
    if (!$auction) return App::notFound();

    if ($auction['status'] !== 'active') {
        return error("Este leilão não pode ser cancelado.");
    }

    if (!empty($auction['current_bidder'])) {
        return error("Este leilão já possui lances e não pode ser cancelado.");
    }

    try {
        Connection::transactional(function () use ($auction) {
            $warehouse = user()->getWarehouse();
            $warehouse->load();
            
            $itemObj = new \Morpheus\Item\Item($auction['item_hex']);
            $itemObj->parse();
            
            if (!$warehouse->addItem($itemObj)) {
                throw new Exception("Não há espaço no baú para devolver o item.");
            }
            $warehouse->update();

            Connection::update("player_auctions", array("status" => "cancelled"), array("id" => $auction['id']));
        });

        return success("Leilão cancelado e item devolvido ao baú!", array("redirect" => "/panel/auctions/my"));
    } catch (Exception $ex) {
        return error("Erro ao cancelar leilão: " . $ex->getMessage());
    }
})->name("panel.auctions.cancel");