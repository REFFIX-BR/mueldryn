<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace PaymentsGateways\Gateway;

class PayPal implements InterfaceGateway
{
    private function _getApiContext()
    {
        $context = new \PayPal\Rest\ApiContext(new \PayPal\Auth\OAuthTokenCredential(trim(config("gateways.paypal.client_id")), trim(config("gateways.paypal.client_secret"))));
        $context->setConfig(array("mode" => config("gateways.paypal.sandbox", false) ? "sandbox" : "live", "log.LogEnabled" => true, "log.FileName" => LOGS_PATH . "paypal.log", "log.LogLevel" => config("debug") ? "DEBUG" : "INFO", "cache.enabled" => true));
        return $context;
    }
    public function checkout($order)
    {
        $amount = round($order["value"], 2);
        $payer = new \PayPal\Api\Payer();
        $payer->setPaymentMethod("paypal");
        $item = new \PayPal\Api\Item();
        $item->setName("Account: " . user()->getUsername() . " - credits: " . $amount)->setQuantity(1)->setCurrency($order["currency"])->setSku("00001")->setPrice($amount);
        $items = new \PayPal\Api\ItemList();
        $items->setItems(array($item));
        $am = new \PayPal\Api\Amount();
        $am->setCurrency($order["currency"])->setTotal($amount);
        $transaction = new \PayPal\Api\Transaction();
        $transaction->setItemList($items)->setAmount($am)->setInvoiceNumber($order["id"]);
        $urls = new \PayPal\Api\RedirectUrls();
        $urls->setReturnUrl(url_to("/donate/notification/paypal", true))->setCancelUrl(url_to("/donate/notification/paypal", true));
        $payment = new \PayPal\Api\Payment();
        $payment->setIntent("sale")->setPayer($payer)->setRedirectUrls($urls)->setTransactions(array($transaction));
        $payment->create($this->_getApiContext());
        return $payment->getApprovalLink();
    }
    public function notification()
    {
        $paymentId = app()->request()->get("paymentId");
        $payerId = app()->request()->get("PayerID");
        try {
            $payment = \PayPal\Api\Payment::get($paymentId, $this->_getApiContext());
            $execution = new \PayPal\Api\PaymentExecution();
            $execution->setPayerId($payerId);
            $result = $payment->execute($execution, $this->_getApiContext());
            $payment = \PayPal\Api\Payment::get($paymentId, $this->_getApiContext());
            $order = \Morpheus\Database\Connection::fetchAssoc("select * \r\n                from mw_orders \r\n                where id = :id\r\n            ", array("id" => $payment->getTransactions()[0]->getInvoiceNumber()));
            if ($order["status"] == 1) {
                list($transaction) = $payment->getTransactions();
                $transactionId = $transaction->getRelatedResources()[0]->getSale()->getId();
                if ($payment->getState() === "approved") {
                    \Morpheus\Database\Connection::transactional(function ($conn) use($payment, $transaction, $order, $transactionId) {
                        $conn->update("mw_orders", array("status" => 4, "currency" => $transaction->getAmount()->getCurrency(), "transaction_id" => $transactionId), array("id" => $order["id"]), array("integer", "integer"));
                        $account = account($order["username"]);
                        $account->addCredit($order["value"]);
                        $account->update();
                    });
                    return success("", array("redirect" => url_to("/donate")));
                }
                \Morpheus\Database\Connection::update("mw_orders", array("status" => 7, "transaction_id" => $transactionId), array("id" => $order["id"]), array("integer", "integer"));
            }
            return error("", array("redirect" => url_to("/donate/paypal/error")));
        } catch (\Exception $ex) {
            \Morpheus\Database\Connection::update("mw_orders", array("status" => 7), array("id" => $order["id"]), array("integer", "integer"));
        }
        return error("", array("redirect" => url_to("/donate/paypal/error")));
    }
}

?>