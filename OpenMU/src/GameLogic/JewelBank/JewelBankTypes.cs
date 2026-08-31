// <copyright file="JewelBankTypes.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.JewelBank;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Classic jewel bank slot identifiers (matches MuAyra CustomJewelBank order for UI).
/// </summary>
public enum JewelBankSlot : byte
{
    Chaos = 0,
    Bless = 1,
    Soul = 2,
    Life = 3,
    Creation = 4,
    Guardian = 5,
    Gemstone = 6,
    Harmony = 7,
    LowStone = 8,
    HighStone = 9,
}

/// <summary>
/// Defines how the withdrawn jewels are delivered into the inventory.
/// </summary>
public enum JewelBankWithdrawMode : byte
{
    /// <summary>
    /// One item per jewel, each in its own inventory slot.
    /// </summary>
    Units = 0,

    /// <summary>
    /// One packed jewel (10, 20 or 30 pieces), like the ones mixed by Lahap.
    /// </summary>
    Pack = 1,

    /// <summary>
    /// As few inventory stacks as possible, filling the stacks the player already has.
    /// </summary>
    Stack = 2,
}

/// <summary>
/// Snapshot of jewel bank counters for the client.
/// </summary>
public sealed class JewelBankStatus
{
    /// <summary>
    /// Counts indexed by <see cref="JewelBankSlot"/>.
    /// </summary>
    public required int[] Counts { get; init; }
}

/// <summary>
/// Result of a deposit / withdraw attempt.
/// </summary>
public enum JewelBankResult : byte
{
    Success = 0,
    Failed = 1,
    InvalidItem = 2,
    InventoryFull = 3,
    NotEnough = 4,
}

/// <summary>
/// Maps slots to item ids and attribute definitions.
/// </summary>
public static class JewelBankCatalog
{
    /// <summary>
    /// Number of supported jewel types.
    /// </summary>
    public const int SlotCount = 10;

    /// <summary>
    /// Display names for the client UI.
    /// </summary>
    public static readonly string[] DisplayNames =
    [
        "Jewel of Chaos",
        "Jewel of Bless",
        "Jewel of Soul",
        "Jewel of Life",
        "Jewel of Creation",
        "Jewel of Guardian",
        "Gemstone",
        "Jewel of Harmony",
        "Lower refining stone",
        "Higher refining stone",
    ];

    /// <summary>
    /// Item identifiers per slot.
    /// </summary>
    public static readonly ItemIdentifier[] Items =
    [
        ItemConstants.JewelOfChaos,
        ItemConstants.JewelOfBless,
        ItemConstants.JewelOfSoul,
        ItemConstants.JewelOfLife,
        ItemConstants.JewelOfCreation,
        ItemConstants.JewelOfGuardian,
        ItemConstants.Gemstone,
        ItemConstants.JewelOfHarmony,
        ItemConstants.LowerRefineStone,
        ItemConstants.HigherRefineStone,
    ];

    /// <summary>
    /// Packed jewel identifiers per slot (item group 12, the ones Lahap mixes).
    /// The piece count is stored in <see cref="Item.Level"/>: 0 = 10, 1 = 20, 2 = 30.
    /// </summary>
    public static readonly ItemIdentifier[] PackedItems =
    [
        new(141, 12), // Jewel of Chaos
        new(30, 12),  // Jewel of Bless
        new(31, 12),  // Jewel of Soul
        new(136, 12), // Jewel of Life
        new(137, 12), // Jewel of Creation
        new(138, 12), // Jewel of Guardian
        new(139, 12), // Gemstone
        new(140, 12), // Jewel of Harmony
        new(142, 12), // Lower refining stone
        new(143, 12), // Higher refining stone
    ];

    /// <summary>
    /// Account attribute definitions per slot (shared across all characters).
    /// </summary>
    public static readonly AttributeDefinition[] Attributes =
    [
        Stats.JewelBankChaos,
        Stats.JewelBankBless,
        Stats.JewelBankSoul,
        Stats.JewelBankLife,
        Stats.JewelBankCreation,
        Stats.JewelBankGuardian,
        Stats.JewelBankGemstone,
        Stats.JewelBankHarmony,
        Stats.JewelBankLowStone,
        Stats.JewelBankHighStone,
    ];

    /// <summary>
    /// The piece counts a packed jewel can hold.
    /// </summary>
    public static readonly int[] PackSizes = [10, 20, 30];

    /// <summary>
    /// Tries to resolve a bank slot for an inventory item.
    /// </summary>
    public static bool TryGetSlot(Item item, out JewelBankSlot slot)
    {
        for (byte i = 0; i < Items.Length; i++)
        {
            var id = Items[i];
            if (item.Definition?.Group == id.Group && item.Definition.Number == id.Number)
            {
                slot = (JewelBankSlot)i;
                return true;
            }
        }

        slot = default;
        return false;
    }

    /// <summary>
    /// Tries to resolve the bank slot and the number of jewels an inventory item is worth.
    /// Single jewels count their stack (durability), packed jewels count their pieces.
    /// </summary>
    public static bool TryGetDeposit(Item item, out JewelBankSlot slot, out int units)
    {
        if (TryGetSlot(item, out slot))
        {
            units = Math.Max(1, (int)item.Durability);
            return true;
        }

        for (byte i = 0; i < PackedItems.Length; i++)
        {
            var id = PackedItems[i];
            if (item.Definition?.Group == id.Group && item.Definition.Number == id.Number)
            {
                slot = (JewelBankSlot)i;
                units = GetPackSize(item.Level);
                return true;
            }
        }

        slot = default;
        units = 0;
        return false;
    }

    /// <summary>
    /// Gets the piece count of a packed jewel by its item level.
    /// </summary>
    public static int GetPackSize(byte itemLevel) => (Math.Min((int)itemLevel, PackSizes.Length - 1) + 1) * 10;

    /// <summary>
    /// Tries to convert a piece count into the item level of a packed jewel.
    /// </summary>
    public static bool TryGetPackLevel(int size, out byte level)
    {
        var index = Array.IndexOf(PackSizes, size);
        level = (byte)Math.Max(index, 0);
        return index >= 0;
    }
}
