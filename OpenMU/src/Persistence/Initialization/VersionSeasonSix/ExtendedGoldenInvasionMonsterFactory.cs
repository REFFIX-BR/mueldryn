// <copyright file="ExtendedGoldenInvasionMonsterFactory.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.Skills;

/// <summary>
/// Adds the extended Season 6 golden invasion monsters (493-502).
/// </summary>
internal static class ExtendedGoldenInvasionMonsterFactory
{
    private const ushort BoxOfKundunNumber = 11;
    private const byte BoxOfKundunGroup = 14;

    /// <summary>
    /// Adds all missing extended golden monsters and fixes the legacy Derkon designation.
    /// </summary>
    internal static void AddMissing(IContext context, GameConfiguration gameConfiguration)
    {
        if (gameConfiguration.Monsters.FirstOrDefault(m => m.Number == 79) is { } derkon)
        {
            derkon.Designation = "Golden Derkon";
        }

        AddMonster(context, gameConfiguration, 493, "Golden Dark Knight", 70, 18000, 250, 290, 180, 350, 120, 2);
        AddMonster(context, gameConfiguration, 494, "Golden Devil", 80, 26000, 300, 350, 220, 400, 145, 3);
        AddMonster(context, gameConfiguration, 495, "Golden Stone Golem", 90, 38000, 380, 430, 300, 470, 170, 3);
        AddMonster(context, gameConfiguration, 496, "Golden Crust", 100, 55000, 450, 520, 380, 540, 210, 4);
        AddMonster(context, gameConfiguration, 497, "Golden Satyros", 110, 75000, 550, 630, 460, 620, 250, 4);
        AddMonster(context, gameConfiguration, 498, "Golden Twin Tail", 120, 95000, 650, 740, 540, 700, 290, 5);
        AddMonster(context, gameConfiguration, 499, "Golden Iron Knight", 130, 125000, 760, 860, 650, 800, 340, 5);
        AddMonster(context, gameConfiguration, 500, "Golden Napin", 125, 110000, 700, 800, 600, 760, 320, 5);
        AddMonster(context, gameConfiguration, 501, "Great Golden Dragon", 140, 200000, 900, 1050, 800, 900, 400, 5, 2);
        AddMonster(context, gameConfiguration, 502, "Golden Rabbit", 60, 12000, 180, 220, 130, 300, 100, 1, 1);
    }

    private static void AddMonster(
        IContext context,
        GameConfiguration gameConfiguration,
        ushort number,
        string designation,
        float level,
        float health,
        float minimumDamage,
        float maximumDamage,
        float defense,
        float attackRate,
        float defenseRate,
        byte boxLevel,
        byte attackRange = 3)
    {
        if (gameConfiguration.Monsters.Any(m => m.Number == number))
        {
            return;
        }

        var monster = context.CreateNew<MonsterDefinition>();
        gameConfiguration.Monsters.Add(monster);
        monster.Number = (short)number;
        monster.Designation = designation;
        monster.MoveRange = 4;
        monster.AttackRange = attackRange;
        monster.AttackSkill = gameConfiguration.Skills.FirstOrDefault(s => s.Number == (short)SkillNumber.MonsterSkill);
        monster.ViewRange = 7;
        monster.MoveDelay = TimeSpan.FromMilliseconds(450);
        monster.AttackDelay = TimeSpan.FromMilliseconds(1600);
        monster.RespawnDelay = TimeSpan.FromMinutes(10);
        monster.Attribute = 2;
        monster.NumberOfMaximumItemDrops = 1;

        var attributes = new Dictionary<AttributeDefinition, float>
        {
            { Stats.Level, level },
            { Stats.MaximumHealth, health },
            { Stats.MinimumPhysBaseDmg, minimumDamage },
            { Stats.MaximumPhysBaseDmg, maximumDamage },
            { Stats.DefenseBase, defense },
            { Stats.AttackRatePvm, attackRate },
            { Stats.DefenseRatePvm, defenseRate },
            { Stats.PoisonResistance, 15f / 255 },
            { Stats.IceResistance, 15f / 255 },
            { Stats.WaterResistance, 15f / 255 },
            { Stats.FireResistance, 15f / 255 },
        };
        monster.AddAttributes(attributes, context, gameConfiguration);

        var box = gameConfiguration.Items.First(item => item.Group == BoxOfKundunGroup && item.Number == BoxOfKundunNumber);
        var itemDrop = context.CreateNew<DropItemGroup>();
        itemDrop.Chance = 1;
        itemDrop.ItemLevel = (byte)(7 + boxLevel);
        itemDrop.Description = $"Box of Kundun +{boxLevel} from {designation}";
        itemDrop.Monster = monster;
        itemDrop.PossibleItems.Add(box);
        monster.DropItemGroups.Add(itemDrop);
        gameConfiguration.DropItemGroups.Add(itemDrop);
    }
}
