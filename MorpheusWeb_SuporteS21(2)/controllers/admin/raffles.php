<?php

Route::map("/raffles", function () {
    $schema = Connection::getSchemaManager();
    
    if (!$schema->tablesExist("mw_raffles")) {
        $table = new \Doctrine\DBAL\Schema\Table("mw_raffles");
        $table->addColumn("id", "integer", array("autoincrement" => true));
        $table->addColumn("name", "string", array("length" => 255));
        $table->addColumn("description", "text", array("notnull" => false));
        $table->addColumn("total_numbers", "integer", array("default" => 100));
        $table->addColumn("coin_id", "string", array("length" => 50, "default" => ""));
        $table->addColumn("price", "integer", array("default" => 0));
        $table->addColumn("reward_vip_type", "integer", array("default" => 0));
        $table->addColumn("reward_vip_days", "integer", array("default" => 0));
        $table->addColumn("reward_credits", "integer", array("default" => 0));
        $table->addColumn("reward_wcoinc", "integer", array("default" => 0));
        $table->addColumn("reward_wcoinp", "integer", array("default" => 0));
        $table->addColumn("reward_goblinpoints", "integer", array("default" => 0));
        $table->addColumn("reward_sql", "text", array("notnull" => false));
        $table->addColumn("winner", "string", array("length" => 50, "notnull" => false));
        $table->addColumn("winner_number", "integer", array("notnull" => false));
        $table->addColumn("active", "boolean", array("default" => 1));
        $table->setPrimaryKey(array("id"));
        $schema->createTable($table);
    } else {
        $columns = $schema->listTableColumns("mw_raffles");
        $tableDiff = new \Doctrine\DBAL\Schema\TableDiff("mw_raffles");
        $changed = false;
        
        if (!array_key_exists('reward_vip_type', $columns) && !array_key_exists('REWARD_VIP_TYPE', $columns)) {
            $tableDiff->addedColumns["reward_vip_type"] = new \Doctrine\DBAL\Schema\Column("reward_vip_type", \Doctrine\DBAL\Types\Type::getType("integer"), array("default" => 0));
            $changed = true;
        }
        if (!array_key_exists('coin_id', $columns) && !array_key_exists('COIN_ID', $columns)) {
            $tableDiff->addedColumns["coin_id"] = new \Doctrine\DBAL\Schema\Column("coin_id", \Doctrine\DBAL\Types\Type::getType("string"), array("length" => 50, "default" => ""));
            $changed = true;
        }
        if (!array_key_exists('winner', $columns) && !array_key_exists('WINNER', $columns)) {
            $tableDiff->addedColumns["winner"] = new \Doctrine\DBAL\Schema\Column("winner", \Doctrine\DBAL\Types\Type::getType("string"), array("length" => 50, "notnull" => false));
            $changed = true;
        }
        if (!array_key_exists('winner_number', $columns) && !array_key_exists('WINNER_NUMBER', $columns)) {
            $tableDiff->addedColumns["winner_number"] = new \Doctrine\DBAL\Schema\Column("winner_number", \Doctrine\DBAL\Types\Type::getType("integer"), array("notnull" => false));
            $changed = true;
        }
        
        if ($changed) {
            $schema->alterTable($tableDiff);
        }
    }

    $raffles = Connection::fetchAll("SELECT * FROM mw_raffles");
    View::display("../../../plugins/Raffles/views/admin/index", array("raffles" => $raffles));
})->via("GET")->name("raffles");

Route::map("/raffles/add", function () {
    if (Request::isPost()) {
        $data = array(
            "name" => Input::post("name"),
            "description" => Input::post("description"),
            "total_numbers" => (int) Input::post("total_numbers"),
            "coin_id" => Input::post("coin_id"),
            "price" => (int) Input::post("price"),
            "reward_vip_type" => (int) Input::post("reward_vip_type"),
            "reward_vip_days" => (int) Input::post("reward_vip_days"),
            "reward_credits" => (int) Input::post("reward_credits"),
            "reward_wcoinc" => (int) Input::post("reward_wcoinc"),
            "reward_wcoinp" => (int) Input::post("reward_wcoinp"),
            "reward_goblinpoints" => (int) Input::post("reward_goblinpoints"),
            "reward_sql" => Input::post("reward_sql"),
            "active" => Input::post("active") ? 1 : 0
        );
        Connection::insert("mw_raffles", $data);
        return success("Rifa adicionada com sucesso!", array("redirect" => "/admin/raffles"));
    }
    
    $coins = config("coins", array());
    $vips = config("vip.types", array());
    View::display("../../../plugins/Raffles/views/admin/form", array("action" => "add", "coins" => $coins, "vips" => $vips));
})->via("GET", "POST")->name("raffles.add");

