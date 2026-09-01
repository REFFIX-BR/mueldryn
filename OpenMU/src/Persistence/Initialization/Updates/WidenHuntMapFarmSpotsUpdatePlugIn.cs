// <copyright file="WidenHuntMapFarmSpotsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Re-applies hunt-map farm spacing with a larger minimum distance. Update 142 used 4.5 tiles;
/// Kanturu Relics (map 38) and similar end-game maps still looked stacked. City curated maps
/// use ~28-tile grids — hunt maps use 12 m as a practical middle ground without Mudream server dumps.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("8F3A1C72-9D4E-4B60-A1E5-6C7D2F908B41")]
public class WidenHuntMapFarmSpotsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Widen hunt-map farm spots (12 m)";
    internal const string PlugInDescription =
        "Re-thins hunt-map farm spots to a 12-tile minimum spacing (Kanturu, Aida, Karutan, etc.).";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.WidenHuntMapFarmSpots;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 01, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        HuntMapFarmSpotSpacing.Apply(gameConfiguration, HuntMapFarmSpotSpacing.WideMinDistanceTiles);
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
