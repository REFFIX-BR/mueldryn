// <copyright file="MudreamT4AncientSets.cs" company="MUnique">
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
/// Registers Mudream T4 ancient sets that already have armor definitions in OpenMU
/// (Ashcrow, Eclipse, Iris, Valiant, Glorious).
/// </summary>
public class MudreamT4AncientSets : InitializerBase
{
    private readonly IDictionary<AttributeDefinition, IncreasableItemOption> _bonusOptions = new Dictionary<AttributeDefinition, IncreasableItemOption>();
    private readonly ItemOptionType _ancientBonusOptionType;
    private readonly ItemOptionType _ancientOptionType;

    /// <summary>
    /// Initializes a new instance of the <see cref="MudreamT4AncientSets"/> class.
    /// </summary>
    public MudreamT4AncientSets(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
        this._ancientBonusOptionType = this.GameConfiguration.ItemOptionTypes.First(iot => iot == ItemOptionTypes.AncientBonus);
        this._ancientOptionType = this.GameConfiguration.ItemOptionTypes.First(iot => iot == ItemOptionTypes.AncientOption);
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        this.EnsurePhysicalSet(
            "Umbral",
            57,
            34,
            includeHelm: true,
            discriminator: 1,
            minDmg: 400,
            maxDmg: 600,
            critRate: 14,
            critDmg: 50,
            excRate: 14,
            excDmg: 50,
            skillDmg: 100,
            flatDmg: 300,
            dmgPercent: 10,
            doubleRate: 4,
            ignoreDef: 1,
            maxLife: 2500);

        this.EnsureWizardSet(
            "Silent",
            58,
            35,
            includeHelm: true,
            discriminator: 1,
            minWiz: 400,
            maxWiz: 600,
            critRate: 14,
            critDmg: 50,
            excRate: 14,
            excDmg: 50,
            skillDmg: 100,
            flatWiz: 300,
            wizPercent: 10,
            doubleRate: 4,
            ignoreDef: 1,
            maxLife: 2500);

        this.EnsurePhysicalSet(
            "Shattered",
            59,
            36,
            includeHelm: true,
            discriminator: 1,
            minDmg: 400,
            maxDmg: 600,
            critRate: 14,
            critDmg: 50,
            excRate: 14,
            excDmg: 50,
            skillDmg: 100,
            flatDmg: 300,
            dmgPercent: 10,
            doubleRate: 4,
            ignoreDef: 1,
            maxLife: 2500);

        this.EnsureLuminousSet();

        this.EnsurePhysicalSet(
            "Celestial",
            60,
            38,
            includeHelm: true,
            discriminator: 1,
            minDmg: 400,
            maxDmg: 600,
            critRate: 14,
            critDmg: 50,
            excRate: 14,
            excDmg: 50,
            skillDmg: 100,
            flatDmg: 300,
            dmgPercent: 10,
            doubleRate: 4,
            ignoreDef: 1,
            maxLife: 2500);

        // Valiant has no helm in OpenMU.
        this.EnsurePhysicalSet(
            "Blazing",
            63,
            37,
            includeHelm: false,
            discriminator: 1,
            minDmg: 400,
            maxDmg: 600,
            critRate: 14,
            critDmg: 50,
            excRate: 14,
            excDmg: 50,
            skillDmg: 0,
            flatDmg: 300,
            dmgPercent: 10,
            doubleRate: 4,
            ignoreDef: 1,
            maxLife: 2500,
            deferFlatAndSkillToFull: true);

        this.EnsureWizardSet(
            "Stormborn",
            78,
            37,
            includeHelm: false,
            discriminator: 2,
            minWiz: 400,
            maxWiz: 600,
            critRate: 14,
            critDmg: 50,
            excRate: 14,
            excDmg: 50,
            skillDmg: 0,
            flatWiz: 300,
            wizPercent: 10,
            doubleRate: 4,
            ignoreDef: 1,
            maxLife: 2500,
            deferFlatAndSkillToFull: true);
    }

    private void EnsureLuminousSet()
    {
        if (this.GameConfiguration.ItemSetGroups.Any(s => s.Name == "Luminous"))
        {
            return;
        }

        var luminous = this.AddAncientSet(
            "Luminous",
            81,
            (Stats.TotalEnergy, 400f, AggregateType.AddRaw),
            (Stats.TotalEnergy, 400f, AggregateType.AddRaw),
            (Stats.TotalEnergy, 400f, AggregateType.AddRaw),
            (Stats.TotalEnergy, 400f, AggregateType.AddRaw),
            (Stats.TotalAgility, 300f, AggregateType.AddRaw),
            (Stats.TotalAgility, 300f, AggregateType.AddRaw),
            (Stats.TotalAgility, 300f, AggregateType.AddRaw),
            (Stats.TotalAgility, 300f, AggregateType.AddRaw),
            (Stats.DefenseBase, 1000f, AggregateType.AddFinal),
            (Stats.MaximumHealth, 4000f, AggregateType.AddRaw),
            (Stats.DefenseIncreaseWithEquippedShield, 0.10f, AggregateType.AddRaw));
        luminous.SetGuid(36, 2);
        this.AddArmorPieces(luminous, 36, includeHelm: true, discriminator: 2);
    }

