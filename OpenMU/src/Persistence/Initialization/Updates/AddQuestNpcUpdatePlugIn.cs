// <copyright file="AddQuestNpcUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the Quest Master NPC in Lorencia (130, 134).
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("D4E5F6A7-B8C9-4012-3456-789ABCDEF012")]
public class AddQuestNpcUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add Quest Master NPC";
    internal const string PlugInDescription = "Spawns the Quest Master NPC in Lorencia at 130,134.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddQuestNpc;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 21, 18, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        new QuestNpcDefinitionInitializer(context, gameConfiguration).Initialize();
        return ValueTask.CompletedTask;
    }
}
