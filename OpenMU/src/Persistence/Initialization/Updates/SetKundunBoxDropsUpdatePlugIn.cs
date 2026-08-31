// <copyright file="SetKundunBoxDropsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sets Box of Kundun +1..+5 drops to the excellent armor/shield lists, without RF or Summoner items.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("6F2A9C41-8B7E-4D13-9C55-1A8E04B7D2F9")]
public class SetKundunBoxDropsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Set Kundun Box Drops";
    internal const string PlugInDescription = "Box of Kundun +1 to +5 drop excellent armors and shields from the published lists, excluding Rage Fighter and Summoner.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.SetKundunBoxDrops;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 18, 13, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        KundunBoxDropCatalog.Apply(context, gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