    private void EnsurePhysicalSet(
        string name,
        short setNumber,
        short itemNumber,
        bool includeHelm,
        int discriminator,
        float minDmg,
        float maxDmg,
        float critRate,
        float critDmg,
        float excRate,
        float excDmg,
        float skillDmg,
        float flatDmg,
        float dmgPercent,
        float doubleRate,
        float ignoreDef,
        float maxLife,
        bool deferFlatAndSkillToFull = false)
    {
        if (this.GameConfiguration.ItemSetGroups.Any(s => s.Name == name))
        {
            return;
        }

        var options = new List<(AttributeDefinition, float, AggregateType)>
        {
            (Stats.MinimumPhysBaseDmg, minDmg, AggregateType.AddRaw),
            (Stats.MaximumPhysBaseDmg, maxDmg, AggregateType.AddRaw),
            (Stats.CriticalDamageChance, critRate / 100f, AggregateType.AddRaw),
            (Stats.CriticalDamageBonus, critDmg, AggregateType.AddRaw),
            (Stats.ExcellentDamageChance, excRate / 100f, AggregateType.AddRaw),
            (Stats.ExcellentDamageBonus, excDmg, AggregateType.AddRaw),
        };

        if (!deferFlatAndSkillToFull)
        {
            if (skillDmg > 0)
            {
                options.Add((Stats.SkillDamageBonus, skillDmg, AggregateType.AddRaw));
            }

            options.Add((Stats.FinalDamageBonus, flatDmg, AggregateType.AddRaw));
        }
        else
        {
            options.Add((Stats.FinalDamageBonus, flatDmg, AggregateType.AddRaw));
        }

        options.Add((Stats.PhysicalBaseDmgIncrease, 1f + (dmgPercent / 100f), AggregateType.Multiplicate));
        options.Add((Stats.DoubleDamageChance, doubleRate / 100f, AggregateType.AddRaw));
        options.Add((Stats.DefenseIgnoreChance, ignoreDef / 100f, AggregateType.AddRaw));
        options.Add((Stats.MaximumHealth, maxLife, AggregateType.AddRaw));

        var set = this.AddAncientSet(name, setNumber, options.ToArray());
        set.SetGuid(itemNumber, (byte)discriminator);
        this.AddArmorPieces(set, itemNumber, includeHelm, discriminator);
    }

    private void EnsureWizardSet(
        string name,
        short setNumber,
        short itemNumber,
        bool includeHelm,
        int discriminator,
        float minWiz,
        float maxWiz,
        float critRate,
        float critDmg,
        float excRate,
        float excDmg,
        float skillDmg,
        float flatWiz,
        float wizPercent,
        float doubleRate,
        float ignoreDef,
        float maxLife,
        bool deferFlatAndSkillToFull = false)
    {
        if (this.GameConfiguration.ItemSetGroups.Any(s => s.Name == name))
        {
            return;
        }

        var options = new List<(AttributeDefinition, float, AggregateType)>
        {
            (Stats.MinimumWizBaseDmg, minWiz, AggregateType.AddRaw),
            (Stats.MaximumWizBaseDmg, maxWiz, AggregateType.AddRaw),
            (Stats.CriticalDamageChance, critRate / 100f, AggregateType.AddRaw),
            (Stats.CriticalDamageBonus, critDmg, AggregateType.AddRaw),
            (Stats.ExcellentDamageChance, excRate / 100f, AggregateType.AddRaw),
            (Stats.ExcellentDamageBonus, excDmg, AggregateType.AddRaw),
        };

        if (!deferFlatAndSkillToFull)
        {
            if (skillDmg > 0)
            {
                options.Add((Stats.SkillDamageBonus, skillDmg, AggregateType.AddRaw));
            }

            options.Add((Stats.WizardryBaseDmg, flatWiz, AggregateType.AddRaw));
        }
        else
        {
            options.Add((Stats.WizardryBaseDmg, flatWiz, AggregateType.AddRaw));
        }

        options.Add((Stats.WizardryBaseDmgIncrease, 1f + (wizPercent / 100f), AggregateType.Multiplicate));
        options.Add((Stats.DoubleDamageChance, doubleRate / 100f, AggregateType.AddRaw));
        options.Add((Stats.DefenseIgnoreChance, ignoreDef / 100f, AggregateType.AddRaw));
        options.Add((Stats.MaximumHealth, maxLife, AggregateType.AddRaw));

        var set = this.AddAncientSet(name, setNumber, options.ToArray());
        set.SetGuid(itemNumber, (byte)discriminator);
        this.AddArmorPieces(set, itemNumber, includeHelm, discriminator);
    }

    private void AddArmorPieces(ItemSetGroup set, short itemNumber, bool includeHelm, int discriminator)
    {
        var pieces = new List<(short Number, ItemGroups Group, AttributeDefinition? BonusOption, int Discriminator)>();
        if (includeHelm)
        {
            pieces.Add((itemNumber, ItemGroups.Helm, Stats.TotalVitality, discriminator));
        }

        pieces.Add((itemNumber, ItemGroups.Armor, Stats.TotalVitality, discriminator));
        pieces.Add((itemNumber, ItemGroups.Pants, Stats.TotalVitality, discriminator));
        pieces.Add((itemNumber, ItemGroups.Gloves, Stats.TotalVitality, discriminator));
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

            // Avoid duplicate membership if re-applied.
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

        // Prefer reusing the existing ancient bonus option created by AncientSets, if present.
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
}
