// <copyright file="AddDungeonEntryAttributesUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the two <see cref="AttributeDefinition"/> records required by the
/// Fortress of Imperial Dungeon daily entry-limit system:
/// <list type="bullet">
///   <item><see cref="Stats.DungeonEntryDateAttribute"/> — last reset date as UTC yyyyMMdd (float).</item>
///   <item><see cref="Stats.DungeonEntriesConsumedAttribute"/> — entries consumed today, 0–3 (float).</item>
/// </list>
/// These are persisted as <c>CharacterStatAttribute</c> rows in the existing
/// OpenMU database and must be registered in <c>GameConfiguration.Attributes</c>
/// before the dungeon logic can read or write them.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("F1E2D3C4-B5A6-4789-0123-456789ABCDEF")]
public class AddDungeonEntryAttributesUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add Fortress of Imperial Dungeon entry attributes";
    internal const string PlugInDescription = "Adds DungeonEntryDate and DungeonEntriesConsumed AttributeDefinitions for the daily entry limit.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddDungeonEntryAttributes;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 12, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        EnsureAttribute(context, gameConfiguration, Stats.DungeonEntryDateAttribute);
        EnsureAttribute(context, gameConfiguration, Stats.DungeonEntriesConsumedAttribute);

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }

    private static void EnsureAttribute(IContext context, GameConfiguration gameConfiguration, AttributeDefinition attribute)
    {
        if (gameConfiguration.Attributes.Contains(attribute))
        {
            return;
        }

        if (gameConfiguration.Attributes.Any(a => a.Id == attribute.Id))
        {
            return;
        }

        var persistent = context.CreateNew<AttributeDefinition>(
            attribute.Id,
            attribute.Designation,
            attribute.Description);

        persistent.MaximumValue = null;
        gameConfiguration.Attributes.Add(persistent);
    }
}
