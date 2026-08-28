<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace PaymentsGateways\Gateway;

class Gateway
{
    public static function factory($gateway)
    {
        $gateways = array(
            "pagseguro" => "PagSeguro", 
            "paypal" => "PayPal", 
            "boletofacil" => "BoletoFacil",
            "picpay" => "PicPay",
            "mercadopago" => "MercadoPago"
        );
        if (!isset($gateways[$gateway])) {
            throw new \Morpheus\Exception\Exception("Gateway " . $gateway . " not found");
        }
        $class = "\\PaymentsGateways\\Gateway\\" . $gateways[$gateway];
        if (!class_exists($class)) {
            throw new \Morpheus\Exception\Exception("Gateway " . $gateways[$gateway] . " not implemented");
        }
        return new $class();
    }
}

?>