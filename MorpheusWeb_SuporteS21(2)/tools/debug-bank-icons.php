<?php
require dirname(__DIR__) . '/constants.php';
require dirname(__DIR__) . '/bootstrap.php';

$login = 'testgm';
$acc = Morpheus\OpenMU\Bridge::findAccount($login);
$rows = Morpheus\OpenMU\VaultSync::fetchVaultItems($acc['Id']);
echo "Comparing parsed hex section/index vs OpenMU definition\n\n";
$n = 0;
foreach ($rows as $row) {
    if ($n++ >= 10) break;
    $section = (int) $row['item_group'];
    $index = (int) $row['item_number'];
    $hex = Morpheus\OpenMU\InventorySync::itemToHex($row);
    $it = (new Morpheus\Item\Item($hex))->parse();
    $ps = (int) $it->getSection();
    $pi = (int) $it->getIndex();
    $view = Morpheus\Util\Item::bankItemView($it, array('slot' => (int)$row['ItemSlot']), 48, $row['item_name']);
    $match = ($ps === $section && $pi === $index) ? 'OK' : 'MISMATCH';
    echo sprintf("%s slot=%2d OpenMU=%d-%d parsed=%d-%d name=%s\n  image=%s\n",
        $match, $row['ItemSlot'], $section, $index, $ps, $pi, $view['name'], $view['image']);
}
