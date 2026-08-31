// <copyright file="AddQuestPanelPermanentPointsAttributeUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds QuestPanelPermanentPoints for permanent main-quest stat bonuses.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("B2C3D4E5-F607-4890-B123-456789ABCDEF")]
public class AddQuestPanelPermanentPointsAttributeUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add Quest Panel Permanent Points attribute";
    internal const string PlugInDescription = "Adds QuestPanelPermanentPoints (survives character reset).";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddQuestPanelPermanentPointsAttribute;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 22, 13, 45, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        Ensure(context, gameConfiguration, Stats.QuestPanelPermanentPoints);
        return ValueTask.CompletedTask;
    }

    private static void Ensure(IContext context, GameConfiguration gameConfiguration, AttributeDefinition attribute)
    {
        if (gameConfiguration.Attributes.Any(a => a.Id == attribute.Id))
        {
            return;
        }

        var persistent = context.CreateNew<AttributeDefinition>(attribute.Id, attribute.Designation, attribute.Description);
        persistent.MaximumValue = null;
        gameConfiguration.Attributes.Add(persistent);
    }
}
