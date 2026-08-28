<?php
chdir(dirname(__DIR__));
require 'constants.php';
require 'bootstrap.php';

Connection::executeUpdate("UPDATE mw_config SET template = 'unique' WHERE id = 1");
$row = Connection::fetchAssoc('SELECT template FROM mw_config WHERE id = 1');
echo 'template=' . $row['template'] . PHP_EOL;

$plugins = Connection::fetchAll("SELECT name, active FROM mw_plugins WHERE name IN ('Market', 'CharMarket')");
foreach ($plugins as $p) {
    echo $p['name'] . '=' . $p['active'] . PHP_EOL;
}
