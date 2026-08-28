<?php
namespace PaymentsGateways\Gateway;

class MercadoPago implements InterfaceGateway
{
    public function checkout($order)
    {
        $url = "https://api.mercadopago.com/checkout/preferences";
        $accessToken = config("gateways.mercadopago.access_token");

        $data = array(
            "items" => array(
                array(
                    "title" => "Credits for " . user()->getUsername(),
                    "quantity" => 1,
                    "currency_id" => "BRL",
                    "unit_price" => (float)$order["value"]
                )
            ),
            "external_reference" => (string)$order["id"],
            "notification_url" => url_to("/donate/notification/mercadopago", true),
            "back_urls" => array(
                "success" => url_to("/donate", true),
                "failure" => url_to("/donate", true)
            ),
            "auto_return" => "approved"
        );

        $http = \Morpheus\Network\Http::create("POST", $url, array(
            "headers" => array(
                "Authorization" => "Bearer " . $accessToken,
                "Content-Type" => "application/json"
            ),
            "body" => json_encode($data)
        ))->send();

        $response = $http->getResponse();
        $result = json_decode($response["body"], true);

        if ($response["header"]["status"] === 201 || $response["header"]["status"] === 200) {
            return $result["init_point"];
        }

        throw new \Morpheus\Exception\Exception("MercadoPago Error: " . (isset($result["message"]) ? $result["message"] : "Unknown error"));
    }

    public function notification()
    {
        $id = app()->request()->get("id") ?: app()->request()->get("data_id");
        $topic = app()->request()->get("topic") ?: app()->request()->get("type");

        if ($topic == 'payment' && !empty($id)) {
            $accessToken = config("gateways.mercadopago.access_token");
            $url = "https://api.mercadopago.com/v1/payments/" . $id;

            $http = \Morpheus\Network\Http::create("GET", $url, array(
                "headers" => array("Authorization" => "Bearer " . $accessToken)
            ))->send();

            $response = $http->getResponse();
            if ($response["header"]["status"] === 200) {
                $payment = json_decode($response["body"], true);
                
                if ($payment["status"] == 'approved') {
                    $orderId = $payment["external_reference"];
                    $order = \Morpheus\Database\Connection::fetchAssoc("select * from mw_orders where id = :id", array("id" => $orderId));
                    
                    if (!empty($order) && !in_array($order["status"], array(3, 4))) {
                        \Morpheus\Database\Connection::transactional(function ($conn) use($order, $id) {
                            $conn->update("mw_orders", array("status" => 3, "transaction_id" => $id), array("id" => $order["id"]));
                            
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
