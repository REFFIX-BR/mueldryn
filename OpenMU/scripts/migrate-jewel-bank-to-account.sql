-- Migrate Jewel Bank counters from Character -> Account (shared across chars).
-- Idempotent: sums character values into account rows, then deletes character rows.

BEGIN;

CREATE TEMP TABLE tmp_jewel_bank_sums ON COMMIT DROP AS
SELECT c."AccountId" AS account_id,
       sa."DefinitionId" AS def_id,
       SUM(sa."Value")::real AS total
FROM data."StatAttribute" sa
INNER JOIN data."Character" c ON c."Id" = sa."CharacterId"
WHERE sa."CharacterId" IS NOT NULL
  AND sa."DefinitionId" IN (
    'a1b2c3d4-1001-4e5f-8a9b-0c1d2e3f4001'::uuid,
    'a1b2c3d4-1002-4e5f-8a9b-0c1d2e3f4002'::uuid,
    'a1b2c3d4-1003-4e5f-8a9b-0c1d2e3f4003'::uuid,
    'a1b2c3d4-1004-4e5f-8a9b-0c1d2e3f4004'::uuid,
    'a1b2c3d4-1005-4e5f-8a9b-0c1d2e3f4005'::uuid,
    'a1b2c3d4-1006-4e5f-8a9b-0c1d2e3f4006'::uuid,
    'a1b2c3d4-1007-4e5f-8a9b-0c1d2e3f4007'::uuid,
    'a1b2c3d4-1008-4e5f-8a9b-0c1d2e3f4008'::uuid,
    'a1b2c3d4-1009-4e5f-8a9b-0c1d2e3f4009'::uuid,
    'a1b2c3d4-1010-4e5f-8a9b-0c1d2e3f4010'::uuid
  )
GROUP BY c."AccountId", sa."DefinitionId"
HAVING SUM(sa."Value") > 0;

UPDATE data."StatAttribute" a
SET "Value" = a."Value" + t.total
FROM tmp_jewel_bank_sums t
WHERE a."AccountId" = t.account_id
  AND a."DefinitionId" = t.def_id
  AND a."CharacterId" IS NULL;

INSERT INTO data."StatAttribute" ("Id", "AccountId", "CharacterId", "DefinitionId", "Value")
SELECT gen_random_uuid(), t.account_id, NULL, t.def_id, t.total
FROM tmp_jewel_bank_sums t
WHERE NOT EXISTS (
  SELECT 1
  FROM data."StatAttribute" a
  WHERE a."AccountId" = t.account_id
    AND a."DefinitionId" = t.def_id
    AND a."CharacterId" IS NULL
);

DELETE FROM data."StatAttribute" sa
WHERE sa."CharacterId" IS NOT NULL
  AND sa."DefinitionId" IN (
    'a1b2c3d4-1001-4e5f-8a9b-0c1d2e3f4001'::uuid,
    'a1b2c3d4-1002-4e5f-8a9b-0c1d2e3f4002'::uuid,
    'a1b2c3d4-1003-4e5f-8a9b-0c1d2e3f4003'::uuid,
    'a1b2c3d4-1004-4e5f-8a9b-0c1d2e3f4004'::uuid,
    'a1b2c3d4-1005-4e5f-8a9b-0c1d2e3f4005'::uuid,
    'a1b2c3d4-1006-4e5f-8a9b-0c1d2e3f4006'::uuid,
    'a1b2c3d4-1007-4e5f-8a9b-0c1d2e3f4007'::uuid,
    'a1b2c3d4-1008-4e5f-8a9b-0c1d2e3f4008'::uuid,
    'a1b2c3d4-1009-4e5f-8a9b-0c1d2e3f4009'::uuid,
    'a1b2c3d4-1010-4e5f-8a9b-0c1d2e3f4010'::uuid
  );

COMMIT;
