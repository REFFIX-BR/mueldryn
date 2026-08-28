<?php

return [
    'item' => [
        'multiple_names' => array(),
    ],

    // Ícones oficiais Mudream (CDN) — mesmos sprites do mercado mudream.online
    'mudream_icons' => [
        'enabled' => true,
        'season' => '6plus',
        'cdn_base' => 'https://dreamassets.fra1.cdn.digitaloceanspaces.com/items_seasons',
        // Itens com index >= min_index usam CDN quando não há .gif local
        'min_index' => 300,
    ],

    // PNGs do OpenMU ItemEditor (itens vanilla)
    'openmu_item_editor' => dirname(dirname(__DIR__)) . '/OpenMU/publish-cashshop/wwwroot/_content/MUnique.OpenMU.Web.ItemEditor/img/items',
];
