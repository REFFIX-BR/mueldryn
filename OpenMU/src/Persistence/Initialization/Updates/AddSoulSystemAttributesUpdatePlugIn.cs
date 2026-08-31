// <copyright file="AddSoulSystemAttributesUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.SoulSystem;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds attribute definitions used by the Soul System.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("B8C9D0E1-2536-4E7F-4051-62738495A6B7")]
public class AddSoulSystemAttributesUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Adds Soul System attributes";
    internal const string PlugInDescription = "Adds SoulPointsRemaining and SoulAlloc00..33 character attributes.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddSoulSystemAttributes;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 20, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        Ensure(context, gameConfiguration, SoulSystemCatalog.RemainingAttribute);
        foreach (var attr in SoulSystemCatalog.AllocAttributes)
        {
            Ensure(context, gameConfiguration, attr);
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }

    private static void Ensure(IContext context, GameConfiguration gameConfiguration, AttributeDefinition attribute)
    {
        if (gameConfiguration.Attributes.Contains(attribute))
        {
            return;
        }

        if (gameConfiguration.Attributes.Any(a => a.Id == attribute.Id))
        {
            return;
        }

        var persistent = context.CreateNew<AttributeDefinition>(attribute.Id, attribute.Designation, attribute.Description);
        persistent.MaximumValue = null;
        gameConfiguration.Attributes.Add(persistent);
    }
}
