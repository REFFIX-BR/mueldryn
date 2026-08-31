// <copyright file="AddDungeonNormalRewardsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sets Normal dungeon loot to 2x Box of Kundun +3 plus a chance of a T1 ancient.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("E7A19C2B-4D58-4F01-9C6A-1B8E2F47D305")]
public class AddDungeonNormalRewardsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add Dungeon Normal Rewards";
    internal const string PlugInDescription = "Normal dungeon: 2x Box of Kundun +3 and 25% chance of a T1 ancient piece.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddDungeonNormalRewards;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 17, 22, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        new DungeonDefinitionInitializer(context, gameConfiguration).ApplyRewards();
        return ValueTask.CompletedTask;
    }
}
