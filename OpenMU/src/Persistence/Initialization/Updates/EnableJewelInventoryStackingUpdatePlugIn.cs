// <copyright file="EnableJewelInventoryStackingUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.JewelBank;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Enables flexible inventory stacking for bank jewels (durability = stack count, max 255).
/// Prefer this over classic Lahap packed jewels (10/20/30 only).
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("E6F7A8B9-9203-4B4C-2D3E-4F5061728394")]
public class EnableJewelInventoryStackingUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Enable jewel inventory stacking";
    internal const string PlugInDescription = "Sets jewel ItemDefinition.Durability to 255 so jewels auto-stack on pickup.";

    /// <summary>Maximum stack size written into ItemDefinition.Durability.</summary>
    public const byte MaxJewelStack = 255;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.EnableJewelInventoryStacking;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 11, 22, 50, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var id in JewelBankCatalog.Items)
        {
            var def = gameConfiguration.Items.FirstOrDefault(i => i.Group == id.Group && i.Number == id.Number);
            if (def is null)
            {
                continue;
            }

            def.Durability = MaxJewelStack;
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
