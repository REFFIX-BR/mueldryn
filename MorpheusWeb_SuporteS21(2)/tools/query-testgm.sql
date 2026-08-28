SELECT d."Group", d."Number", d."Name", i."ItemSlot"
FROM data."Account" a
JOIN data."Item" i ON i."ItemStorageId" = a."VaultId"
JOIN config."ItemDefinition" d ON d."Id" = i."DefinitionId"
WHERE lower(a."LoginName") = lower('testgm')
ORDER BY i."ItemSlot";
