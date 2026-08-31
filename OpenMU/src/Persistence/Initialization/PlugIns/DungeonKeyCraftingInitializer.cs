// <copyright file="DungeonKeyCraftingInitializer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.PlugIns;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Dungeons;
using MUnique.OpenMU.GameLogic.PlayerActions.Craftings;

/// <summary>
/// Creates dungeon ticket/key items, 100% Chaos Goblin recipes and barmaid shop entries.
/// </summary>
internal class DungeonKeyCraftingInitializer : InitializerBase
{
    private static readonly short[] BarmaidNpcNumbers = [255, 244];
    private static readonly short[] RetiredShopItemNumbers = [111, 113];

    /// <summary>
    /// Initializes a new instance of the <see cref="DungeonKeyCraftingInitializer"/> class.
    /// </summary>
    public DungeonKeyCraftingInitializer(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        this.CreateItemDefinitions();
        this.AddChaosGoblinCraftings();
        this.AddBarmaidShopItems();
    }

    private void CreateItemDefinitions()
    {
        this.EnsureItemDefinition(DungeonKeyItems.TicketNumber, DungeonKeyItems.TicketName, DungeonKeyItems.KeyLevel);
        this.EnsureItemDefinition(DungeonKeyItems.NormalKeyNumber, DungeonKeyItems.NormalKeyName, DungeonKeyItems.KeyLevel);
        this.EnsureItemDefinition(DungeonKeyItems.HardKeyNumber, DungeonKeyItems.HardKeyName, DungeonKeyItems.KeyLevel);
        this.EnsureItemDefinition(DungeonKeyItems.HellKeyNumber, DungeonKeyItems.HellKeyName, DungeonKeyItems.KeyLevel);
        this.RestoreOfficialGoldKey();
    }

    private void EnsureItemDefinition(short number, string name, byte maximumItemLevel)
    {
        var definition = this.GameConfiguration.Items.FirstOrDefault(item => item.Group == DungeonKeyItems.Group && item.Number == number);
        if (definition is null)
        {
            definition = this.Context.CreateNew<ItemDefinition>();
            definition.Number = number;
            definition.Group = DungeonKeyItems.Group;
            definition.DropLevel = 0;
            definition.DropsFromMonsters = false;
            definition.SetGuid(definition.Group, definition.Number);
            this.GameConfiguration.Items.Add(definition);
        }

        definition.Name = name;
        definition.Width = 1;
        definition.Height = 1;
        if (number != DungeonKeyItems.NormalKeyNumber || definition.Durability <= 0)
        {
            definition.Durability = 1;
        }

        if (definition.Value <= 0)
        {
            definition.Value = 110;
        }

        if (definition.MaximumItemLevel < maximumItemLevel)
        {
            definition.MaximumItemLevel = maximumItemLevel;
        }

        foreach (var characterClass in this.GameConfiguration.CharacterClasses)
        {
            if (!definition.QualifiedCharacters.Contains(characterClass))
            {
                definition.QualifiedCharacters.Add(characterClass);
            }
        }
    }

    private void RestoreOfficialGoldKey()
    {
        var goldKey = this.GameConfiguration.Items.FirstOrDefault(item => item.Group == 14 && item.Number == 113);
        if (goldKey is not null)
        {
            goldKey.Name = "Gold Key";
        }
    }

    private void AddChaosGoblinCraftings()
    {
        var chaosGoblin = this.GameConfiguration.Monsters.FirstOrDefault(monster => monster.NpcWindow == NpcWindow.ChaosMachine);
        if (chaosGoblin is null)
        {
            return;
        }

        this.ConfigureNormalKeyCrafting(this.GetOrCreateCrafting(chaosGoblin, DungeonKeyItems.NormalMixNumber, "Dungeon Key Normal"));
        this.ConfigureHardKeyCrafting(this.GetOrCreateCrafting(chaosGoblin, DungeonKeyItems.HardMixNumber, "Dungeon Key Hard"));
        this.ConfigureHellKeyCrafting(this.GetOrCreateCrafting(chaosGoblin, DungeonKeyItems.HellMixNumber, "Dungeon Key Hell"));
    }

