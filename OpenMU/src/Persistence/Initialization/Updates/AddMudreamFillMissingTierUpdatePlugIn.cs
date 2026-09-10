// <copyright file="AddMudreamFillMissingTierUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Registers missing Mudream tier pieces: Aurelia/Mythic (SU T4), Drakzar/Thorned (RF T4), Vega/Chamer (RF T3).
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("E91A4C2D-7B38-4F15-9D60-A1B2C3D4E5F8")]
public class AddMudreamFillMissingTierUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add Mudream missing T3/T4 sets";
    internal const string PlugInDescription = "Adds Aurelia+Mythic (SU T4), Drakzar+Thorned (RF T4), and Vega/Chamer (RF T3 Sacred).";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddMudreamFillMissingTier;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 10, 15, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        new MudreamFillMissingTier(context, gameConfiguration).Initialize();
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
