<?php
chdir(dirname(__DIR__));
require 'constants.php';
require 'bootstrap.php';

$login = 'testgm2';
$raw = Morpheus\Database\Connection::fetchColumn(
    "SELECT CONVERT(VARCHAR(MAX), Items, 2) FROM warehouse WHERE AccountID = ?",
    array($login)
);
echo "hexlen=" . strlen((string)$raw) . "\n";
$acc = account($login);
if (!$acc || !$acc->exists()) {
    // force load memb
    $acc = new Morpheus\Account\Account();
    $acc->read($login);
}
$wh = new Morpheus\Account\Warehouse($acc, 8, 15, 1);
$wh->load($raw);
$n = 0;
foreach ($wh->getItems() as $x => $col) {
    if (!is_array($col)) continue;
    foreach ($col as $y => $it) {
        if ($it) {
            $n++;
            if ($n <= 3) {
                echo "item {$x},{$y} " . $it->getName() . " +" . $it->getLevel() . "\n";
            }
        }
    }
}
echo "total=$n\n";
