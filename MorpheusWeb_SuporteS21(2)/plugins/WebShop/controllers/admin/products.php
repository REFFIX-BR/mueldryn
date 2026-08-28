<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::map("/webshop/products", function () {
    $qb = Connection::createQueryBuilder();
    $where = $total = array();
    $name = Input::params("name");
    $shop = Input::params("shop_id");
    $category = Input::params("category_id");
    if ($name) {
        $where[] = "lower(i.name) like '%" . strtolower($name) . "%'";
    }
    if ($shop) {
        $where[] = "s.id = " . $shop;
    }
    if ($category) {
        $where[] = "c.id = " . $category;
    }
    $total = Connection::fetchAssoc("SELECT count(1) total FROM mw_shop_items i LEFT JOIN mw_shop_categories c ON c.id = i.category_id LEFT JOIN mw_shops s ON s.id = c.shop_id" . (empty($where) ? "" : " WHERE " . join(" AND ", $where)));
    $max = 50;
    $page = Input::get("page") == NULL ? 1 : Input::get("page");
    $init = (int) ($page - 1) * $max;
    $qb->select(array("i.id", "i.name", "i.active", "c.name AS category_name", "s.name AS shop_name"))->from("mw_shop_items", "i")->leftJoin("i", "mw_shop_categories", "c", "c.id = i.category_id")->leftJoin("c", "mw_shops", "s", "s.id = c.shop_id")->orderBy("i.active", "DESC")->addOrderBy("i.name")->setFirstResult($init)->setMaxResults($max);
    foreach ($where as $index => $w) {
        if ($index === 0) {
            $qb->where($w);
        } else {
            $qb->andWhere($w);
        }
    }
    $products = $qb->execute();
    $params = Input::params();
    if (isset($params["page"])) {
        unset($params["page"]);
    }
    $params = http_build_query($params);
    $paginator = new Morpheus\Core\Paginator($total["total"], $max, $page, url_to("/admin/webshop/products?page=(:num)" . (!empty($params) ? "&" . $params : "")));
    View::display("WebShop.products/index", array("products" => $products, "paginator" => $paginator, "page" => $page, "total" => $total["total"]));
})->name("shops.products")->via("GET", "POST");
Route::map("/webshop/products/add", function () {
    $version = config("item.db_version", 3);
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED), "slug" => array(Morpheus\Core\Validation::REQUIRED), "section" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "index" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "shop" => array(Morpheus\Core\Validation::REQUIRED), "category_id" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price_level" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price_option" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price_luck" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price_skill" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price_ancient" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price_harmony" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price_refine" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price_socket" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price_excellent" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS)));
        if ($validation->isValid()) {
            try {
                $classes = Input::post("classes");
                $image = Input::file("image");
                $filename = NULL;
                if (!empty($image)) {
                    $dir = ROOT . DS . "uploads" . DS . "webshop" . DS;
                    list($file) = rework_upload_files($image);
                    $upload = new upload($file, config("locale", "pt_BR"));
                    $upload->file_new_name_body = uniqid();
                    $upload->file_auto_rename = true;
                    $upload->process($dir);
                    if ($upload->processed) {
                        $upload->clean();
                        $filename = $upload->file_dst_name;
                    } else {
                        return error($upload->error);
                    }
                }
                $ancients = Input::post("ancient");
                Connection::insert("mw_shop_items", array("name" => Input::post("name"), "slug" => strtolower(util("Inflector")->slug(Input::post("name"))), "image" => $filename, "section" => Input::post("section"), "index_" => Input::post("index"), "fix_level" => Input::post("fix_level"), "fix_option" => Input::post("fix_option"), "max_excellent" => Input::post("max_excellent"), "max_sockets" => Input::post("max_sockets"), "category_id" => Input::post("category_id"), "luck" => Input::post("luck") == 1, "skill" => Input::post("skill") == 1, "ancient" => 1.1 <= $version && !empty($ancients) ? join(",", $ancients) : "", "refine" => 2 < $version && Input::post("refine") == 1, "sockets" => 2 < $version && Input::post("sockets") == 1, "harmony" => 2 < $version && Input::post("harmony") == 1, "active" => Input::post("active") == 1, "offer" => Input::post("offer") == 1, "classes" => join(",", !empty($classes) ? $classes : array()), "price" => Input::post("price"), "price_level" => Input::post("price_level"), "price_option" => Input::post("price_option"), "price_skill" => Input::post("price_skill"), "price_luck" => Input::post("price_luck"), "price_ancient" => 1.1 <= $version ? Input::post("price_ancient") : 0, "price_refine" => 2 < $version ? Input::post("price_refine") : 0, "price_harmony" => 2 < $version ? Input::post("price_harmony") : 0, "price_socket" => 2 < $version ? Input::post("price_socket") : 0, "price_excellent" => Input::post("price_excellent")), array("string", "string", "string", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "boolean", "boolean", "boolean", "boolean", "boolean", "boolean", "boolean", "boolean", "string", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer"));
                return success(__("Product has been saved"), array("redirect" => "/admin/webshop/products/add"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    $shops = Connection::fetchAll("SELECT * FROM mw_shops WHERE active = 1");
    View::display("WebShop.products/add", array("shops" => $shops, "version" => $version));
})->via("GET", "POST")->name("shops.products.add");
Route::map("/webshop/products/edit/:id", function ($id) {
    $version = config("item.db_version", 3);
    $product = Connection::fetchAssoc("SELECT * \n        FROM mw_shop_items \n        where id = ?\n    ", array($id));
    if (!$product) {
        return error(__("Product not found"), array("redirect" => "/admin/webshop/products"));
    }
    if (!empty($product["classes"])) {
        $product["classes"] = explode(",", $product["classes"]);
    } else {
        $product["classes"] = array();
    }
    if (!empty($product["category_id"])) {
        $category = Connection::fetchAssoc("SELECT * FROM mw_shop_categories WHERE id = :id", array("id" => $product["category_id"]));
        $product["shop_id"] = $category["shop_id"];
    }
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED), "slug" => array(Morpheus\Core\Validation::REQUIRED), "section" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "index" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "shop" => array(Morpheus\Core\Validation::REQUIRED), "category_id" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price_level" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price_option" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price_luck" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price_skill" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price_ancient" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price_harmony" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price_refine" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price_socket" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS), "price_excellent" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS)));
        if ($validation->isValid()) {
            try {
                $classes = Input::post("classes");
                $image = Input::file("image");
                $filename = NULL;
                if (!empty($image)) {
                    $dir = ROOT . DS . "uploads" . DS . "webshop" . DS;
                    list($file) = rework_upload_files($image);
                    $upload = new upload($file, config("locale", "pt_BR"));
                    $upload->file_new_name_body = uniqid();
                    $upload->file_auto_rename = true;
                    $upload->process($dir);
                    if ($upload->processed) {
                        $upload->clean();
                        $filename = $upload->file_dst_name;
                    } else {
                        return error($upload->error);
                    }
                }
                $ancients = Input::post("ancient");
                Connection::update("mw_shop_items", array("name" => Input::post("name"), "slug" => Input::post("slug"), "image" => $filename === NULL ? $product["image"] : $filename, "section" => Input::post("section"), "index_" => Input::post("index"), "fix_level" => Input::post("fix_level"), "fix_option" => Input::post("fix_option"), "max_excellent" => Input::post("max_excellent"), "max_sockets" => Input::post("max_sockets"), "category_id" => Input::post("category_id"), "luck" => Input::post("luck") == 1, "skill" => Input::post("skill") == 1, "ancient" => 1.1 <= $version && !empty($ancients) ? join(",", $ancients) : "", "refine" => 2 < $version && Input::post("refine") == 1, "sockets" => 2 < $version && Input::post("sockets") == 1, "harmony" => 2 < $version && Input::post("harmony") == 1, "active" => Input::post("active") == 1, "offer" => Input::post("offer") == 1, "classes" => join(",", !empty($classes) ? $classes : array()), "price" => Input::post("price"), "price_level" => Input::post("price_level"), "price_option" => Input::post("price_option"), "price_skill" => Input::post("price_skill"), "price_luck" => Input::post("price_luck"), "price_ancient" => 1.1 <= $version ? Input::post("price_ancient") : 0, "price_refine" => 2 < $version ? Input::post("price_refine") : 0, "price_harmony" => 2 < $version ? Input::post("price_harmony") : 0, "price_socket" => 2 < $version ? Input::post("price_socket") : 0, "price_excellent" => Input::post("price_excellent")), array("id" => $id), array("string", "string", "string", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "boolean", "boolean", "boolean", "boolean", "boolean", "boolean", "boolean", "boolean", "string", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer"));
                return success(__("Product has been updated"), array("redirect" => "/admin/webshop/products/edit/" . $id));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    $shops = Connection::fetchAll("SELECT * FROM mw_shops WHERE active = 1");
    View::display("WebShop.products/edit", array("product" => $product, "shops" => $shops, "version" => $version));
})->via("GET", "POST")->name("shops.products.edit");
Route::get("/webshop/products/delete/:id", function ($id) {
    try {
        Connection::delete("mw_shop_items", array("id" => $id));
        return success(__("Product has been deleted"), array("redirect" => "/admin/webshop/products"));
    } catch (Exception $ex) {
        return error($ex->getMessage(), array("redirect" => "/admin/webshop/products"));
    }
})->name("shops.products.delete");
Route::map("/webshop/products/import", function () {
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("shop" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            $itemkor = Input::file("itemkor");
            list($itemkor) = rework_upload_files($itemkor);
            $storage = Morpheus\Item\Storage\Storage::factory(config("item.file_adapter", "ItemKOR"), $itemkor["tmp_name"]);
            $count = 0;
            $shop = Connection::fetchAssoc("SELECT * FROM mw_shops WHERE id = ?", array(Input::post("shop_id")));
            $categories = Input::post("categories");
            $types = config("item.categories");
            $version = config("item.db_version", 3);
            foreach ($storage->getItems() as $section => $items) {
                foreach ($items as $index => $item) {
                    if (!in_array($section, $categories)) {
                        continue;
                    }
                    $exists = Connection::fetchColumn("select 1 \n                        from mw_shop_items i\n                        left join mw_shop_categories c on c.id = i.category_id\n                        where i.section = ?\n                        and i.index_ = ?\n                        and c.shop_id = ?\n                    ", array($section, $index, $shop["id"]));
                    if (!$exists) {
                        $type = strtolower($types[$section]);
                        $category = Connection::fetchAssoc("SELECT * FROM mw_shop_categories WHERE shop_id = ? and type = ?", array($shop["id"], $type));
                        $classes = array();
                        $for = util("Item")->canEquip($section, $index);
                        if (!empty($for)) {
                            $cs = util("Character")->getClassesByShortName();
                            foreach ($for as $c) {
                                if (isset($cs[strtoupper($c)])) {
                                    $classes[] = $cs[strtoupper($c)];
                                }
                            }
                        }
                        $ancients = array();
                        if (1.1 <= $version && util("Item")->hasAncient($section, $index)) {
                            $ancients = array_keys(util("Item")->getAvailableAncients($section, $index));
                        }
                        Connection::insert("mw_shop_items", array("name" => $item["name"], "section" => $section, "index_" => $index, "x_size" => $item["width"], "y_size" => $item["height"], "skill" => util("Item")->hasSkill($section, $index), "luck" => 1, "sockets" => 3 <= $version && util("Item")->hasSockets($section, $index), "refine" => 3 <= $version && util("Item")->hasRefine($section, $index), "harmony" => 3 <= $version && util("Item")->hasHarmony($section), "ancient" => join(",", $ancients), "category_id" => !empty($category) ? $category["id"] : NULL, "slug" => strtolower(util("Inflector")->slug($item["name"], "-")), "active" => 0, "max_excellent" => $shop["default_max_excellent"] ?: 6, "max_sockets" => $shop["default_max_sockets"] ?: 3 <= $version ? util("Item")->getMaxSockets($section, $index) : 0, "price_level" => $shop["default_price_level"] ?: 0, "price_option" => $shop["default_price_option"] ?: 0, "price_skill" => $shop["default_price_skill"] ?: 0, "price_luck" => $shop["default_price_luck"] ?: 0, "price_excellent" => $shop["default_price_excellent"] ?: 0, "price_ancient" => $shop["default_price_ancient"] ?: 0, "price_harmony" => $shop["default_price_harmony"] ?: 0, "price_refine" => $shop["default_price_refine"] ?: 0, "price_socket" => $shop["default_price_socket"] ?: 0, "classes" => join(",", $classes), "fix_level" => -1, "fix_option" => -1), array("string", "integer", "integer", "integer", "integer", "boolean", "boolean", "boolean", "boolean", "boolean", "string", "integer", "string", "boolean", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "string", "integer", "integer"));
                        $count++;
                    }
                }
            }
            if (0 < $count) {
                return success(__("%d items has been added", array($count)));
            }
            return error(__("No items added"));
        } else {
            return error($validation->getErrors());
        }
    } else {
        $shops = Connection::fetchAll("SELECT * FROM mw_shops WHERE active = 1");
        View::display("WebShop.products/import", array("shops" => $shops));
    }
})->via("GET", "POST")->name("shops.products.import");
Route::get("/webshop/products/search", function () {
    $shops = Connection::fetchAll("SELECT * FROM mw_shops WHERE active = 1");
    View::display("WebShop.products/search", array("shops" => $shops));
})->name("shops.products.search");
Route::get("/webshop/products/activate", function () {
    $ids = Input::get("ids");
    if (empty($ids)) {
        return error(__("No products found"));
    }
    $ids = explode(",", $ids);
    foreach ($ids as $id) {
        Connection::update("mw_shop_items", array("active" => true), array("id" => $id), array("boolean"));
    }
    return success(__("Products has been activated"), array("redirect" => "/admin/webshop/products?page=" . Input::get("page")));
});
Route::get("/webshop/products/deactivate", function () {
    $ids = Input::get("ids");
    if (empty($ids)) {
        return error(__("No products found"));
    }
    $ids = explode(",", $ids);
    foreach ($ids as $id) {
        Connection::update("mw_shop_items", array("active" => false), array("id" => $id), array("boolean"));
    }
    return success(__("Products has been deactivated"), array("redirect" => "/admin/webshop/products?page=" . Input::get("page")));
});
Route::get("/webshop/products/delete-all", function () {
    $ids = Input::get("ids");
    if (empty($ids)) {
        return error(__("No products found"));
    }
    $ids = explode(",", $ids);
    foreach ($ids as $id) {
        Connection::delete("mw_shop_items", array("id" => $id));
    }
    return success(__("Products has been deleted"), array("redirect" => "/admin/webshop/products?page=" . Input::get("page")));
});

?>