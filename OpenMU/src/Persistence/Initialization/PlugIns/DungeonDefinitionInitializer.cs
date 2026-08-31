// <copyright file="DungeonDefinitionInitializer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.PlugIns;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Dungeons;
using MUnique.OpenMU.GameLogic.NPC;

/// <summary>
/// Initializes the Fortress of Imperial Guardian dungeon MiniGame definitions and entry NPC.
/// </summary>
internal class DungeonDefinitionInitializer : InitializerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DungeonDefinitionInitializer"/> class.
    /// </summary>
    public DungeonDefinitionInitializer(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        this.CreateDungeonNpcDefinition();
        this.SpawnDungeonNpcInLorencia();
        new DungeonKeyCraftingInitializer(this.Context, this.GameConfiguration).Initialize();

        if (this.GameConfiguration.MiniGameDefinitions.Any(d => d.Type == MiniGameType.ImperialFortress))
        {
            return;
        }

        this.CreateDungeonDefinition(DungeonDifficulty.Normal, 100, TimeSpan.FromMinutes(20));
        this.CreateDungeonDefinition(DungeonDifficulty.Hard, 250, TimeSpan.FromMinutes(18));
        this.CreateDungeonDefinition(DungeonDifficulty.Hell, 400, TimeSpan.FromMinutes(15));
    }

    private MiniGameDefinition CreateDungeonDefinition(DungeonDifficulty difficulty, int minLevel, TimeSpan gameDuration)
    {
        var definition = this.Context.CreateNew<MiniGameDefinition>();
        definition.SetGuid((short)MiniGameType.ImperialFortress, (short)difficulty);
        this.GameConfiguration.MiniGameDefinitions.Add(definition);

        definition.Name = $"Fortress of Imperial Guardian - {difficulty}";
        definition.Description = $"Instanced dungeon with three progressive rooms, {difficulty} difficulty.";
        definition.Type = MiniGameType.ImperialFortress;
        definition.GameLevel = (byte)difficulty;
        definition.MinimumCharacterLevel = minLevel;
        definition.MaximumCharacterLevel = 4000;
        definition.ArePlayerKillersAllowedToEnter = false;
        definition.EnterDuration = TimeSpan.FromSeconds(8);
        definition.GameDuration = gameDuration;
        definition.ExitDuration = TimeSpan.FromSeconds(45);
        definition.MapCreationPolicy = MiniGameMapCreationPolicy.OnePerParty;
        definition.MaximumPlayerCount = 5;
        definition.AllowParty = true;

        var fortressMap = this.GameConfiguration.Maps.FirstOrDefault(m => m.Number == 69);
        if (fortressMap?.ExitGates.Any() == true)
        {
            definition.Entrance = fortressMap.ExitGates.First();
        }

        this.CreateSpawnWaves(definition, difficulty);
        this.CreateRoomProgressionEvents(definition);
        this.CreateRewards(definition, difficulty);
        return definition;
    }

    /// <summary>
    /// Replaces Imperial Fortress reward tables (fresh DB and live updates).
    /// </summary>
    public void ApplyRewards()
    {
        foreach (var definition in this.GameConfiguration.MiniGameDefinitions.Where(definition => definition.Type == MiniGameType.ImperialFortress))
        {
            definition.Rewards.Clear();
            this.CreateRewards(definition, (DungeonDifficulty)definition.GameLevel);
        }
    }

    private void CreateSpawnWaves(MiniGameDefinition definition, DungeonDifficulty difficulty)
    {
        var room1Wave = this.Context.CreateNew<MiniGameSpawnWave>();
        definition.SpawnWaves.Add(room1Wave);
        room1Wave.WaveNumber = 1;
        room1Wave.Description = $"Room 1 - Initial Wave ({difficulty})";
        room1Wave.Message = "Entering Room 1...";
        room1Wave.StartTime = TimeSpan.Zero;
        room1Wave.EndTime = definition.GameDuration;

        var room2Wave = this.Context.CreateNew<MiniGameSpawnWave>();
        definition.SpawnWaves.Add(room2Wave);
        room2Wave.WaveNumber = 2;
        room2Wave.Description = $"Room 2 - Elite Wave ({difficulty})";
        room2Wave.Message = "Entering Room 2...";
        room2Wave.StartTime = TimeSpan.Zero;
        room2Wave.EndTime = definition.GameDuration;

        var room3Wave = this.Context.CreateNew<MiniGameSpawnWave>();
        definition.SpawnWaves.Add(room3Wave);
        room3Wave.WaveNumber = 3;
        room3Wave.Description = $"Room 3 - Boss Wave ({difficulty})";
        room3Wave.Message = "Prepare for the Boss!";
        room3Wave.StartTime = TimeSpan.Zero;
        room3Wave.EndTime = definition.GameDuration;
    }

    private void CreateRoomProgressionEvents(MiniGameDefinition definition)
    {
        var room1Complete = this.Context.CreateNew<MiniGameChangeEvent>();
        definition.ChangeEvents.Add(room1Complete);
        room1Complete.Index = 0;
        room1Complete.Description = "Room 1 Complete - Advance to Room 2";
        room1Complete.Target = KillTarget.AnyMonster;
        room1Complete.NumberOfKills = 7;
        room1Complete.Message = "Room 1 cleared! Advance to Room 2.";

        var room2Complete = this.Context.CreateNew<MiniGameChangeEvent>();
        definition.ChangeEvents.Add(room2Complete);
        room2Complete.Index = 1;
        room2Complete.Description = "Room 2 Complete - Advance to Room 3 (Boss)";
        room2Complete.Target = KillTarget.AnyMonster;
        room2Complete.NumberOfKills = 7;
        room2Complete.Message = "Room 2 cleared! Prepare for the Boss in Room 3.";

        var bossDefeated = this.Context.CreateNew<MiniGameChangeEvent>();
        definition.ChangeEvents.Add(bossDefeated);
        bossDefeated.Index = 2;
        bossDefeated.Description = "Boss Defeated - Dungeon Complete";
        bossDefeated.Target = KillTarget.AnyMonster;
        bossDefeated.NumberOfKills = 1;
        bossDefeated.Message = "{0} has defeated the Boss! Dungeon complete!";
    }

    private void CreateRewards(MiniGameDefinition definition, DungeonDifficulty difficulty)
    {
        var kundun = this.GameConfiguration.Items.FirstOrDefault(i => i.Group == DungeonRewards.KundunGroup && i.Number == DungeonRewards.KundunNumber);
        if (kundun is null)
        {
            return;
        }

        if (difficulty == DungeonDifficulty.Normal)
        {
            this.AddItemReward(
                definition,
                $"Imperial Fortress Normal - {DungeonRewards.NormalKundunCount}x Kundun +3",
                kundunLevel: DungeonRewards.KundunPlus3Level,
                amount: DungeonRewards.NormalKundunCount,
                chance: 1,
                kundun,
                SpecialItemType.None);

            var tier1Items = this.GetTier1AncientItems().ToList();
            if (tier1Items.Count > 0)
            {
                this.AddItemReward(
                    definition,
                    "Imperial Fortress Normal - T1 ancient",
                    kundunLevel: null,
                    amount: 1,
                    chance: DungeonRewards.NormalAncientChance,
                    possibleItems: tier1Items,
                    itemType: SpecialItemType.Ancient);
            }

            return;
        }

        var placeholderLevel = (byte)(DungeonRewards.KundunPlus3Level - (byte)(DungeonDifficulty.Hell - difficulty));
        this.AddItemReward(
            definition,
            $"Imperial Fortress {difficulty} - Kundun placeholder",
            kundunLevel: placeholderLevel,
            amount: 1,
            chance: 1,
            kundun,
            SpecialItemType.None);
    }

    private void AddItemReward(
        MiniGameDefinition definition,
        string description,
        byte? kundunLevel,
        int amount,
        double chance,
        ItemDefinition item,
        SpecialItemType itemType)
    {
        this.AddItemReward(definition, description, kundunLevel, amount, chance, [item], itemType);
    }

    private void AddItemReward(
        MiniGameDefinition definition,
        string description,
        byte? kundunLevel,
        int amount,
        double chance,
        IEnumerable<ItemDefinition> possibleItems,
        SpecialItemType itemType)
    {
        var dropGroup = this.Context.CreateNew<DropItemGroup>();
        dropGroup.Description = description;
        dropGroup.Chance = chance;
        dropGroup.ItemLevel = kundunLevel;
        dropGroup.ItemType = itemType;
        foreach (var item in possibleItems)
        {
            dropGroup.PossibleItems.Add(item);
        }

        this.GameConfiguration.DropItemGroups.Add(dropGroup);

        var itemReward = this.Context.CreateNew<MiniGameReward>();
        itemReward.RewardType = MiniGameRewardType.Item;
        itemReward.RewardAmount = amount;
        itemReward.RequiredSuccess = MiniGameSuccessFlags.WinnerOrInWinningParty;
        itemReward.ItemReward = dropGroup;
        definition.Rewards.Add(itemReward);
    }

    private IEnumerable<ItemDefinition> GetTier1AncientItems()
    {
        return this.GameConfiguration.ItemSetGroups
            .Where(set => DungeonRewards.IsTier1AncientSet(set.Name))
            .SelectMany(set => set.Items)
            .Select(itemOfSet => itemOfSet.ItemDefinition)
            .Where(definition => definition is not null)
            .Cast<ItemDefinition>()
            .Distinct();
    }

    private void CreateDungeonNpcDefinition()
    {
        if (this.GameConfiguration.Monsters.Any(m => m.Number == 690))
        {
            return;
        }

        var npcDefinition = this.Context.CreateNew<MonsterDefinition>();
        this.GameConfiguration.Monsters.Add(npcDefinition);
        npcDefinition.Number = 690;
        npcDefinition.Designation = "Imperial Guardian";
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
        npcDefinition.SetGuid(690);
    }

    private void SpawnDungeonNpcInLorencia()
    {
        var lorencia = this.GameConfiguration.Maps.FirstOrDefault(m => m.Number == 0);
        if (lorencia is null)
        {
            return;
        }

        if (lorencia.MonsterSpawns.Any(s => s.MonsterDefinition?.Number == 690))
        {
            return;
        }

        var npcDefinition = this.GameConfiguration.Monsters.FirstOrDefault(m => m.Number == 690);
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
        spawnArea.X1 = 147;
        spawnArea.X2 = 147;
        spawnArea.Y1 = 127;
        spawnArea.Y2 = 127;
        spawnArea.SetGuid(690, 0);
    }
}
