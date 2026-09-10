// <copyright file="MudreamT5ToT8ArmorsAndSets.cs" company="MUnique">
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
/// Registers Mudream T5–T8 armor definitions and ancient sets (client SetItemType / SetItemOption).
/// </summary>
public class MudreamT5ToT8ArmorsAndSets : ArmorInitializerBase
{
    private readonly IDictionary<AttributeDefinition, IncreasableItemOption> _bonusOptions = new Dictionary<AttributeDefinition, IncreasableItemOption>();
    private readonly ItemOptionType _ancientBonusOptionType;
    private readonly ItemOptionType _ancientOptionType;

    /// <summary>
    /// Initializes a new instance of the <see cref="MudreamT5ToT8ArmorsAndSets"/> class.
    /// </summary>
    public MudreamT5ToT8ArmorsAndSets(IContext context, GameConfiguration gameConfiguration)
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
        this.EnsureArmors();
        this.EnsureAncientSets();
    }

    private void EnsureArmors()
    {
        this.EnsureSet(77, "Ravager", ClassMask.Dk, true, true, 5);
        this.EnsureSet(78, "Netherion", ClassMask.Dw, true, true, 5);
        this.EnsureSet(79, "Sylvaria", ClassMask.Fe, true, true, 5);
        this.EnsureSet(80, "Varkrul", ClassMask.Dl, true, true, 5);
        this.EnsureSet(81, "Nymberis", ClassMask.Su, true, true, 5);
        this.EnsureSet(82, "Bloodgrin", ClassMask.Rf, true, false, 5);
        this.EnsureSet(89, "Aetheron", ClassMask.Mg, false, true, 5);

        this.EnsureSet(83, "Gravion", ClassMask.Dk, true, true, 6);
        this.EnsureSet(84, "Luxorion", ClassMask.Dw, true, true, 6);
        this.EnsureSet(85, "Crimessia", ClassMask.Fe, true, true, 6);
        this.EnsureSet(86, "Thalrion", ClassMask.Dl, true, true, 6);
        this.EnsureSet(87, "Virelith", ClassMask.Su, true, true, 6);
        this.EnsureSet(88, "Carnavor", ClassMask.Rf, true, false, 6);
        this.EnsureSet(90, "Nexarion", ClassMask.Mg, false, true, 6);

        this.EnsureSet(101, "Valmor", ClassMask.Dk, true, true, 7);
        this.EnsureSet(107, "Thalrok", ClassMask.Dw, true, true, 7);
        this.EnsureSet(104, "Skylorn", ClassMask.Fe, true, true, 7);
        this.EnsureSet(106, "Grimshade", ClassMask.Dl, true, true, 7);
        this.EnsureSet(100, "Asterion", ClassMask.Su, true, true, 7);
        this.EnsureSet(108, "Eldran", ClassMask.Rf, true, false, 7);
        this.EnsureSet(102, "Brimstar", ClassMask.Mg, false, true, 7);

        this.EnsureSet(111, "Valther", ClassMask.Dk, true, true, 8);
        this.EnsureSet(117, "Zerion", ClassMask.Dw, true, true, 8);
        this.EnsureSet(114, "Nexor", ClassMask.Fe, true, true, 8);
        this.EnsureSet(116, "Morvyn", ClassMask.Dl, true, true, 8);
        this.EnsureSet(110, "Kaelor", ClassMask.Su, true, true, 8);
        this.EnsureSet(118, "Vaelix", ClassMask.Rf, true, false, 8);
        this.EnsureSet(112, "Korvax", ClassMask.Mg, false, true, 8);
    }

    private void EnsureAncientSets()
    {
        this.EnsurePhysical("Dread", 64, 77, true, true, 1, 5, true);
        this.EnsureWizard("Voidborn", 65, 78, true, true, 1, 5, true);
        this.EnsurePhysical("Verdant", 66, 79, true, true, 1, 5, true);
        this.EnsureDefense("Feyguard", 82, 79, true, true, 2, 5);
        this.EnsurePhysical("Ironclad", 67, 80, true, true, 1, 5, true);
        this.EnsureWizard("Duskwraith", 68, 81, true, true, 1, 5, true);
        this.EnsurePhysical("Savage", 69, 82, true, false, 1, 5, false);
        this.EnsurePhysical("Arcane", 70, 89, false, true, 1, 5, false);
        this.EnsureWizard("Astral", 79, 89, false, true, 2, 5, false);

        this.EnsurePhysical("Forsaken", 71, 83, true, true, 1, 6, true);
        this.EnsureWizard("Empyreal", 72, 84, true, true, 1, 6, true);
        this.EnsurePhysical("Crimson", 73, 85, true, true, 1, 6, true);
        this.EnsureDefense("Scarlet", 83, 85, true, true, 2, 6);
        this.EnsurePhysical("Imperial", 74, 86, true, true, 1, 6, true);
        this.EnsureWizard("Wicked", 75, 87, true, true, 1, 6, true);
        this.EnsurePhysical("Horned", 76, 88, true, false, 1, 6, false);
        this.EnsurePhysical("Crystal", 77, 90, false, true, 1, 6, false);
        this.EnsureWizard("Oblivion", 80, 90, false, true, 2, 6, false);

        this.EnsureWizard("Asterion", 84, 100, true, true, 1, 7, true);
        this.EnsurePhysical("Valmor", 85, 101, true, true, 1, 7, true);
        this.EnsurePhysical("Skylorn", 86, 104, true, true, 1, 7, true);
        this.EnsureDefense("Mystral", 92, 104, true, true, 2, 7);
        this.EnsurePhysical("Grimshade", 87, 106, true, true, 1, 7, true);
        this.EnsureWizard("Thalrok", 88, 107, true, true, 1, 7, true);
        this.EnsurePhysical("Eldran", 89, 108, true, false, 1, 7, false);
        this.EnsurePhysical("Brimstar", 90, 102, false, true, 1, 7, false);
        this.EnsureWizard("Kargoth", 91, 102, false, true, 2, 7, false);

        this.EnsureWizard("Kaelor", 93, 110, true, true, 1, 8, true);
        this.EnsurePhysical("Valther", 94, 111, true, true, 1, 8, true);
        this.EnsurePhysical("Nexor", 95, 114, true, true, 1, 8, true);
        this.EnsureDefense("Azrion", 101, 114, true, true, 2, 8);
        this.EnsurePhysical("Morvyn", 96, 116, true, true, 1, 8, true);
        this.EnsureWizard("Zerion", 97, 117, true, true, 1, 8, true);
        this.EnsurePhysical("Vaelix", 98, 118, true, false, 1, 8, false);
        this.EnsurePhysical("Korvax", 99, 112, false, true, 1, 8, false);
        this.EnsureWizard("Nythera", 100, 112, false, true, 2, 8, false);
    }

    private void EnsureSet(byte number, string baseName, ClassMask cls, bool includeHelm, bool includeGloves, int tier)
    {
        var (drop, helmDef, armorDef, pantsDef, glovesDef, bootsDef, str, agi) = TierStats(tier);
        if (includeHelm)
        {
            this.EnsurePiece(number, 2, 2, 2, $"{baseName} Helm", drop, helmDef, 90, str, agi, cls);
        }

        this.EnsurePiece(number, 3, 2, 3, $"{baseName} Armor", drop, armorDef, 90, str, agi, cls);
        this.EnsurePiece(number, 4, 2, 2, $"{baseName} Pants", drop, pantsDef, 90, str, agi, cls);
        if (includeGloves)
        {
            this.EnsurePiece(number, 5, 2, 2, $"{baseName} Gloves", drop, glovesDef, 90, str, agi, cls);
        }

        this.EnsurePiece(number, 6, 2, 2, $"{baseName} Boots", drop, bootsDef, 90, str, agi, cls);
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

    private static (byte Drop, int Helm, int Armor, int Pants, int Gloves, int Boots, int Str, int Agi) TierStats(int tier)
    {
        return tier switch
        {
            5 => (95, 45, 70, 55, 40, 40, 200, 60),
            6 => (110, 60, 90, 70, 55, 55, 240, 70),
            7 => (125, 75, 110, 85, 70, 70, 280, 80),
            _ => (140, 90, 130, 100, 85, 85, 320, 90),
        };
    }

    private void EnsurePhysical(string name, short setNumber, short itemNumber, bool includeHelm, bool includeGloves, int discriminator, int tier, bool hasSkill)
    {
        if (this.GameConfiguration.ItemSetGroups.Any(s => s.Name == name))
        {
            return;
        }

        var (min, max, critR, critD, excR, excD, skill, flat, pct, dbl, ign, hp) = CombatTier(tier);
        var options = new List<(AttributeDefinition, float, AggregateType)>
        {
            (Stats.MinimumPhysBaseDmg, min, AggregateType.AddRaw),
            (Stats.MaximumPhysBaseDmg, max, AggregateType.AddRaw),
            (Stats.CriticalDamageChance, critR / 100f, AggregateType.AddRaw),
            (Stats.CriticalDamageBonus, critD, AggregateType.AddRaw),
            (Stats.ExcellentDamageChance, excR / 100f, AggregateType.AddRaw),
            (Stats.ExcellentDamageBonus, excD, AggregateType.AddRaw),
        };
        if (hasSkill)
        {
            options.Add((Stats.SkillDamageBonus, skill, AggregateType.AddRaw));
        }

        options.Add((Stats.FinalDamageBonus, flat, AggregateType.AddRaw));
        options.Add((Stats.PhysicalBaseDmgIncrease, 1f + (pct / 100f), AggregateType.Multiplicate));
        options.Add((Stats.DoubleDamageChance, dbl / 100f, AggregateType.AddRaw));
        options.Add((Stats.DefenseIgnoreChance, ign / 100f, AggregateType.AddRaw));
        options.Add((Stats.MaximumHealth, hp, AggregateType.AddRaw));

        var set = this.AddAncientSet(name, setNumber, options.ToArray());
        set.SetGuid(itemNumber, (byte)discriminator);
        this.AddArmorPieces(set, itemNumber, includeHelm, includeGloves, discriminator);
    }

    private void EnsureWizard(string name, short setNumber, short itemNumber, bool includeHelm, bool includeGloves, int discriminator, int tier, bool hasSkill)
    {
        if (this.GameConfiguration.ItemSetGroups.Any(s => s.Name == name))
        {
            return;
        }

        var (min, max, critR, critD, excR, excD, skill, flat, pct, dbl, ign, hp) = CombatTier(tier);
        var options = new List<(AttributeDefinition, float, AggregateType)>
        {
            (Stats.MinimumWizBaseDmg, min, AggregateType.AddRaw),
            (Stats.MaximumWizBaseDmg, max, AggregateType.AddRaw),
            (Stats.CriticalDamageChance, critR / 100f, AggregateType.AddRaw),
            (Stats.CriticalDamageBonus, critD, AggregateType.AddRaw),
            (Stats.ExcellentDamageChance, excR / 100f, AggregateType.AddRaw),
            (Stats.ExcellentDamageBonus, excD, AggregateType.AddRaw),
        };
        if (hasSkill)
        {
            options.Add((Stats.SkillDamageBonus, skill, AggregateType.AddRaw));
        }

        options.Add((Stats.WizardryBaseDmg, flat, AggregateType.AddRaw));
        options.Add((Stats.WizardryBaseDmgIncrease, 1f + (pct / 100f), AggregateType.Multiplicate));
        options.Add((Stats.DoubleDamageChance, dbl / 100f, AggregateType.AddRaw));
        options.Add((Stats.DefenseIgnoreChance, ign / 100f, AggregateType.AddRaw));
        options.Add((Stats.MaximumHealth, hp, AggregateType.AddRaw));

        var set = this.AddAncientSet(name, setNumber, options.ToArray());
        set.SetGuid(itemNumber, (byte)discriminator);
        this.AddArmorPieces(set, itemNumber, includeHelm, includeGloves, discriminator);
    }

    private void EnsureDefense(string name, short setNumber, short itemNumber, bool includeHelm, bool includeGloves, int discriminator, int tier)
    {
        if (this.GameConfiguration.ItemSetGroups.Any(s => s.Name == name))
        {
            return;
        }

        var (str, agi, def, hp, shield) = DefenseTier(tier);
        var set = this.AddAncientSet(
            name,
            setNumber,
            (Stats.TotalStrength, str, AggregateType.AddRaw),
            (Stats.TotalStrength, str, AggregateType.AddRaw),
            (Stats.TotalStrength, str, AggregateType.AddRaw),
            (Stats.TotalStrength, str, AggregateType.AddRaw),
            (Stats.TotalAgility, agi, AggregateType.AddRaw),
            (Stats.TotalAgility, agi, AggregateType.AddRaw),
            (Stats.TotalAgility, agi, AggregateType.AddRaw),
            (Stats.TotalAgility, agi, AggregateType.AddRaw),
            (Stats.DefenseBase, def, AggregateType.AddFinal),
            (Stats.MaximumHealth, hp, AggregateType.AddRaw),
            (Stats.DefenseIncreaseWithEquippedShield, shield / 100f, AggregateType.AddRaw));
        set.SetGuid(itemNumber, (byte)discriminator);
        this.AddArmorPieces(set, itemNumber, includeHelm, includeGloves, discriminator);
    }

    private static (float Min, float Max, float CritR, float CritD, float ExcR, float ExcD, float Skill, float Flat, float Pct, float Dbl, float Ign, float Hp) CombatTier(int tier)
    {
        return tier switch
        {
            5 => (600, 800, 16, 75, 16, 75, 150, 400, 15, 6, 2, 3500),
            6 => (800, 1000, 18, 100, 18, 100, 200, 500, 20, 8, 3, 4500),
            7 => (1000, 1200, 20, 125, 20, 125, 250, 600, 25, 10, 4, 5500),
            _ => (1200, 1400, 22, 150, 22, 150, 300, 700, 30, 12, 5, 6500),
        };
    }

    private static (float Str, float Agi, float Def, float Hp, float Shield) DefenseTier(int tier)
    {
        return tier switch
        {
            5 => (500, 400, 1500, 5000, 15),
            6 => (600, 500, 2000, 6000, 20),
            7 => (700, 600, 2500, 7000, 25),
            _ => (800, 700, 3000, 8000, 30),
        };
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
            ClassMask.Dk => (0, 1, 0, 0, 0, 0, 0),
            ClassMask.Dw => (1, 0, 0, 0, 0, 0, 0),
            ClassMask.Fe => (0, 0, 1, 0, 0, 0, 0),
            ClassMask.Dl => (0, 0, 0, 0, 1, 0, 0),
            ClassMask.Su => (0, 0, 0, 0, 0, 1, 0),
            ClassMask.Rf => (0, 0, 0, 0, 0, 0, 1),
            ClassMask.Mg => (0, 0, 0, 1, 0, 0, 0),
            _ => (0, 0, 0, 0, 0, 0, 0),
        };
    }

    private enum ClassMask
    {
        Dk,
        Dw,
        Fe,
        Dl,
        Su,
        Rf,
        Mg,
    }
}