    private ItemCrafting GetOrCreateCrafting(MonsterDefinition chaosGoblin, byte mixNumber, string name)
    {
        var crafting = chaosGoblin.ItemCraftings.FirstOrDefault(itemCrafting => itemCrafting.Number == mixNumber);
        if (crafting is null)
        {
            crafting = this.Context.CreateNew<ItemCrafting>();
            crafting.Number = mixNumber;
            chaosGoblin.ItemCraftings.Add(crafting);
        }

        crafting.Name = name;
        if (crafting.SimpleCraftingSettings is null)
        {
            crafting.SimpleCraftingSettings = this.Context.CreateNew<SimpleCraftingSettings>();
        }

        crafting.SimpleCraftingSettings.RequiredItems.Clear();
        crafting.SimpleCraftingSettings.ResultItems.Clear();
        crafting.SimpleCraftingSettings.SuccessPercent = 100;
        crafting.SimpleCraftingSettings.ResultItemSelect = ResultItemSelection.All;
        return crafting;
    }

    private void ConfigureNormalKeyCrafting(ItemCrafting crafting)
    {
        crafting.SimpleCraftingSettings!.Money = DungeonKeyItems.NormalZen;
        this.SetResult(crafting, this.Item(DungeonKeyItems.NormalKeyNumber));
        this.AddRequiredItem(crafting, this.Item(DungeonKeyItems.TicketNumber), 1);
        this.AddJewelRequirements(crafting, 8);
        this.AddOptionalTalisman(crafting);
    }

    private void ConfigureHardKeyCrafting(ItemCrafting crafting)
    {
        crafting.SimpleCraftingSettings!.Money = DungeonKeyItems.HardZen;
        this.SetResult(crafting, this.Item(DungeonKeyItems.HardKeyNumber));
        this.AddRequiredItem(crafting, this.Item(DungeonKeyItems.NormalKeyNumber), 1, DungeonKeyItems.KeyLevel, DungeonKeyItems.KeyLevel);
        this.AddJewelRequirements(crafting, 15);
        this.AddExcellentRequirement(crafting, minimumLevel: 6);
        this.AddOptionalTalisman(crafting);
    }

    private void ConfigureHellKeyCrafting(ItemCrafting crafting)
    {
        crafting.ItemCraftingHandlerClassName = typeof(DungeonHellKeyCraftingHandler).FullName!;
        crafting.SimpleCraftingSettings!.Money = DungeonKeyItems.HellZen;
        this.SetResult(crafting, this.Item(DungeonKeyItems.HellKeyNumber));
        this.AddRequiredItem(crafting, this.Item(DungeonKeyItems.HardKeyNumber), 1, DungeonKeyItems.KeyLevel, DungeonKeyItems.KeyLevel);
        this.AddJewelRequirements(crafting, 25);
        this.AddExcellentRequirement(crafting, minimumLevel: 9);
        this.AddOptionalTalisman(crafting);
    }

    private void SetResult(ItemCrafting crafting, ItemDefinition resultItem)
    {
        var result = this.Context.CreateNew<ItemCraftingResultItem>();
        result.ItemDefinition = resultItem;
        result.Durability = 1;
        result.RandomMinimumLevel = DungeonKeyItems.KeyLevel;
        result.RandomMaximumLevel = DungeonKeyItems.KeyLevel;
        crafting.SimpleCraftingSettings!.ResultItems.Add(result);
    }

    private void AddJewelRequirements(ItemCrafting crafting, byte amount)
    {
        this.AddRequiredItem(crafting, this.NamedItem("Jewel of Bless"), amount);
        this.AddRequiredItem(crafting, this.NamedItem("Jewel of Soul"), amount);
        this.AddRequiredItem(crafting, this.NamedItem("Jewel of Creation"), amount);
    }

