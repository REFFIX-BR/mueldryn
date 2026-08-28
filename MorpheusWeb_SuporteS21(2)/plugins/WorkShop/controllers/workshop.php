<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

View::add("script", "WorkShop.workshop");
Route::get("/workshop", authenticate(), function () {
    View::display("WorkShop.index");
});
Route::get("/workshop/inventory(/:character)(/:slot)", authenticate(), function ($character = NULL, $slot = NULL) {
    $characters = user()->getCharacters();
    $item = NULL;
    $inventory = NULL;
    if ($character !== NULL) {
        $character = character($character, user()->getUsername());
        if ($character->exists()) {
            $inventory = $character->getInventory();
            $inventory->load();
            if ($slot !== NULL) {
                $item = $inventory->getItemBySlot($slot);
                if ($item !== NULL) {
                    $blockeds = config("workshop.blocked_items", array());
                    View::replace(array("blocked" => !empty($blockeds) && in_array($item->getSection() . "-" . $item->getIndex(), $blockeds), "coin" => config("coins")[config("workshop.coin")]["name"]));
                }
            }
        }
    }
    View::display("WorkShop.inventory", array("character" => $character, "item" => $item, "slot" => $slot, "characters" => $characters, "inventory" => $inventory));
});
Route::get("/workshop/warehouse(/:slot)", authenticate(), function ($slot = NULL) {
    $extended = Input::get("extended");
    $account = user();
    $warehouse = $account->getWarehouse();
    $warehouse->create();
    $warehouse->load();
    $item = NULL;
    if ($slot !== NULL) {
        $item = $warehouse->getItemBySlot($slot);
        if ($item !== NULL) {
            $blockeds = config("workshop.blocked_items", array());
            View::replace(array("blocked" => !empty($blockeds) && in_array($item->getSection() . "-" . $item->getIndex(), $blockeds), "coin" => config("coins")[config("workshop.coin")]["name"]));
        }
    }
    View::display("WorkShop.warehouse", array("warehouse" => $warehouse, "slot" => $slot, "item" => $item, "extended" => $extended));
});
Route::post("/workshop/warehouse/:slot/upgrade", authenticate(), function ($slot) {
    $post = Input::post();
    $account = user();
    $warehouse = $account->getWarehouse();
    $warehouse->load();
    $extended = Input::get("extended");
    if (user()->isConnected()) {
        return error(__("You need to quit the game"));
    }
    try {
        $item = $warehouse->getItemBySlot($slot);
        if ($item !== NULL) {
            $old = strtoupper($item->getHex());
            $price = 0;
            $blockeds = config("workshop.blocked_items", array());
            $blocked = !empty($blockeds) && in_array($item->getSection() . "-" . $item->getIndex(), $blockeds);
            if ($blocked) {
                return error(__("Item blocked for upgrade"));
            }
            if ($post["level"] != $item->getLevel()) {
                if (config("workshop.max_level", 0) < $post["level"]) {
                    return error(__("The maximum level of the item is +%d", array(config("workshop.max_level", 0))));
                }
                $levels = $post["level"] - $item->getLevel();
                $item->addLevel($levels);
                $price += config("workshop.prices.level", 0) * $levels;
            }
            if ($post["option"] != $item->getOption()) {
                if (config("workshop.max_option", 0) < $post["option"]) {
                    return error(__("The maximum option of the item is +%d", array(config("workshop.max_option", 0))));
                }
                $options = $post["option"] - $item->getOption();
                $item->addOption($options);
                $price += config("workshop.prices.option", 0) * $options / 4;
            }
            if (isset($post["skill"]) && $post["skill"] == 1) {
                $item->addSkill();
                $price += config("workshop.prices.skill", 0);
            }
            if (isset($post["luck"]) && $post["luck"] == 1) {
                $item->addLuck();
                $price += config("workshop.prices.luck", 0);
            }
            if (isset($post["excellent"])) {
                foreach ($post["excellent"] as $index => $excellent) {
                    if ($excellent) {
                        $item->addExcellent($index, true);
                        $price += config("workshop.prices.excellent", 0);
                    }
                    $excellents = count(array_filter($item->getExcellents(), function ($exc) {
                        return $exc;
                    }));
                    if (config("workshop.max_excellent", 0) < $excellents) {
                        return error(__("The item accepts only %d excellent options", array(config("workshop.max_excellent", 0))));
                    }
                }
            }
            if (config("workshop.allow_refine", 0) && isset($post["refine"]) && $post["refine"] == 1) {
                $item->addRefine();
                $price += config("workshop.prices.refine", 0);
            }
            if (config("workshop.allow_harmony", 0) && isset($post["harmony"]) && $item->getDefinitions()->hasHarmony()) {
                if (empty($post["harmony"]) && 0 < $item->getHarmony()->getType()) {
                    $item->addHarmony(0, 0);
                    $price += config("workshop.prices.harmony", 0);
                } else {
                    if (!empty($post["harmony"]) && $item->getHarmony()->getType() . "," . $item->getHarmony()->getLevel() !== $post["harmony"]) {
                        list($htype, $hlevel) = explode(",", $post["harmony"]);
                        $item->addHarmony($htype, $hlevel);
                        $price += config("workshop.prices.harmony", 0);
                    }
                }
            }
            if (config("workshop.allow_ancient", 0) && isset($post["ancient"])) {
                if (empty($post["ancient"]) && $item->getAncient()->has()) {
                    $item->setAncient(0);
                    $price += config("workshop.prices.ancient", 0);
                } else {
                    if (!empty($post["ancient"]) && $post["ancient"] != $item->getAncient()->get()) {
                        $item->setAncient($post["ancient"]);
                        $price += config("workshop.prices.ancient", 0);
                    }
                }
            }
            if (config("workshop.allow_sockets", 0) && isset($post["socket"]) && util("Item")->hasSockets($item->getSection(), $item->getIndex())) {
                foreach ($post["socket"] as $index => $sock) {
                    $osocket = $item->getSocket($index)->get();
                    if ($osocket != $sock) {
                        $price += config("workshop.prices.socket", 0);
                        $allowedSockets = (new Morpheus\Item\Socket())->available($item->getSection());
                        $allowedSockets = util("Hash")->extract($allowedSockets, "{s}.{n}.value");
                        $allowedSockets = array_merge($allowedSockets, array(Morpheus\Item\Socket::getNoValue(), Morpheus\Item\Socket::getEmptyValue()));
                        $okSocket = true;
                        if (!in_array($sock, $allowedSockets)) {
                            $okSocket = false;
                        }
                        if (!$okSocket) {
                            return error(__("Invalid socket make your item again"));
                        }
                        $item->addSocket($index, $sock);
                        $filter = array();
                        $countSockets = 0;
                        foreach ($item->getSockets() as $socket) {
                            if ($socket->hasValue()) {
                                $filter[] = $socket->get();
                            }
                            if ($socket->hasValue() || $socket->isEmpty()) {
                                $countSockets++;
                            }
                        }
                        if (config("workshop.max_sockets", 0) < $countSockets) {
                            return error(__("The item accepts only %d sockets options", array(config("workshop.max_sockets", 0))));
                        }
                        if (config("workshop.unique_sockets")) {
                            $socks = array_count_values($filter);
                            foreach ($socks as $count) {
                                if (1 < $count) {
                                    return error(__("Duplicated socket"));
                                }
                            }
                        }
                    }
                }
            }
            if (in_array(config("server.team", "gmo"), array("xteam", "muemu")) && $item->getHarmony()->has() && $item->hasSocket()) {
                return error(__("Item can't be sockets and harmony"));
            }
        }
        $coin = config("workshop.coin");
        if ($coin !== NULL && user()->getCoin($coin) < $price) {
            return error(__("You do not have enough %s", array(config("coins")[$coin]["name"])));
        }
        Connection::transactional(function () use($warehouse, $price, $coin, $item, $old) {
            $warehouse->update();
            if (0 < $price) {
                user()->addCoin($coin, 0 - $price);
                user()->update();
            }
            user()->log(_e("Upgrade item %s to %s by %s", array($old, $item->getHex(), $price . " " . util("Coin")->name($coin))), "workshop", NULL, NULL, array("old" => $old, "new" => $item->getHex()));
        });
        return success(__("Item was updated"), array("redirect" => "/workshop/warehouse/" . $slot . ($extended !== NULL ? "?extended" : ""), "partial" => "login"));
    } catch (Exception $ex) {
        return error($ex);
    }
});
Route::post("/workshop/inventory/:character/:slot/upgrade", authenticate(), function ($character, $slot) {
    if (user()->isConnected()) {
        return error(__("You need to quit the game"));
    }
    try {
        $post = Input::post();
        $character = character($character, user()->getUsername());
        $inventory = $character->getInventory();
        $inventory->load();
        if ($slot !== NULL) {
            $item = $inventory->getItemBySlot($slot);
            if ($item !== NULL) {
                $old = strtoupper($item->getHex());
                $price = 0;
                $blockeds = config("workshop.blocked_items", array());
                $blocked = !empty($blockeds) && in_array($item->getSection() . "-" . $item->getIndex(), $blockeds);
                if ($blocked) {
                    return error(__("Item blocked for upgrade"));
                }
                if ($post["level"] != $item->getLevel()) {
                    if (config("workshop.max_level", 0) < $post["level"]) {
                        return error(__("The maximum level of the item is +%d", array(config("workshop.max_level", 0))));
                    }
                    $levels = $post["level"] - $item->getLevel();
                    $item->addLevel($levels);
                    $price += config("workshop.prices.level", 0) * $levels;
                }
                if ($post["level"] != $item->getOption()) {
                    if (config("workshop.max_option", 0) < $post["option"]) {
                        return error(__("The maximum option of the item is +%d", array(config("workshop.max_option", 0))));
                    }
                    $options = $post["option"] - $item->getOption();
                    $item->addOption($options);
                    $price += config("workshop.prices.option", 0) * $options / 4;
                }
                if (isset($post["skill"]) && $post["skill"] == 1) {
                    $item->addSkill();
                    $price += config("workshop.prices.skill", 0);
                }
                if (isset($post["luck"]) && $post["luck"] == 1) {
                    $item->addLuck();
                    $price += config("workshop.prices.luck", 0);
                }
                if (isset($post["excellent"])) {
                    foreach ($post["excellent"] as $index => $excellent) {
                        if ($excellent) {
                            $item->addExcellent($index, true);
                            $price += config("workshop.prices.excellent", 0);
                        }
                        $excellents = count(array_filter($item->getExcellents(), function ($exc) {
                            return $exc;
                        }));
                        if (config("workshop.max_excellent", 0) < $excellents) {
                            return error(__("The item accepts only %d excellent options", array(config("workshop.max_excellent", 0))));
                        }
                    }
                }
                if (config("workshop.allow_refine", 0) && isset($post["refine"]) && $post["refine"] == 1) {
                    $item->addRefine();
                    $price += config("workshop.prices.refine", 0);
                }
                if (config("workshop.allow_harmony", 0) && isset($post["harmony"]) && $item->getDefinitions()->hasHarmony()) {
                    if (empty($post["harmony"]) && 0 < $item->getHarmony()->getType()) {
                        $item->addHarmony(0, 0);
                        $price += config("workshop.prices.harmony", 0);
                    } else {
                        if (!empty($post["harmony"]) && $item->getHarmony()->getType() . "," . $item->getHarmony()->getLevel() !== $post["harmony"]) {
                            list($htype, $hlevel) = explode(",", $post["harmony"]);
                            $item->addHarmony($htype, $hlevel);
                            $price += config("workshop.prices.harmony", 0);
                        }
                    }
                }
                if (config("workshop.allow_ancient", 0) && isset($post["ancient"])) {
                    if (empty($post["ancient"]) && $item->getAncient()->has()) {
                        $item->setAncient(0);
                        $price += config("workshop.prices.ancient", 0);
                    } else {
                        if (!empty($post["ancient"]) && $post["ancient"] != $item->getAncient()->get()) {
                            $item->setAncient($post["ancient"]);
                            $price += config("workshop.prices.ancient", 0);
                        }
                    }
                }
                if (config("workshop.allow_sockets", 0) && isset($post["socket"]) && util("Item")->hasSockets($item->getSection(), $item->getIndex())) {
                    foreach ($post["socket"] as $index => $sock) {
                        $osocket = $item->getSocket($index)->get();
                        if ($osocket != $sock) {
                            $price += config("workshop.prices.socket", 0);
                            $allowedSockets = (new Morpheus\Item\Socket())->available($item->getSection());
                            $allowedSockets = util("Hash")->extract($allowedSockets, "{s}.{n}.value");
                            $allowedSockets = array_merge($allowedSockets, array(Morpheus\Item\Socket::getNoValue(), Morpheus\Item\Socket::getEmptyValue()));
                            $okSocket = true;
                            if (!in_array($sock, $allowedSockets)) {
                                $okSocket = false;
                            }
                            if (!$okSocket) {
                                return error(__("Invalid socket make your item again"));
                            }
                            $item->addSocket($index, $sock);
                            $filter = array();
                            foreach ($item->getSockets() as $socket) {
                                if ($socket->hasValue()) {
                                    $filter[] = $socket->get();
                                }
                            }
                            if (config("workshop.max_sockets", 0) < count($filter)) {
                                return error(__("The item accepts only %d sockets options", array(config("workshop.max_sockets", 0))));
                            }
                            if (config("workshop.unique_sockets")) {
                                $socks = array_count_values($filter);
                                foreach ($socks as $count) {
                                    if (1 < $count) {
                                        return error(__("Duplicated socket"));
                                    }
                                }
                            }
                        }
                    }
                }
                if (in_array(config("server.team", "gmo"), array("xteam", "muemu")) && 0 < $item->getHarmony()->has() && $item->hasSocket()) {
                    return error(__("Item can't be sockets and harmony"));
                }
            }
        }
        $coin = config("workshop.coin");
        if ($coin !== NULL && user()->getCoin($coin) < $price) {
            return error(__("You do not have enough %s", array(config("coins")[$coin]["name"])));
        }
        Connection::transactional(function () use($inventory, $price, $coin, $item, $old) {
            $inventory->update();
            if (0 < $price) {
                user()->addCoin($coin, 0 - $price);
                user()->update();
            }
            user()->log(_e("Upgrade item %s to %s by %s", array($old, $item->getHex(), $price . " " . util("Coin")->name($coin))), "workshop", NULL, NULL, array("old" => $old, "new" => $item->getHex()));
        });
        return success(__("Item was updated"), array("redirect" => "/workshop/inventory/" . $character->getName() . "/" . $slot, "partial" => "login"));
    } catch (Exception $ex) {
        return error($ex->getMessage());
    }
});

?>