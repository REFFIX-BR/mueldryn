// <copyright file="AddExtendedGoldenInvasionMobsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the extended Season 6 golden invasion monsters to existing databases.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("85B62ED3-AE18-48C5-B952-792DCF8F69B1")]
public class AddExtendedGoldenInvasionMobsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add Extended Golden Invasion Monsters";
    internal const string PlugInDescription = "Adds Golden Invasion monsters 493-502, their Box of Kundun drops, and renames monster 79 to Golden Derkon.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddExtendedGoldenInvasionMobs;

    /// <inheritdoc />
    public override string DataInitializationKey => DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 14, 12, 30, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        ExtendedGoldenInvasionMonsterFactory.AddMissing(context, gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
