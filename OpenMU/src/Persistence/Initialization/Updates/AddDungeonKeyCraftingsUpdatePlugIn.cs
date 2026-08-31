// <copyright file="AddDungeonKeyCraftingsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds dungeon ticket/key items, 100% Chaos Goblin recipes and barmaid shop entries.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("A91C3E74-6B2D-4F18-9E55-8D4C1A07B6F2")]
public class AddDungeonKeyCraftingsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add Dungeon Key Craftings";
    internal const string PlugInDescription = "Adds dungeon ticket/keys, 100% chaos mixes and barmaid shop items.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddDungeonKeyCraftings;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 17, 19, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        new DungeonKeyCraftingInitializer(context, gameConfiguration).Initialize();
        return ValueTask.CompletedTask;
    }
}
