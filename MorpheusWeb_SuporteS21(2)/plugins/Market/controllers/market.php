<?php
/*
 * Market — itens (UI Mudream + multi-joia)
 */

use Morpheus\Account\JewelBank;

function market_ensure_schema()
{
    static $done = false;
    if ($done) {
        return;
    }
    $done = true;
    $cols = array(
        'price_jb' => 'INT NOT NULL DEFAULT 0',
        'price_js' => 'INT NOT NULL DEFAULT 0',
        'price_jl' => 'INT NOT NULL DEFAULT 0',
        'price_jcr' => 'INT NOT NULL DEFAULT 0',
        'price_jc' => 'INT NOT NULL DEFAULT 0',
        'item_luck' => 'TINYINT NOT NULL DEFAULT 0',
        'item_skill' => 'TINYINT NOT NULL DEFAULT 0',
        'item_excellent' => 'TINYINT NOT NULL DEFAULT 0',
    );
    try {
        $existing = Connection::fetchAll(
            "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'mw_market_items'"
        );
        $have = array();
        foreach ($existing as $c) {
            $have[strtolower($c['COLUMN_NAME'])] = true;
        }
        foreach ($cols as $name => $def) {
            if (!isset($have[strtolower($name)])) {
                Connection::executeUpdate("ALTER TABLE mw_market_items ADD {$name} {$def}");
            }
        }
    } catch (Exception $e) {
        error_log('market_ensure_schema: ' . $e->getMessage());
    }
}

function market_jewel_prices_from_row($row)
{
    $prices = array(
        'jb' => isset($row['price_jb']) ? (int) $row['price_jb'] : 0,
        'js' => isset($row['price_js']) ? (int) $row['price_js'] : 0,
        'jl' => isset($row['price_jl']) ? (int) $row['price_jl'] : 0,
        'jcr' => isset($row['price_jcr']) ? (int) $row['price_jcr'] : 0,
        'jc' => isset($row['price_jc']) ? (int) $row['price_jc'] : 0,
    );
    // Legacy: single jewel payment_type
    $map = array('jb' => 'jb', 'js' => 'js', 'jl' => 'jl', 'jcr' => 'jcr', 'jc' => 'jc');
    if (!JewelBank::hasAnyPrice($prices) && isset($map[$row['payment_type']])) {
        $prices[$map[$row['payment_type']]] = (int) $row['price'];
    }
    return $prices;
}

market_ensure_schema();
\Morpheus\Account\SiteBank::ensureSchema();

Route::get('/market/sync-vault', authenticate(), function () {
    $login = user()->getUsername();
    try {
        $n = \Morpheus\OpenMU\VaultSync::syncAccount($login);
        $bytes = Connection::fetchColumn(
            'SELECT DATALENGTH(Items) FROM warehouse WHERE AccountID = ?',
            array($login)
        );
        return success("Baú atualizado: {$n} itens ({$bytes} bytes)", array('redirect' => '/market/sell'));
    } catch (Exception $e) {
        return error('Falha ao sincronizar baú: ' . $e->getMessage());
    }
});

app()->view()->add("script", array("Market.market"));
App::hook("perfil.character", function ($character) {
    $account = $character->getAccount();
    if ($account === null) {
        return;
    }
    $q = Connection::fetchAll(
        "select * from mw_market_items where bought_by is null and account = ?",
        array($account->getUsername())
    );
    $items = array();
    foreach ($q as $item) {
        $item["item"] = (new Morpheus\Item\Item($item["hex"]))->parse();
        $item["jewel_prices"] = market_jewel_prices_from_row($item);
        $items[] = $item;
    }
    echo View::fetch("Market.perfil", array("layout" => false, "items" => $items));
});

Route::get("/market/purchases", service("purchases.market"), function () {
    $q = Connection::fetchAll(
        "select * from mw_market_items where bought_by = ? order by bought_date desc",
        array(user()->getUsername())
    );
    $items = array();
    foreach ($q as $item) {
        $item["item"] = (new Morpheus\Item\Item($item["hex"]))->parse();
        $item["jewel_prices"] = market_jewel_prices_from_row($item);
        $items[] = $item;
    }
    View::service("Market.purchases", "purchases.market", array("items" => $items));
});

