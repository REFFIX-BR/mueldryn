// <copyright file="FixMudreamCosmeticBowSlotUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Moves Mudream cosmetic bows and crossbows to the left hand.
/// The catalog import treated every weapon group as a right-hand item, but vanilla bows
/// occupy the left hand (the client only accepts arrows in the right hand while a bow is
/// equipped), so the cosmetic skins rendered in the wrong hand.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("C7A1E4D9-3B62-4F08-9E15-8D2C6A0B4F31")]
public class FixMudreamCosmeticBowSlotUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Fix Mudream cosmetic bow slot";
    internal const string PlugInDescription = "Moves Mudream cosmetic bows and crossbows to the left hand equipment slot.";

    private const byte BowGroup = 4;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixMudreamCosmeticBowSlot;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 31, 17, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var slotType = gameConfiguration.ItemSlotTypes.FirstOrDefault(t => t.ItemSlots.Contains(1));
        if (slotType is null)
        {
            return;
        }

        foreach (var item in MudreamCosmeticItemCatalog.Items.Where(i => i.Group == BowGroup))
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
