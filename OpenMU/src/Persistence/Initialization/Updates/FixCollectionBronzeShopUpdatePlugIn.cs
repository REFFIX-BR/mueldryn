// <copyright file="FixCollectionBronzeShopUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Fixes collection Bronze shop items: Epic Reflect rarity and 1 zen StorePrice.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("b2c3d4e5-f6a7-4890-b123-456789abcdef")]
public class FixCollectionBronzeShopUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Fix collection Bronze shop rarity and price";
    internal const string PlugInDescription = "Sets Bronze collection shop items to Epic Reflect and StorePrice = 1 zen.";

    private static readonly (short Npc, byte[] Slots)[] Targets =
    [
        (251, [80, 82, 96, 98, 112]), // Hanzo
        (248, [8, 24, 42, 58, 74]),   // Martin
        (253, [40, 42, 56, 58, 72]),  // Amy
    ];

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixCollectionBronzeShop;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 20, 16, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var helper = new ItemHelper(context, gameConfiguration);
        foreach (var (npcNumber, slots) in Targets)
        {
            var npc = gameConfiguration.Monsters.FirstOrDefault(m => m.Number == npcNumber);
            if (npc?.MerchantStore is null)
            {
                continue;
            }

            // Remove previous collection bronze pieces in those slots.
            foreach (var old in npc.MerchantStore.Items
                         .Where(i => slots.Contains(i.ItemSlot)
                                     && i.Definition is { Number: 0, Group: >= 7 and <= 11 }
                                     && i.Level >= 11)
                         .ToList())
            {
                npc.MerchantStore.Items.Remove(old);
            }

            npc.MerchantStore.Items.Add(MakePiece(helper, slots[0], ItemGroups.Helm));
            npc.MerchantStore.Items.Add(MakePiece(helper, slots[1], ItemGroups.Armor));
            npc.MerchantStore.Items.Add(MakePiece(helper, slots[2], ItemGroups.Pants));
            npc.MerchantStore.Items.Add(MakePiece(helper, slots[3], ItemGroups.Gloves));
            npc.MerchantStore.Items.Add(MakePiece(helper, slots[4], ItemGroups.Boots));
        }

        return default;
    }

    private static Item MakePiece(ItemHelper helper, byte slot, ItemGroups group)
    {
        var item = helper.CreateSetItem(slot, 0, group, Stats.DamageReflection, 11, 4, true);
        foreach (var link in item.ItemOptions.Where(o => o.ItemOption?.OptionType == ItemOptionTypes.Excellent))
        {
            link.Level = 3; // Epic -> Reflect +5% on MuMain
        }

        item.StorePrice = 1;
        return item;
    }
}
