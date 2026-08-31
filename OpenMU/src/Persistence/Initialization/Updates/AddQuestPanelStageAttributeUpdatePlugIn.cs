// <copyright file="AddQuestPanelStageAttributeUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds QuestPanelStage for sequential main-quest progression.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("F6A7B8C9-D0E1-4234-5678-9ABCDEF01234")]
public class AddQuestPanelStageAttributeUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add Quest Panel Stage attribute";
    internal const string PlugInDescription = "Adds QuestPanelStage for sequential main quest unlocks.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddQuestPanelStageAttribute;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 21, 19, 10, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        Ensure(context, gameConfiguration, Stats.QuestPanelStage);
        Ensure(context, gameConfiguration, Stats.QuestPanelAccepted);
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
