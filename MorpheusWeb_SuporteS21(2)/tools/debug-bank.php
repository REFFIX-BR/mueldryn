<?php
chdir(dirname(__DIR__));
require 'constants.php';
require 'bootstrap.php';

$login = 'testgm2';
error_reporting(E_ALL);
ini_set('display_errors', '1');

try {
    echo "1 find\n";
    $acc = Morpheus\OpenMU\Bridge::findAccount($login);
    var_export($acc ? $acc['Id'] : null);
    echo "\n2 fetch\n";
    $rows = Morpheus\OpenMU\VaultSync::fetchVaultItems($acc['Id']);
    echo "count=" . count($rows) . "\n";
    echo "3 list\n";
    $list = Morpheus\OpenMU\VaultSync::listForLogin($login);
    echo "list=" . count($list) . "\n";
    echo "4 money\n";
    echo Morpheus\OpenMU\Bridge::getVaultMoney($login) . "\n";
} catch (Throwable $e) {
    echo "ERR: " . $e->getMessage() . "\n" . $e->getFile() . ':' . $e->getLine() . "\n";
}
