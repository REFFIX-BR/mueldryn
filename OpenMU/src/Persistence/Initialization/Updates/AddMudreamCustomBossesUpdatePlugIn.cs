// <copyright file="AddMudreamCustomBossesUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds Mudream custom boss monster definitions (client IDs) to existing Season 6 databases.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("A7D3E91F-2C48-4B6A-9F01-8E5D47C2B0A6")]
public class AddMudreamCustomBossesUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add Mudream Custom Bosses";
    internal const string PlugInDescription =
        "Registers Mudream BossHealthBar type=3 customs (604 Golden Kundun, 583 Silvester, 753 Jack O'Lantern, …) with mirrored HP/ATK/DEF for /spawn.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddMudreamCustomBosses;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 11, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        MudreamCustomBossFactory.AddMissing(context, gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
