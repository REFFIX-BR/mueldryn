<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/panel/character/:character/info", authenticate(), function ($character) {
    $character = new Morpheus\Character\Character($character, user()->getUsername());
    View::service("characters/info", "character.info", array("character" => $character));
});
Route::map("/panel/character/:character/transfer-resets", service("character.transfer-resets"), function ($character) {
    $character = new Morpheus\Character\Character($character, user()->getUsername());
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("resets" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "character" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            if (user()->isConnected()) {
                return error(__("You need to quit the game"));
            }
            try {
                $resets = (int) Input::post("resets");
                if ($character->getResets() < $resets) {
                    return error(__("Insufficient resets"));
                }
                Connection::transactional(function () use($character, $resets) {
                    $char = new Morpheus\Character\Character(Input::post("character"), user()->getUsername());
                    $char->addResets($resets);
                    $char->update();
                    $character->addResets(0 - $resets);
                    $character->update();
                    Morpheus\Account\Service::payRates("character.transfer-resets", user(), $character);
                });
                return success(__("Resets successfully transferred"), array("redirect" => "/panel/character/" . $character->getName() . "/transfer-resets", "partial" => "login"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    $available = array();
    foreach (user()->getCharacters() as $c) {
        if ($character->getName() === $c->getName()) {
            continue;
        }
        $available[] = $c->getName();
    }
    View::service("characters/transfer-resets", "character.transfer-resets", array("character" => $character, "available" => $available));
})->via("GET", "POST");
Route::map("/panel/character/:character/move", service("character.move"), function ($character) {
    $character = new Morpheus\Character\Character($character, user()->getUsername());
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("map" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            if (user()->isConnected()) {
                return error(__("You need to quit the game"));
            }
            if (!config("move.when_pk", false) && $character->isPk()) {
                return error(__("You can't move while you are pk"));
            }
            try {
                Connection::transactional(function () use($character) {
                    $map = config("maps")[Input::post("map")];
                    list($x, $y) = explode("x", $map["born"]);
                    $character->move(Input::post("map"), $x, $y);
                    Morpheus\Account\Service::payRates("character.move", user(), $character);
                });
                return success(__("Character moved to %s successfully", array(util("Character")->mapName(Input::post("map")))), array("redirect" => "/panel/character/" . $character->getName() . "/move", "partial" => "login"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::service("characters/move", "character.move", array("character" => $character));
})->via("GET", "POST");
Route::map("/panel/character/:character/clear-inventory", service("character.clear-inventory"), function ($character) {
    $character = new Morpheus\Character\Character($character, user()->getUsername());
    if (Request::isPost()) {
        $pid = Request::post("pid", false);
        if (!$pid) {
            return error("", array("required_pid" => true));
        }
        if (user()->getPersonalId() !== $pid) {
            return error(__("Personal ID is incorrect"));
        }
        try {
            Connection::transactional(function () use($character) {
                $character->getInventory()->clear();
                Morpheus\Account\Service::payRates("character.clear-inventory", user(), $character);
            });
            return success(__("Clean inventory successfully"), array("redirect" => "/panel/character/" . $character->getName() . "/clear-inventory", "partial" => "login"));
        } catch (Morpheus\Database\Exception $ex) {
            return error($ex);
        }
    }
    View::service("characters/clear-inventory", "character.clear-inventory", array("character" => $character));
})->via("GET", "POST");
Route::map("/panel/character/:character/change-nick", service("character.change-nick"), function ($character) {
    $character = new Morpheus\Character\Character($character, user()->getUsername());
    if (Request::isPost()) {
        $validate = new Morpheus\Core\Validation(Input::post(), array("nickname" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ALPHANUMERIC, __("Field must contain a min %s characters", array(4)) => array("rule" => array("minLength", 4)), __("Field must contain a max %s characters", array(10)) => array("rule" => array("maxLength", 10)))));
        if ($validate->isValid()) {
            if (user()->isConnected()) {
                return error(__("You need to quit the game"));
            }
            $reserved = array("webzen", "adm", "gm", "md", "nt", "dv", "gamemaster", "admin", "moderator", "support", "staff", "supportstaff", "administrator");
            foreach ($reserved as $word) {
                if (strpos(strtolower(Input::post("nickname")), $word) !== false) {
                    return error(__("Can't use the words %s", array(util("Text")->toList($reserved))));
                }
            }
            if ($character->inGuild()) {
                return error(__("Your character can't be in any guild"));
            }
            $c = new Morpheus\Character\Character(Input::post("nickname"));
            if ($c->exists()) {
                return error(__("Nickname currently in use by another character"));
            }
            try {
                Connection::transactional(function () use($character) {
                    $character->rename(Input::post("nickname"));
                    Morpheus\Account\Service::payRates("character.change-nick", user(), $character);
                });
                return success(__("Nickname changed to %s successfully", array(Input::post("nickname"))), array("redirect" => "/panel/character/" . Input::post("nickname") . "/change-nick", "partial" => "login"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validate->getErrors());
        }
    }
    View::service("characters/change-nick", "character.change-nick", array("character" => $character));
})->via("GET", "POST");
Route::map("/panel/character/:character/change-class", service("character.change-class"), function ($character) {
    $character = new Morpheus\Character\Character($character, user()->getUsername());
    if (Request::isPost()) {
        $validate = new Morpheus\Core\Validation(Input::post(), array("class" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validate->isValid()) {
            $class = Input::post("class");
            if (user()->isConnected()) {
                return error(__("You need to quit the game"));
            }
            $inventory = $character->getInventory();
            $inventory->load();
            if ($inventory->hasItemsEquipped()) {
                return error(__("You can't have items equipped"));
            }
            try {
                Connection::transactional(function () use($character, $class) {
                    $character->changeClass($class);
                    if (config("change_class.equalize_points", false) && util("Character")->isClass($character->getClass(), array("MG", "DM", "MK", "DUK", "DL", "LE", "EL", "FOE", "RF", "FM", "FB", "BF", "GL", "ML", "SHL", "AL"))) {
                        $defaults = $character->getDefaultProperties();
                        if ($defaults["Strength"] < $character->getStrength()) {
                            $character->setStrength($character->getStrength() - $character->getStrength() * 0.14);
                        }
                        if ($defaults["Agility"] < $character->getAgility()) {
                            $character->getAgility($character->getAgility() - $character->getAgility() * 0.14);
                        }
                        if ($defaults["Energy"] < $character->getEnergy()) {
                            $character->getEnergy($character->getEnergy() - $character->getEnergy() * 0.14);
                        }
                        if ($defaults["Vitality"] < $character->getVitality()) {
                            $character->getVitality($character->getVitality() - $character->getVitality() * 0.14);
                        }
                        if ($defaults["Leadership"] < $character->getCommand()) {
                            $character->getCommand($character->getCommand() - $character->getCommand() * 0.14);
                        }
                        $character->update();
                    }
                    Morpheus\Account\Service::payRates("character.change-class", user(), $character);
                });
                return success(__("Class changed to %s successfully", util("Character")->className($class)), array("redirect" => "/panel/character/" . $character->getName() . "/change-class", "partial" => "login"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validate->getErrors());
        }
    }
    View::service("characters/change-class", "character.change-class", array("character" => $character));
})->via("GET", "POST");
Route::map("/panel/character/:character/redistribute-points", service("character.redistribute-points"), function ($character) {
    $character = new Morpheus\Character\Character($character, user()->getUsername());
    $totalPoints = $character->getPoints();
    $defaults = $character->getDefaultProperties();
    $totalPoints += $character->getStrength() - $defaults["Strength"];
    $totalPoints += $character->getAgility() - $defaults["Dexterity"];
    $totalPoints += $character->getVitality() - $defaults["Vitality"];
    $totalPoints += $character->getEnergy() - $defaults["Energy"];
    if (util("Character")->isClass($character->getClass(), array("DL", "LE", "EL", "FOE"))) {
        $totalPoints += $character->getCommand() - $defaults["Leadership"];
    }
    if (Request::isPost()) {
        if (user()->isConnected()) {
            return error(__("You need to quit the game"));
        }
        $character->setPoints($totalPoints)->setStrength($defaults["Strength"])->setAgility($defaults["Dexterity"])->setVitality($defaults["Vitality"])->setEnergy($defaults["Energy"]);
        if (isset($defaults["Leadership"])) {
            $character->setCommand($defaults["Leadership"]);
        }
        try {
            Connection::transactional(function () use($character) {
                $character->update();
                Morpheus\Account\Service::payRates("character.redistribute-points", user(), $character);
            });
            return success(__("Redistributed points successfully completed"), array("redirect" => "/panel/character/" . $character->getName() . "/redistribute-points", "partial" => "login"));
        } catch (Exception $ex) {
            return error($ex);
        }
    }
    View::service("characters/redistribute-points", "character.redistribute-points", array("character" => $character, "totalPoints" => $totalPoints));
})->via("GET", "POST");
Route::map("/panel/character/:character/master-reset", service("character.master-reset"), function ($character) {
    $character = new Morpheus\Character\Character($character, user()->getUsername());
    if (Request::isPost()) {
        if (user()->isConnected()) {
            return error(__("You need to quit the game"));
        }
        $full = false;
        if (config("server.max_status") <= $character->getAgility() && config("server.max_status") <= $character->getStrength() && config("server.max_status") <= $character->getVitality() && config("server.max_status") <= $character->getEnergy()) {
            if (isset($character->getSchema()["Leadership"]) && util("Character")->isClass($character->getClass(), array("DL", "LE", "EL", "FOE"))) {
                $full = config("server.max_status") <= $character->getCommand();
            } else {
                $full = true;
            }
        }
        if (config("mr.only_full", true) && !$full) {
            return error(__("Your character needs to be full"));
        }
        $defaults = $character->getDefaultProperties();
        if (config("mr.type", 1) == 1) {
            $requirements = config("mr.requirements")[user()->getVipType()];
            $resets = $requirements["resets"];
            $level = $requirements["level"];
            if ($character->getResets() < $resets) {
                return error(__("You must have %s resets", array($resets)));
            }
            if ($character->getLevel() < $level) {
                return error(__("You must be at level %s", array($level)));
            }
            $character->setLevel(1);
            $character->setPoints(0);
            $character->setExperience(0);
            $character->setVitality($defaults["Vitality"]);
            $character->setStrength($defaults["Strength"]);
            $character->setAgility($defaults["Dexterity"]);
            $character->setEnergy($defaults["Energy"]);
            if (isset($character->getSchema()["Leadership"]) && util("Character")->isClass($character->getClass(), array("DL", "LE", "EL", "FOE"))) {
                $character->setCommand($defaults["Leadership"]);
            }
            $character->setMap($defaults["MapNumber"]);
            $character->setPositionX($defaults["MapPosX"]);
            $character->setPositionY($defaults["MapPosY"]);
            $character->setLife($defaults["Life"]);
            $character->setMaxLife($defaults["MaxLife"]);
            $character->setMana($defaults["Mana"]);
            $character->setMaxMana($defaults["MaxMana"]);
            if (isset($requirements["clear_resets"]) && $requirements["clear_resets"]) {
                $character->setResets(0);
            } else {
                $character->addResets(0 - $resets);
            }
            $character->addMasterResets(1);
            try {
                Connection::transactional(function () use($character, $defaults) {
                    $character->update();
                    if (config("mr.reset_class", false)) {
                        $character->changeClass($defaults["Class"]);
                    } else {
                        if (config("mr.clear_skills")) {
                            $character->clearSkills();
                        }
                    }
                    Morpheus\Account\Service::payRates("character.master-reset", user(), $character);
                });
                return success(__("Master Reset successfully completed"), array("redirect" => "/panel/character/" . $character->getName() . "/master-reset", "partial" => "login"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        }
    }
    $requirements = config("mr.requirements")[user()->getVipType()];
    View::service("characters/master-reset", "character.master-reset", array("character" => $character, "requirements" => $requirements));
})->via("GET", "POST");
Route::map("/panel/character/:character/rebuild-master-skill", service("character.rebuild-master-skill"), function ($character) {
    $character = new Morpheus\Character\Character($character, user()->getUsername());
    if (Request::isPost()) {
        if (user()->isConnected()) {
            return error(__("You need to quit the game"));
        }
        try {
            Connection::transactional(function () use($character) {
                $character->getMasterSkill()->reset();
                Morpheus\Account\Service::payRates("character.rebuild-master-skill", user(), $character);
            });
            return success(__("Master skill was resets successfully"), array("redirect" => "/panel/character/" . $character->getName() . "/rebuild-master-skill", "partial" => "login"));
        } catch (Morpheus\Database\Exception $ex) {
            return error($ex);
        }
    }
    View::service("characters/rebuild-master-skill", "character.rebuild-master-skill", array("character" => $character));
})->via("GET", "POST");

?>