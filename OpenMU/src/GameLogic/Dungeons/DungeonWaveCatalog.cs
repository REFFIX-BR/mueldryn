// <copyright file="DungeonWaveCatalog.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Dungeons;

/// <summary>
/// Arena spawn and sequential wave layout for the Fortress dungeon.
/// </summary>
public static class DungeonWaveCatalog
{
    /// <summary>
    /// Player spawn and fight arena (Gayion hall).
    /// </summary>
    public const byte ArenaX = 179;

    /// <summary>
    /// Player spawn and fight arena (Gayion hall).
    /// </summary>
    public const byte ArenaY = 83;

    /// <summary>
    /// Final Gaia boss (Gayion The Gladiator).
    /// </summary>
    public const short GaiaMonsterNumber = 504;

    /// <summary>
    /// Wave 5 tank boss (Jerry). Distinct from Gaia on wave 10.
    /// </summary>
    public const short Wave5BossMonsterNumber = 505;

    /// <summary>
    /// Total number of waves, including Gaia.
    /// </summary>
    public const int WaveCount = 10;

    /// <summary>
    /// Seconds to wait after a wave is cleared before the next one spawns.
    /// </summary>
    public const int IntermissionSeconds = 20;

    /// <summary>
    /// Seconds players stay in the dungeon after Gaia dies, to collect rewards.
    /// </summary>
    public const int LootWindowSeconds = 20;

    /// <summary>
    /// Inner fight box used for wave spawns (keeps mobs off walls and gates).
    /// </summary>
    public const byte SpawnMinX = 175;

    /// <summary>Inner fight box used for wave spawns.</summary>
    public const byte SpawnMaxX = 184;

    /// <summary>Inner fight box used for wave spawns.</summary>
    public const byte SpawnMinY = 81;

    /// <summary>Inner fight box used for wave spawns.</summary>
    public const byte SpawnMaxY = 90;

    /// <summary>
    /// Walkable corridor + arena so players can reach the hall around (179, 72–86).
    /// </summary>
    public const byte WalkMinX = 168;

    /// <summary>Walkable corridor + arena.</summary>
    public const byte WalkMaxX = 192;

    /// <summary>Walkable corridor + arena.</summary>
    public const byte WalkMinY = 68;

    /// <summary>Walkable corridor + arena.</summary>
    public const byte WalkMaxY = 98;

    /// <summary>
    /// Gets the wave layouts. Wave 5 is a tank boss, wave 10 is Gaia.
    /// </summary>
    public static IReadOnlyList<DungeonWaveLayout> Waves { get; } =
    [
        new(1, [518, 519], 6, 1f, 1f, false),
        new(2, [518, 519, 515], 8, 1f, 1f, false),
        new(3, [515, 516, 517], 10, 1.1f, 1.1f, false),
        new(4, [516, 517, 520], 12, 1.2f, 1.15f, false),
        new(5, [Wave5BossMonsterNumber], 1, 8f, 1.5f, true),
        new(6, [520, 512], 10, 1.3f, 1.2f, false),
        new(7, [512, 513, 521], 12, 1.4f, 1.25f, false),
        new(8, [513, 521, 509], 14, 1.5f, 1.3f, false),
        new(9, [509, 510, 512], 14, 1.6f, 1.35f, false),
        new(10, [GaiaMonsterNumber], 1, 3f, 1.8f, true),
    ];
}

/// <summary>
/// Layout of a single dungeon wave.
/// </summary>
/// <param name="Number">Wave number, 1-based.</param>
/// <param name="MonsterNumbers">Monster definition numbers to pick from.</param>
/// <param name="Count">How many monsters to spawn.</param>
/// <param name="ExtraHpMultiplier">Extra HP applied after party/difficulty scaling.</param>
/// <param name="ExtraDamageMultiplier">Extra damage applied after party/difficulty scaling.</param>
/// <param name="IsBoss">Whether this wave is a named boss fight.</param>
public readonly record struct DungeonWaveLayout(
    int Number,
    short[] MonsterNumbers,
    int Count,
    float ExtraHpMultiplier,
    float ExtraDamageMultiplier,
    bool IsBoss);
