// <copyright file="FixPegasusCollectionAttributesUpdatePlugIn.cs" company="MUnique">
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
/// Re-adds Pegasus Collection attribute definitions when update 125 was marked installed
/// but the rows were never persisted (or were lost). Without them, enter-world EnsureDefinition
/// fails and character select hangs on the client loading screen.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("C011EC70-A131-4B02-9C03-D4E5F6071310")]
public class FixPegasusCollectionAttributesUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Fix Pegasus Collection attributes";
    internal const string PlugInDescription = "Ensures Collection Mask/Bonus HP/Reward Claimed attribute definitions exist.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixPegasusCollectionAttributes;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 24, 13, 0, 0, DateTimeKind.Utc);

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
