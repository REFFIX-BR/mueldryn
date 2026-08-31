-- Manual Fallback SQL Migration for Fortress of Imperial Dungeon Entry Attributes
-- 
-- This script provides a PostgreSQL fallback for adding the two AttributeDefinition
-- records required by the Fortress of Imperial Dungeon daily entry-limit system.
--
-- IMPORTANT: This script is only needed if the automatic UpdatePlugIn fails to execute.
-- Under normal circumstances, the AddDungeonEntryAttributesUpdatePlugIn will handle
-- this migration automatically when the server starts.
--
-- The automatic plugin is located at:
-- src/Persistence/Initialization/Updates/AddDungeonEntryAttributesUpdatePlugIn.cs
--
-- Attribute Definitions:
--   1. DungeonEntryDate - Last reset date as UTC yyyyMMdd (float)
--   2. DungeonEntriesConsumed - Entries consumed today, 0-3 (float)
--
-- These are persisted as CharacterStatAttribute rows and must be registered in
-- the GameConfiguration.Attributes collection before the dungeon logic can use them.
--
-- Usage:
--   psql -U [username] -d [database] -f DungeonEntryAttributes_Manual_Fallback.sql
--
-- Or execute directly in your PostgreSQL client.

-- Check if the attributes already exist before inserting
DO $$
DECLARE
    v_dungeon_entry_date_id UUID := 'E5D4C3B2-A101-4F8E-9C7D-6B5A4F3E2D01';
    v_dungeon_entries_consumed_id UUID := 'E5D4C3B2-A102-4F8E-9C7D-6B5A4F3E2D02';
    v_exists_count INTEGER;
BEGIN
    -- Check if DungeonEntryDate attribute already exists
    SELECT COUNT(*) INTO v_exists_count
    FROM config."AttributeDefinition"
    WHERE "Id" = v_dungeon_entry_date_id;

    IF v_exists_count = 0 THEN
        INSERT INTO config."AttributeDefinition" ("Id", "Designation", "Description", "MaximumValue")
        VALUES (
            v_dungeon_entry_date_id,
            'DungeonEntryDate',
            'Fortress of Imperial Dungeon — last entry reset date',
            NULL
        );
        RAISE NOTICE 'Added DungeonEntryDate AttributeDefinition';
    ELSE
        RAISE NOTICE 'DungeonEntryDate AttributeDefinition already exists, skipping';
    END IF;

    -- Check if DungeonEntriesConsumed attribute already exists
    SELECT COUNT(*) INTO v_exists_count
    FROM config."AttributeDefinition"
    WHERE "Id" = v_dungeon_entries_consumed_id;

    IF v_exists_count = 0 THEN
        INSERT INTO config."AttributeDefinition" ("Id", "Designation", "Description", "MaximumValue")
        VALUES (
            v_dungeon_entries_consumed_id,
            'DungeonEntriesConsumed',
            'Fortress of Imperial Dungeon — entries consumed today',
            NULL
        );
        RAISE NOTICE 'Added DungeonEntriesConsumed AttributeDefinition';
    ELSE
        RAISE NOTICE 'DungeonEntriesConsumed AttributeDefinition already exists, skipping';
    END IF;
END $$;

-- Verify the insertion
SELECT "Id", "Designation", "Description"
FROM config."AttributeDefinition"
WHERE "Id" IN (
    'E5D4C3B2-A101-4F8E-9C7D-6B5A4F3E2D01'::uuid,
    'E5D4C3B2-A102-4F8E-9C7D-6B5A4F3E2D02'::uuid
);