Route::get("/market", authenticate(), function () {
    $qMine = Connection::fetchAll(
        "select * from mw_market_items where account = ?",
        array(user()->getUsername())
    );
    $myitems = array();
    foreach ($qMine as $item) {
        $item["item"] = (new Morpheus\Item\Item($item["hex"]))->parse();
        $item["jewel_prices"] = market_jewel_prices_from_row($item);
        $myitems[] = $item;
    }

    $page = Input::get("page") == NULL ? 1 : (int) Input::get("page");
    $max = (int) config("market.items_page", 24);
    $init = (int) ($page - 1) * $max;

    $q = Request::get("q");
    $category = Request::get("category");
    $pay = Request::get("pay");
    $optLuck = Request::get("luck");
    $optSkill = Request::get("skill");
    $optExe = Request::get("exe");
    $tab = Request::get("tab") ?: "market";

    $params = array();
    $where = "account <> ? and bought_by is null";
    $params[] = user()->getUsername();
    $url = array();

    if ($q) {
        $where .= " and item_name like ?";
        $params[] = "%" . $q . "%";
        $url["q"] = $q;
    }
    if ($category !== NULL && $category !== "") {
        $where .= " and item_section = ?";
        $params[] = (int) $category;
        $url["category"] = $category;
    }
    if ($pay) {
        if ($pay === "jewels") {
            $where .= " and (payment_type = 'jewels' or payment_type in ('jb','js','jl','jcr','jc') or price_jb+price_js+price_jl+price_jcr+price_jc > 0)";
        } elseif ($pay === "coin") {
            $where .= " and payment_type = 'coin'";
        } elseif ($pay === "zen") {
            $where .= " and payment_type = 'zen'";
        } else {
            $where .= " and payment_type = ?";
            $params[] = $pay;
        }
        $url["pay"] = $pay;
    }
    if ($optLuck) {
        $where .= " and item_luck = 1";
        $url["luck"] = 1;
    }
    if ($optSkill) {
        $where .= " and item_skill = 1";
        $url["skill"] = 1;
    }
    if ($optExe) {
        $where .= " and item_excellent = 1";
        $url["exe"] = 1;
    }
    if ($tab && $tab !== "market") {
        $url["tab"] = $tab;
    }

    $qb2 = Connection::createQueryBuilder();
    $qb2->select("count(1) as total")->from("mw_market_items")->where($where);
    foreach ($params as $index => $param) {
        $qb2->setParameter($index, $param);
    }
    $total = $qb2->execute()->fetch();

    $qb = Connection::createQueryBuilder();
    $qb->select("*")->from("mw_market_items")->where($where)
        ->orderBy("id", "DESC")
        ->setFirstResult($init)->setMaxResults($max);
    foreach ($params as $index => $param) {
        $qb->setParameter($index, $param);
    }
    $query = $qb->execute();

    $qs = !empty($url) ? "&" . http_build_query($url) : "";
    $paginator = new Morpheus\Core\Paginator(
        $total["total"],
        $max,
        $page,
        url_to("/market?page=(:num)" . $qs)
    );

    $items = array();
    foreach ($query as $item) {
        $item["item"] = (new Morpheus\Item\Item($item["hex"]))->parse();
        $item["jewel_prices"] = market_jewel_prices_from_row($item);
        $items[] = $item;
    }

    View::display("Market.index", array(
        "myitems" => $myitems,
        "items" => $items,
        "paginator" => $paginator,
        "q" => $q,
        "category" => $category,
        "pay" => $pay,
        "optLuck" => $optLuck,
        "optSkill" => $optSkill,
        "optExe" => $optExe,
        "tab" => $tab,
        "totalItems" => (int) $total["total"],
        "jewelCatalog" => JewelBank::catalog(),
        "jewels" => JewelBank::counts(user()),
        "coins" => config("coins", array()),
    ));
});

