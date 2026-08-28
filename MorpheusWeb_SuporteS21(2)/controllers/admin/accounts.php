<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::post("/accounts/find", function () {
    $term = Input::post("term");
    $accounts = Connection::fetchAll("select top 30\n        mi.memb_guid as guid,\n        mi.memb___id as username,\n        mi.memb__pwd as password,\n        mi.memb_name as name,\n        mi.mail_addr as email,\n        mi.tel__numb as phone,\n        mi.fpas_ques as secretQuestion,\n        mi.fpas_answ as secretAnswer,\n        mi.sno__numb as personalId,\n        mi.bloc_code as blocked,\n        ms.IP as ip\n        from " . table_with_db("MEMB_INFO") . " mi\n        left join " . table_with_db("MEMB_STAT") . " ms on mi.memb___id = ms.memb___id COLLATE DATABASE_DEFAULT\n        where mi.memb___id like :term\n        or mi.mail_addr like :term\n        or mi.tel__numb like :term\n        or ms.IP like :term\n        or exists (select 1 from Character where Name like :term and AccountID = mi.memb___id)\n    ", array("term" => "%" . $term . "%", "name" => $term));
    foreach ($accounts as $key => $account) {
        $accounts[$key]["characters"] = Connection::fetchAll("SELECT Name AS name FROM Character WHERE AccountID = :account", array("account" => $account["username"]));
    }
    View::display("accounts/find", array("accounts" => $accounts));
})->name("accounts.find");
Route::map("/accounts/edit/:username", function ($username) {
    $account = new Morpheus\Account\Account($username);
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED)));
        $password = Input::post("password");
        $coins = array();
        foreach (config("coins", array()) as $coin) {
            $coins[$coin["id"]] = Input::post("coin_" . $coin["id"]);
        }
        if ($validation->isValid()) {
            try {
                $account->setName(Input::post("name"))->setSecretQuestion(Input::post("secret_question"))->setSecretAnswer(Input::post("secret_answer"))->setEmail(Input::post("email"))->setBlocked(Input::post("blocked") == 1)->setConfirmedEmail(Input::post("confirmed_email") == 1)->setPersonalId(Input::post("pid"))->setCredit(util("Number")->toFloat(Input::post("credit")))->setVipType(Input::post("vip_type"))->setVipExpire(Input::post("vip_expire") ? DateTime::createFromFormat("d/m/Y", Input::post("vip_expire"))->format(DateTime::ISO8601) : NULL);
                if (!empty($coins)) {
                    $account->setCoins($coins);
                }
                if ($password != NULL && !empty($password)) {
                    $account->updatePassword($password);
                }
                $account->update();
                return success(__("Account %s has been updated", $username), array("redirect" => "/admin/accounts/edit/" . $username));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    if ($account->exists()) {
        $data = $account->toArray();
        $data["bans"] = Connection::fetchAll("select *\n            FROM mw_banned_accounts\n            WHERE account = ?\n        ", array($username));
        View::display("accounts/edit", array("account" => $data));
    } else {
        App::notFound();
    }
})->via("GET", "POST")->name("accounts.edit");
Route::get("/accounts/logs/:username", function ($username) {
    $logs = Connection::fetchAll("select * \n        from mw_logs\n        where account = ?\n        order by id desc\n    ", array($username));
    View::display("accounts/logs", array("logs" => $logs));
})->name("accounts.log");
Route::map("/accounts/ban/:username", function ($username) {
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("cause" => array(Morpheus\Core\Validation::REQUIRED), "days" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS)));
        if ($validation->isValid()) {
            try {
                $exists = Connection::fetchAssoc("SELECT * FROM mw_banned_accounts WHERE account = ? AND status = 1", array(Input::post("account")));
                if (!empty($exists)) {
                    return error(__("Account %s is already banned", array(Input::post("account"))));
                }
                Connection::transactional(function () use($username) {
                    $account = new Morpheus\Account\Account($username);
                    Connection::insert("mw_banned_accounts", array("account" => $account->getUsername(), "cause" => Input::post("cause"), "block_date" => (new DateTime())->format(DATETIME_ISO_FORMAT), "unblock_date" => (new DateTime("+ " . Input::post("days") . " days"))->format(DATETIME_ISO_FORMAT), "user_block_id" => Morpheus\Admin\Auth::getUser("id"), "status" => 1), array("string", "string", "string", "string", "integer", "integer"));
                    $account->setBlocked(true);
                    $account->update();
                });
                return success(__("Account %s has been banned for %s days", array(Input::post("account"), Input::post("days"))), array("redirect" => "/admin/accounts/edit/" . $username));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("accounts/ban", array("username" => $username));
})->via("GET", "POST")->name("accounts.ban");
Route::get("/accounts/unban/:username", function ($username = "") {
    $acc = Connection::fetchAssoc("select bloc_code from MEMB_INFO where memb___id = ?", array($username));
    if ($acc["bloc_code"] == 0) {
        return error(__("Account %s is not banned", $username));
    }
    try {
        Connection::transactional(function () use($username) {
            $account = new Morpheus\Account\Account($username);
            Connection::update("mw_banned_accounts", array("status" => 0), array("account" => $username), array("integer"));
            $account->setBlocked(false);
            $account->update();
        });
        return success(__("Account %s has been loose", $username), array("redirect" => "/admin/accounts/edit/" . $username));
    } catch (Exception $ex) {
        return error($ex->getMessage());
    }
})->name("accounts.unban");
Route::map("/accounts/warehouse/:username", function ($username) {
    $account = new Morpheus\Account\Account($username);
    $warehouse = $account->getWarehouse();
    $warehouse->load();
    if (Request::isPost()) {
        try {
            $warehouse->setMoney(Input::post("money"));
            $warehouse->update(false);
            return success(__("Zen has been udpated"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    $serial = Input::get("serial");
    View::display("accounts/warehouse", array("account" => $account, "serial" => $serial, "warehouse" => $warehouse));
})->via("GET", "POST")->name("accounts.warehouse");
Route::get("/accounts/warehouse/organize/:username", function ($username) {
    $account = new Morpheus\Account\Account($username);
    $warehouse = $account->getWarehouse();
    $items = str_split(bin2hex($account->getBinWarehouse()), $warehouse->getItemSize());
    for ($i = 0; $i < $warehouse->getWidth() * $warehouse->getHeight(); $i++) {
        $item = new Morpheus\Item\Item($items[$i]);
        $item->parse();
        if (!$item->isHexEmpty()) {
            $warehouse->addItem($item);
        }
    }
    try {
        $warehouse->update();
        return success("Báu organizado com sucesso", array("refresh" => "accounts.warehouse"));
    } catch (Exception $ex) {
        return error($ex->getMessage());
    }
})->name("accounts.warehouse.organize");
Route::get("/accounts/logout/:name", function ($name) {
    $account = account($name);
    try {
        $account->disconnect();
        return success(__("Account %s has been disconnected from the game", $name));
    } catch (Exception $ex) {
        return error($ex->getMessage());
    }
})->name("accounts.logout");

?>