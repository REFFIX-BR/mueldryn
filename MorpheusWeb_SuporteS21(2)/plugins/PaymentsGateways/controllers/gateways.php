<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

Route::get("/donate", authenticate(), function () {
    $orders = Connection::fetchAll("select * \n        from mw_orders \n        where username = ? \n        order by id desc\n    ", array(user()->getUsername()));
    View::display("PaymentsGateways.gateways", array("orders" => $orders));
});
Route::map("/donate/:gateway", authenticate(), function ($gateway) {
    $gateways = array();
    foreach (config("gateways", array()) as $gat => $config) {
        if ($config["active"]) {
            $gateways[] = $gat;
        }
    }
    if (!in_array($gateway, $gateways)) {
        return App::notFound();
    }
    $config = config("gateways." . $gateway);
    if (Request::isPost()) {
        $validations = array("amount" => array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::ONLY_NUMBERS));
        if (isset($config["require_cpf"]) && $config["require_cpf"]) {
            $validations["cpf"] = array(Morpheus\Core\Validation::REQUIRED, Morpheus\Core\Validation::CPF);
        }
        $validation = new Morpheus\Core\Validation(Input::post(), $validations);
        if ($validation->isValid()) {
            try {
                $url = NULL;
                Connection::transactional(function ($conn) use($gateway, &$url) {
                    $post = Input::post();
                    $amount = util("Number")->toFloat($post["amount"]);
                    $order = array("username" => user()->getUsername(), "value" => $amount, "gateway" => $gateway, "currency" => isset($post["currency"]) ? $post["currency"] : "BRL", "status" => 1);
                    $conn->insert("mw_orders", $order, array("string", "integer", "string", "string", "integer"));
                    $id = $conn->lastInsertId();
                    $order = array_merge($order, array("id" => $id));
                    $gate = PaymentsGateways\Gateway\Gateway::factory($gateway);
                    $url = $gate->checkout($order);
                });
                return success("", array((is_ajax() ? "force_" : "") . "redirect" => $url));
            } catch (Exception $ex) {
                return error($ex);
            }
        } else {
            return error($validation->getErrors());
        }
    }
    $currencies = array("BRL" => "BRL", "USD" => "USD");
    View::display("PaymentsGateways.donate", array("gateway" => $gateway, "config" => $config, "currencies" => $currencies));
})->via("GET", "POST");
Route::map("/donate/notification/:gateway", function ($gateway) {
    try {
        $gate = PaymentsGateways\Gateway\Gateway::factory($gateway);
        $gate->notification();
    } catch (Exception $ex) {
    }
})->via("GET", "POST");
Route::get("/donate/:gateway/error", authenticate(), function ($gateway, $type) {
    View::display("PaymentsGateways.error", array("gateway" => $gateway));
});

?>