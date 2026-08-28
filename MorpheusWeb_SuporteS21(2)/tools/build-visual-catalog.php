<?php
/**
 * Gera tmp/cache/visual-items.php a partir do catálogo Mudream (OpenMU/tools).
 * Uso: php tools/build-visual-catalog.php
 */

define('ROOT', dirname(__DIR__));
define('DS', DIRECTORY_SEPARATOR);

require ROOT . DS . 'src' . DS . 'Item' . DS . 'VisualCatalog.php';

$items = Morpheus\Item\VisualCatalog::buildFromJson();
$file = Morpheus\Item\VisualCatalog::writeCache($items);

echo 'Visual catalog: ' . count($items) . " itens\n";
echo 'Cache: ' . $file . "\n";
