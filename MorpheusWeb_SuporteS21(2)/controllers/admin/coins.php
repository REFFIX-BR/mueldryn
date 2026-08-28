<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/coins", function () {
    $coins = config("coins", array());
    View::display("coins/index", array("coins" => $coins));
})->name("coins");
Route::map("/coins/add", function () {
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("id" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ALPHA), "name" => array(Morpheus\Core\Validation::REQUIRED), "table" => array(Morpheus\Core\Validation::REQUIRED), "column" => array(Morpheus\Core\Validation::REQUIRED), "foreign_key" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            $coins = config("coins", array());
            $id = strtolower(Input::post("id"));
            $coin = array("id" => $id, "name" => Input::post("name"), "table" => Input::post("table"), "column" => Input::post("column"), "foreign_key" => Input::post("foreign_key"));
            $coins[$id] = $coin;
            try {
                $test = Connection::fetchColumn("SELECT " . $coin["column"] . " FROM " . $coin["table"] . " WHERE " . $coin["foreign_key"] . " IS NOT NULL");
                Morpheus\Core\Config::save("coins", $coins);
                return success(__("Coin has been saved"), array("redirect" => "/admin/coins"));
            } catch (Doctrine\DBAL\DBALException $ex) {
                return error(__("Unable to validate the data entered next to the database"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    $tables = Connection::fetchAll("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME");
    View::display("coins/add", array("tables" => $tables));
})->via("GET", "POST")->name("coins.add");
Route::get("/coins/columns/:table", function ($table) {
    $columns = Connection::fetchAll("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ? ORDER BY ORDINAL_POSITION", array($table));
    echo json_encode($columns);
})->name("coins.columns");
Route::map("/coins/edit/:id", function ($id) {
    $coins = config("coins");
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("id" => array(Morpheus\Core\Validation::REQUIRED), "name" => array(Morpheus\Core\Validation::REQUIRED), "table" => array(Morpheus\Core\Validation::REQUIRED), "column" => array(Morpheus\Core\Validation::REQUIRED), "foreign_key" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            $coin = array("id" => $id, "name" => Input::post("name"), "table" => Input::post("table"), "column" => Input::post("column"), "foreign_key" => Input::post("foreign_key"));
            $coins[$id] = $coin;
            try {
                $test = Connection::fetchColumn("SELECT " . $coin["column"] . " FROM " . $coin["table"] . " WHERE " . $coin["foreign_key"] . " IS NOT NULL");
                Morpheus\Core\Config::save("coins", $coins);
                return success(__("Coin has been updated"), array("redirect" => "/admin/coins"));
            } catch (Doctrine\DBAL\DBALException $ex) {
                return error(__("Unable to validate the data entered next to the database"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    $coin = $coins[$id];
    $tables = Connection::fetchAll("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME");
    View::display("coins/edit", array("coin" => $coin, "tables" => $tables));
})->via("GET", "POST")->name("coins.edit");
Route::get("/coins/delete/:id", function ($id) {
    $coins = config("coins", array());
    unset($coins[$id]);
    try {
        Morpheus\Core\Config::save("coins", $coins);
        return success(__("Coin has been deleted"), array("redirect" => "/admin/coins"));
    } catch (Exception $ex) {
        return error($ex->getMessage(), array("redirect" => "/admin/coins"));
    }
})->name("coins.delete");

?>