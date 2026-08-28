<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace PaymentsGateways\Gateway;

class BoletoFacil implements InterfaceGateway
{
    public function checkout($order)
    {
        $url = "https://" . (config("gateways.boletofacil.sandbox", false) ? "sandbox." : "") . "boletobancario.com/boletofacil/integration/api/v1/issue-charge";
        $token = config("gateways.boletofacil.token");
        $http = \Morpheus\Network\Http::create("GET", $url, array("query" => array("token" => $token, "description" => "Account: " . user()->getUsername() . " - credits: " . util("Number")->currency($order["value"]), "reference" => $order["id"], "amount" => $order["value"], "dueDate" => date("d/m/Y", strtotime("+5 days")), "payerName" => user()->getName(), "payerCpfCnpj" => app()->request()->post("cpf"), "payerEmail" => user()->getEmail(), "notificationUrl" => url_to("/donate/notification/boletofacil", true))))->send();
        $response = $http->getResponse();
        if ($response["header"]["status"] === 200) {
            $data = json_decode($response["body"], true);
            if ($data["success"]) {
                $charge = $data["data"]["charges"][0];
                return $charge["link"];
            }
            throw new \Morpheus\Exception\Exception($data["errorMessage"]);
        }
        $data = json_decode($response["body"], true);
        throw new \Morpheus\Exception\Exception($data["errorMessage"]);
    }
    public function notification()
    {
        $token = app()->request()->post("paymentToken");
        if (!empty($token)) {
            $url = "https://" . (config("gateways.boletofacil.sandbox", false) ? "sandbox." : "") . "boletobancario.com/boletofacil/integration/api/v1/fetch-payment-details";
            $http = \Morpheus\Network\Http::create("GET", $url, array("query" => array("paymentToken" => $token)))->send();
            $response = $http->getResponse();
            if ($response["header"]["status"] === 200) {
                $data = json_decode($response["body"], true);
                if ($data["success"]) {
                    $payment = $data["data"]["payment"];
                    $charge = $payment["charge"];
                    $order = \Morpheus\Database\Connection::fetchAssoc("select * \r\n                        from mw_orders \r\n                        where id = :id\r\n                    ", array("id" => $charge["reference"]));
                    if (!empty($order) && !in_array($order["status"], array(3, 4))) {
                        \Morpheus\Database\Connection::transactional(function ($conn) use($order) {
                            $conn->update("mw_orders", array("status" => 3), array("id" => $order["id"]), array("integer", "integer"));
                            $account = account($order["username"]);
                            $account->addCredit($order["value"]);
                            $account->update();
                        });
                    }
                }
            }
        }
    }
}

?>