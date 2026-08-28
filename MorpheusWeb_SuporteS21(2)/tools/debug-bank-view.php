<?php
require dirname(__DIR__) . '/constants.php';
require dirname(__DIR__) . '/bootstrap.php';

$login = 'testgm';
\app()->session->set('user', $login);
$user = Morpheus\Account\Account::find($login);
if ($user) {
    Morpheus\Account\Auth::login($user);
}

$siteItems = Morpheus\Account\SiteBank::listForLogin($login);
$gameItems = Morpheus\OpenMU\VaultSync::listForLogin($login);
$gameZen = Morpheus\OpenMU\Bridge::getVaultMoney($login);

echo "gameItems=" . count($gameItems) . " zen=$gameZen\n\n";
foreach (array_slice($gameItems, 0, 6) as $v) {
    echo "slot={$v['slot']} name={$v['name']}\n  image={$v['image']}\n";
}
