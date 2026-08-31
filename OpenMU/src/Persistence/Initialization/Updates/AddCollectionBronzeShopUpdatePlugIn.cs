// <copyright file="AddCollectionBronzeShopUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds Bronze collection test set (+11 / Luck / Option+16 / Exc Reflect) to merchant shops
/// used with MuMain Collections UI testing.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("a1b2c3d4-e5f6-4789-a012-3456789abcde")]
public class AddCollectionBronzeShopUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add collection Bronze shop items";
    internal const string PlugInDescription = "Puts Bronze +11 Luck Opt4 Exc Reflect into Hanzo, Wandering Merchant Martin and Potion Girl Amy shops for Collections testing.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddCollectionBronzeShop;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 20, 15, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var helper = new ItemHelper(context, gameConfiguration);

        AddBronzeSet(helper, gameConfiguration, 251, [80, 82, 96, 98, 112]); // Hanzo
        AddBronzeSet(helper, gameConfiguration, 248, [8, 24, 42, 58, 74]);   // Martin
        AddBronzeSet(helper, gameConfiguration, 253, [40, 42, 56, 58, 72]);  // Amy

        return default;
    }

    private static void AddBronzeSet(ItemHelper helper, GameConfiguration gameConfiguration, short npcNumber, byte[] slots)
    {
        var npc = gameConfiguration.Monsters.FirstOrDefault(m => m.Number == npcNumber);
        if (npc?.MerchantStore is null || slots.Length < 5)
        {
            return;
        }

        if (npc.MerchantStore.Items.Any(i =>
                i.Definition is { Group: (byte)ItemGroups.Helm, Number: 0 }
                && i.Level >= 11
                && i.ItemOptions.Any(o => o.ItemOption?.OptionType == ItemOptionTypes.Excellent)))
        {
            return;
        }

        npc.MerchantStore.Items.Add(helper.CreateSetItem(slots[0], 0, ItemGroups.Helm, Stats.DamageReflection, 11, 4, true));
        npc.MerchantStore.Items.Add(helper.CreateSetItem(slots[1], 0, ItemGroups.Armor, Stats.DamageReflection, 11, 4, true));
        npc.MerchantStore.Items.Add(helper.CreateSetItem(slots[2], 0, ItemGroups.Pants, Stats.DamageReflection, 11, 4, true));
        npc.MerchantStore.Items.Add(helper.CreateSetItem(slots[3], 0, ItemGroups.Gloves, Stats.DamageReflection, 11, 4, true));
        npc.MerchantStore.Items.Add(helper.CreateSetItem(slots[4], 0, ItemGroups.Boots, Stats.DamageReflection, 11, 4, true));
    }
}
