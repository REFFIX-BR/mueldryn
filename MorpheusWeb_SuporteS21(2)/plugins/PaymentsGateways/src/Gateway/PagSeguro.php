<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace PaymentsGateways\Gateway;

class PagSeguro implements InterfaceGateway
{
    private function _initialize()
    {
        \PagSeguro\Library::initialize();
        \PagSeguro\Configuration\Configure::setEnvironment(config("gateways.pagseguro.sandbox", false) ? "sandbox" : "production");
        \PagSeguro\Configuration\Configure::setAccountCredentials(trim(config("gateways.pagseguro.email")), trim(config("gateways.pagseguro.token")));
        \PagSeguro\Configuration\Configure::setCharset("UTF-8");
        \PagSeguro\Configuration\Configure::setLog(true, LOGS_PATH . "pagseguro.log");
    }
    public function checkout($order)
    {
        $this->_initialize();
        $payment = new \PagSeguro\Domains\Requests\Payment();
        $payment->addItems()->withParameters("0001", "Account: " . user()->getUsername() . " - credits: " . $order["value"], 1, $order["value"]);
        $payment->setCurrency($order["currency"]);
        $payment->setReference($order["id"]);
        $payment->setSender()->setName(user()->getName() . " " . user()->getUsername());
        $payment->setSender()->setEmail(user()->getEmail());
        $payment->setRedirectUrl(url_to("/donate", true));
        $payment->setNotificationUrl(url_to("/donate/notification/pagseguro", true));
        $response = $payment->register(\PagSeguro\Configuration\Configure::getAccountCredentials(), true);
        \Morpheus\Database\Connection::update("mw_orders", array("transaction_id" => $response->getCode()), array("id" => $order["id"]));
        $connection = new \PagSeguro\Resources\Connection\Data(\PagSeguro\Configuration\Configure::getAccountCredentials());
        return $connection->buildPaymentResponseUrl() . "?code=" . $response->getCode();
    }
    public function notification()
    {
        $this->_initialize();
        if (\PagSeguro\Helpers\Xhr::hasPost()) {
            $response = \PagSeguro\Services\Transactions\Notification::check(\PagSeguro\Configuration\Configure::getAccountCredentials());
            $order = \Morpheus\Database\Connection::fetchAssoc("select *\r\n                from mw_orders\r\n                where id = :id\r\n            ", array("id" => $response->getReference()));
            if (!empty($order) && $order["status"] != $response->getStatus()) {
                \Morpheus\Database\Connection::transactional(function ($conn) use($response, $order) {
                    $status = $response->getStatus();
                    $conn->update("mw_orders", array("status" => $status), array("id" => $order["id"]), array("integer", "integer"));
                    if (!in_array($order["status"], array(3, 4)) && in_array($status, array(3, 4))) {
                        $account = account($order["username"]);
                        $account->addCredit($order["value"]);
                        $account->update();
                    }
                });
            }
        } else {
            throw new \InvalidArgumentException($_POST);
        }
    }
}

?>