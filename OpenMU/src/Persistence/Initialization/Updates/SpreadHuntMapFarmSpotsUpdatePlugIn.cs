// <copyright file="SpreadHuntMapFarmSpotsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Hunt maps outside the curated city set inherited vanilla 1-mob grids. Farm density then
/// put Quantity=6 on every point, so packs sit on top of each other. This keeps one farm
/// spot per 4.5 tiles (1 tile ≈ 1 m) and opens remaining point packs into a 3×3 so the
/// six mobs are not born on the same coordinate.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("6A8C2E91-4B7D-4F13-A9E0-2C5D8F1B0476")]
public class SpreadHuntMapFarmSpotsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Spread hunt-map farm spots";
    internal const string PlugInDescription =
        "Enforces 4.5-tile minimum spacing between hunt-map farm spots and expands stacked Quantity=6 points into a 3x3 pack.";

    /// <summary>1 tile ≈ 1 gameplay metre; user-requested minimum between spots.</summary>
    private const float MinDistanceTiles = 4.5f;

    private const float MinDistanceSquared = MinDistanceTiles * MinDistanceTiles;

    /// <summary>
    /// Spawns larger than this on a side are area packs, not point spots — leave them.
    /// </summary>
    private const int MaxSpotSide = 8;

    /// <summary>Radius added around a Quantity&gt;1 point so the pack can occupy distinct tiles.</summary>
    private const int PackRadius = 1;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.SpreadHuntMapFarmSpots;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 31, 21, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
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
                if (kept.Any(existing => DistanceSquared(existing, spawn) < MinDistanceSquared))
                {
                    spawn.Quantity = 0;
                    continue;
                }

                kept.Add(spawn);
                ExpandPointPack(spawn);
            }
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
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
