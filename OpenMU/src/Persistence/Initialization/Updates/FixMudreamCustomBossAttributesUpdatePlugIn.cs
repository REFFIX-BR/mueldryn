// <copyright file="FixMudreamCustomBossAttributesUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Deduplicates Mudream custom boss attributes so /spawn no longer throws on duplicate Level keys.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("B8E4F02A-3D59-4C7B-A012-9F6E58D3C1B7")]
public class FixMudreamCustomBossAttributesUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Fix Mudream Custom Boss Attributes";
    internal const string PlugInDescription =
        "Removes duplicate AttributeDefinition entries on Mudream custom bosses that caused /spawn to fail with 'same key already added: Level'.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixMudreamCustomBossAttributes;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 11, 13, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        MudreamCustomBossFactory.DeduplicateBossAttributes(gameConfiguration);
        MudreamCustomBossFactory.AddMissing(context, gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
