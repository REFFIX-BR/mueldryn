<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

return array("name" => "Market", "author" => "Flavio Hernandes", "version" => "1.0.5", "description" => "Sistema de compra e venda de ítens", "website" => "https://morpheusmuweb.com/", "license" => false, "services" => array("purchases" => array("name" => "Minhas Compras", "allowed" => true, "url" => "/panel/purchases"), "purchases.market" => array("name" => "Market", "allowed" => true, "parent_id" => "purchases", "url" => "/market/purchases")));

?>