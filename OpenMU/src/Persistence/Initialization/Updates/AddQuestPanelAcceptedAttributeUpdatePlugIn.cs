// <copyright file="AddQuestPanelAcceptedAttributeUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the QuestPanelAccepted attribute used by the Quest Master NPC accept flow.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("E5F6A7B8-C9D0-4123-4567-89ABCDEF0123")]
public class AddQuestPanelAcceptedAttributeUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add Quest Panel Accepted attribute";
    internal const string PlugInDescription = "Adds QuestPanelAccepted so NPC quest accept/progress works with EF persistence.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddQuestPanelAcceptedAttribute;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 21, 18, 40, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
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