    private void AddRequiredItem(ItemCrafting crafting, ItemDefinition definition, byte amount, byte minimumLevel = 0, byte maximumLevel = 0)
    {
        var required = this.Context.CreateNew<ItemCraftingRequiredItem>();
        required.PossibleItems.Add(definition);
        required.MinimumAmount = amount;
        required.MaximumAmount = amount;
        required.MinimumItemLevel = minimumLevel;
        required.MaximumItemLevel = maximumLevel;
        required.SuccessResult = MixResult.Disappear;
        required.FailResult = MixResult.Disappear;
        crafting.SimpleCraftingSettings!.RequiredItems.Add(required);
    }

    private void AddExcellentRequirement(ItemCrafting crafting, byte minimumLevel)
    {
        var required = this.Context.CreateNew<ItemCraftingRequiredItem>();
        required.MinimumAmount = 1;
        required.MaximumAmount = 1;
        required.MinimumItemLevel = minimumLevel;
        required.MaximumItemLevel = 15;
        required.SuccessResult = MixResult.Disappear;
        required.FailResult = MixResult.Disappear;
        required.RequiredItemOptions.Add(this.GameConfiguration.ItemOptionTypes.First(option => option == ItemOptionTypes.Excellent));
        crafting.SimpleCraftingSettings!.RequiredItems.Add(required);
    }

    private void AddOptionalTalisman(ItemCrafting crafting)
    {
        var talisman = this.GameConfiguration.Items.FirstOrDefault(item => item.Group == 14 && item.Number == 53);
        if (talisman is null)
        {
            return;
        }

        var required = this.Context.CreateNew<ItemCraftingRequiredItem>();
        required.PossibleItems.Add(talisman);
        required.MinimumAmount = 0;
        required.MaximumAmount = 1;
        required.AddPercentage = 25;
        required.SuccessResult = MixResult.Disappear;
        required.FailResult = MixResult.Disappear;
        crafting.SimpleCraftingSettings!.RequiredItems.Add(required);
    }

    private void AddBarmaidShopItems()
    {
        foreach (var npcNumber in BarmaidNpcNumbers)
        {
            var npc = this.GameConfiguration.Monsters.FirstOrDefault(monster => monster.Number == npcNumber);
            if (npc?.MerchantStore is not { } store)
            {
                continue;
            }

            this.RemoveShopItems(store, RetiredShopItemNumbers);
            this.AddOrUpdateShopItem(store, DungeonKeyItems.TicketNumber, 0);
            this.AddOrUpdateShopItem(store, DungeonKeyItems.NormalKeyNumber, DungeonKeyItems.KeyLevel);
            this.AddOrUpdateShopItem(store, DungeonKeyItems.HardKeyNumber, DungeonKeyItems.KeyLevel);
            this.AddOrUpdateShopItem(store, DungeonKeyItems.HellKeyNumber, DungeonKeyItems.KeyLevel);
        }
    }

    private void RemoveShopItems(ItemStorage store, short[] itemNumbers)
    {
        var toRemove = store.Items
            .Where(item => item.Definition?.Group == DungeonKeyItems.Group && itemNumbers.Contains(item.Definition.Number))
            .ToList();
        foreach (var item in toRemove)
        {
            store.Items.Remove(item);
        }
    }

    private void AddOrUpdateShopItem(ItemStorage store, short itemNumber, byte level)
    {
        var existing = store.Items.FirstOrDefault(item =>
            item.Definition?.Group == DungeonKeyItems.Group && item.Definition.Number == itemNumber);
        if (existing is not null)
        {
            existing.Level = level;
            existing.Durability = 1;
            return;
        }

        var usedSlots = store.Items.Select(item => item.ItemSlot).ToHashSet();
        byte slot = 0;
        while (usedSlots.Contains(slot) && slot < 120)
        {
            slot++;
        }

        var shopItem = this.Context.CreateNew<Item>();
        shopItem.Definition = this.Item(itemNumber);
        shopItem.Durability = 1;
        shopItem.Level = level;
        shopItem.ItemSlot = slot;
        store.Items.Add(shopItem);
    }

    private ItemDefinition Item(short number) =>
        this.GameConfiguration.Items.First(item => item.Group == DungeonKeyItems.Group && item.Number == number);

    private ItemDefinition NamedItem(string name) =>
        this.GameConfiguration.Items.First(item => item.Name == name);
}