Route::map("/market/sell(/:slot)", authenticate(), function ($slot = NULL) {
    $extended = Input::get("extended");
    $account = user();
    $login = $account->getUsername();
    $siteBankId = Input::get("site_bank");

    \Morpheus\Account\SiteBank::ensureSchema();
    $siteItems = \Morpheus\Account\SiteBank::listForLogin($login);

    $item = NULL;
    if ($siteBankId) {
        $bankRow = \Morpheus\Account\SiteBank::get($login, $siteBankId);
        if ($bankRow) {
            $item = (new \Morpheus\Item\Item($bankRow["hex"]))->parse();
        }
    }

    $payments = array(
        "coin" => __("Coin"),
        "zen" => __("Zen"),
        "jewels" => "Joias (multi)",
    );

    if (Request::isPost()) {
        $post = Input::post();
        if (user()->isConnected()) {
            return error(__("You need to quit the game"));
        }

        $postSiteBankId = isset($post["site_bank_id"]) ? (int) $post["site_bank_id"] : (int) $siteBankId;
        if ($postSiteBankId <= 0) {
            return error("Selecione um item do Banco do Site.");
        }

        $bankRow = \Morpheus\Account\SiteBank::get($login, $postSiteBankId);
        if (!$bankRow) {
            return error("Item não encontrado no Banco do Site.");
        }

        $sellHex = strtoupper(preg_replace('/[^0-9A-Fa-f]/', '', $bankRow["hex"]));
        if ($sellHex === "") {
            return error("Item inválido no Banco do Site.");
        }
        $itemObj = (new \Morpheus\Item\Item($sellHex))->parse();

        $payment = isset($post["payment"]) ? $post["payment"] : "";
        if (!array_key_exists($payment, $payments)) {
            return error(__("Payment is not allowed"));
        }

        $jewelPrices = JewelBank::parsePrices(array(
            "jb" => isset($post["price_jb"]) ? $post["price_jb"] : 0,
            "js" => isset($post["price_js"]) ? $post["price_js"] : 0,
            "jl" => isset($post["price_jl"]) ? $post["price_jl"] : 0,
            "jcr" => isset($post["price_jcr"]) ? $post["price_jcr"] : 0,
            "jc" => isset($post["price_jc"]) ? $post["price_jc"] : 0,
        ));

        $price = isset($post["price"]) ? (int) $post["price"] : 0;
        $coin = isset($post["coin"]) ? $post["coin"] : null;

        if ($payment === "jewels") {
            if (!JewelBank::hasAnyPrice($jewelPrices)) {
                return error("Defina a quantidade de pelo menos uma joia.");
            }
            $price = 0;
            $coin = null;
        } elseif ($payment === "coin") {
            if ($price <= 0 || empty($coin)) {
                return error("Informe a moeda e o valor.");
            }
        } else {
            if ($price <= 0) {
                return error("Informe o preço.");
            }
        }

        try {
            $exists = Connection::fetchAssoc(
                "select * from mw_market_items where hex = ? and bought_by is null",
                array($sellHex)
            );
            if (!empty($exists)) {
                return error(__("This item is already on the market"));
            }

            Connection::transactional(function ($conn) use ($itemObj, $payment, $coin, $price, $jewelPrices, $login, $postSiteBankId, $sellHex) {
                $conn->insert(
                    "mw_market_items",
                    array(
                        "hex" => $sellHex,
                        "payment_type" => $payment,
                        "coin" => $payment === "coin" ? $coin : null,
                        "price" => $price,
                        "account" => $login,
                        "selling_start_date" => date("Y-m-d"),
                        "item_section" => $itemObj->getSection(),
                        "item_name" => $itemObj->getName() ? $itemObj->getName() : "Item",
                        "item_level" => $itemObj->getLevel(),
                        "item_option" => $itemObj->getOption(),
                        "price_jb" => $jewelPrices["jb"],
                        "price_js" => $jewelPrices["js"],
                        "price_jl" => $jewelPrices["jl"],
                        "price_jcr" => $jewelPrices["jcr"],
                        "price_jc" => $jewelPrices["jc"],
                        "item_luck" => $itemObj->hasLuck() ? 1 : 0,
                        "item_skill" => $itemObj->hasSkill() ? 1 : 0,
                        "item_excellent" => $itemObj->isExcellent() ? 1 : 0,
                    )
                );
                \Morpheus\Account\SiteBank::takeForMarket($login, $postSiteBankId);
            });
            return success(__("Item added to the market"), array(
                "redirect" => "/market?tab=mine",
            ));
        } catch (Exception $ex) {
            return error($ex);
        }
    }

    View::display("Market.sell", array(
        "siteItems" => $siteItems,
        "siteBankId" => $siteBankId,
        "extended" => $extended,
        "item" => $item,
        "payments" => $payments,
        "jewelCatalog" => JewelBank::catalog(),
        "jewels" => JewelBank::counts(user()),
        "coins" => config("coins", array()),
    ));
})->via("GET", "POST");

