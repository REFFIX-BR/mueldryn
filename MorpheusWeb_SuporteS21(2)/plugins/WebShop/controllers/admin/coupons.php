<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/webshop/coupons", function () {
    $coupons = Connection::fetchAll("SELECT * FROM mw_shop_coupons");
    View::display("WebShop.coupons/index", array("coupons" => $coupons));
})->name("shops.coupons");
Route::map("/webshop/coupons/add", function () {
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("coupon" => array(Morpheus\Core\Validation::REQUIRED), "discount" => array(Morpheus\Core\Validation::REQUIRED), "start_date" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                Connection::insert("mw_shop_coupons", array("coupon" => Input::post("coupon"), "discount" => Input::post("discount"), "start_date" => DateTime::createFromFormat("d/m/Y", Input::post("start_date"))->format("Y-m-d"), "expire_date" => Input::post("expire_date") ? DateTime::createFromFormat("d/m/Y", Input::post("expire_date"))->format("Y-m-d") : NULL, "active" => Input::post("active") == 1), array("string", "float", "string", "string", "boolean"));
                return success(__("Coupon has been saved"), array("redirect" => "/admin/webshop/coupons"));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("WebShop.coupons/add");
})->via("GET", "POST")->name("shops.coupons.add");
Route::map("/webshop/coupons/edit/:id", function ($id) {
    $coupon = Connection::fetchAssoc("SELECT * FROM mw_shop_coupons WHERE id = ?", array($id));
    if (Request::isPost()) {
        $validation = new Morpheus\Core\Validation(Input::post(), array("coupon" => array(Morpheus\Core\Validation::REQUIRED, __("Coupon already exist") => array("rule" => array("isUnique", "mw_shop_coupons", "coupon"))), "discount" => array(Morpheus\Core\Validation::REQUIRED), "start_date" => array(Morpheus\Core\Validation::REQUIRED)));
        if ($validation->isValid()) {
            try {
                Connection::update("mw_shop_coupons", array("coupon" => Input::post("coupon"), "discount" => Input::post("discount"), "start_date" => DateTime::createFromFormat("d/m/Y", Input::post("start_date"))->format("Y-m-d"), "expire_date" => Input::post("expire_date") ? DateTime::createFromFormat("d/m/Y", Input::post("expire_date"))->format("Y-m-d") : NULL, "active" => Input::post("active") == 1), array("id" => $id), array("string", "float", "string", "string", "boolean"));
                return success(__("Coupon has been updated"), array("redirect" => "/admin/webshop/coupons/edit/" . $id));
            } catch (Exception $ex) {
                return error($ex->getMessage());
            }
        } else {
            return error($validation->getErrors());
        }
    }
    View::display("WebShop.coupons/edit", array("coupon" => $coupon));
})->via("GET", "POST")->name("shops.coupons.edit");
Route::get("/webshop/coupons/delete/:id", function ($id) {
    try {
        Connection::delete("mw_shop_coupons", array("id" => $id));
        return success(__("Coupon has been deleted"), array("redirect" => "/admin/webshop/coupons"));
    } catch (Exception $ex) {
        return error($ex->getMessage(), array("redirect" => "/admin/webshop/coupons"));
    }
})->name("shops.coupons.delete");

?>