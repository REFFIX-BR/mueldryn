-- Link Move "Arena" warp to ExitGate at 62,117 and mark as spawn gate.
UPDATE config."ExitGate" e
SET
  "X1" = 62,
  "Y1" = 117,
  "X2" = 62,
  "Y2" = 117,
  "IsSpawnGate" = true,
  "Direction" = 0
FROM config."GameMapDefinition" m
WHERE e."MapId" = m."Id"
  AND m."Number" = 6
  AND e."X1" = 62
  AND e."Y1" = 117;

UPDATE config."WarpInfo" w
SET "GateId" = e."Id"
FROM config."ExitGate" e
JOIN config."GameMapDefinition" m ON e."MapId" = m."Id"
WHERE w."Index" = 1
  AND w."Name" = 'Arena'
  AND m."Number" = 6
  AND e."X1" = 62
  AND e."Y1" = 117;

SELECT w."Index", w."Name", w."GateId", e."X1", e."Y1", e."X2", e."Y2", e."IsSpawnGate"
FROM config."WarpInfo" w
LEFT JOIN config."ExitGate" e ON w."GateId" = e."Id"
WHERE w."Index" = 1;
