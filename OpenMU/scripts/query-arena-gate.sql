SELECT e."Id", e."X1", e."Y1", e."X2", e."Y2", e."IsSpawnGate", e."Direction"
FROM config."ExitGate" e
JOIN config."GameMapDefinition" m ON e."MapId" = m."Id"
WHERE m."Number" = 6;

SELECT w."Id", w."Index", w."Name", w."GateId", w."Costs", w."LevelRequirement"
FROM config."WarpInfo" w
WHERE w."Name" ILIKE '%Arena%' OR w."Index" = 1;

-- spawn gates for arena
SELECT e."Id", e."X1", e."Y1", e."X2", e."Y2", e."IsSpawnGate"
FROM config."ExitGate" e
JOIN config."GameMapDefinition" m ON e."MapId" = m."Id"
WHERE m."Number" = 6 AND e."IsSpawnGate" = true;
