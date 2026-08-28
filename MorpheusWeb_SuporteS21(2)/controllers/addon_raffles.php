<?php

Route::map("/panel/raffles", function () {
    if (!logged_in()) {
        return App::redirect("/login");
    }

    $schema = Connection::getSchemaManager();
    if (!$schema->tablesExist("mw_raffles_numbers")) {
        $table = new \Doctrine\DBAL\Schema\Table("mw_raffles_numbers");
        $table->addColumn("id", "integer", array("autoincrement" => true));
        $table->addColumn("raffle_id", "integer");
        $table->addColumn("number", "integer");
        $table->addColumn("account", "string", array("length" => 50));
        $table->addColumn("buy_date", "datetime");
        $table->setPrimaryKey(array("id"));
        $schema->createTable($table);
    }

    $raffles = Connection::fetchAll("SELECT * FROM mw_raffles WHERE active = 1");
    $account = user()->getUsername();

    $numbers = array();
    foreach ($raffles as $raffle) {
        $numbers[$raffle['id']] = Connection::fetchAll("SELECT * FROM mw_raffles_numbers WHERE raffle_id = ?", array($raffle['id']));
    }

    View::display("raffles", array("raffles" => $raffles, "numbers" => $numbers, "account" => $account));
})->via("GET", "POST")->name("panel.raffles");

Route::post("/panel/raffles/buy/:raffle_id", function ($raffle_id) {
    if (!logged_in()) return App::redirect("/login");

    $number = (int) Input::post("number");
    $raffle = Connection::fetchAssoc("SELECT * FROM mw_raffles WHERE id = ? AND active = 1", array($raffle_id));

    if (!$raffle) return error("Rifa não encontrada ou inativa.", array("redirect" => "/panel/raffles"));
    if ($number < 1 || $number > $raffle['total_numbers']) return error("Número inválido.", array("redirect" => "/panel/raffles"));

    $exists = Connection::fetchAssoc("SELECT * FROM mw_raffles_numbers WHERE raffle_id = ? AND number = ?", array($raffle_id, $number));
    if ($exists) return error("Este número já foi comprado.", array("redirect" => "/panel/raffles"));

    $username = user()->getUsername();
    $coinConfig = config("coins", array());
    $coin = isset($coinConfig[$raffle['coin_id']]) ? $coinConfig[$raffle['coin_id']] : null;
    if (!$coin) return error("Moeda inválida configurada para esta rifa.", array("redirect" => "/panel/raffles"));

    $cTable = $coin["table"]; $cColumn = $coin["column"]; $cKey = $coin["foreign_key"];
    $currentCoins = (int) Connection::fetchColumn("SELECT {$cColumn} FROM {$cTable} WHERE {$cKey} = ?", array($username));
    if ($currentCoins < $raffle['price']) return error("Você não tem saldo suficiente para comprar este número.", array("redirect" => "/panel/raffles"));

    try {
        Connection::transactional(function () use ($raffle, $number, $username, $cTable, $cColumn, $cKey) {
            Connection::executeUpdate("UPDATE {$cTable} SET {$cColumn} = {$cColumn} - ? WHERE {$cKey} = ?", array($raffle['price'], $username));
            Connection::insert("mw_raffles_numbers", array("raffle_id" => $raffle['id'], "number" => $number, "account" => $username, "buy_date" => date('Y-m-d H:i:s')));
        });
        return success("Número comprado com sucesso!", array("redirect" => "/panel/raffles"));
    } catch (Exception $ex) { return error("Erro ao processar a compra: " . $ex->getMessage(), array("redirect" => "/panel/raffles")); }
})->name("panel.raffles.buy");