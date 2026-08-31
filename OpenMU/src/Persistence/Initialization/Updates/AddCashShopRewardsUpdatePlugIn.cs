// <copyright file="AddCashShopRewardsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Defines what the reward items of the item shop (lucky tickets, chaos cards, keys, rare item tickets
/// and boxes) hand out. The rewards are drop groups, so they can be adjusted in the admin panel later.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("8D14C7B2-6A39-4F5E-B0D7-2E9A5C41F836")]
public class AddCashShopRewardsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add cash shop rewards";
    internal const string PlugInDescription = "Defines the random rewards of the lucky tickets, chaos cards, keys, rare item tickets and boxes of the item shop.";

    /// <summary>Armor groups of the equipment slots which the lucky tickets can reward.</summary>
    private const byte HelmGroup = 7;
    private const byte ArmorGroup = 8;
    private const byte PantsGroup = 9;
    private const byte GlovesGroup = 10;
    private const byte BootsGroup = 11;

    private static readonly byte[] WeaponAndShieldGroups = [0, 1, 2, 3, 4, 5, 6];

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddCashShopRewards;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 13, 16, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        // Lucky tickets: the first ones give a normal piece of equipment, the second ones an excellent one.
        AddLuckyTicket(context, gameConfiguration, 135, ArmorGroup, false);
        AddLuckyTicket(context, gameConfiguration, 136, PantsGroup, false);
        AddLuckyTicket(context, gameConfiguration, 137, HelmGroup, false);
        AddLuckyTicket(context, gameConfiguration, 138, GlovesGroup, false);
        AddLuckyTicket(context, gameConfiguration, 139, BootsGroup, false);
        AddLuckyTicket(context, gameConfiguration, 140, ArmorGroup, true);
        AddLuckyTicket(context, gameConfiguration, 141, PantsGroup, true);
        AddLuckyTicket(context, gameConfiguration, 142, HelmGroup, true);
        AddLuckyTicket(context, gameConfiguration, 143, GlovesGroup, true);
        AddLuckyTicket(context, gameConfiguration, 144, BootsGroup, true);

        var highEquipment = Pool(gameConfiguration, [.. WeaponAndShieldGroups, HelmGroup, ArmorGroup, PantsGroup, GlovesGroup, BootsGroup], 100, 255);
        var midEquipment = Pool(gameConfiguration, [.. WeaponAndShieldGroups, HelmGroup, ArmorGroup, PantsGroup, GlovesGroup, BootsGroup], 60, 130);
        var jewels = Jewels(gameConfiguration);

        AddReward(context, gameConfiguration, 14, 146, "Rare Item Ticket 8", SpecialItemType.Excellent, highEquipment, 7, 9);
        AddReward(context, gameConfiguration, 14, 149, "Rare Item Ticket 11", SpecialItemType.Ancient, [], 0, 0);
        AddReward(context, gameConfiguration, 14, 137, "Package Box D", SpecialItemType.RandomItem, jewels, 0, 0);
        AddReward(context, gameConfiguration, 14, 92, "Chaos Card Gold", SpecialItemType.Excellent, highEquipment, 9, 9);
        AddReward(context, gameConfiguration, 14, 95, "Chaos Card Mini", SpecialItemType.RandomItem, midEquipment, 3, 5);
        AddReward(context, gameConfiguration, 14, 112, "Silver Key", SpecialItemType.RandomItem, jewels, 0, 0);
        AddReward(context, gameConfiguration, 14, 113, "Gold Key", SpecialItemType.Excellent, highEquipment, 7, 9);

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }

    private static void AddLuckyTicket(IContext context, GameConfiguration gameConfiguration, short number, byte itemGroup, bool isSecondTicket)
    {
        var pool = isSecondTicket
            ? Pool(gameConfiguration, [itemGroup], 100, 255)
            : Pool(gameConfiguration, [itemGroup], 50, 140);

        AddReward(
            context,
            gameConfiguration,
            13,
            number,
            isSecondTicket ? "2nd Lucky Ticket" : "1st Lucky Ticket",
            isSecondTicket ? SpecialItemType.Excellent : SpecialItemType.RandomItem,
            pool,
            isSecondTicket ? (byte)7 : (byte)4,
            isSecondTicket ? (byte)9 : (byte)6);
    }

    private static void AddReward(
        IContext context,
        GameConfiguration gameConfiguration,
        byte group,
        short number,
        string description,
        SpecialItemType itemType,
        IReadOnlyCollection<ItemDefinition> possibleItems,
        byte minimumLevel,
        byte maximumLevel)
    {
        if (gameConfiguration.Items.FirstOrDefault(item => item.Group == group && item.Number == number) is not { } definition
            || definition.DropItems.Count > 0)
        {
            return;
        }

        if (possibleItems.Count == 0 && itemType != SpecialItemType.Ancient)
        {
            return;
        }

        var dropGroup = context.CreateNew<ItemDropItemGroup>();
        dropGroup.Description = description;
        dropGroup.Chance = 1.0;
        dropGroup.ItemType = itemType;
        dropGroup.SourceItemLevel = 0;
        dropGroup.MinimumLevel = minimumLevel;
        dropGroup.MaximumLevel = maximumLevel;
        foreach (var item in possibleItems)
        {
            dropGroup.PossibleItems.Add(item);
        }

        definition.DropItems.Add(dropGroup);
    }

    private static List<ItemDefinition> Pool(GameConfiguration gameConfiguration, byte[] groups, byte minimumDropLevel, byte maximumDropLevel)
    {
        return gameConfiguration.Items
            .Where(item => groups.Contains(item.Group)
                           && item.ItemSlot is not null
                           && item.DropsFromMonsters
                           && item.DropLevel >= minimumDropLevel
                           && item.DropLevel <= maximumDropLevel)
            .ToList();
    }

    private static List<ItemDefinition> Jewels(GameConfiguration gameConfiguration)
    {
        string[] names = ["Jewel of Bless", "Jewel of Soul", "Jewel of Chaos", "Jewel of Life", "Jewel of Creation"];
        return gameConfiguration.Items.Where(item => names.Contains(item.Name)).ToList();
    }
}
