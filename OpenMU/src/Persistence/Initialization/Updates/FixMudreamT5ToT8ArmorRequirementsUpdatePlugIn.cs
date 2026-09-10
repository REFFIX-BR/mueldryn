// <copyright file="FixMudreamT5ToT8ArmorRequirementsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Re-applies Mudream T5–T8 armor names, class flags, and strength/agility requirements
/// (fixes MG T6 equip fail and T7/T8 duplicate tooltip names on existing DBs).
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("D9A4B2C3-5E6F-7081-9BA2-C3D4E5F60718")]
public class FixMudreamT5ToT8ArmorRequirementsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Fix Mudream T5-T8 Armor Requirements";
    internal const string PlugInDescription =
        "Updates existing T5–T8 armor definitions: piece-only T7/T8 names, MG class flags, and wearable STR/AGI bases.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixMudreamT5ToT8ArmorRequirements;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 10, 18, 30, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        new MudreamT5ToT8ArmorsAndSets(context, gameConfiguration).Initialize();
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
