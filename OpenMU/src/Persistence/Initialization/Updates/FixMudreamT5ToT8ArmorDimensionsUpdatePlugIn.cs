// <copyright file="FixMudreamT5ToT8ArmorDimensionsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Re-applies Mudream T5–T8 armor Width/Height/ItemSlot/Durability on existing DBs
/// (stubs reconciled without dimensions blocked CheckInvSpace / equip).
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("E0B5C3D4-6F70-8192-ACB3-D4E5F6071829")]
public class FixMudreamT5ToT8ArmorDimensionsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Fix Mudream T5-T8 Armor Dimensions";
    internal const string PlugInDescription =
        "Updates existing T5–T8 armor definitions: Width, Height, ItemSlot, and Durability for ground pickup.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixMudreamT5ToT8ArmorDimensions;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 10, 19, 10, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        new MudreamT5ToT8ArmorsAndSets(context, gameConfiguration).Initialize();
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
