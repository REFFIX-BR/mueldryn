<?php
namespace PaymentsGateways\Gateway;

class PicPay implements InterfaceGateway
{
    public function checkout($order)
    {
        $url = "https://appws.picpay.com/ecommerce/public/payments";
        $token = config("gateways.picpay.token");
        
        $data = array(
            "referenceId" => $order["id"],
            "callbackUrl" => url_to("/donate/notification/picpay", true),
            "returnUrl"   => url_to("/donate", true),
            "value"       => (float)$order["value"],
            "expiresAt"   => date('c', strtotime("+1 day")),
            "buyer" => array(
                "firstName" => user()->getName(),
                "lastName"  => user()->getUsername(),
                "document"  => app()->request()->post("cpf") ?: "000.000.000-00",
                "email"     => user()->getEmail()
            )
        );

        $http = \Morpheus\Network\Http::create("POST", $url, array(
            "headers" => array(
                "x-picpay-token" => $token,
                "Content-Type"   => "application/json"
            ),
            "body" => json_encode($data)
        ))->send();

        $response = $http->getResponse();
        $result = json_decode($response["body"], true);

        if ($response["header"]["status"] === 200 && isset($result["paymentUrl"])) {
            return $result["paymentUrl"];
        }

        throw new \Morpheus\Exception\Exception(isset($result["message"]) ? $result["message"] : "Error connecting to PicPay");
    }

    public function notification()
    {
        $referenceId = app()->request()->post("referenceId");
        if (empty($referenceId)) return;

        $token = config("gateways.picpay.token");
        $url = "https://appws.picpay.com/ecommerce/public/payments/" . $referenceId . "/status";

        $http = \Morpheus\Network\Http::create("GET", $url, array(
            "headers" => array("x-picpay-token" => $token)
        ))->send();

        $response = $http->getResponse();
        if ($response["header"]["status"] === 200) {
            $data = json_decode($response["body"], true);
            
            // Status PicPay: created, expired, analysis, paid, completed, refunded, chargeback
            if (in_array($data["status"], array("paid", "completed"))) {
                $order = \Morpheus\Database\Connection::fetchAssoc("select * from mw_orders where id = :id", array("id" => $referenceId));
                
                if (!empty($order) && !in_array($order["status"], array(3, 4))) {
                    \Morpheus\Database\Connection::transactional(function ($conn) use($order) {
                        $conn->update("mw_orders", array("status" => 3), array("id" => $order["id"]));
                        
                        $account = account($order["username"]);
                        $account->addCredit($order["value"]);
                        $account->update();
                    });
                }
            }
        }
    }
}
