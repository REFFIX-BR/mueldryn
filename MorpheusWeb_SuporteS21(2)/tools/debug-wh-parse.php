<?php
require dirname(__DIR__) . '/constants.php';
require dirname(__DIR__) . '/bootstrap.php';

$login = 'testgm';
$raw = Morpheus\Database\Connection::fetchColumn(
    "SELECT CONVERT(VARCHAR(MAX), Items, 2) FROM warehouse WHERE AccountID = ?",
    array($login)
);
$itemSize = (int) config('item.size', 32);
$slotCount = 120;
$hex = strtoupper(preg_replace('/[^0-9A-Fa-f]/', '', (string) $raw));
echo "hex chars=" . strlen($hex) . " expected=" . ($slotCount * $itemSize * 2) . "\n";

$n = 0;
for ($slot = 0; $slot < $slotCount; $slot++) {
    $off = $slot * $itemSize * 2;
    $chunk = substr($hex, $off, $itemSize * 2);
    if ($chunk === false || preg_match('/^0+$/', $chunk)) {
        continue;
    }
    try {
        $it = (new Morpheus\Item\Item($chunk))->parse();
    } catch (Exception $e) {
        continue;
    }
    $view = Morpheus\Util\Item::bankItemView($it, array('slot' => $slot), 48);
    if ($n++ < 8) {
        echo sprintf("slot=%2d %d-%d name=%s\n  img=%s\n", $slot, $it->getSection(), $it->getIndex(), $view['name'], $view['image']);
    }
}
echo "items in warehouse hex: $n\n";
