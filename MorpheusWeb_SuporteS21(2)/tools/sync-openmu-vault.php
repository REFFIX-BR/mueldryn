<?php
/**
 * Sincroniza vault OpenMU → warehouse MSSQL.
 * Uso: php tools/sync-openmu-vault.php [login]
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
use Morpheus\OpenMU\VaultSync;

if (!Bridge::enabled()) {
    fwrite(STDERR, "OpenMU bridge desabilitado\n");
    exit(1);
}

$filter = isset($argv[1]) ? $argv[1] : null;

try {
    Bridge::pdo()->query('SELECT 1')->fetch();
} catch (Exception $e) {
    fwrite(STDERR, "Falha Postgres: " . $e->getMessage() . "\n");
    exit(1);
}

$st = Bridge::pdo()->query('SELECT "LoginName" FROM data."Account" ORDER BY "LoginName"');
$logins = $st->fetchAll(PDO::FETCH_COLUMN);
$total = 0;
foreach ($logins as $login) {
    if ($filter && strcasecmp($login, $filter) !== 0) {
        continue;
    }
    try {
        $n = VaultSync::syncAccount($login);
        echo "OK {$login}: {$n} itens\n";
        $total += $n;
    } catch (Exception $e) {
        echo "FAIL {$login}: " . $e->getMessage() . "\n";
    }
}
echo "Total itens: {$total}\n";
