<?php
require dirname(__DIR__) . '/constants.php';
require dirname(__DIR__) . '/bootstrap.php';

$login = 'testgm';
echo "=== OpenMU ===\n";
try {
    $open = Morpheus\OpenMU\VaultSync::listForLogin($login);
    echo 'count=' . count($open) . "\n";
    if ($open) {
        echo 'first: ' . $open[0]['name'] . ' img=' . $open[0]['image'] . "\n";
    }
} catch (Exception $e) {
    echo 'ERR: ' . $e->getMessage() . "\n";
}

echo "\n=== MSSQL warehouse ===\n";
try {
    $raw = Morpheus\Database\Connection::fetchColumn(
        "SELECT CONVERT(VARCHAR(MAX), Items, 2) FROM warehouse WHERE AccountID = ?",
        array($login)
    );
    echo 'hex len=' . strlen((string)$raw) . "\n";
    if ($raw) {
        $wh = Morpheus\Account\User::find($login)->getWarehouse();
        $wh->load($raw);
        $n = 0;
        foreach ($wh->getItems() as $x => $col) {
            if (!is_array($col)) continue;
            foreach ($col as $y => $it) {
                if (!$it) continue;
                if ($n++ >= 5) break 2;
                $view = Morpheus\Util\Item::bankItemView($it, array('slot' => $wh->getSlotByCord($x,$y)), 48);
                echo sprintf("slot=%s %d-%d name=%s img=%s\n", $wh->getSlotByCord($x,$y), $it->getSection(), $it->getIndex(), $view['name'], $view['image']);
            }
        }
        echo 'total parsed items scan...' . $n . "\n";
    }
} catch (Exception $e) {
    echo 'ERR: ' . $e->getMessage() . "\n";
}
