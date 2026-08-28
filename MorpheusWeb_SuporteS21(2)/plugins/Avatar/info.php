<?php
/*
 * @ PHP 5.6
 * @ Decoder version : 1.0.0.1
 * @ Release on : 24.03.2018
 * @ Website    : http://EasyToYou.eu
 */

return array("name" => "Avatar", "author" => "Flavio Hernandes", "version" => "1.0.0", "description" => "Alterar imagem de perfil de um personagem", "website" => "https://morpheusmuweb.com/", "config_url" => "/configs/avatar", "license" => false, "services" => array("character.avatar" => array("name" => "Avatar", "parent_id" => "character", "allowed" => true, "url" => "/change-avatar", "config_url" => "/configs/avatar")));

?>