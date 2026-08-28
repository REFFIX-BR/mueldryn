<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

app()->view()->add("script", "WebShop.shop-product");
App::hook("webshop.offer", function () {
    $products = Connection::fetchAll("select top 5 i.*\n        , s.coin\n        , c.slug as category_slug\n        , s.slug as shop_slug\n        from mw_shop_items i\n        join mw_shop_categories c on c.id = i.category_id\n        join mw_shops s on s.id = c.shop_id\n        where i.offer = 1\n        order by i.solds desc\n    ");
    View::display("WebShop.offer", array("products" => $products, "layout" => false));
});
Route::get("/shops", function () {
    $shops = Connection::fetchAll("SELECT * FROM mw_shops WHERE active = 1 ORDER BY sequence");
    if (count($shops) === 1) {
        return App::redirect("/shop/" . $shops[0]["slug"]);
    }
    View::display("WebShop.index", array("shops" => $shops));
});
Route::get("/shop/purchases", service("purchases.webshop"), function () {
    $q = Connection::fetchAll("SELECT * FROM mw_logs WHERE account = ? and TYPE = ? ORDER BY create_date DESC", array(user()->getUsername(), "webshop"));
    $items = array();
    $blockeds = config("webshop.blocked_recover_items", array());
    foreach ($q as $log) {
        $metadata = json_decode($log["metadata"], true);
        $item = (new Morpheus\Item\Item($metadata["hex"]))->parse();
        $blocked = !empty($blockeds) && in_array($item->getSection() . "-" . $item->getIndex(), $blockeds);
        $log["metadata"] = $metadata;
        $log["item"] = $item;
        $log["in_market"] = (bool) Connection::fetchColumn("select 1 from mw_market_items where hex like ? and bought_by is null", array("%" . $log["item"]->getSerial() . "%"));
        $log["recoverable"] = !$blocked;
        $items[] = $log;
    }
    View::service("WebShop.purchases", "purchases.webshop", array("items" => $items));
});
Route::get("/shop/purchases/recover-item/:id", authenticate(), function ($id) {
    $log = Connection::fetchAssoc("SELECT * FROM mw_logs WHERE account = ? AND id = ?", array(user()->getUsername(), $id));
    if (empty($log)) {
        return App::notFound();
    }
    $metadata = json_decode($log["metadata"], true);
    $hex = $metadata["hex"];
    $item = (new Morpheus\Item\Item($hex))->parse();
    $blockeds = config("webshop.blocked_recover_items", array());
    $blocked = !empty($blockeds) && in_array($item->getSection() . "-" . $item->getIndex(), $blockeds);
    if ($blocked) {
        return error(__("Item blocked for recover"));
    }
    if (user()->isConnected()) {
        return error(__("You need to quit the game"));
    }
    $name = util("Item")->getFullName($item);
    $inMarket = (bool) Connection::fetchColumn("select 1 from mw_market_items where hex like ? and bought_by is null", array("%" . $item->getSerial() . "%"));
    if ($inMarket) {
        return error(__("The item %s have been in market", array($name)));
    }
    if ($account = Morpheus\Item\Finder::inWarehouse($item)) {
        if ($account->getUsername() === user()->getUsername()) {
            return error(__("The item %s have been in your warehouse", array($name)));
        }
        return error(__("The item %s have been in the warehouse of another account", array($name)));
    }
    if ($account = Morpheus\Item\Finder::inExtWarehouse($item)) {
        if ($account->getUsername() === user()->getUsername()) {
            return error(__("The item %s have been in your ext warehouse", array($name)));
        }
        return error(__("The item %s have been in the ext warehouse of another account", array($name)));
    }
    if ($character = Morpheus\Item\Finder::inInventory($item)) {
        if ($character->getAccount()->getUsername() === user()->getUsername()) {
            return error(__("The item %s have been in the inventory of your character %s", array($name, $character->getName())));
        }
        return error(__("The item %s have been in the inventory of another account", $name));
    }
    $warehouse = user()->getWarehouse();
    $warehouse->load();
    if (!$warehouse->addItem($item)) {
        return error(__("No space in your warehouse"));
    }
    try {
        $warehouse->update();
        return success(__("Item added to your warehouse"), array("redirect" => "/shop/purchases"));
    } catch (Exception $ex) {
        return error($ex->getMessage());
    }
});
Route::get("/shop/:slug", authenticate(), function ($slug) {
    $shop = Connection::fetchAssoc("SELECT * FROM mw_shops WHERE active = 1 and slug = ?", array($slug));
    if (empty($shop)) {
        App::notFound();
    } else {
        $page = Input::get("page") == NULL ? 1 : Input::get("page");
        $max = config("webshop.items_page", 15);
        $init = (int) ($page - 1) * $max;
        $categories = util("Hash")->nest(Connection::fetchAll("SELECT * FROM mw_shop_categories WHERE active = 1 and shop_id = ? ORDER BY sequence", array($shop["id"])), array("idPath" => "{n}.id", "parentPath" => "{n}.parent_id"));
        $total = Connection::fetchAssoc("select count(1) total \n            from mw_shop_items i\n            join mw_shop_categories c on c.id = i.category_id\n            where i.active = 1 \n            and c.shop_id = ?", array($shop["id"]));
        $qb = Connection::createQueryBuilder();
        $qb->select(array("i.*", "c.slug as category_slug"))->from("mw_shop_items", "i")->join("i", "mw_shop_categories", "c", "c.id = i.category_id")->where("i.active = 1 and c.shop_id = " . $qb->createPositionalParameter($shop["id"]))->orderBy("i.offer", "DESC")->addOrderBy("i.name")->setFirstResult($init)->setMaxResults($max);
        $products = $qb->execute()->fetchAll();
        $paginator = new Morpheus\Core\Paginator($total["total"], $max, $page, url_to("/shop/" . $shop["slug"] . "?page=(:num)"));
        View::display("WebShop.view", array("shop" => $shop, "categories" => $categories, "products" => $products, "paginator" => $paginator));
    }
});
Route::get("/shop/:shop/search", authenticate(), function ($shop) {
    $shop = Connection::fetchAssoc("SELECT * FROM mw_shops WHERE active = 1 and slug = ?", array($shop));
    if (empty($shop)) {
        App::notFound();
    } else {
        $q = Input::get("q");
        $products = Connection::fetchAll("SELECT TOP 20 i.*\n            , c.slug as category_slug\n            FROM mw_shop_items i\n            JOIN mw_shop_categories c ON c.id = i.category_id\n            WHERE i.active = 1 \n            AND lower(i.name) like ? \n        ", array("%" . strtolower($q) . "%"));
        $categories = util("Hash")->nest(Connection::fetchAll("SELECT * FROM mw_shop_categories WHERE active = 1 and shop_id = ? ORDER BY sequence", array($shop["id"])), array("idPath" => "{n}.id", "parentPath" => "{n}.parent_id"));
        View::display("WebShop.search", array("shop" => $shop, "products" => $products, "categories" => $categories, "q" => $q));
    }
});
Route::get("/shop/:slug/:category", authenticate(), function ($slug, $category) {
    $shop = Connection::fetchAssoc("SELECT * FROM mw_shops WHERE active = 1 and slug = ?", array($slug));
    $category = Connection::fetchAssoc("SELECT * FROM mw_shop_categories WHERE active = 1 and slug = ? and shop_id = ?", array($category, $shop["id"]));
    if (empty($shop) || empty($category)) {
        App::notFound();
    } else {
        $page = Input::get("page") == NULL ? 1 : Input::get("page");
        $max = config("webshop.items_page", 15);
        $init = (int) ($page - 1) * $max;
        $categories = util("Hash")->nest(Connection::fetchAll("SELECT * FROM mw_shop_categories WHERE active = 1 and shop_id = ? ORDER BY sequence", array($shop["id"])), array("idPath" => "{n}.id", "parentPath" => "{n}.parent_id"));
        $total = Connection::fetchAssoc("SELECT count(1) total FROM mw_shop_items WHERE active = 1 AND category_id = ?", array($category["id"]));
        $qb = Connection::createQueryBuilder();
        $qb->select("*")->from("mw_shop_items")->where("active = 1 and category_id = " . $qb->createPositionalParameter($category["id"]))->orderBy("offer", "DESC")->addOrderBy("name")->setFirstResult($init)->setMaxResults($max);
        $products = $qb->execute()->fetchAll();
        $paginator = new Morpheus\Core\Paginator($total["total"], $max, $page, url_to("/shop/" . $shop["slug"] . "/" . $category["slug"] . "?page=(:num)"));
        View::display("WebShop.products", array("shop" => $shop, "products" => $products, "paginator" => $paginator, "category" => $category, "categories" => $categories));
    }
});
Route::map("/shop/:slug/:category/:product", authenticate(), function ($slug, $category, $product) {
    $shop = Connection::fetchAssoc("SELECT * FROM mw_shops WHERE active = 1 and slug = ?", array($slug));
    $category = Connection::fetchAssoc("SELECT * FROM mw_shop_categories WHERE active = 1 and slug = ? and shop_id = ?", array($category, $shop["id"]));
    $product = Connection::fetchAssoc("SELECT * FROM mw_shop_items WHERE active = 1 and slug = ? and category_id = ?", array($product, $category["id"]));
    if (empty($shop) || empty($category) || empty($product)) {
        App::notFound();
    } else {
        $classes = array();
        if (!empty($product["classes"])) {
            foreach (explode(",", $product["classes"]) as $code) {
                $classes[$code] = config("characters.classes")[$code]["name"];
            }
        }
        $product["classes"] = $classes;
        $excellents = util("Item")->getExcellentOptions($product["section"], $product["index_"]);
        $harmonys = (new Morpheus\Item\Harmony())->available($product["section"]);
        $sockets = (new Morpheus\Item\Socket())->available($product["section"]);
        $maxExcellents = count($excellents) <= $product["max_excellent"] ? count($excellents) : $product["max_excellent"];
        $ancients = array();
        if (!empty($product["ancient"])) {
            foreach (explode(",", $product["ancient"]) as $anc) {
                $ancs = (new Morpheus\Item\Ancient())->available($product["section"], $product["index_"]);
                if (isset($ancs[$anc])) {
                    $ancients[$anc] = $ancs[$anc];
                }
            }
        }
        $prices = array("default" => $product["price"], "level" => $product["price_level"], "option" => $product["price_option"], "skill" => $product["price_skill"], "luck" => $product["price_luck"], "ancient" => $product["price_ancient"], "harmony" => $product["price_harmony"], "refine" => $product["price_refine"], "socket" => $product["price_socket"], "excellent" => $product["price_excellent"]);
        View::display("WebShop.product", array("shop" => $shop, "product" => $product, "category" => $category, "excellents" => $excellents, "ancients" => $ancients, "harmonys" => $harmonys, "sockets" => $sockets, "maxExcellents" => $maxExcellents, "prices" => $prices));
    }
})->via("GET", "POST");
Route::post("/shop/:slug/:category/:product/buy", authenticate(), function ($slug, $category, $product) {
    $shop = Connection::fetchAssoc("select * \n        from mw_shops \n        where active = 1 \n        and slug = ?\n    ", array($slug));
    $category = Connection::fetchAssoc("select * \n        from mw_shop_categories \n        where active = 1 \n        and slug = ? \n        and shop_id = ?\n    ", array($category, $shop["id"]));
    $product = Connection::fetchAssoc("select * \n        from mw_shop_items \n        where active = 1 \n        and slug = ? \n        and category_id = ?\n    ", array($product, $category["id"]));
    if (empty($shop) || empty($category) || empty($product)) {
        App::notFound();
    } else {
        $level = Input::post("level");
        $option = Input::post("option");
        $luck = Input::post("luck");
        $skill = Input::post("skill");
        $excellents = Input::post("excellents");
        $ancient = Input::post("ancient");
        $refine = Input::post("refine");
        $harmony = Input::post("harmony");
        $sockets = Input::post("sockets");
        $coupon = Input::post("coupon");
        if (user()->isConnected()) {
            return error(__("You need to quit the game"));
        }
        if (!empty($coupon)) {
            $coupon = Connection::fetchAssoc("select * \n                from mw_shop_coupons \n                where coupon = ? \n                and active = 1 \n                and start_date < getdate() \n                and (expire_date is null or expire_date >= getdate())\n            ", array($coupon));
            if (empty($coupon)) {
                return error(__("Coupon %s doen't exist or is invalid", array($coupon["coupon"])));
            }
        }
        if ($product["max_excellent"] < count($excellents)) {
            return error(__("The item accepts only %d excellent options", array($product["max_excellent"])));
        }
        if ($product["sockets"] && $product["max_sockets"] < count($sockets)) {
            return error(__("The item accepts only %d sockets options", array($product["max_sockets"])));
        }
        if ($shop["max_level"] < $level) {
            return error(__("The maximum level of the item is +%d", array($shop["max_level"])));
        }
        if ($shop["max_option"] < $option) {
            return error(__("The maximum option of the item is +%d", array($shop["max_option"])));
        }
        if (!$product["luck"]) {
            $luck = 0;
        }
        if (0 <= $product["fix_level"]) {
            $level = $product["fix_level"];
        }
        if (0 <= $product["fix_option"]) {
            $option = $product["fix_option"];
        }
        $price = $product["price"];
        $price += $product["fix_level"] < 0 ? $level * $product["price_level"] : 0;
        $price += $product["fix_option"] < 0 ? $option / 4 * $product["price_option"] : 0;
        $price += $luck ? $product["price_luck"] : 0;
        $price += $skill ? $product["price_skill"] : 0;
        $price += count($excellents) * $product["price_excellent"];
        $price += 0 < $ancient ? $product["price_ancient"] : 0;
        $price += $product["refine"] && $refine == 1 ? $product["price_refine"] : 0;
        $price += $product["harmony"] && $harmony ? $product["price_harmony"] : 0;
        $item = new Morpheus\Item\Item();
        if (!empty($sockets)) {
            $filter = array();
            foreach ($sockets as $socket) {
                if (!in_array($socket, array(Morpheus\Item\Socket::getNoValue()))) {
                    $filter[] = (int) $socket;
                }
            }
            $price += $product["sockets"] && 0 < count($filter) ? count($filter) * $product["price_socket"] : 0;
            $allowedSockets = (new Morpheus\Item\Socket())->available($product["section"]);
            $allowedSockets = util("Hash")->extract($allowedSockets, "{s}.{n}.value");
            $allowedSockets[] = Morpheus\Item\Socket::getEmptyValue();
            $okSockets = true;
            foreach ($filter as $sock) {
                if (!in_array($sock, $allowedSockets)) {
                    $okSockets = false;
                    break;
                }
            }
            if (!$okSockets) {
                return error(__("Invalid socket make your item again"));
            }
        }
        if (!empty($coupon)) {
            $price -= $price * $coupon["discount"] / 100;
        }
        $item->setIndex($product["index_"]);
        $item->setSection($product["section"]);
        $item->setLevel($level);
        $item->setOption($option);
        $item->setLuck($luck == 1);
        $item->setSkill($product["skill"] && $skill == 1);
        $item->getSerial()->generate();
        if (!empty($excellents)) {
            foreach ($excellents as $exc) {
                $item->addExcellent($exc, true);
            }
        }
        if ($product["ancient"]) {
            $item->setAncient(in_array($ancient, array(1, 2)) ? $ancient : 0);
        }
        if ($product["refine"]) {
            $item->setRefine($refine == 1);
        }
        if ($product["harmony"] && !empty($harmony)) {
            list($htype, $hlevel) = explode(",", $harmony);
            $item->addHarmony($htype, $hlevel);
        }
        if ($product["sockets"] && !empty($sockets)) {
            if ($shop["unique_sockets"]) {
                $filter = array();
                foreach ($sockets as $socket) {
                    if (!in_array($socket, array(Morpheus\Item\Socket::getNoValue(), Morpheus\Item\Socket::getEmptyValue()))) {
                        $filter[] = (int) $socket;
                    }
                }
                $socks = array_count_values($filter);
                foreach ($socks as $count) {
                    if (1 < $count) {
                        return error(__("Duplicated socket"));
                    }
                }
            }
            foreach ($sockets as $index => $sock) {
                $item->addSocket($index, $sock);
            }
        }
        $item->setDurability($item->getDefinitions()->getDurability());
        if (in_array(config("server.team", "gmo"), array("xteam", "muemu")) && 0 < $item->getHarmony()->has() && $item->hasSocket()) {
            return error(__("Item can't be sockets and harmony"));
        }
        $warehouse = user()->getWarehouse();
        $warehouse->create();
        $warehouse->load();
        $coins = user()->getCoin($shop["coin"]);
        $price = floor($price);
        if ($coins < $price) {
            return error(__("You do not have enough %s", array(config("coins")[$shop["coin"]]["name"])));
        }
        if (!$warehouse->addItem($item)) {
            return error(__("No space in your warehouse"));
        }
        try {
            Connection::transactional(function ($conn) use($warehouse, $shop, $price, $product, $item) {
                $warehouse->update();
                user()->addCoin($shop["coin"], 0 - $price);
                user()->update();
                $metadata = array("hex" => $item->getHex(), "item" => $item->toArray());
                user()->log(_e("Bought the item %s by %s", array($product["name"] . "[" . $item->getHex() . "]", $price . " " . config("coins")[$shop["coin"]]["name"])), "webshop", "mw_shop_items", $product["id"], $metadata);
                $conn->executeUpdate("UPDATE mw_shop_items SET solds = solds + 1 WHERE id = ?", array($product["id"]));
            });
            return success(__("Item added to your warehouse"), array("partial" => "login", "redirect" => "/shop/" . $shop["slug"] . "/" . $category["slug"] . "/" . $product["slug"]));
        } catch (Exception $ex) {
            return error($ex);
        }
    }
});
Route::get("/json/shop/coupon/:coupon", function ($coupon) {
    $coupon = Connection::fetchAssoc("select * \n        from mw_shop_coupons \n        where coupon = ? \n        and active = 1 \n        and start_date < getdate() \n        and (expire_date is null or expire_date >= getdate())\n    ", array($coupon));
    if (!empty($coupon)) {
        echo json_encode(array("success" => true, "data" => $coupon));
    } else {
        echo json_encode(array("success" => false));
    }
});

?>