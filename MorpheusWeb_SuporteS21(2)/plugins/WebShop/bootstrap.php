<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Morpheus\Menu\Menu::getInstance("admin")->add(array("id" => "shop", "title" => __("WebShop"), "icon" => "usd"))->add(array("id" => "shop.sep1", "title" => "-"))->add(array("id" => "shop.config_shops", "title" => __("Shops"), "url" => "/admin/webshop/shops"))->add(array("id" => "shop.config_products", "title" => __("Products"), "url" => "/admin/webshop/products"))->add(array("id" => "shop.coupons", "title" => __("Coupons"), "url" => "/admin/webshop/coupons"))->add(array("id" => "shop.config", "title" => __("Config"), "url" => "/admin/webshop/config"));
App::hook("admin.accounts.edit.render", function ($account) {
});

?>