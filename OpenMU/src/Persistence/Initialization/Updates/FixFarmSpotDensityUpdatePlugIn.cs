// <copyright file="FixFarmSpotDensityUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Re-applies <see cref="FarmSpotDensityUpdatePlugIn"/> when version 135 was marked installed
/// without persisting spawn Quantity / RespawnDelay changes (hung GameConfiguration load workaround).
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("C8F5B02D-3E69-501B-AD42-9F706B23E158")]
public class FixFarmSpotDensityUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Fix farm spot density (re-apply 135)";
    internal const string PlugInDescription =
        "Re-applies Quantity=6 and RespawnDelay=3s farm density after update 135 was recorded without side effects.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixFarmSpotDensity;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 24, 18, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        FarmSpotDensityUpdatePlugIn.ApplyFarmDensity(context, gameConfiguration);
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
