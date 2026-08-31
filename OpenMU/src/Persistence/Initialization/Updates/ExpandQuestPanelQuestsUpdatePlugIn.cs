// <copyright file="ExpandQuestPanelQuestsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds attribute definitions used by the two-quest side panel.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("7A3B8D52-9C6E-4F14-8D66-2B9E15C8A3F0")]
public class ExpandQuestPanelQuestsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Expand Quest Panel quests";
    internal const string PlugInDescription = "Adds Bull Fighter / Elite Yeti kill counters and Q1/Q2 claimed flags.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.ExpandQuestPanelQuests;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 18, 13, 40, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        Ensure(context, gameConfiguration, Stats.QuestPanelBullFighterKills);
        Ensure(context, gameConfiguration, Stats.QuestPanelEliteYetiKills);
        Ensure(context, gameConfiguration, Stats.QuestPanelQ1Claimed);
        Ensure(context, gameConfiguration, Stats.QuestPanelQ2Claimed);
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
