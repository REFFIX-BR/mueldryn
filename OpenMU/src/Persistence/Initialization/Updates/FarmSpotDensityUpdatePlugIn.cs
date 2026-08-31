// <copyright file="FarmSpotDensityUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sets hunt-map farm spots to Quantity 6 with 3-second respawn.
/// Re-applies curated city/wilderness spots (update 105 often marked installed without persisting),
/// scales remaining hunt-map Automatic monster spots, and leaves towns/events/bosses alone.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("B7E4A91C-2D58-4F0A-9C31-8E6F5A12D047")]
public class FarmSpotDensityUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Farm spot density 6 @ 3s";
    internal const string PlugInDescription =
        "Populate hunt-map monster spots with Quantity=6 and RespawnDelay=3s; curated spots on main cities/fields; skip towns/events/bosses.";

    /// <summary>Mobs per farm spot (within required 5–7).</summary>
    private const short FarmQuantity = 6;

    /// <summary>Boss / special mobs typically use longer delays; leave those alone.</summary>
    private static readonly TimeSpan MaxFarmRespawn = TimeSpan.FromSeconds(60);

    private static readonly TimeSpan FarmRespawn = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Event, siege, arena, and safe-zone maps that must not receive farm density changes.
    /// </summary>
    internal static readonly HashSet<short> ExcludedMaps =
    [
        5,  // Exile
        6,  // Arena
        9,  // Devil Square 1–4
        11, 12, 13, 14, 15, 16, 17, 52, // Blood Castle
        18, 19, 20, 21, 22, 23, 53, // Chaos Castle
        30, // Valley of Loren (siege)
        32, // Devil Square 5–7
        39, // Kanturu Event
        40, // Silent Map
        45, 46, 47, 48, 49, 50, // Illusion Temple
        58, // Raklion Boss
        62, // Santa Village
        64, // Duel Arena
        65, 66, 67, 68, // Doppelgänger
        69, 70, 71, 72, // Imperial Guardian instances
        79, // Loren Market (safe)
    ];

    /// <summary>
    /// Maps that use curated MuAyra-style point spots instead of dense area packs / 1-mob grids.
    /// </summary>
    internal static readonly HashSet<short> CuratedSpotMaps = [0, 1, 2, 3, 4, 7, 8, 51];

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FarmSpotDensity;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 24, 15, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        ApplyFarmDensity(context, gameConfiguration);
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Applies farm density (Quantity 6, 3s respawn). Shared so a later fix update can re-run
    /// when this version was marked installed without persisting side effects.
    /// </summary>
    internal static void ApplyFarmDensity(IContext context, GameConfiguration gameConfiguration)
    {
        var monstersByNumber = gameConfiguration.Monsters.ToDictionary(m => m.Number);
        var farmMonsters = new HashSet<MonsterDefinition>();

        ApplyCuratedSpots(context, gameConfiguration, monstersByNumber, farmMonsters);
        ScaleRemainingHuntMaps(gameConfiguration, farmMonsters);

        foreach (var monster in farmMonsters)
        {
            if (monster.RespawnDelay <= MaxFarmRespawn)
            {
                monster.RespawnDelay = FarmRespawn;
            }
        }
    }

    private static void ApplyCuratedSpots(
        IContext context,
        GameConfiguration gameConfiguration,
        IReadOnlyDictionary<short, MonsterDefinition> monstersByNumber,
        ISet<MonsterDefinition> farmMonsters)
    {
        foreach (var mapNumber in CuratedSpotMaps)
        {
            var map = gameConfiguration.Maps.FirstOrDefault(m => m.Number == mapNumber);
            if (map is null)
            {
                continue;
            }

            // Clear automatic hunt packs/points so only curated spots remain (NPCs/guards untouched).
            foreach (var spawn in map.MonsterSpawns.Where(IsAutomaticFarmMonster))
            {
                spawn.Quantity = 0;
            }
        }

        short nextId = 1000;
        short lastMap = -1;
        foreach (var (mapNumber, monsterNumber, x, y) in AddCityFiveMobSpotsUpdatePlugIn.Spots)
        {
            if (mapNumber != lastMap)
            {
                nextId = 1000;
                lastMap = mapNumber;
            }

            var map = gameConfiguration.Maps.FirstOrDefault(m => m.Number == mapNumber);
            if (map is null || !monstersByNumber.TryGetValue(monsterNumber, out var monster))
            {
                nextId++;
                continue;
            }

            var existing = map.MonsterSpawns.FirstOrDefault(s =>
                s.SpawnTrigger == SpawnTrigger.Automatic
                && s.MonsterDefinition?.Number == monsterNumber
                && s.X1 == x && s.Y1 == y && s.X2 == x && s.Y2 == y);

            if (existing is not null)
            {
                existing.Quantity = FarmQuantity;
                farmMonsters.Add(monster);
                nextId++;
                continue;
            }

            var spawn = context.CreateNew<MonsterSpawnArea>();
            spawn.SetGuid(mapNumber, nextId++);
            spawn.GameMap = map;
            spawn.MonsterDefinition = monster;
            spawn.SpawnTrigger = SpawnTrigger.Automatic;
            spawn.Direction = Direction.Undefined;
            spawn.Quantity = FarmQuantity;
            spawn.X1 = x;
            spawn.X2 = x;
            spawn.Y1 = y;
            spawn.Y2 = y;
            map.MonsterSpawns.Add(spawn);
            farmMonsters.Add(monster);
        }
    }

    private static void ScaleRemainingHuntMaps(GameConfiguration gameConfiguration, ISet<MonsterDefinition> farmMonsters)
    {
        foreach (var map in gameConfiguration.Maps)
        {
            if (ExcludedMaps.Contains(map.Number) || CuratedSpotMaps.Contains(map.Number))
            {
                continue;
            }

            foreach (var spawn in map.MonsterSpawns.Where(IsAutomaticFarmMonster))
            {
                if (spawn.Quantity <= 0)
                {
                    continue;
                }

                // Skip boss-like defs (long respawn); do not multiply unique bosses.
                if (spawn.MonsterDefinition!.RespawnDelay > MaxFarmRespawn)
                {
                    continue;
                }

                spawn.Quantity = FarmQuantity;
                farmMonsters.Add(spawn.MonsterDefinition);
            }
        }
    }

    private static bool IsAutomaticFarmMonster(MonsterSpawnArea spawn) =>
        spawn.SpawnTrigger == SpawnTrigger.Automatic
        && spawn.MonsterDefinition is { ObjectKind: NpcObjectKind.Monster };
}
