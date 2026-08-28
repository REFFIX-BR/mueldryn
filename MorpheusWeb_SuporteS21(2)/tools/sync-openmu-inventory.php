<?php
/**
 * Re-sincroniza inventários OpenMU → MSSQL.
 * Uso: php tools/sync-openmu-inventory.php [characterName]
 */
error_reporting(E_ALL);
ini_set('display_errors', '1');

chdir(dirname(__DIR__));
require __DIR__ . '/../constants.php';
require ROOT . DS . 'vendor' . DS . 'autoload.php';
require ROOT . DS . 'src' . DS . 'basics.php';

Morpheus\Core\Config::addFiles(array('app.php', 'characters.php', 'maps.php', 'items.php', 'database.php', 'openmu.php'));

foreach (config('conn', array()) as $name => $conf) {
    if (!isset($conf['active']) || $conf['active']) {
        $conn = Morpheus\Database\DriverManager::addConnection($name, $conf);
        $conn->connect();
    }
}

use Morpheus\OpenMU\Bridge;
use Morpheus\OpenMU\InventorySync;

if (!Bridge::enabled()) {
    fwrite(STDERR, "OpenMU bridge desabilitado\n");
    exit(1);
}

$filter = isset($argv[1]) ? $argv[1] : null;

try {
    $pdo = Bridge::pdo();
    $pdo->query('SELECT 1')->fetch();
} catch (Exception $e) {
    fwrite(STDERR, "Falha ao conectar OpenMU Postgres: " . $e->getMessage() . "\n");
    fwrite(STDERR, "Verifique se o container 'database' está up (porta 5433).\n");
    exit(1);
}

if ($filter) {
    $names = array($filter);
} else {
    $st = $pdo->query('SELECT "Name" FROM data."Character" ORDER BY "Name"');
    $names = $st->fetchAll(PDO::FETCH_COLUMN);
}

$ok = 0;
$err = 0;
foreach ($names as $name) {
    try {
        $n = InventorySync::syncCharacter($name);
        echo "OK {$name}: {$n} itens\n";
        $ok++;
    } catch (Exception $e) {
        echo "ERR {$name}: " . $e->getMessage() . "\n";
        $err++;
    }
}

echo "Done. ok={$ok} err={$err}\n";
