SELECT COUNT(*) AS total_items FROM data."Item";

SELECT COUNT(*) AS inventory_items
FROM data."Item" i
JOIN data."Character" c ON c."InventoryId" = i."ItemStorageId";

SELECT a."LoginName", c."Name", COUNT(i."Id") AS inv_items
FROM data."Character" c
JOIN data."Account" a ON a."Id" = c."AccountId"
LEFT JOIN data."Item" i ON i."ItemStorageId" = c."InventoryId"
WHERE lower(a."LoginName") = lower('testgm')
GROUP BY a."LoginName", c."Name"
ORDER BY c."Name";

SELECT COUNT(*) AS vault_items
FROM data."Account" a
JOIN data."Item" i ON i."ItemStorageId" = a."VaultId"
WHERE lower(a."LoginName") = lower('testgm');

SELECT sa."Value", ad."Designation"
FROM data."Account" a
JOIN data."StatAttribute" sa ON sa."AccountId" = a."Id"
JOIN config."AttributeDefinition" ad ON ad."Id" = sa."DefinitionId"
WHERE lower(a."LoginName") = lower('testgm')
  AND sa."Value" > 0
ORDER BY ad."Designation";
