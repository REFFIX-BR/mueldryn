// <copyright file="AddPegasusCollectionAttributesUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Collections;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds attribute definitions used by Pegasus Collections progress masks.
/// Without these, donate progress cannot persist and each piece overwrites the last.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("C011EC70-A001-4B02-9C03-D4E5F6071250")]
public class AddPegasusCollectionAttributesUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Adds Pegasus Collection attributes";
    internal const string PlugInDescription = "Adds Collection Mask 0/1/2 character attributes for donate progress.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddPegasusCollectionAttributes;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 21, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var attr in PegasusCollectionCatalog.MaskAttributes)
        {
            Ensure(context, gameConfiguration, attr);
        }

        Ensure(context, gameConfiguration, PegasusCollectionCatalog.BonusHpAttribute);
        Ensure(context, gameConfiguration, PegasusCollectionCatalog.RewardClaimedAttribute);
        Ensure(context, gameConfiguration, Stats.WCoinC);

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }

    private static void Ensure(IContext context, GameConfiguration gameConfiguration, AttributeDefinition attribute)
    {
        if (gameConfiguration.Attributes.Any(a => a.Id == attribute.Id))
        {
            var existing = gameConfiguration.Attributes.First(a => a.Id == attribute.Id);
            existing.MaximumValue = null;
            return;
        }

        var persistent = context.CreateNew<AttributeDefinition>(attribute.Id, attribute.Designation, attribute.Description);
        persistent.MaximumValue = null;
        gameConfiguration.Attributes.Add(persistent);
    }
}
