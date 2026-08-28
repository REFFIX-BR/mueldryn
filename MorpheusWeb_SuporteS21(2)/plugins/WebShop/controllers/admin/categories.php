<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/webshop/categories", function () {
    $shop = Connection::fetchAssoc("SELECT * FROM mw_shops WHERE id = ?", array(Input::get("shop_id")));
    if (!$shop) {
        return error(__("Shop not found"), array("redirect" => "/admin/webshop/shops"));
    }
    $categories = Connection::fetchAll("SELECT * FROM mw_shop_categories WHERE shop_id = ? ORDER BY sequence", array(Input::get("shop_id")));
    View::display("WebShop.categories/index", array("categories" => util("Hash")->nest($categories, array("idPath" => "{n}.id", "parentPath" => "{n}.parent_id")), "shop" => $shop));
})->name("shops.categories");
Route::map("/webshop/categories/add", function () {
    $shop = Connection::fetchAssoc("SELECT * FROM mw_shops WHERE id = ?", array(Input::get("shop_id")));
    if (!$shop) {
        return error(__("Shop not found"), array("redirect" => "/admin/webshop/shops"));
    }
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                $parent = Input::post("parent_id");
                $slug = util("Inflector")->slug(Input::post("name"), "-");
                if (!empty($parent)) {
                    $column = Connection::fetchAssoc("SELECT name FROM mw_shop_categories WHERE id = ?", array($parent), array("integer"));
                    if (!empty($column)) {
                        $slug = util("Inflector")->slug($column["name"], "-") . "-" . $slug;
                    }
                }
                $sequence = Connection::fetchAssoc("SELECT COALESCE(MAX(sequence), 0) + 1 AS next FROM mw_shop_categories WHERE parent_id = ?", array($parent));
                Connection::insert("mw_shop_categories", array("name" => Input::post("name"), "slug" => $slug, "type" => Input::post("type"), "parent_id" => $parent, "active" => Input::post("active") == 1, "sequence" => $sequence["next"], "shop_id" => $shop["id"]), array("string", "string", "string", "integer", "boolean", "integer", "integer"));
                return success(__("Category has beed saved"), array("redirect" => "/admin/webshop/categories?shop_id=" . $shop["id"]));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    $categories = Connection::fetchAll("SELECT * FROM mw_shop_categories WHERE shop_id = ? AND (parent_id is null OR parent_id = 0) ORDER BY sequence", array($shop["id"]));
    View::display("WebShop.categories/add", array("categories" => $categories, "shop" => $shop, "types" => config("item.categories")));
})->via("GET", "POST")->name("shops.categories.add");
Route::map("/webshop/categories/edit/:id", function ($id) {
    $category = Connection::fetchAssoc("SELECT * FROM mw_shop_categories WHERE id = ?", array($id), array("integer"));
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED), "slug" => array(Morpheus\Core\Validation::REQUIRED, __("URL already exist") => array("rule" => array("isUnique", "mw_shop_categories", "slug"), "condition" => $category["slug"] !== Input::post("slug")))));
        if ($validation->isValid()) {
            try {
                $parent = Input::post("parent_id");
                $slug = Input::post("slug");
                if (empty($slug)) {
                    $slug = util("Inflector")->slug(Input::post("name"), "-");
                    if (!empty($parent)) {
                        $column = Connection::fetchAssoc("SELECT name FROM mw_shop_categories WHERE id = ?", array($parent), array("integer"));
                        if (!empty($column)) {
                            $slug = util("Inflector")->slug($column["name"], "-") . "-" . $slug;
                        }
                    }
                }
                Connection::update("mw_shop_categories", array("name" => Input::post("name"), "slug" => $slug, "type" => Input::post("type"), "parent_id" => $parent, "active" => Input::post("active") == 1), array("id" => $id), array("string", "string", "string", "integer", "boolean", "integer"));
                if (empty($parent) && !Input::post("active")) {
                    Connection::update("mw_shop_categories", array("active" => false), array("parent_id" => $id), array("boolean"));
                }
                return success(__("Category has need updated"), array("redirect" => "/admin/webshop/categories?shop_id=" . $category["shop_id"]));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    $categories = Connection::fetchAll("SELECT * FROM mw_shop_categories WHERE shop_id = ? AND (parent_id is null OR parent_id = 0) ORDER BY sequence", array($category["shop_id"]));
    View::display("WebShop.categories/edit", array("categories" => $categories, "category" => $category, "types" => config("item.categories")));
})->via("GET", "POST")->name("shops.categories.edit");
Route::get("/webshop/categories/delete/:id", function ($id) {
    $category = Connection::fetchAssoc("SELECT * FROM mw_shop_categories WHERE id = ?", array($id));
    try {
        Connection::transactional(function () use($category) {
            Connection::delete("mw_shop_categories", array("id" => $category["id"]));
            Connection::update("mw_shop_items", array("category_id" => NULL), array("category_id" => $category["id"]), array("integer"));
        });
        return success(__("Category has been deleted"), array("redirect" => "/admin/webshop/categories?shop_id=" . $category["shop_id"]));
    } catch (Exception $ex) {
        return error($ex->getMessage(), array("redirect" => "/admin/webshop/categories?shop_id=" . $category["shop_id"]));
    }
})->name("shops.categories.delete");
Route::post("/webshop/categories/reorder", function () {
    $sequences = Input::post("sequence");
    foreach ($sequences as $sequence => $id) {
        Connection::update("mw_shop_categories", array("sequence" => $sequence + 1), array("id" => $id));
    }
});

?>