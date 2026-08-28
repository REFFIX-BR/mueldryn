<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/characters/loggedin", function () {
    $q = Input::get("q");
    $page = (int) Input::get("page", 1);
    if ($page < 1) $page = 1;
    $limit = 50;
    $offset = ($page - 1) * $limit;

    $params = array();
    $where = "";

    if (!empty($q)) {
        $where = " WHERE mi.memb___id LIKE :q OR mi.mail_addr LIKE :q OR ms.IP LIKE :q OR EXISTS (SELECT 1 FROM Character WHERE Name LIKE :q AND AccountID = mi.memb___id COLLATE DATABASE_DEFAULT)";
        $params["q"] = "%" . $q . "%";
    }

    $total = Connection::fetchColumn("SELECT COUNT(*) FROM " . table_with_db("MEMB_INFO") . " mi LEFT JOIN " . table_with_db("MEMB_STAT") . " ms ON mi.memb___id = ms.memb___id COLLATE DATABASE_DEFAULT" . $where, $params);
    $totalPages = ceil($total / $limit);

    $accounts = Connection::fetchAll("SELECT * FROM (
        SELECT 
            mi.memb___id AS account, 
            mi.mail_addr AS email, 
            ms.IP AS ip,
            ROW_NUMBER() OVER(ORDER BY mi.memb___id ASC) AS RowNum
        FROM " . table_with_db("MEMB_INFO") . " mi 
        LEFT JOIN " . table_with_db("MEMB_STAT") . " ms ON mi.memb___id = ms.memb___id COLLATE DATABASE_DEFAULT" . $where . "
    ) AS ResultSet WHERE RowNum > " . $offset . " AND RowNum <= " . ($offset + $limit), $params);

    foreach ($accounts as $key => $account) {
        $chars = Connection::fetchAll("SELECT Name FROM Character WHERE AccountID = :account", array("account" => $account["account"]));
        $charList = array();
        foreach ($chars as $char) {
            $charList[] = $char["Name"];
        }
        $accounts[$key]["characters"] = $charList;
    }
    View::display("characters/loggedin", array("accounts" => $accounts, "q" => $q, "page" => $page, "totalPages" => $totalPages));
})->name("characters.loggedin");
Route::get("/characters/online", function () {
    $servers = array();
    foreach (server()->getCharactersOnline() as $server) {
        $servers[$server["server_name"]][] = $server;
    }
    View::display("characters/online", array("servers" => $servers, "view" => Input::get("view", "list")));
})->name("characters.online");
Route::get("/characters/map/:name", function ($name) {
    $character = new Morpheus\Character\Character($name);
    View::display("characters/map", array("character" => $character));
});
Route::map("/characters/edit/:name", function ($name) {
    $character = new Morpheus\Character\Character($name);
    if (!$character->exists()) {
        return App::notFound();
    }
    if (Request::isPost()) {
        $maxStatus = config("server.max_status", 32767);
        $maxLevel = config("server.max_level", 350);
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED), "level" => array(Morpheus\Core\Validation::REQUIRED, __("Level deve estar entre 1 e %s", array($maxLevel)) => array("rule" => array("between", 1, $maxLevel))), "strength" => array(Morpheus\Core\Validation::REQUIRED, __("The value must be between %s and %s", array(0, $maxStatus)) => array("rule" => array("between", 0, $maxStatus))), "agility" => array(Morpheus\Core\Validation::REQUIRED, __("The value must be between %s and %s", array(0, $maxStatus)) => array("rule" => array("between", 0, $maxStatus))), "energy" => array(Morpheus\Core\Validation::REQUIRED, __("The value must be between %s and %s", array(0, $maxStatus)) => array("rule" => array("between", 0, $maxStatus))), "vitality" => array(Morpheus\Core\Validation::REQUIRED, __("The value must be between %s and %s", array(0, $maxStatus)) => array("rule" => array("between", 0, $maxStatus))), "command" => array(__("Required field") => array("rule" => "notEmpty", "condition" => isset($character->getSchema()["Leadership"])), __("The value must be between %s and %s", array(0, $maxStatus)) => array("rule" => array("between", 0, $maxStatus), "condition" => isset($character->getSchema()["Leadership"])))));
        if ($validation->isValid()) {
            try {
                $character = new Morpheus\Character\Character($name);
                if ($character->exists()) {
                    Connection::transactional(function () use($character) {
                        $character->setLevel(Input::post("level"))->setMap(Input::post("map"))->setPositionX(Input::post("x"))->setPositionY(Input::post("y"))->setPoints(Input::post("points"))->setStrength(Input::post("strength"))->setAgility(Input::post("agility"))->setEnergy(Input::post("energy"))->setVitality(Input::post("vitality"))->setExperience(Input::post("experience"))->setPoints(Input::post("points"))->setCode(Input::post("code"))->setPkLevel(Input::post("pk"))->setMoney(Input::post("money"));
                        if (isset($character->getSchema()["Leadership"])) {
                            $character->setCommand(Input::post("command"));
                        }
                        if (isset($character->getSchema()["Ruud"])) {
                            $character->setRuud(Input::post("ruud"));
                        }
                        if (config("columns.resets")) {
                            $character->setResets(Input::post("resets"));
                        }
                        if (config("columns.master_resets")) {
                            $character->setMasterResets(Input::post("master_resets"));
                        }
                        $character->update();
                        if ($character->getName() !== Input::post("name")) {
                            $character->rename(Input::post("name"));
                        }
                        if ($character->getClass() !== Input::post("class")) {
                            $character->changeClass(Input::post("class"));
                        }
                    });
                    return success(__("Character %s has been updated", Input::post("name")), array("redirect" => "/admin/characters/edit/" . Input::post("name")));
                }
                App::notFound();
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("characters/edit", array("character" => $character->toArray()));
})->via("GET", "POST")->name("characters.edit");

?>