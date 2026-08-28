<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

require_once PLUGIN_PATH . "PaymentsGateways" . DS . "vendor" . DS . "autoload.php";
function configurePagseguro()
{
    PagSeguro\Library::initialize();
    PagSeguro\Configuration\Configure::setEnvironment(config("gateways.pagseguro.sandbox", false) ? "sandbox" : "production");
    PagSeguro\Configuration\Configure::setAccountCredentials(trim(config("gateways.pagseguro.email")), trim(config("gateways.pagseguro.token")));
    PagSeguro\Configuration\Configure::setCharset("UTF-8");
    PagSeguro\Configuration\Configure::setLog(true, LOGS_PATH . "pagseguro.log");
}
function getPaypalApiContext()
{
    $context = new PayPal\Rest\ApiContext(new PayPal\Auth\OAuthTokenCredential(trim(config("gateways.paypal.client_id")), trim(config("gateways.paypal.client_secret"))));
    $context->setConfig(array("mode" => config("gateways.paypal.sandbox", false) ? "sandbox" : "live", "log.LogEnabled" => true, "log.FileName" => LOGS_PATH . "paypal.log", "log.LogLevel" => config("debug") ? "DEBUG" : "INFO", "cache.enabled" => true));
    return $context;
}

?>