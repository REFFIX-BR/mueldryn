// <copyright file="SyncMudreamImperialFortressTerrainUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Reloads Imperial Fortress terrain (maps 69–72) from embedded Mudream-synced Terrain70–73.att.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("C9F5A13B-4E6A-4D8C-B123-A07F69E4D2C8")]
public class SyncMudreamImperialFortressTerrainUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Sync Mudream Imperial Fortress Terrain";
    internal const string PlugInDescription =
        "Updates GameMapDefinition.TerrainData for Fortress of Imperial Guardian maps 69–72 from Mudream EncTerrain70–73 walkmesh.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.SyncMudreamImperialFortressTerrain;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 11, 16, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        // Map 69 → Terrain70.att … Map 72 → Terrain73.att (OpenMU Number+1 resource naming).
        foreach (var mapNumber in new short[] { 69, 70, 71, 72 })
        {
            var map = gameConfiguration.Maps.FirstOrDefault(m => m.Number == mapNumber);
            map?.UpdateTerrainFromResources();
        }

        return ValueTask.CompletedTask;
    }
}
