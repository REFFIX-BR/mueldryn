<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Morpheus\Menu\Menu::getInstance("admin")->add(array("id" => "configs.gateways", "url" => "/admin/gateways/config", "title" => __("gateways")))->add(array("id" => "configs.sep-gatways", "title" => "-"));
App::hook("admin.dashboard.after.render", function () {
    $status = Connection::fetchAll("select count(1) as total, status\n        from mw_orders\n        group by status");
    $total = Connection::fetchColumn("select count(1) from mw_orders");
    $data = Connection::fetchAll("select count(1) as total, sum(value) as price, convert(varchar(10), create_date, 103) as date\n        from mw_orders\n        where create_date between dateadd(day, -15, getdate()) and getdate()\n        group by convert(varchar(10), create_date, 103), convert(date, create_date)\n        order by convert(date, create_date)\n    ");
    $paid = Connection::fetchAll("select count(1) as total, sum(value) as price, convert(varchar(10), create_date, 103) as date\n        from mw_orders\n        where status in (3, 4) \n        and create_date between dateadd(day, -15, getdate()) and getdate()\n        group by convert(varchar(10), create_date, 103), convert(date, create_date)\n        order by convert(date, create_date)\n    ");
    View::display("PaymentsGateways.dashboard", array("layout" => false, "data" => $data, "paid" => $paid, "total" => $total, "status" => $status));
});
Route::map("/gateways/config", function () {
    if (Request::isPost()) {
        try {
            $gateways = array(
                "pagseguro" => array("currency" => false, "active" => Input::post("pagseguro_active") == 1, "email" => Input::post("pagseguro_email"), "token" => Input::post("pagseguro_token"), "sandbox" => Input::post("pagseguro_sandbox") == 1), 
                "paypal" => array("currency" => true, "active" => Input::post("paypal_active") == 1, "client_id" => Input::post("paypal_client_id"), "client_secret" => Input::post("paypal_client_secret"), "sandbox" => Input::post("paypal_sandbox") == 1), 
                "boletofacil" => array("currency" => false, "active" => Input::post("boletofacil_active") == 1, "token" => Input::post("boletofacil_token"), "require_cpf" => true, "sandbox" => Input::post("boletofacil_sandbox") == 1),
                "picpay" => array("currency" => false, "active" => Input::post("picpay_active") == 1, "token" => Input::post("picpay_token"), "seller_token" => Input::post("picpay_seller_token")),
                "mercadopago" => array("currency" => false, "active" => Input::post("mercadopago_active") == 1, "public_key" => Input::post("mercadopago_public_key"), "access_token" => Input::post("mercadopago_access_token"))
            );
            Morpheus\Core\Config::save("gateways", $gateways);
            return success(__("Settings has been updated"), array("redirect" => "/admin/gateways/config"));
        } catch (Exception $ex) {
            return error($ex->getMessage());
        }
    }
    View::display("PaymentsGateways.config");
})->via("GET", "POST")->name("gateways.config");

?>