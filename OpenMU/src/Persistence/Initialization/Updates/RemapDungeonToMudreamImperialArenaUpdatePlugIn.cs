// <copyright file="RemapDungeonToMudreamImperialArenaUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Reloads Fortress maps 69–72 walkmesh from Mudream World82 (Karutan 2 / Imperial Guardian arena).
/// Client must use World70–73 / Object70–73 overlaid from World82 / Object82 for matching visuals.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("E2A7C91D-6B4F-4E18-9C55-1D8A0F3B7E62")]
public class RemapDungeonToMudreamImperialArenaUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Remap Dungeon To Mudream Imperial Arena";
    internal const string PlugInDescription =
        "Reloads Fortress map TerrainData (69–72) from Mudream EncTerrain82 walkmesh used by Imperial Guardian (arena 137,113).";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.RemapDungeonToMudreamImperialArena;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 11, 18, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var mapNumber in new short[] { 69, 70, 71, 72 })
        {
            var map = gameConfiguration.Maps.FirstOrDefault(m => m.Number == mapNumber);
            map?.UpdateTerrainFromResources();
        }

        return ValueTask.CompletedTask;
    }
}
