<?php
$pdo = new PDO('pgsql:host=127.0.0.1;port=5433;dbname=openmu', 'postgres', 'admin', [
    PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
]);
$st = $pdo->prepare(
    'SELECT d."Group" AS grp, d."Number" AS num, d."Name" AS name, i."ItemSlot" AS slot
     FROM data."Account" a
     JOIN data."Item" i ON i."ItemStorageId" = a."VaultId"
     JOIN config."ItemDefinition" d ON d."Id" = i."DefinitionId"
     WHERE lower(a."LoginName") = lower(:login)
     ORDER BY i."ItemSlot"'
);
$st->execute(['login' => 'testgm']);
$rows = $st->fetchAll(PDO::FETCH_ASSOC);
echo "testgm vault items: " . count($rows) . PHP_EOL;
foreach ($rows as $r) {
    $key = $r['grp'] . '-' . $r['num'];
    $gif = dirname(__DIR__) . '/resources/images/items/' . $key . '.gif';
    echo sprintf("slot=%2d %s => %s | gif=%s\n", $r['slot'], $key, $r['name'], is_file($gif) ? 'YES' : 'NO');
}
