// <copyright file="PegasusCollectionCatalog.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Collections;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Collection donate catalog matching MuMain Pegasus Collections (6 sets × 5 pieces).
/// </summary>
public static class PegasusCollectionCatalog
{
    public const int SetCount = 6;
    public const int SlotCount = 5;
    public const int MaskDwordCount = 3;
    public const byte RequiredLevel = 11;
    public const byte RequiredOptionLevel = 4;

    public static readonly AttributeDefinition[] MaskAttributes =
    [
        new(new Guid("C011EC70-0001-4A11-9C01-C011EC700001"), "Pegasus Collection Mask 0", "Collection progress bits 0-9"),
        new(new Guid("C011EC70-0002-4A11-9C01-C011EC700002"), "Pegasus Collection Mask 1", "Collection progress bits 10-19"),
        new(new Guid("C011EC70-0003-4A11-9C01-C011EC700003"), "Pegasus Collection Mask 2", "Collection progress bits 20-29"),
    ];

    /// <summary>Permanent Max HP granted by completed sets (sum).</summary>
    public static readonly AttributeDefinition BonusHpAttribute =
        new(new Guid("C011EC70-0004-4A11-9C01-C011EC700004"), "Pegasus Collection Bonus HP", "Flat Max HP from completed collections.");

    /// <summary>Bitmask of sets that already received completion rewards.</summary>
    public static readonly AttributeDefinition RewardClaimedAttribute =
        new(new Guid("C011EC70-0005-4A11-9C01-C011EC700005"), "Pegasus Collection Rewards Claimed", "Bits for sets that already paid rewards.");

    /// <summary>Reward jewels: Soul then Bless (group 14).</summary>
    public static readonly ItemIdentifier[] RewardJewels =
    [
        ItemConstants.JewelOfSoul,
        ItemConstants.JewelOfBless,
    ];

    /// <summary>
    /// Piece definitions: [set][slot] = (group, number). Slot order: Helm, Armor, Pants, Gloves, Boots.
    /// </summary>
    public static readonly (byte Group, short Number)[,] Pieces =
    {
        // Bronze
        { (7, 0), (8, 0), (9, 0), (10, 0), (11, 0) },
        // Dragon
        { (7, 1), (8, 1), (9, 1), (10, 1), (11, 1) },
        // Pad
        { (7, 2), (8, 2), (9, 2), (10, 2), (11, 2) },
        // Legendary
        { (7, 3), (8, 3), (9, 3), (10, 3), (11, 3) },
        // Dark Phoenix
        { (7, 9), (8, 9), (9, 9), (10, 9), (11, 9) },
        // Guardian (Dark Lord)
        { (7, 10), (8, 10), (9, 10), (10, 10), (11, 10) },
    };

    public const int RewardHp = 1000;
    public const int RewardCoins = 5000;

    public static readonly AttributeDefinition RequiredExcellentStat = Stats.DamageReflection;
}
