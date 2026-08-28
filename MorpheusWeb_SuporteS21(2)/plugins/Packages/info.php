<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

return array("name" => "Packages", "author" => "Flavio Hernandes", "version" => "1.0.0", "description" => "Venda de Vips, moedas ou pacotes", "website" => "https://morpheusmuweb.com/", "license" => false, "config_url" => "/packages", "services" => array("purchases" => array("name" => "Minhas Compras", "allowed" => true, "url" => "/panel/purchases"), "credit-shop" => array("name" => "Loja de Créditos", "allowed" => true, "url" => "/packages", "config_url" => "/vip/payment")));

?>