// <copyright file="QuestNpcDefinitionInitializer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.PlugIns;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.QuestPanel;

/// <summary>
/// Creates the custom Quest NPC and spawns it in Lorencia (130, 134).
/// </summary>
internal class QuestNpcDefinitionInitializer : InitializerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuestNpcDefinitionInitializer"/> class.
    /// </summary>
    public QuestNpcDefinitionInitializer(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        this.CreateQuestNpcDefinition();
        this.SpawnQuestNpcInLorencia();
    }

    private void CreateQuestNpcDefinition()
    {
        if (this.GameConfiguration.Monsters.Any(m => m.Number == QuestPanelService.QuestNpcNumber))
        {
            return;
        }

        var npcDefinition = this.Context.CreateNew<MonsterDefinition>();
        this.GameConfiguration.Monsters.Add(npcDefinition);
        npcDefinition.Number = QuestPanelService.QuestNpcNumber;
        npcDefinition.Designation = "Quest Master";
        npcDefinition.ObjectKind = NpcObjectKind.PassiveNpc;
        npcDefinition.NpcWindow = NpcWindow.Undefined;
        npcDefinition.MoveRange = 0;
        npcDefinition.AttackRange = 0;
        npcDefinition.ViewRange = 3;
        npcDefinition.MoveDelay = TimeSpan.Zero;
        npcDefinition.AttackDelay = TimeSpan.Zero;
        npcDefinition.RespawnDelay = TimeSpan.Zero;
        npcDefinition.Attribute = 0;
        npcDefinition.NumberOfMaximumItemDrops = 0;
        npcDefinition.SetGuid(QuestPanelService.QuestNpcNumber);
    }

    private void SpawnQuestNpcInLorencia()
    {
        var lorencia = this.GameConfiguration.Maps.FirstOrDefault(m => m.Number == 0);
        if (lorencia is null)
        {
            return;
        }

        if (lorencia.MonsterSpawns.Any(s => s.MonsterDefinition?.Number == QuestPanelService.QuestNpcNumber))
        {
            return;
        }

        var npcDefinition = this.GameConfiguration.Monsters.FirstOrDefault(m => m.Number == QuestPanelService.QuestNpcNumber);
        if (npcDefinition is null)
        {
            return;
        }

        var spawnArea = this.Context.CreateNew<MonsterSpawnArea>();
        lorencia.MonsterSpawns.Add(spawnArea);
        spawnArea.MonsterDefinition = npcDefinition;
        spawnArea.GameMap = lorencia;
        spawnArea.Quantity = 1;
        spawnArea.Direction = Direction.South;
        spawnArea.SpawnTrigger = SpawnTrigger.Automatic;
        spawnArea.X1 = 130;
        spawnArea.X2 = 130;
        spawnArea.Y1 = 134;
        spawnArea.Y2 = 134;
        spawnArea.SetGuid(QuestPanelService.QuestNpcNumber, 0);
    }
}
