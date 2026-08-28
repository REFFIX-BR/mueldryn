<?php
return array(
"name" => "Auction", 
"author" => "Wellwisher22", 
"version" => "1.0.0", 
"description" => "Sistema de Leilão de itens para os jogadores", 
"website" => "#", 
"license" => false, 
"config_url" => "/auctions/settings", 
"services" => array("auction" => array("name" => "Leilões", "allowed" => true, "url" => "/panel/auctions")));