Route::get("/market/buy/:id", authenticate(), function ($id) {
    $item = Connection::fetchAssoc(
        "select * from mw_market_items where id = ? and bought_by is null",
        array($id)
    );
    if (empty($item)) {
        return error(__("Item doen't exists"));
    }
    if (user()->isConnected()) {
        return error(__("You need to quit the game"));
    }
    $account = account($item["account"]);
    $target = user();
    $account->getWarehouse()->load();
    $target->getWarehouse()->load();

    $jewelPrices = market_jewel_prices_from_row($item);
    $useJewels = JewelBank::hasAnyPrice($jewelPrices);

    if ($item["payment_type"] === "coin") {
        if ($target->getCoin($item["coin"]) < $item["price"]) {
            return error(__("You don't have enough %s", array(config("coins")[$item["coin"]]["name"])));
        }
        $target->addCoin($item["coin"], 0 - $item["price"]);
        $account->addCoin($item["coin"], $item["price"]);
    } elseif ($item["payment_type"] === "zen") {
        if ($target->getWarehouse()->getMoney() < $item["price"]) {
            return error(__("You don't have enough %s", array("Zen")));
        }
        $target->getWarehouse()->addMoney(0 - $item["price"]);
        $account->getWarehouse()->addMoney($item["price"]);
    } elseif ($useJewels || $item["payment_type"] === "jewels") {
        $ok = JewelBank::canAfford($target->getUsername(), $jewelPrices);
        if ($ok !== true) {
            return error($ok);
        }
        try {
            JewelBank::transfer($target->getUsername(), $account->getUsername(), $jewelPrices);
        } catch (Exception $e) {
            return error($e->getMessage());
        }
    } else {
        // legacy single jewel via warehouse items
        $payments = array(
            "jc" => array("section" => 12, "index" => 15, "name" => __("Jewel of Chaos")),
            "jb" => array("section" => 14, "index" => 13, "name" => __("Jewel of Bless")),
            "js" => array("section" => 14, "index" => 14, "name" => __("Jewel of Soul")),
            "jl" => array("section" => 14, "index" => 16, "name" => __("Jewel of Life")),
            "jcr" => array("section" => 14, "index" => 22, "name" => __("Jewel of Creation")),
        );
        if (!isset($payments[$item["payment_type"]])) {
            return error("Tipo de pagamento inválido.");
        }
        $payment = $payments[$item["payment_type"]];
        $itemsPay = $target->getWarehouse()->getItemsByType($payment["section"], $payment["index"]);
        if (count($itemsPay) < $item["price"]) {
            return error(__("You don't have enough %s", array($payment["name"])));
        }
        for ($i = 0; $i < $item["price"]; $i++) {
            $target->getWarehouse()->removeItem($itemsPay[$i]);
            if (!$account->getWarehouse()->addItem($itemsPay[$i])) {
                return error(__("No space in the seller's warehouse"));
            }
        }
    }

    $it = new Morpheus\Item\Item($item["hex"]);
    $it->parse();

    // Entrega no Banco do Site (não no baú do jogo)
    try {
        Connection::transactional(function ($conn) use ($account, $target, $item, $it) {
            $target->update();
            $account->update();
            // Zen/legacy podem ter alterado warehouse — persiste só se necessário
            try {
                $target->getWarehouse()->update();
                $account->getWarehouse()->update();
            } catch (Exception $e) {
            }
            $conn->update(
                "mw_market_items",
                array("bought_by" => $target->getUsername(), "bought_date" => date("Y-m-d")),
                array("id" => $item["id"])
            );
            $dep = \Morpheus\Account\SiteBank::depositHex(
                $target->getUsername(),
                $item["hex"],
                "market"
            );
            if (!$dep) {
                throw new Exception("Não foi possível guardar o item no Banco do Site.");
            }
        });
        return success(
            "Compra concluída! O item foi para o Banco do Site. Saque para o baú do jogo quando quiser.",
            array("redirect" => "/market/bank", "partial" => "login")
        );
    } catch (Exception $ex) {
        return error($ex->getMessage());
    }
});

