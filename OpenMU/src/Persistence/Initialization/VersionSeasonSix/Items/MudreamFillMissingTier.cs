// <copyright file="MudreamFillMissingTier.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Persistence.Initialization.Items;

/// <summary>
/// Fills Mudream tier gaps that vanilla OpenMU / earlier Mudream updates missed:
/// RF T3 Vega/Chamer (Sacred #59), SU T4 Aurelia+Mythic (#75/61), RF T4 Drakzar+Thorned (#76/62).
/// </summary>
public class MudreamFillMissingTier : ArmorInitializerBase
{
    private readonly IDictionary<AttributeDefinition, IncreasableItemOption> _bonusOptions = new Dictionary<AttributeDefinition, IncreasableItemOption>();
    private readonly ItemOptionType _ancientBonusOptionType;
    private readonly ItemOptionType _ancientOptionType;

    /// <summary>
    /// Initializes a new instance of the <see cref="MudreamFillMissingTier"/> class.
    /// </summary>
    public MudreamFillMissingTier(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
        this._ancientBonusOptionType = this.GameConfiguration.ItemOptionTypes.First(iot => iot == ItemOptionTypes.AncientBonus);
        this._ancientOptionType = this.GameConfiguration.ItemOptionTypes.First(iot => iot == ItemOptionTypes.AncientOption);
    }

