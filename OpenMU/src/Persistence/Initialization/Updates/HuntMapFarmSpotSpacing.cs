// <copyright file="HuntMapFarmSpotSpacing.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using MUnique.OpenMU.DataModel.Configuration;

/// <summary>
/// Shared logic to thin hunt-map farm spots by minimum centre distance (1 tile ≈ 1 m).
/// Curated city maps use ~28-tile grids (<see cref="AddCityFiveMobSpotsUpdatePlugIn"/>).
/// </summary>
internal static class HuntMapFarmSpotSpacing
{
    /// <summary>First pass spacing requested for hunt maps.</summary>
    internal const float InitialMinDistanceTiles = 4.5f;

    /// <summary>Wider spacing so end-game maps (Kanturu, Aida, …) are not visually stacked.</summary>
    internal const float WideMinDistanceTiles = 12f;

    /// <summary>Extra-wide spacing (~20 m) for hunt maps that still feel dense at 12 m.</summary>
    internal const float ExtraWideMinDistanceTiles = 20f;

    /// <summary>
    /// Spawns larger than this on a side are area packs, not point spots — leave them.
    /// </summary>
    private const int MaxSpotSide = 8;

    /// <summary>Radius added around a Quantity&gt;1 point so the pack can occupy distinct tiles.</summary>
    private const int PackRadius = 1;

    /// <summary>
    /// Keeps farm spots at least <paramref name="minDistanceTiles"/> apart (centre to centre)
    /// and expands surviving point packs into a 3×3 tile area.
    /// </summary>
    internal static void Apply(GameConfiguration gameConfiguration, float minDistanceTiles)
    {
        var minDistanceSquared = minDistanceTiles * minDistanceTiles;

        foreach (var map in gameConfiguration.Maps)
        {
            if (FarmSpotDensityUpdatePlugIn.ExcludedMaps.Contains(map.Number))
            {
                continue;
            }

            var spots = map.MonsterSpawns
                .Where(IsFarmSpot)
                .OrderBy(s => CenterX(s))
                .ThenBy(s => CenterY(s))
                .ToList();

            var kept = new List<MonsterSpawnArea>();
            foreach (var spawn in spots)
            {
                if (kept.Any(existing => DistanceSquared(existing, spawn) < minDistanceSquared))
                {
                    spawn.Quantity = 0;
                    continue;
                }

                kept.Add(spawn);
                ExpandPointPack(spawn);
            }
        }
    }

    private static bool IsFarmSpot(MonsterSpawnArea spawn)
    {
        if (spawn.SpawnTrigger != SpawnTrigger.Automatic
            || spawn.Quantity <= 0
            || spawn.MonsterDefinition is not { ObjectKind: NpcObjectKind.Monster })
        {
            return false;
        }

        var width = Math.Abs(spawn.X2 - spawn.X1) + 1;
        var height = Math.Abs(spawn.Y2 - spawn.Y1) + 1;
        return width <= MaxSpotSide && height <= MaxSpotSide;
    }

    private static void ExpandPointPack(MonsterSpawnArea spawn)
    {
        if (!spawn.IsPoint() || spawn.Quantity <= 1)
        {
            return;
        }

        var x = spawn.X1;
        var y = spawn.Y1;
        spawn.X1 = ClampMap(x - PackRadius);
        spawn.Y1 = ClampMap(y - PackRadius);
        spawn.X2 = ClampMap(x + PackRadius);
        spawn.Y2 = ClampMap(y + PackRadius);
    }

    private static float CenterX(MonsterSpawnArea spawn) => (spawn.X1 + spawn.X2) / 2f;

    private static float CenterY(MonsterSpawnArea spawn) => (spawn.Y1 + spawn.Y2) / 2f;

    private static float DistanceSquared(MonsterSpawnArea a, MonsterSpawnArea b)
    {
        var dx = CenterX(a) - CenterX(b);
        var dy = CenterY(a) - CenterY(b);
        return (dx * dx) + (dy * dy);
    }

    private static byte ClampMap(int value) => (byte)Math.Clamp(value, 0, 255);
}