Route::get("/market/remove/:id", authenticate(), function ($id) {
    $item = Connection::fetchAssoc(
        "select * from mw_market_items where id = ? and bought_by is null and account = ?",
        array($id, user()->getUsername())
    );
    if (empty($item)) {
        return error(__("Item doen't exists"));
    }
    if (user()->isConnected()) {
        return error(__("You need to quit the game"));
    }
    // Cancelar anúncio → Banco do Site
    $i = (new Morpheus\Item\Item($item["hex"]))->parse();
    try {
        Connection::transactional(function ($conn) use ($item) {
            $conn->delete("mw_market_items", array("id" => $item["id"]));
            $dep = \Morpheus\Account\SiteBank::depositHex(user()->getUsername(), $item["hex"], "market_cancel");
            if (!$dep) {
                throw new Exception("Falha ao devolver item ao Banco do Site.");
            }
        });
        return success("Item removido do mercado e devolvido ao Banco do Site.", array("redirect" => "/market/bank"));
    } catch (Exception $ex) {
        return error($ex);
    }
});

// —— Banco do Site ——
Route::get("/market/bank", authenticate(), function () {
    $login = user()->getUsername();
    \Morpheus\Account\SiteBank::ensureSchema();
    $siteItems = \Morpheus\Account\SiteBank::listForLogin($login);
    $gameItems = array();
    $gameZen = 0;
    $bankError = null;
    $doSync = Input::get('sync') !== null;
    $openMuLoaded = false;

    // Baú do jogo: OpenMU (Postgres) é a fonte de verdade — nomes vêm de config."ItemDefinition"
    if (class_exists('\\Morpheus\\OpenMU\\VaultSync') && \Morpheus\OpenMU\Bridge::enabled()) {
        try {
            if ($doSync) {
                \Morpheus\OpenMU\VaultSync::syncAccount($login);
            }
            $gameItems = \Morpheus\OpenMU\VaultSync::listForLogin($login);
            $gameZen = \Morpheus\OpenMU\Bridge::getVaultMoney($login);
            $openMuLoaded = true;
        } catch (Exception $e) {
            $bankError = 'OpenMU indisponível: ' . $e->getMessage();
            error_log('bank openmu: ' . $e->getMessage());
        }
    }

    // Fallback MSSQL warehouse só se OpenMU não responder
    if (!$openMuLoaded) {
        try {
            $raw = Connection::fetchColumn(
                "SELECT CONVERT(VARCHAR(MAX), Items, 2) FROM warehouse WHERE AccountID = ?",
                array($login)
            );
            $money = Connection::fetchColumn(
                "SELECT Money FROM warehouse WHERE AccountID = ?",
                array($login)
            );
            if ($money !== null && $money !== false) {
                $gameZen = (int) $money;
            }
            if ($raw) {
                $wh = user()->getWarehouse();
                $wh->load($raw);
                $itemsMap = $wh->getItems();
                if (is_array($itemsMap)) {
                    foreach ($itemsMap as $x => $col) {
                        if (!is_array($col)) {
                            continue;
                        }
                        foreach ($col as $y => $it) {
                            if (!$it || !($it instanceof \Morpheus\Item\Item)) {
                                continue;
                            }
                            $slot = (int) $wh->getSlotByCord((int) $x, (int) $y);
                            $muName = null;
                            if (class_exists('\\Morpheus\\OpenMU\\Bridge') && \Morpheus\OpenMU\Bridge::enabled()) {
                                try {
                                    $muName = \Morpheus\OpenMU\Bridge::itemDefinitionName($it->getSection(), $it->getIndex());
                                    if ($muName === '') {
                                        $muName = null;
                                    }
                                } catch (Exception $ex) {
                                    $muName = null;
                                }
                            }
                            $gameItems[] = \Morpheus\Util\Item::bankItemView($it, array(
                                'id' => 'slot:' . $slot,
                                'slot' => $slot,
                                'durability' => (int) $it->getDurability(),
                                'skill' => $it->hasSkill(),
                            ), 48, $muName);
                        }
                    }
                }
            }
        } catch (Exception $e) {
            error_log('bank warehouse: ' . $e->getMessage());
        }
    }

    View::display("Market.bank", array(
        "siteItems" => $siteItems,
        "gameItems" => $gameItems,
        "gameZen" => $gameZen,
        "siteZen" => \Morpheus\Account\SiteBank::getZen($login),
        "connected" => user()->isConnected(),
        "bankError" => $bankError,
    ));
});

