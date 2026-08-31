// <copyright file="AddImperialFortressDungeonUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds Fortress of Imperial dungeon MiniGame definitions and the Lorencia entry NPC.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("C0D1E2F3-A4B5-4678-9012-3456789ABCDE")]
public class AddImperialFortressDungeonUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add Imperial Fortress Dungeon";
    internal const string PlugInDescription = "Adds Normal/Hard/Hell Imperial Fortress dungeon definitions and the Lorencia NPC.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddImperialFortressDungeon;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 17, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        new DungeonDefinitionInitializer(context, gameConfiguration).Initialize();
        return ValueTask.CompletedTask;
    }
}
