// <copyright file="WidenHuntMapFarmSpots20UpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Re-applies hunt-map farm spacing at 20 tiles after update 143 (12 m) still felt dense on Kanturu Relics.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("2E9B5D41-7C8A-4F03-B6D1-9A4E7083C512")]
public class WidenHuntMapFarmSpots20UpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Widen hunt-map farm spots (20 m)";
    internal const string PlugInDescription =
        "Re-thins hunt-map farm spots to a 20-tile minimum spacing (Kanturu, Aida, Karutan, etc.).";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.WidenHuntMapFarmSpots20;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 01, 13, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        HuntMapFarmSpotSpacing.Apply(gameConfiguration, HuntMapFarmSpotSpacing.ExtraWideMinDistanceTiles);
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
