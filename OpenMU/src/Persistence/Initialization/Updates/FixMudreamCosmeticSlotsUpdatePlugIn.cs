// <copyright file="FixMudreamCosmeticSlotsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Fixes Mudream cosmetic ItemSlot + Width/Height.
/// First import mapped item <c>Group</c> directly to equipment slot (helm→wings, bow→pants, etc.).
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("B5F9D3E2-0C4A-5F8B-9A7D-2E3F4B5C6D7E")]
public class FixMudreamCosmeticSlotsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Fix Mudream cosmetic slots";
    internal const string PlugInDescription = "Corrects equipment slot and inventory size for Mudream cosmetic items.";

    private const byte NoSlot = 255;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixMudreamCosmeticSlots;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 25, 12, 30, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var item in MudreamCosmeticItemCatalog.Items)
        {
            var definition = gameConfiguration.Items.FirstOrDefault(i => i.Group == item.Group && i.Number == item.Number);
            if (definition is null)
            {
                continue;
            }

            definition.Name = item.Name;
            definition.Width = item.Width;
            definition.Height = item.Height;
            definition.Durability = 255;
            definition.MaximumItemLevel = 0;
            definition.DropsFromMonsters = false;
            definition.Value = 0;
            definition.MaximumSockets = 0;

            if (item.Slot == NoSlot)
            {
                definition.ItemSlot = null;
            }
            else
            {
                var slotType = gameConfiguration.ItemSlotTypes.FirstOrDefault(t => t.ItemSlots.Contains(item.Slot));
                if (slotType is not null)
                {
                    definition.ItemSlot = slotType;
                }
            }
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
