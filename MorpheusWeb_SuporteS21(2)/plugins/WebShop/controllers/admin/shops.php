<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/webshop/shops/", function () {
    $shops = Connection::fetchAll("SELECT * FROM mw_shops ORDER BY sequence");
    View::display("WebShop.shops/index", array("shops" => $shops));
})->name("shops");
Route::map("/webshop/shops/add", function () {
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED), "coin" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                Connection::insert("mw_shops", array("name" => Input::post("name"), "slug" => util("Inflector")->slug(Input::post("name"), "-"), "description" => Input::post("description"), "coin" => Input::post("coin"), "active" => Input::post("active") == 1, "sequence" => 0, "max_level" => Input::post("max_level"), "max_option" => Input::post("max_option"), "unique_sockets" => Input::post("unique_sockets") == 1, "default_price_level" => Input::post("default_price_level"), "default_price_option" => Input::post("default_price_option"), "default_price_skill" => Input::post("default_price_skill"), "default_price_luck" => Input::post("default_price_luck"), "default_price_ancient" => Input::post("default_price_ancient"), "default_price_harmony" => Input::post("default_price_harmony"), "default_price_refine" => Input::post("default_price_refine"), "default_price_socket" => Input::post("default_price_socket"), "default_price_excellent" => Input::post("default_price_excellent"), "default_max_excellent" => Input::post("default_max_excellent"), "default_max_sockets" => Input::post("default_max_sockets")), array("string", "string", "string", "string", "boolean", "integer", "integer", "integer", "boolean", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer"));
                return success(__("Shop has been saved"), array("redirect" => "/admin/webshop/shops"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    $version = config("item.db_version", 3);
    View::display("WebShop.shops/add", array("version" => $version));
})->via("GET", "POST")->name("shops.add");
Route::map("/webshop/shops/edit/:id", function ($id) {
    $shop = Connection::fetchAssoc("SELECT * FROM mw_shops WHERE id = ?", array($id), array("integer"));
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("name" => array(Morpheus\Core\Validation::REQUIRED), "coin" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                $name = Input::post("name");
                $slug = Input::post("slug");
                Connection::update("mw_shops", array("name" => $name, "slug" => empty($slug) ? util("Inflector")->slug($name, "-") : $slug, "description" => Input::post("description"), "coin" => Input::post("coin"), "active" => Input::post("active") == 1, "max_level" => Input::post("max_level"), "max_option" => Input::post("max_option"), "unique_sockets" => Input::post("unique_sockets") == 1, "default_price_level" => Input::post("default_price_level"), "default_price_option" => Input::post("default_price_option"), "default_price_skill" => Input::post("default_price_skill"), "default_price_luck" => Input::post("default_price_luck"), "default_price_ancient" => Input::post("default_price_ancient"), "default_price_harmony" => Input::post("default_price_harmony"), "default_price_refine" => Input::post("default_price_refine"), "default_price_socket" => Input::post("default_price_socket"), "default_price_excellent" => Input::post("default_price_excellent"), "default_max_excellent" => Input::post("default_max_excellent"), "default_max_sockets" => Input::post("default_max_sockets")), array("id" => $id), array("string", "string", "string", "string", "boolean", "integer", "integer", "boolean", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer", "integer"));
                return success(__("Shop has been updated"), array("redirect" => "/admin/webshop/shops/edit/" . $id));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    $version = config("item.db_version", 3);
    View::display("WebShop.shops/edit", array("shop" => $shop, "version" => $version));
})->via("GET", "POST")->name("shops.edit");
Route::post("/webshop/shops/reorder", function () {
    $sequences = Input::post("sequence");
    foreach ($sequences as $sequence => $id) {
        Connection::update("mw_shops", array("sequence" => $sequence + 1), array("id" => $id));
    }
});
Route::get("/json/webshop/shops/:id/categories", function ($id) {
    $categories = util("Hash")->nest(Connection::fetchAll("SELECT * FROM mw_shop_categories WHERE active = 1 and shop_id = ? ORDER BY sequence", array($id)), array("idPath" => "{n}.id", "parentPath" => "{n}.parent_id"));
    echo json_encode($categories);
});

?>