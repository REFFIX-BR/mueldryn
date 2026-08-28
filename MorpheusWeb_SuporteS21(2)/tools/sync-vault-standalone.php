<?php
/**
 * Sync vault sem Doctrine (evita incompatibilidade PHP CLI).
 * php tools/sync-vault-standalone.php testgm2
 */
$login = isset($argv[1]) ? $argv[1] : 'testgm2';

$pg = new PDO('pgsql:host=127.0.0.1;port=5433;dbname=openmu', 'postgres', 'admin', array(
    PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
));

$st = $pg->prepare('SELECT a."Id", a."VaultId" FROM data."Account" a WHERE lower(a."LoginName") = lower(?)');
$st->execute(array($login));
$acc = $st->fetch(PDO::FETCH_ASSOC);
if (!$acc) {
    fwrite(STDERR, "Account not found\n");
    exit(1);
}

$st = $pg->prepare(
    'SELECT i."Id", i."ItemSlot", i."Level", i."Durability", i."HasSkill", i."SocketCount",
            d."Group" AS g, d."Number" AS n, d."Name" AS name
     FROM data."Item" i
     JOIN config."ItemDefinition" d ON d."Id" = i."DefinitionId"
     WHERE i."ItemStorageId" = ?
     ORDER BY i."ItemSlot"'
);
$st->execute(array($acc['VaultId']));
$rows = $st->fetchAll(PDO::FETCH_ASSOC);
echo "Vault items: " . count($rows) . "\n";

// Minimal hex: empty 120 slots * 32 nibbles, put FF-filled then overwrite with basic section/index encoding
// Better: call web endpoint. For now write placeholder using php built-in + sqlsrv

$itemSize = 32;
$maxSlots = 120;
$slots = array_fill(0, $maxSlots, str_repeat('F', $itemSize));

// Use Morpheus GenerateHex via including only Item classes without Doctrine
chdir(dirname(__DIR__));
require __DIR__ . '/../constants.php';
require ROOT . '/vendor/autoload.php';
require ROOT . '/src/basics.php';

// Fake config without loading DB
class _Cfg {
    public static $d = array('item.db_version' => 3);
}
if (!function_exists('config')) {
    function config($k, $d = null) {
        $map = array(
            'item.db_version' => 3,
            'item.size' => null,
            'warehouse.extended' => false,
        );
        return array_key_exists($k, $map) ? $map[$k] : $d;
    }
}

use Morpheus\OpenMU\InventorySync;

foreach ($rows as $row) {
    $row['item_group'] = $row['g'];
    $row['item_number'] = $row['n'];
    $row['item_name'] = $row['name'];
    $row['options'] = array();
    $row['ancient'] = null;
    $slot = (int) $row['ItemSlot'];
    if ($slot < 0 || $slot >= $maxSlots) continue;
    $hex = InventorySync::itemToHex($row);
    if ($hex && strlen($hex) === $itemSize) {
        $slots[$slot] = $hex;
    }
}
$full = strtoupper(implode('', $slots));
echo "Hex length: " . strlen($full) . "\n";

$sqlsrv = new PDO(
    'sqlsrv:Server=localhost\\SQLEXPRESS;Database=MuOnline',
    'morpheus',
    'Morph3us@Local!',
    array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION)
);
$sqlsrv->exec("UPDATE warehouse SET Items = 0x{$full} WHERE AccountID = " . $sqlsrv->quote($login));
$chk = $sqlsrv->query("SELECT DATALENGTH(Items) FROM warehouse WHERE AccountID = " . $sqlsrv->quote($login))->fetchColumn();
echo "MSSQL warehouse bytes: {$chk}\n";
echo "OK\n";
