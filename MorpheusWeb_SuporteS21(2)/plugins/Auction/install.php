<?php

$db = \Morpheus\Core\DB::getInstance();

// Cria a tabela principal de Leilões
$db->query("
    CREATE TABLE IF NOT EXISTS `auctions` (
        `id` INT(11) NOT NULL AUTO_INCREMENT,
        `start_date` DATETIME NOT NULL,
        `end_date` DATETIME NOT NULL,
        `active` TINYINT(1) DEFAULT 1,
        PRIMARY KEY (`id`)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8;
");

// Cria a tabela de Itens do Leilão
$db->query("
    CREATE TABLE IF NOT EXISTS `auction_items` (
        `id` INT(11) NOT NULL AUTO_INCREMENT,
        `auction_id` INT(11) NOT NULL,
        `item` VARCHAR(255) NOT NULL,
        `winner` VARCHAR(50) DEFAULT NULL,
        `winner_date` DATETIME DEFAULT NULL,
        `delivery` TINYINT(1) DEFAULT 0,
        `active` TINYINT(1) DEFAULT 1,
        `can_delivery` TINYINT(1) DEFAULT 0,
        `initial_bid` INT(11) DEFAULT 0,
        `coin` VARCHAR(50) DEFAULT NULL,
        PRIMARY KEY (`id`),
        KEY `auction_id` (`auction_id`)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8;
");