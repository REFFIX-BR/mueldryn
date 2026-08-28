<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

namespace PaymentsGateways\Util;

class Gateway
{
    public static function name($gateway)
    {
        $names = array(
            "pagseguro" => "PagSeguro",
            "paypal" => "PayPal",
            "mercadopago" => "Mercado Pago",
            "picpay" => "PicPay",
            "boletofacil" => "Boleto Fácil",
        );
        return isset($names[strtolower($gateway)]) ? $names[strtolower($gateway)] : ucfirst($gateway);
    }
    public static function simplePaymentStatus($status)
    {
        switch ($status) {
            case 1:
                return __("Waiting payment");
            case 2:
            case 5:
                return __("In analysis");
            case 3:
            case 4:
                return __("Paid");
            case 6:
                return __("Returned");
            case 7:
                return __("Canceled");
        }
        return "-";
    }
    public static function statusColor($status)
    {
        switch ($status) {
            case 1:
                return "yellow";
            case 2:
            case 5:
                return "yellow";
            case 3:
            case 4:
                return "green";
            case 6:
                return "red";
            case 7:
                return "red";
        }
        return "aqua";
    }
}

?>