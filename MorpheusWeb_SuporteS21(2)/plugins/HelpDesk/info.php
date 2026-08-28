<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

return array("name" => "HelpDesk", "author" => "Flavio Hernandes", "version" => "1.0.2", "description" => "Sistema de chamado de suporte e ajuda", "website" => "https://morpheusmuweb.com/", "license" => false, "config_url" => "/helpdesk/config", "services" => array("support.tickets" => array("name" => "Suporte", "allowed" => true, "url" => "/support/tickets", "config_url" => "/helpdesk/config")));

?>