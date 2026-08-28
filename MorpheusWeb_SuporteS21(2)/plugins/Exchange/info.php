<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

return array("name" => "Exchange", "author" => "Flavio Hernandes", "version" => "1.0.6", "description" => "Transferencia de moedas", "website" => "https://morpheusmuweb.com/", "config_url" => "/exchange/config", "license" => false, "services" => array("account.exchange" => array("name" => "Troca de moeda", "parent_id" => "account", "allowed" => true, "url" => "/panel/account/exchange", "config_url" => "/exchange/config")));

?>