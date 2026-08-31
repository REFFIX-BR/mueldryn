// <copyright file="AddMudreamCosmeticItemsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Registers Mudream transmog / visual cosmetic items (Tooltip index ≥300, tooltip text 966).
/// Stats stay at zero — appearance only, for Inventory Visual / future transmog UI.
/// Catalog is generated from Mudream.online/Data/Local/ItemTooltip/Tooltip.xml.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("A4E8C2F1-9B3D-4E7A-8F6C-1D2E3A4B5C6D")]
public class AddMudreamCosmeticItemsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add Mudream cosmetic items";
    internal const string PlugInDescription = "Adds Mudream visual/skin item definitions (zero combat stats) for transmog and Inventory Visual.";

    private const byte NoSlot = 255;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddMudreamCosmeticItems;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 24, 20, 30, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var item in MudreamCosmeticItemCatalog.Items)
        {
            var existing = gameConfiguration.Items.FirstOrDefault(i => i.Group == item.Group && i.Number == item.Number);
            if (existing is not null)
            {
                existing.Name = item.Name;
                existing.Width = item.Width;
                existing.Height = item.Height;
                existing.Durability = 255;
                existing.MaximumItemLevel = 0;
                existing.DropsFromMonsters = false;
                existing.DropLevel = 0;
                existing.Value = 0;
                existing.MaximumSockets = 0;
                if (item.Slot == NoSlot)
                {
                    existing.ItemSlot = null;
                }
                else
                {
                    var slotType = gameConfiguration.ItemSlotTypes.FirstOrDefault(t => t.ItemSlots.Contains(item.Slot));
                    if (slotType is not null)
                    {
                        existing.ItemSlot = slotType;
                    }
                }

                continue;
            }

            var definition = context.CreateNew<ItemDefinition>();
            gameConfiguration.Items.Add(definition);
            definition.Group = item.Group;
            definition.Number = item.Number;
            definition.Name = item.Name;
            definition.Width = item.Width;
            definition.Height = item.Height;
            definition.Durability = 255;
            definition.MaximumItemLevel = 0;
            definition.DropsFromMonsters = false;
            definition.DropLevel = 0;
            definition.Value = 0;
            definition.MaximumSockets = 0;
            definition.SetGuid(definition.Group, definition.Number);

            if (item.Slot != NoSlot)
            {
                var slotType = gameConfiguration.ItemSlotTypes.FirstOrDefault(t => t.ItemSlots.Contains(item.Slot));
                if (slotType is not null)
                {
                    definition.ItemSlot = slotType;
                }
            }

            MudreamCosmeticClassRules.ApplyQualifiedCharacters(definition, gameConfiguration, item.Group, item.Name);
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
