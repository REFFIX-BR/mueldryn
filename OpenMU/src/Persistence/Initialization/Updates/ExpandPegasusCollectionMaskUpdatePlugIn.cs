// <copyright file="ExpandPegasusCollectionMaskUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Collections;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds extra Collection Mask attributes (3-9) for 20 sets × 5 pieces (100 progress bits).
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("C011EC70-A146-4B02-9C03-D4E5F6071460")]
public class ExpandPegasusCollectionMaskUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Expand Pegasus Collection mask storage";
    internal const string PlugInDescription = "Adds Collection Mask 3-9 attributes for 20 Mudream sets.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.ExpandPegasusCollectionMask;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 01, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var attr in PegasusCollectionCatalog.MaskAttributes)
        {
            Ensure(context, gameConfiguration, attr);
        }

        Ensure(context, gameConfiguration, PegasusCollectionCatalog.BonusHpAttribute);
        Ensure(context, gameConfiguration, PegasusCollectionCatalog.RewardClaimedAttribute);

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }

    private static void Ensure(IContext context, GameConfiguration gameConfiguration, AttributeDefinition attribute)
    {
        var existing = gameConfiguration.Attributes.FirstOrDefault(a => a.Id == attribute.Id);
        if (existing is not null)
        {
            existing.Designation = attribute.Designation;
            existing.Description = attribute.Description;
            existing.MaximumValue = null;
            return;
        }

        var persistent = context.CreateNew<AttributeDefinition>(attribute.Id, attribute.Designation, attribute.Description);
        persistent.MaximumValue = null;
        gameConfiguration.Attributes.Add(persistent);
    }
}
