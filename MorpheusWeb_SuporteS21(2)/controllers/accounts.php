<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::post("/login", function () {
    if (Morpheus\Account\Auth::login(Input::post("username"), Input::post("password"))) {
        notify("accounts.login", user(), Input::post());
        return success("", array("partial" => "login", "redirect" => "/"));
    }
    return error(__("User and/or password are incorrect"));
});
Route::get("/logout", function () {
    $user = user();
    Morpheus\Account\Auth::logout();
    notify("accounts.logout", $user);
    return success("", array("redirect" => "/", "partial" => "login"));
});
Route::map("/register", function () {
    if (Request::isPost()) {
        $post = Input::post();
        $validates = array("name" => array(Morpheus\Core\Validation::REQUIRED), "username" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ALPHANUMERIC_LOWER, __("Username already exists") => array("rule" => array("isUnique", table_with_db("MEMB_INFO"), "memb___id"))), "password" => array(Morpheus\Core\Validation::REQUIRED), "repassword" => array(Morpheus\Core\Validation::REQUIRED, __("Password is not match") => array("rule" => array("equals", "password"))), "email" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::EMAIL, __("E-mail already exists") => array("rule" => array("isUnique", table_with_db("MEMB_INFO"), "mail_addr"))), "reemail" => array(Morpheus\Core\Validation::REQUIRED, __("E-mail is not match") => array("rule" => array("equals", "email"))), "pid" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS, __("Enter exactly 7 digits") => array("rule" => array("length", 7))), "captcha" => array(Morpheus\Core\Validation::CAPTCHA));
        $questions = config("register.secret_questions", array());
        if (!empty($questions)) {
            $validates += array("secret_question" => array(Morpheus\Core\Validation::REQUIRED), "secret_answer" => array(Morpheus\Core\Validation::REQUIRED));
        }
        $fields = config("register.custom_fields", array());
        foreach ($fields as $field) {
            if ($field["required"]) {
                $validates[$field["name"]] = array(Morpheus\Core\Validation::REQUIRED);
            }
        }
        $validation = new Morpheus\Core\Validation($post, $validates);
        if ($validation->isValid()) {
            if (!isset($post["terms"]) || !$post["terms"]) {
                return error(__("Required accept terms of use"));
            }
            $account = account();
            $account->setName($post["name"])->setUsername($post["username"])->setPassword($post["password"])->setEmail($post["email"])->setBlocked(config("register.create_blocked"))->setConfirmedEmail(!config("register.confirm_email"))->setPersonalId($post["pid"]);
            if (config("register.confirm_email", false)) {
                $account->setToken(md5(uniqid(rand(), true)));
            }
            if (!empty($questions)) {
                $account->setSecretQuestion($post["secret_question"])->setSecretAnswer($post["secret_answer"]);
            }
            $awards = array();
            if (config("register.bonus.vip.type") && config("register.bonus.vip.days")) {
                $awards[] = config("register.bonus.vip.days", 0) . " " . __("days") . " " . config("vip.types")[config("register.bonus.vip.type")];
                $account->setVipType(config("register.bonus.vip.type"))->setVipExpire(new DateTime("+ " . config("register.bonus.vip.days", 0) . " days"));
            }
            if (config("register.bonus.coin.type")) {
                $awards[] = config("register.bonus.coin.amount") . " " . config("coins")[config("register.bonus.coin.type")]["name"];
                $account->addCoin(config("register.bonus.coin.type"), config("register.bonus.coin.amount"));
            }
            $message = !empty($awards) ? "<br />" . __("Congratulations! You received %s", array(util("Text")->toList($awards))) : "";
            foreach ($fields as $field) {
                if (isset($post[$field["name"]])) {
                    $account->addCustomField($field["name"], $post[$field["name"]]);
                }
            }
            try {
                Connection::transactional(function () use($account, $post) {
                    // Cria no OpenMU (fonte do jogo) e espelha no MSSQL do Morpheus
                    if (\Morpheus\OpenMU\Bridge::enabled()) {
                        if (\Morpheus\OpenMU\Bridge::accountExists($post["username"])) {
                            throw new Exception(__("Username already exists"));
                        }
                        \Morpheus\OpenMU\Bridge::createAccount($post["username"], $post["password"], $post["email"]);
                        \Morpheus\OpenMU\Bridge::syncToMorpheus($post["username"]);
                    } else {
                        $account->insert();
                    }
                    notify("accounts.register", $account);
                });
                if (config("register.confirm_email", false)) {
                    $mail = new Morpheus\Network\Mail();
                    $mail->addAddress($post["email"], $post["name"])->setSubject(config("server.name") . " - " . __("Registration confirmation"))->setMessageFromTemplate("confirm-email", array("account" => $account));
                    if ($mail->send()) {
                        return success(__("Successful registration") . "<br />" . __("To validate your registration access your email and activate your registration") . $message, array("redirect" => "/register"));
                    }
                    return error($mail->getError());
                }
                return success(__("Successful registration") . $message, array("redirect" => "/register"));
            } catch (Exception $ex) {
                return error($ex);
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("accounts/register");
})->via("GET", "POST");
Route::get("/register/activation/:username/:token", function ($username, $token) {
    if (config("register.confirm_email", false)) {
        $account = new Morpheus\Account\Account($username);
        if (!$account->exists()) {
            error(__("Account not exists"));
        } else {
            $t = $account->getToken();
            if (empty($t) || $t !== $token) {
                error(__("Invalid token"));
            } else {
                try {
                    Connection::transactional(function () use($account) {
                        $account->setBlocked(false);
                        $account->setToken("");
                        $account->update();
                        notify("accounts.activation", $account);
                    });
                    success(__("Account activated successfully"));
                } catch (Exception $ex) {
                    error($ex->getMessage());
                }
            }
        }
        View::display("accounts/activation");
    } else {
        App::notFound();
    }
});
Route::get("/generate-password/:username/:token", function ($username, $token) {
    $account = new Morpheus\Account\Account($username);
    $error = false;
    if (!$account->exists()) {
        $error = __("Account not exists");
    } else {
        $t = $account->getToken();
        if (empty($t) || $t !== $token) {
            $error = __("Invalid token");
        } else {
            try {
                Connection::transactional(function () use($account) {
                    $account->setToken("");
                    $account->update();
                    $password = password();
                    $account->updatePassword($password);
                    $mail = new Morpheus\Network\Mail();
                    $mail->addAddress($account->getEmail(), $account->getName())->setSubject(config("server.name") . " - " . __("New password"))->setMessageFromTemplate("new-password", array("account" => $account, "password" => $password));
                    if (!$mail->send()) {
                        throw new Exception($mail->getError());
                    }
                });
            } catch (Exception $ex) {
                $error = $ex->getMessage();
            }
        }
    }
    View::display("accounts/generate-password", array("error" => $error));
});
Route::map("/forgot-password", function () {
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("username" => array(Morpheus\Core\Validation::REQUIRED), "email" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::EMAIL), "captcha" => array(Morpheus\Core\Validation::CAPTCHA)));
        if ($validation->isValid()) {
            $account = account(Input::post("username"));
            if ($account->exists() && $account->getEmail() === Input::post("email")) {
                try {
                    $md5 = config("use_md5", false);
                    if ($md5) {
                        $account->setToken(md5(uniqid(rand(), true)));
                        $account->update();
                    }
                    $mail = new Morpheus\Network\Mail();
                    $mail->addAddress($account->getEmail(), $account->getName())->setSubject(config("server.name") . " - " . __("Recover password"))->setMessageFromTemplate("forgot-password", array("account" => $account, "md5" => $md5));
                    if ($mail->send()) {
                        return success(__("Your password has been sent to your email"), array("redirect" => "/forgot-password"));
                    }
                    return error($mail->getError());
                } catch (Exception $ex) {
                    return error($ex);
                }
            } else {
                return error(__("Data does not match"));
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("accounts/forgot-password");
})->via("GET", "POST");
Route::get("/panel/account/info", service("account.info"), function () {
    $account = user()->toArray();
    View::service("accounts/info", "account.info", array("account" => $account));
});
Route::get("/panel/account/last-connections", service("account.last-connections"), function () {
    $account = user();
    View::service("accounts/last-connections", "account.last-connections", array("connections" => $account->getLastConnections()));
});
Route::map("/panel/account/change-pid", service("account.change-pid"), function () {
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("current_pid" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "pid" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS, __("Campo deve ter obrigatóriamente 7 digitos") => array("rule" => array("length", 7))), "repid" => array(Morpheus\Core\Validation::REQUIRED, __("Chaves não conferem") => array("rule" => array("equals", "pid")))));
        if ($validation->isValid()) {
            if (user()->getPersonalId() !== Input::post("current_pid")) {
                return error(__("Invalid Personal ID"));
            }
            try {
                user()->setPersonalId(Input::post("pid"));
                user()->update();
                return success(__("Persoanl ID changed successfully"), array("redirect" => "/panel/account/change-pid"));
            } catch (Exception $ex) {
                return error($ex);
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::service("accounts/change-pid", "account.change-pid");
})->via("GET", "POST");
Route::map("/panel/account/change-password", service("account.change-password"), function () {
    $account = user();
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("current_password" => array(Morpheus\Core\Validation::REQUIRED), "password" => array(Morpheus\Core\Validation::REQUIRED), "repassword" => array(Morpheus\Core\Validation::REQUIRED, __("Senhas não conferem") => array("rule" => array("equals", "password")))));
        if ($validation->isValid()) {
            $password = Input::post("current_password");
            if (config("use_md5", false)) {
                $password = Connection::fetchColumn("select dbo.hashmd5('" . $account->getUsername() . "', '" . $password . "')");
            }
            if ($account->getPassword() !== $password) {
                return error(__("Invalid password"));
            }
            try {
                $account->updatePassword(Input::post("password"));
                return success(__("Password changed successfully"), array("redirect" => "/panel/account/change-password"));
            } catch (Exception $ex) {
                return error($ex);
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::service("accounts/change-password", "account.change-password");
})->via("GET", "POST");
Route::map("/panel/account/clear-vault", service("account.clear-vault"), function () {
    if (Request::isPost()) {
        $items = Input::post("items");
        $zen = Input::post("zen");
        if (empty($items) && empty($zen)) {
            return error(__("Select any option"));
        }
        $warehouse = user()->getWarehouse();
        try {
            $warehouse->clear((bool) $items, (bool) $zen);
            $text = array();
            if ($items) {
                $text[] = "Itens";
            }
            if ($zen) {
                $text[] = "Zen";
            }
            if ($zen && empty($items)) {
                $message = "was cleaned of your warehouse";
            } else {
                $message = "were foram cleaned of your warehouse";
            }
            return success(util("Text")->toList($text, " e ") . " " . __($message), array("redirect" => "/panel/account/clear-vault"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    View::service("accounts/clear-vault", "account.clear-vault");
})->via("GET", "POST");
Route::map("/panel/account/repair-items", service("account.repair-items"), function () {
    if (Request::isPost()) {
        try {
            $warehouse = user()->getWarehouse();
            $warehouse->load();
            for ($y = 0; $y < $warehouse->getHeight(); $y++) {
                for ($x = 0; $x < $warehouse->getWidth(); $x++) {
                    $item = $warehouse->getItem($x, $y);
                    if ($item !== NULL && !$item->isHexEmpty()) {
                        $item->parse()->repair();
                    }
                }
            }
            $warehouse->update();
            return success(__("Items has been repaired"), array("redirect" => "/panel/account/repair-items"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    View::service("accounts/repair-items", "account.repair-items");
})->via("GET", "POST");

?>