<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

return array("name" => "WebShop", "author" => "Flavio Hernandes", "version" => "1.1.4", "description" => "Venda de itens contidos no jogo", "website" => "https://morpheusmuweb.com/", "license" => false, "services" => array("purchases" => array("name" => "Minhas Compras", "allowed" => true, "url" => "/panel/purchases"), "purchases.webshop" => array("name" => "WebShop", "allowed" => true, "parent_id" => "purchases", "url" => "/shop/purchases")));

?>