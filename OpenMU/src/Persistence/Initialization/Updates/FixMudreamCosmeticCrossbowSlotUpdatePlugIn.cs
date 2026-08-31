// <copyright file="FixMudreamCosmeticCrossbowSlotUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Moves Mudream cosmetic crossbows back to the weapon hand.
/// <see cref="FixMudreamCosmeticBowSlotUpdatePlugIn"/> moved all of group 4 to the shield
/// slot, but only bows are held that way; crossbows are carried like a sword.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("5F0B7C42-9D18-4A6E-B3C7-1E84A9F2D065")]
public class FixMudreamCosmeticCrossbowSlotUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Fix Mudream cosmetic crossbow slot";
    internal const string PlugInDescription = "Moves Mudream cosmetic crossbows to the weapon hand equipment slot.";

    private const byte BowGroup = 4;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixMudreamCosmeticCrossbowSlot;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 31, 18, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        // Crossbows are two cells wide, so they take the right-hand-only type rather than
        // the one-handed "either hand" type vanilla gives to narrow knight weapons.
        var slotType = gameConfiguration.ItemSlotTypes.FirstOrDefault(t => t.ItemSlots.Contains(0) && !t.ItemSlots.Contains(1))
            ?? gameConfiguration.ItemSlotTypes.FirstOrDefault(t => t.ItemSlots.Contains(0));
        if (slotType is null)
        {
            return;
        }

        foreach (var item in MudreamCosmeticItemCatalog.Items
                     .Where(i => i.Group == BowGroup && i.Name.Contains("Crossbow", StringComparison.OrdinalIgnoreCase)))
        {
            var definition = gameConfiguration.Items.FirstOrDefault(i => i.Group == item.Group && i.Number == item.Number);
            if (definition is not null)
            {
                definition.ItemSlot = slotType;
            }
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
