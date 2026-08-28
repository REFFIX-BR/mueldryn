<?php
// Reset OpenMU password for an account using Bridge hash logic if available.
chdir(dirname(__DIR__));
require 'constants.php';
require 'bootstrap.php';

$login = isset($argv[1]) ? $argv[1] : 'testgm2';
$pass = isset($argv[2]) ? $argv[2] : 'testgm123';

$hash = password_hash($pass, PASSWORD_BCRYPT);
$pdo = Morpheus\OpenMU\Bridge::pdo();
$st = $pdo->prepare('UPDATE data."Account" SET "PasswordHash" = :h WHERE lower("LoginName") = lower(:l)');
$st->execute(array('h' => $hash, 'l' => $login));
echo "updated rows=" . $st->rowCount() . PHP_EOL;
echo "verify=" . (Morpheus\OpenMU\Bridge::verifyPassword($login, $pass) ? 'OK' : 'FAIL') . PHP_EOL;
