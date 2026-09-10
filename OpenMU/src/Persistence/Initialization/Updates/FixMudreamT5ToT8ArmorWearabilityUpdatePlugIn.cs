// <copyright file="FixMudreamT5ToT8ArmorWearabilityUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Re-applies Mudream T5–T8 armor STR/AGI bases, ItemSlot, dimensions, and MG class flags
/// so +15 ancient pieces are wearable for GMs with ~500–800 strength.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("F1C6D4E5-7081-92A3-BCD4-E5F60718293A")]
public class FixMudreamT5ToT8ArmorWearabilityUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Fix Mudream T5-T8 Armor Wearability";
    internal const string PlugInDescription =
        "Re-lowers T5–T8 STR/AGI requirement bases and re-sets ItemSlot/Width/MG QualifiedCharacters for equip.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixMudreamT5ToT8ArmorWearability;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 10, 20, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        new MudreamT5ToT8ArmorsAndSets(context, gameConfiguration).Initialize();
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
