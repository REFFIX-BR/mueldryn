// <copyright file="DungeonRewards.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Dungeons;

/// <summary>
/// Shared dungeon reward tables (Normal launch loot).
/// </summary>
public static class DungeonRewards
{
    /// <summary>
    /// Box of Kundun item group.
    /// </summary>
    public const byte KundunGroup = 14;

    /// <summary>
    /// Box of Kundun item number.
    /// </summary>
    public const short KundunNumber = 11;

    /// <summary>
    /// Item level of Box of Kundun +3.
    /// </summary>
    public const byte KundunPlus3Level = 10;

    /// <summary>
    /// Guaranteed Kundun +3 boxes for Normal clears.
    /// </summary>
    public const int NormalKundunCount = 2;

    /// <summary>
    /// Chance of an extra T1 ancient piece on Normal.
    /// </summary>
    public const double NormalAncientChance = 0.25;

    /// <summary>
    /// Ancient set names treated as T1 (Leather, Bronze, Scale, Pad, Bone, Vine, Silk).
    /// </summary>
    public static readonly HashSet<string> Tier1AncientSetNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Warrior",
        "Anonymous",
        "Hyperion",
        "Mist",
        "Eplete",
        "Berserker",
        "Apollo",
        "Barnake",
        "Evis",
        "Sylion",
        "Ceto",
        "Drake",
        "Gaia",
        "Fase",
    };

    /// <summary>
    /// Returns whether <paramref name="setName"/> is a T1 ancient set.
    /// </summary>
    public static bool IsTier1AncientSet(string? setName) =>
        setName is not null && Tier1AncientSetNames.Contains(setName);
}