    /// <inheritdoc />
    protected override byte MaximumArmorLevel => 15;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();
        this.EnsureT4Armors();
        this.EnsureT4AncientSets();
        this.EnsureRfT3AncientSets();
    }

    private void EnsureT4Armors()
    {
        // Aurelia (SU) — full set including gloves.
        this.EnsurePiece(75, 2, 2, 2, "Aurelia Helm", 67, 24, 72, 52, 16, ClassMask.Su);
        this.EnsurePiece(75, 3, 2, 3, "Aurelia Armor", 67, 38, 72, 52, 16, ClassMask.Su);
        this.EnsurePiece(75, 4, 2, 2, "Aurelia Pants", 67, 30, 72, 52, 16, ClassMask.Su);
        this.EnsurePiece(75, 5, 2, 2, "Aurelia Gloves", 67, 19, 72, 52, 16, ClassMask.Su);
        this.EnsurePiece(75, 6, 2, 2, "Aurelia Boots", 67, 20, 72, 52, 16, ClassMask.Su);

        // Drakzar (RF) — no gloves (client SetItemType).
        this.EnsurePiece(76, 2, 2, 2, "Drakzar Helm", 67, 27, 72, 160, 50, ClassMask.Rf);
        this.EnsurePiece(76, 3, 2, 3, "Drakzar Armor", 67, 40, 72, 160, 50, ClassMask.Rf);
        this.EnsurePiece(76, 4, 2, 2, "Drakzar Pants", 67, 32, 72, 160, 50, ClassMask.Rf);
        this.EnsurePiece(76, 6, 2, 2, "Drakzar Boots", 67, 22, 72, 160, 50, ClassMask.Rf);
    }

    private void EnsureT4AncientSets()
    {
        // Mythic — SU wizard set on Aurelia (same T4 numbers as Silent).
        if (!this.GameConfiguration.ItemSetGroups.Any(s => s.Name == "Mythic"))
        {
            var set = this.AddAncientSet(
                "Mythic",
                61,
                (Stats.MinimumWizBaseDmg, 400f, AggregateType.AddRaw),
                (Stats.MaximumWizBaseDmg, 600f, AggregateType.AddRaw),
                (Stats.CriticalDamageChance, 0.14f, AggregateType.AddRaw),
                (Stats.CriticalDamageBonus, 50f, AggregateType.AddRaw),
                (Stats.ExcellentDamageChance, 0.14f, AggregateType.AddRaw),
                (Stats.ExcellentDamageBonus, 50f, AggregateType.AddRaw),
                (Stats.SkillDamageBonus, 100f, AggregateType.AddRaw),
                (Stats.WizardryBaseDmg, 300f, AggregateType.AddRaw),
                (Stats.WizardryBaseDmgIncrease, 1.10f, AggregateType.Multiplicate),
                (Stats.DoubleDamageChance, 0.04f, AggregateType.AddRaw),
                (Stats.DefenseIgnoreChance, 0.01f, AggregateType.AddRaw),
                (Stats.MaximumHealth, 2500f, AggregateType.AddRaw));
            set.SetGuid(75, 1);
            this.AddArmorPieces(set, 75, includeHelm: true, includeGloves: true, discriminator: 1);
        }

        // Thorned — RF physical set on Drakzar (same T4 numbers as Blazing / Thorned client).
        if (!this.GameConfiguration.ItemSetGroups.Any(s => s.Name == "Thorned"))
        {
            var set = this.AddAncientSet(
                "Thorned",
                62,
                (Stats.MinimumPhysBaseDmg, 400f, AggregateType.AddRaw),
                (Stats.MaximumPhysBaseDmg, 600f, AggregateType.AddRaw),
                (Stats.CriticalDamageChance, 0.14f, AggregateType.AddRaw),
                (Stats.CriticalDamageBonus, 50f, AggregateType.AddRaw),
                (Stats.ExcellentDamageChance, 0.14f, AggregateType.AddRaw),
                (Stats.ExcellentDamageBonus, 50f, AggregateType.AddRaw),
                (Stats.FinalDamageBonus, 300f, AggregateType.AddRaw),
                (Stats.PhysicalBaseDmgIncrease, 1.10f, AggregateType.Multiplicate),
                (Stats.DoubleDamageChance, 0.04f, AggregateType.AddRaw),
                (Stats.DefenseIgnoreChance, 0.01f, AggregateType.AddRaw),
                (Stats.MaximumHealth, 2500f, AggregateType.AddRaw));
            set.SetGuid(76, 1);
            this.AddArmorPieces(set, 76, includeHelm: true, includeGloves: false, discriminator: 1);
        }
    }

    private void EnsureRfT3AncientSets()
    {
        // Vega — Sacred #59 disc 1 (helm/armor/pants + Sacred Glove).
        if (!this.GameConfiguration.ItemSetGroups.Any(s => s.Name == "Vega"))
        {
            var vega = this.AddAncientSet(
                "Vega",
                37,
                (Stats.MaximumHealth, 50f, AggregateType.AddRaw),
                (Stats.TotalVitality, 50f, AggregateType.AddRaw),
                (Stats.MaximumPhysBaseDmg, 30f, AggregateType.AddRaw),
                (Stats.ExcellentDamageChance, 0.15f, AggregateType.AddRaw),
                (Stats.DoubleDamageChance, 0.05f, AggregateType.AddRaw),
                (Stats.DefenseIgnoreChance, 0.05f, AggregateType.AddRaw));
            vega.SetGuid(59, 1);
            this.AddItems(
                vega,
                (59, ItemGroups.Helm, Stats.TotalVitality, 1),
                (59, ItemGroups.Armor, Stats.TotalVitality, 1),
                (59, ItemGroups.Pants, Stats.TotalVitality, 1),
                (32, ItemGroups.Swords, Stats.TotalVitality, 1)); // Sacred Glove (group 0)
        }

        // Chamer — Sacred #59 disc 2 (armor/pants/boots + Sacred Glove).
        if (!this.GameConfiguration.ItemSetGroups.Any(s => s.Name == "Chamer"))
        {
            var chamer = this.AddAncientSet(
                "Chamer",
                38,
                (Stats.MaximumMana, 50f, AggregateType.AddRaw),
                (Stats.DoubleDamageChance, 0.05f, AggregateType.AddRaw),
                (Stats.FinalDamageBonus, 30f, AggregateType.AddRaw),
                (Stats.CriticalDamageBonus, 30f, AggregateType.AddRaw),
                (Stats.ExcellentDamageChance, 0.15f, AggregateType.AddRaw),
                (Stats.SkillDamageBonus, 30f, AggregateType.AddRaw),
                (Stats.ExcellentDamageBonus, 20f, AggregateType.AddRaw));
            chamer.SetGuid(59, 2);
            this.AddItems(
                chamer,
                (59, ItemGroups.Armor, Stats.TotalVitality, 2),
                (59, ItemGroups.Pants, Stats.TotalVitality, 2),
                (59, ItemGroups.Boots, Stats.TotalVitality, 2),
                (32, ItemGroups.Swords, Stats.TotalVitality, 2)); // Sacred Glove
        }
    }

    private void EnsurePiece(byte number, byte slot, byte width, byte height, string name, byte dropLevel, int defense, byte durability, int str, int agi, ClassMask cls)
    {
        var group = (byte)(slot + 5);
        if (this.GameConfiguration.Items.Any(i => i.Group == group && i.Number == number))
        {
            return;
        }

        var (dw, dk, fe, mg, dl, su, rf) = ToFlags(cls);
        var item = this.CreateArmor(
            number, slot, width, height, name, dropLevel, defense, durability,
            0, str, agi, 0, 0, 0,
            dw, dk, fe, mg, dl, su, rf);
        item.DropsFromMonsters = false;
    }

    private void AddArmorPieces(ItemSetGroup set, short itemNumber, bool includeHelm, bool includeGloves, int discriminator)
    {
        var pieces = new List<(short Number, ItemGroups Group, AttributeDefinition? BonusOption, int Discriminator)>();
        if (includeHelm)
        {
            pieces.Add((itemNumber, ItemGroups.Helm, Stats.TotalVitality, discriminator));
        }

        pieces.Add((itemNumber, ItemGroups.Armor, Stats.TotalVitality, discriminator));
        pieces.Add((itemNumber, ItemGroups.Pants, Stats.TotalVitality, discriminator));
        if (includeGloves)
        {
            pieces.Add((itemNumber, ItemGroups.Gloves, Stats.TotalVitality, discriminator));
        }

        pieces.Add((itemNumber, ItemGroups.Boots, Stats.TotalVitality, discriminator));
        this.AddItems(set, pieces.ToArray());
    }

    private void AddItems(ItemSetGroup set, params (short Number, ItemGroups Group, AttributeDefinition? BonusOption, int Discriminator)[] items)
    {
        foreach (var itemTuple in items)
        {
            var item = this.GameConfiguration.Items.FirstOrDefault(i => i.Number == itemTuple.Number && i.Group == (byte)itemTuple.Group);
            if (item is null)
            {
                continue;
            }

            if (set.Items.Any(i => i.ItemDefinition == item && i.AncientSetDiscriminator == itemTuple.Discriminator))
            {
                continue;
            }

            var itemOfSet = this.Context.CreateNew<ItemOfItemSet>();
            itemOfSet.AncientSetDiscriminator = itemTuple.Discriminator;
            itemOfSet.ItemDefinition = item;
            itemOfSet.BonusOption = this.CreateAncientBonusOption(itemTuple.BonusOption);
            itemOfSet.ItemSetGroup = set;
            itemOfSet.SetGuid(item.Group, item.Number, (byte)itemTuple.Discriminator);
            set.Items.Add(itemOfSet);
            if (!item.PossibleItemSetGroups.Contains(set))
            {
                item.PossibleItemSetGroups.Add(set);
            }
        }
    }

    private ItemSetGroup AddAncientSet(string name, short setNumber, params (AttributeDefinition Attribute, float Value, AggregateType AggregateType)[] ancientOptions)
    {
        var set = this.Context.CreateNew<ItemSetGroup>();
        set.SetGuid(setNumber);
        set.Name = name;
        set.CountDistinct = true;
        set.MinimumItemCount = 2;
        int number = 1;
        var options = this.Context.CreateNew<ItemOptionDefinition>();
        options.SetGuid(ItemOptionDefinitionNumbers.AncientOption, setNumber, (byte)number);
        options.Name = $"{name} (Ancient Set)";
        set.Options = options;
        foreach (var optionTuple in ancientOptions)
        {
            var option = this.Context.CreateNew<IncreasableItemOption>();
            option.SetGuid(ItemOptionDefinitionNumbers.AncientOption, setNumber, (byte)number);
            option.Number = number++;
            option.OptionType = this._ancientOptionType;
            option.PowerUpDefinition = this.Context.CreateNew<PowerUpDefinition>();
            option.PowerUpDefinition.TargetAttribute = optionTuple.Attribute.GetPersistent(this.GameConfiguration);
            option.PowerUpDefinition.Boost = this.Context.CreateNew<PowerUpDefinitionValue>();
            option.PowerUpDefinition.Boost.ConstantValue.AggregateType = optionTuple.AggregateType;
            option.PowerUpDefinition.Boost.ConstantValue.Value = optionTuple.Value;
            options.PossibleOptions.Add(option);
        }

        this.GameConfiguration.ItemSetGroups.Add(set);
        this.GameConfiguration.ItemOptions.Add(options);
        return set;
    }

    private IncreasableItemOption? CreateAncientBonusOption(AttributeDefinition? attribute)
    {
        if (attribute is null)
        {
            return null;
        }

        if (this._bonusOptions.TryGetValue(attribute, out var option))
        {
            return option;
        }

        var existingDefinition = this.GameConfiguration.ItemOptions
            .FirstOrDefault(o => o.Name == $"Ancient Bonus of {attribute.Designation}");
        if (existingDefinition?.PossibleOptions.FirstOrDefault() is IncreasableItemOption existingOption)
        {
            this._bonusOptions.Add(attribute, existingOption);
            return existingOption;
        }

        var optionDefinition = this.Context.CreateNew<ItemOptionDefinition>();
        optionDefinition.SetGuid(ItemOptionDefinitionNumbers.AncientBonus, attribute.Id.ExtractFirstTwoBytes());
        optionDefinition.Name = $"Ancient Bonus of {attribute.Designation}";
        optionDefinition.AddsRandomly = false;
        optionDefinition.MaximumOptionsPerItem = 1;
        this.GameConfiguration.ItemOptions.Add(optionDefinition);

        option = this.Context.CreateNew<IncreasableItemOption>();
        option.SetGuid(ItemOptionDefinitionNumbers.AncientBonus, attribute.Id.ExtractFirstTwoBytes());
        option.OptionType = this._ancientBonusOptionType;
        option.PowerUpDefinition = this.Context.CreateNew<PowerUpDefinition>();
        option.PowerUpDefinition.TargetAttribute = attribute.GetPersistent(this.GameConfiguration);

        var level1 = this.Context.CreateNew<ItemOptionOfLevel>();
        level1.Level = 1;
        level1.PowerUpDefinition = this.Context.CreateNew<PowerUpDefinition>();
        level1.PowerUpDefinition.TargetAttribute = attribute.GetPersistent(this.GameConfiguration);
        level1.PowerUpDefinition.Boost = this.Context.CreateNew<PowerUpDefinitionValue>();
        level1.PowerUpDefinition.Boost.ConstantValue.Value = 5;

        var level2 = this.Context.CreateNew<ItemOptionOfLevel>();
        level2.Level = 2;
        level2.PowerUpDefinition = this.Context.CreateNew<PowerUpDefinition>();
        level2.PowerUpDefinition.TargetAttribute = attribute.GetPersistent(this.GameConfiguration);
        level2.PowerUpDefinition.Boost = this.Context.CreateNew<PowerUpDefinitionValue>();
        level2.PowerUpDefinition.Boost.ConstantValue.Value = 10;

        option.LevelDependentOptions.Add(level1);
        option.LevelDependentOptions.Add(level2);
        optionDefinition.PossibleOptions.Add(option);
        this._bonusOptions.Add(attribute, option);
        return option;
    }

    private static (int Dw, int Dk, int Fe, int Mg, int Dl, int Su, int Rf) ToFlags(ClassMask cls)
    {
        return cls switch
        {
            ClassMask.Su => (0, 0, 0, 0, 0, 1, 0),
            ClassMask.Rf => (0, 0, 0, 0, 0, 0, 1),
            _ => (0, 0, 0, 0, 0, 0, 0),
        };
    }

    private enum ClassMask
    {
        Su,
        Rf,
    }
}