Route::post("/market/bank/withdraw/:id", authenticate(), function ($id) {
    if (user()->isConnected()) {
        return error(__("You need to quit the game"));
    }
    try {
        \Morpheus\Account\SiteBank::withdrawToGame(user()->getUsername(), (int) $id);
        return success("Item sacado para o baú do jogo.", array("redirect" => "/market/bank"));
    } catch (Exception $e) {
        return error($e->getMessage());
    }
});

Route::post("/market/bank/deposit", authenticate(), function () {
    if (user()->isConnected()) {
        return error(__("You need to quit the game"));
    }
    $vaultId = Input::post("vault_item_id");
    if (!$vaultId) {
        return error("Item inválido.");
    }
    // Espelho warehouse usa id slot:N — resolve pelo slot no OpenMU
    if (strpos($vaultId, 'slot:') === 0) {
        $slot = (int) substr($vaultId, 5);
        try {
            $login = user()->getUsername();
            $acc = \Morpheus\OpenMU\Bridge::findAccount($login);
            if (!$acc) {
                return error("Conta OpenMU não encontrada.");
            }
            $rows = \Morpheus\OpenMU\VaultSync::fetchVaultItems($acc['Id']);
            $found = null;
            foreach ($rows as $r) {
                if ((int) $r['ItemSlot'] === $slot) {
                    $found = $r['Id'];
                    break;
                }
            }
            if (!$found) {
                return error("Item não encontrado no baú do jogo. Clique em sincronizar (↻) e tente de novo.");
            }
            $vaultId = $found;
        } catch (Exception $e) {
            return error("OpenMU indisponível. Reinicie o Docker (database) e sincronize.");
        }
    }
    try {
        \Morpheus\Account\SiteBank::depositFromGame(user()->getUsername(), $vaultId);
        return success("Item depositado no Banco do Site.", array("redirect" => "/market/bank"));
    } catch (Exception $e) {
        return error($e->getMessage());
    }
});

Route::post("/market/bank/zen", authenticate(), function () {
    if (user()->isConnected()) {
        return error(__("You need to quit the game"));
    }
    $amount = (int) Input::post("amount");
    $dir = Input::post("dir");
    try {
        \Morpheus\Account\SiteBank::transferZen(user()->getUsername(), $amount, $dir);
        return success("Zen transferido.", array("redirect" => "/market/bank"));
    } catch (Exception $e) {
        return error($e->getMessage());
    }
});
