// <copyright file="DungeonKeyItems.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Dungeons;

/// <summary>
/// Item identifiers and chaos-mix numbers for dungeon tickets and keys.
/// </summary>
public static class DungeonKeyItems
{
    /// <summary>
    /// Shared item group (misc / potion group).
    /// </summary>
    public const byte Group = 14;

    /// <summary>
    /// Ticket used as the Normal mix material.
    /// </summary>
    public const short TicketNumber = 110;

    /// <summary>
    /// Silver Key — Normal dungeon (official cash-shop item).
    /// </summary>
    public const short NormalKeyNumber = 112;

    /// <summary>
    /// Red Key — Hard dungeon.
    /// </summary>
    public const short HardKeyNumber = 210;

    /// <summary>
    /// Purple Key — Hell dungeon.
    /// </summary>
    public const short HellKeyNumber = 211;

    /// <summary>
    /// Item level of crafted and shop dungeon keys.
    /// </summary>
    public const byte KeyLevel = 9;

    /// <summary>
    /// Daily dungeon entries allowed per character.
    /// </summary>
    public const int MaxDailyEntries = 2;

    /// <summary>
    /// Gets the item number of the key required for a difficulty.
    /// </summary>
    public static short GetRequiredKeyNumber(DungeonDifficulty difficulty) => difficulty switch
    {
        DungeonDifficulty.Hard => HardKeyNumber,
        DungeonDifficulty.Hell => HellKeyNumber,
        _ => NormalKeyNumber,
    };

    /// <summary>
    /// Gets the display name of the key required for a difficulty.
    /// </summary>
    public static string GetRequiredKeyName(DungeonDifficulty difficulty) => difficulty switch
    {
        DungeonDifficulty.Hard => HardKeyName,
        DungeonDifficulty.Hell => HellKeyName,
        _ => NormalKeyName,
    };

    /// <summary>
    /// Display name of the dungeon ticket.
    /// </summary>
    public const string TicketName = "Ticket da Dungeon";

    /// <summary>
    /// Display name of the Normal key.
    /// </summary>
    public const string NormalKeyName = "Silver Key";

    /// <summary>
    /// Display name of the Hard key.
    /// </summary>
    public const string HardKeyName = "Red Key";

    /// <summary>
    /// Display name of the Hell key.
    /// </summary>
    public const string HellKeyName = "Purple Key";

    /// <summary>
    /// Chaos mix number for the Normal key recipe.
    /// </summary>
    public const byte NormalMixNumber = 80;

    /// <summary>
    /// Chaos mix number for the Hard key recipe.
    /// </summary>
    public const byte HardMixNumber = 81;

    /// <summary>
    /// Chaos mix number for the Hell key recipe.
    /// </summary>
    public const byte HellMixNumber = 82;

    /// <summary>
    /// Zen cost of the Normal mix.
    /// </summary>
    public const int NormalZen = 5_000_000;

    /// <summary>
    /// Zen cost of the Hard mix.
    /// </summary>
    public const int HardZen = 10_000_000;

    /// <summary>
    /// Zen cost of the Hell mix.
    /// </summary>
    public const int HellZen = 15_000_000;
}
