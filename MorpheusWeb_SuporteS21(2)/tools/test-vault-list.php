<?php
require dirname(__DIR__) . '/constants.php';
require dirname(__DIR__) . '/bootstrap.php';

$login = isset($argv[1]) ? $argv[1] : 'testgm2';
echo "Bridge enabled: " . (Morpheus\OpenMU\Bridge::enabled() ? 'yes' : 'no') . PHP_EOL;
$acc = Morpheus\OpenMU\Bridge::findAccount($login);
echo "Account: " . ($acc ? $acc['Id'] : 'NOT FOUND') . PHP_EOL;
if (!$acc) {
    exit(1);
}
$raw = Morpheus\OpenMU\VaultSync::fetchVaultItems($acc['Id']);
echo "fetchVaultItems: " . count($raw) . PHP_EOL;
$list = Morpheus\OpenMU\VaultSync::listForLogin($login);
echo "listForLogin: " . count($list) . PHP_EOL;
if ($list) {
    $first = $list[0];
    echo "first: {$first['name']} slot={$first['slot']} id={$first['id']}\n";
    echo "image: {$first['image']}\n";
    echo "hex len: " . strlen((string) $first['hex']) . PHP_EOL;
}
