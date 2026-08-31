// <copyright file="KundunBoxDropCatalog.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence.Initialization.Items;

/// <summary>
/// Box of Kundun +1..+5 drop tables: excellent armors and shields only.
/// Rage Fighter and Summoner sets are left out on purpose.
/// </summary>
public static class KundunBoxDropCatalog
{
    private const byte BoxGroup = 14;
    private const short BoxNumber = 11;

    /// <summary>
    /// Replaces the Kundun +1..+5 loot on the Box of Luck item.
    /// </summary>
    public static void Apply(IContext context, GameConfiguration gameConfiguration)
    {
        var box = gameConfiguration.Items.FirstOrDefault(item => item.Group == BoxGroup && item.Number == BoxNumber);
        if (box is null)
        {
            return;
        }

        Apply(context, gameConfiguration, box);
    }

    /// <summary>
    /// Replaces the Kundun +1..+5 loot on an already loaded box definition.
    /// </summary>
    public static void Apply(IContext context, GameConfiguration gameConfiguration, ItemDefinition box)
    {
        ApplyLevel(context, gameConfiguration, box, 8, "Box of Kundun+1",
            armorSets: [2, 4, 5, 0, 6, 10, 11], // Pad, Bone, Leather, Bronze, Scale, Vine, Silk
            shields: [0, 1, 2, 4, 10]); // Small, Horn, Kite, Buckler, Big Round

        ApplyLevel(context, gameConfiguration, box, 9, "Box of Kundun+2",
            armorSets: [7, 8, 9, 12, 13, 25], // Sphinx, Brass, Plate, Wind, Spirit, Light Plate
            shields: [3, 6, 7, 9, 11]); // Elven, Skull, Spiked, Plate, Serpent (Scale)

        ApplyLevel(context, gameConfiguration, box, 10, "Box of Kundun+3",
            armorSets: [3, 1, 14, 15, 26], // Legendary, Dragon, Guardian, Storm Crow, Adamantine
            shields: [5, 8, 14, 13, 16]); // Dragon Slayer, Tower, Legendary, Dragon, Elemental (Nature)

        ApplyLevel(context, gameConfiguration, box, 11, "Box of Kundun+4",
            armorSets: [18, 16, 17, 19, 20, 27], // Grand Soul, Black Dragon, Dark Phoenix, Divine, Thunder Hawk, Dark Steel
            shields: [12, 15, 17, 20]); // Bronze, Grand Soul, Crimson Glory, Guardian

        ApplyLevel(context, gameConfiguration, box, 12, "Box of Kundun+5",
            armorSets: [22, 21, 24, 28, 23], // Dark Soul, Great Dragon, Red Spirit, Dark Master, Hurricane
            shields: [18, 19, 21]); // Salamander, Frost Barrier, Cross
    }

    private static void ApplyLevel(
        IContext context,
        GameConfiguration gameConfiguration,
        ItemDefinition box,
        byte sourceLevel,
        string description,
        short[] armorSets,
        short[] shields)
    {
        var existing = box.DropItems.Where(group => group.SourceItemLevel == sourceLevel).ToList();
        var group = existing.FirstOrDefault(item => item.ItemType == SpecialItemType.Excellent)
                    ?? existing.FirstOrDefault();

        foreach (var extra in existing.Where(item => item != group))
        {
            extra.PossibleItems.Clear();
            box.DropItems.Remove(extra);
        }

        if (group is null)
        {
            group = context.CreateNew<ItemDropItemGroup>();
            box.DropItems.Add(group);
        }

        group.SourceItemLevel = sourceLevel;
        group.ItemType = SpecialItemType.Excellent;
        group.Chance = 1.0;
        group.Description = description;
        group.MinimumLevel = 0;
        group.MaximumLevel = 0;
        group.PossibleItems.Clear();

        foreach (var setNumber in armorSets)
        {
            AddArmorSet(gameConfiguration, group, setNumber);
        }

        foreach (var shieldNumber in shields)
        {
            TryAdd(gameConfiguration, group, (byte)ItemGroups.Shields, shieldNumber);
        }
    }

    private static void AddArmorSet(GameConfiguration gameConfiguration, ItemDropItemGroup dropGroup, short number)
    {
        TryAdd(gameConfiguration, dropGroup, (byte)ItemGroups.Helm, number);
        TryAdd(gameConfiguration, dropGroup, (byte)ItemGroups.Armor, number);
        TryAdd(gameConfiguration, dropGroup, (byte)ItemGroups.Pants, number);
        TryAdd(gameConfiguration, dropGroup, (byte)ItemGroups.Gloves, number);
        TryAdd(gameConfiguration, dropGroup, (byte)ItemGroups.Boots, number);
    }

    private static void TryAdd(GameConfiguration gameConfiguration, ItemDropItemGroup dropGroup, byte group, short number)
    {
        if (gameConfiguration.Items.FirstOrDefault(item => item.Group == group && item.Number == number) is { } item
            && !dropGroup.PossibleItems.Contains(item))
        {
            dropGroup.PossibleItems.Add(item);
        }
    }
}