Route::map("/raffles/edit/:id", function ($id) {
    $raffle = Connection::fetchAssoc("SELECT * FROM mw_raffles WHERE id = ?", array($id));
    if (!$raffle) return App::notFound();

    if (Request::isPost()) {
        $data = array(
            "name" => Input::post("name"),
            "description" => Input::post("description"),
            "total_numbers" => (int) Input::post("total_numbers"),
            "coin_id" => Input::post("coin_id"),
            "price" => (int) Input::post("price"),
            "reward_vip_type" => (int) Input::post("reward_vip_type"),
            "reward_vip_days" => (int) Input::post("reward_vip_days"),
            "reward_credits" => (int) Input::post("reward_credits"),
            "reward_wcoinc" => (int) Input::post("reward_wcoinc"),
            "reward_wcoinp" => (int) Input::post("reward_wcoinp"),
            "reward_goblinpoints" => (int) Input::post("reward_goblinpoints"),
            "reward_sql" => Input::post("reward_sql"),
            "active" => Input::post("active") ? 1 : 0
        );
        Connection::update("mw_raffles", $data, array("id" => $id));
        return success("Rifa editada com sucesso!", array("redirect" => "/admin/raffles"));
    }
    
    $coins = config("coins", array());
    $vips = config("vip.types", array());
    View::display("../../../plugins/Raffles/views/admin/form", array("action" => "edit", "raffle" => $raffle, "coins" => $coins, "vips" => $vips));
})->via("GET", "POST")->name("raffles.edit");

Route::map("/raffles/delete/:id", function ($id) {
    Connection::delete("mw_raffles", array("id" => $id));
    return success("Rifa excluída com sucesso!", array("reload" => true));
})->via("GET", "POST")->name("raffles.delete");

Route::get("/raffles/draw/:id", function ($id) {
    $raffle = Connection::fetchAssoc("SELECT * FROM mw_raffles WHERE id = ?", array($id));
    if (!$raffle) return App::notFound();

    if (!empty($raffle['winner'])) {
        return error("Esta rifa já foi sorteada! Ganhador: " . $raffle['winner']);
    }

    $boughtNumbers = Connection::fetchAll("SELECT * FROM mw_raffles_numbers WHERE raffle_id = ?", array($id));
    if (empty($boughtNumbers)) {
        return error("Nenhum número foi comprado nesta rifa. O sorteio não pode ser realizado.");
    }

    $winningTicket = $boughtNumbers[array_rand($boughtNumbers)];
    $winner = $winningTicket['account'];
    $winnerNumber = $winningTicket['number'];

    try {
        Connection::transactional(function () use ($raffle, $winner, $winnerNumber) {
            if ($raffle['reward_vip_type'] > 0 && $raffle['reward_vip_days'] > 0) {
                $vipTable = config('vip.table');
                $vipColAccount = config('vip.column_account');
                $vipColType = config('vip.column_type');
                $vipColExpire = config('vip.column_expire');
                if ($vipTable && $vipColAccount) {
                    $current = Connection::fetchAssoc("SELECT {$vipColType} as type, {$vipColExpire} as expire FROM {$vipTable} WHERE {$vipColAccount} = ?", array($winner));
                    if ($current) {
                        $newExpire = date('Y-m-d H:i:s', strtotime("+ " . $raffle['reward_vip_days'] . " days"));
                        if (!empty($current['expire']) && strtotime($current['expire']) > time() && $current['type'] == $raffle['reward_vip_type']) {
                            $newExpire = date('Y-m-d H:i:s', strtotime($current['expire'] . " + " . $raffle['reward_vip_days'] . " days"));
                        }
                        Connection::executeUpdate("UPDATE {$vipTable} SET {$vipColType} = ?, {$vipColExpire} = ? WHERE {$vipColAccount} = ?", array($raffle['reward_vip_type'], $newExpire, $winner));
                    }
                }
            }

            if ($raffle['reward_credits'] > 0) {
                $accObj = new \Morpheus\Account\Account($winner);
                $accObj->setCredit($accObj->getCredit() + $raffle['reward_credits']);
                $accObj->update();
            }

            $coinsConfig = config("coins", array());
            foreach (array('wcoinc' => 'reward_wcoinc', 'wcoinp' => 'reward_wcoinp', 'goblinpoints' => 'reward_goblinpoints') as $cid => $rewardField) {
                if ($raffle[$rewardField] > 0 && isset($coinsConfig[$cid])) {
                    $c = $coinsConfig[$cid];
                    Connection::executeUpdate("UPDATE {$c['table']} SET {$c['column']} = {$c['column']} + ? WHERE {$c['foreign_key']} = ?", array($raffle[$rewardField], $winner));
                }
            }

            if (!empty($raffle['reward_sql'])) {
                $sql = str_replace("{account}", $winner, $raffle['reward_sql']);
                Connection::executeUpdate($sql);
            }

            Connection::update("mw_raffles", array("winner" => $winner, "winner_number" => $winnerNumber, "active" => 0), array("id" => $raffle['id']));
        });

        return success("Sorteio realizado com sucesso! O ganhador foi " . $winner . " com o número " . $winnerNumber . ".", array("reload" => true));
    } catch (Exception $ex) {
        return error("Erro ao processar o sorteio: " . $ex->getMessage());
    }
})->name("raffles.draw");