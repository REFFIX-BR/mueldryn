// <copyright file="AddColoredDungeonKeysUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Switches dungeon crafts to Silver/Red/Purple Key +9 and sells them at the barmaid.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("B4C8D17A-2E90-4F61-8B3C-91D05E64A8C1")]
public class AddColoredDungeonKeysUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add Colored Dungeon Keys";
    internal const string PlugInDescription = "Uses Silver Key +9 for Normal, Red Key for Hard and Purple Key for Hell, and sells them at the bar.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddColoredDungeonKeys;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 17, 20, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        new DungeonKeyCraftingInitializer(context, gameConfiguration).Initialize();
        return ValueTask.CompletedTask;
    }
}
