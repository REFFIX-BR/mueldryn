// <copyright file="MudreamCustomBossFactory.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.Skills;

/// <summary>
/// Registers Mudream custom bosses (client CustomMonsters.xml / BossHealthBar type=3 IDs)
/// with invented HP/ATK/DEF mirrored from vanilla OpenMU bosses (no Mudream server stats in dump).
/// </summary>
internal static class MudreamCustomBossFactory
{
    private const ushort BoxOfKundunNumber = 11;
    private const byte BoxOfKundunGroup = 14;

    /// <summary>
    /// Monster numbers added by this factory (for boss life bar / docs).
    /// </summary>
    internal static readonly short[] BossNumbers =
    [
        583, 588, 592, 602, 603, 604, 611, 612, 618, 619, 623, 624, 694, 702, 724, 733, 753,
    ];

    /// <summary>
    /// Adds all missing Mudream custom boss definitions.
    /// </summary>
    internal static void AddMissing(IContext context, GameConfiguration gameConfiguration)
    {
        // Kundun-tier (classic ~5M HP style)
        AddBoss(context, gameConfiguration, 604, "Golden Kundun", 147, 5_000_000, 2000, 2500, 1500, 2000, 1000, 4, 10);
        AddBoss(context, gameConfiguration, 694, "Netherlord", 145, 4_500_000, 1900, 2400, 1400, 1900, 950, 4, 8);

        // Erohim-tier
        AddBoss(context, gameConfiguration, 602, "Golden Erohim", 128, 3_200_000, 1550, 2100, 1050, 1550, 850, 4, 6);
        AddBoss(context, gameConfiguration, 612, "Infernal Overlord", 140, 3_800_000, 1800, 2300, 1200, 1800, 900, 4, 8);
        AddBoss(context, gameConfiguration, 702, "Abbadon", 142, 3_600_000, 1750, 2250, 1180, 1750, 880, 4, 7);
        AddBoss(context, gameConfiguration, 724, "Nefarius", 143, 3_700_000, 1780, 2280, 1190, 1780, 890, 4, 7);
        AddBoss(context, gameConfiguration, 733, "Obsidar", 144, 3_900_000, 1850, 2350, 1220, 1850, 920, 4, 8);

        // Map-native high bosses (spawnable anywhere for GM)
        AddBoss(context, gameConfiguration, 583, "Lord Silvester", 140, 4_000_000, 1850, 2350, 1300, 1850, 1000, 4, 8);
        AddBoss(context, gameConfiguration, 588, "Lord of Ferea", 142, 4_200_000, 1900, 2400, 1350, 1900, 1050, 4, 8);
        AddBoss(context, gameConfiguration, 592, "Nix", 138, 3_500_000, 1700, 2200, 1250, 1700, 950, 4, 7);

        // Mid-high / themed
        AddBoss(context, gameConfiguration, 611, "Frozen King", 130, 2_800_000, 1400, 1900, 1100, 1500, 800, 3, 6);
        AddBoss(context, gameConfiguration, 618, "Pharaoh", 132, 2_900_000, 1450, 1950, 1120, 1520, 820, 3, 6);
        AddBoss(context, gameConfiguration, 619, "Lord Of Darkness", 135, 3_100_000, 1500, 2000, 1150, 1600, 850, 4, 7);
        AddBoss(context, gameConfiguration, 623, "Firestorm Dragonlord", 138, 3_300_000, 1600, 2100, 1180, 1650, 870, 4, 8);
        AddBoss(context, gameConfiguration, 624, "Flamestone Giant", 136, 3_600_000, 1400, 1800, 1600, 1500, 1100, 3, 4); // tank DEF

        // Golden Hell Maine (mirror Hell Maine, buffed)
        AddBoss(context, gameConfiguration, 603, "Golden Hell Maine", 110, 250_000, 650, 750, 600, 950, 350, 3, 5);

        // Event mid
        AddBoss(context, gameConfiguration, 753, "Jack O'Lantern", 100, 180_000, 500, 650, 450, 700, 280, 3, 4);
    }

    private static void AddBoss(
        IContext context,
        GameConfiguration gameConfiguration,
        short number,
        string designation,
        float level,
        float health,
        float minimumDamage,
        float maximumDamage,
        float defense,
        float attackRate,
        float defenseRate,
        byte boxLevel,
        byte attackRange)
    {
        if (gameConfiguration.Monsters.Any(m => m.Number == number))
        {
            return;
        }

        var monster = context.CreateNew<MonsterDefinition>();
        gameConfiguration.Monsters.Add(monster);
        monster.Number = number;
        monster.Designation = designation;
        monster.MoveRange = 4;
        monster.AttackRange = attackRange;
        monster.AttackSkill = gameConfiguration.Skills.FirstOrDefault(s => s.Number == (short)SkillNumber.MonsterSkill);
        monster.ViewRange = 8;
        monster.MoveDelay = TimeSpan.FromMilliseconds(500);
        monster.AttackDelay = TimeSpan.FromMilliseconds(1600);
        monster.RespawnDelay = TimeSpan.FromHours(12);
        monster.Attribute = 2;
        monster.NumberOfMaximumItemDrops = 1;
        monster.SetGuid(monster.Number);

        var attributes = new Dictionary<AttributeDefinition, float>
        {
            { Stats.Level, level },
            { Stats.MaximumHealth, health },
            { Stats.MinimumPhysBaseDmg, minimumDamage },
            { Stats.MaximumPhysBaseDmg, maximumDamage },
            { Stats.DefenseBase, defense },
            { Stats.AttackRatePvm, attackRate },
            { Stats.DefenseRatePvm, defenseRate },
            { Stats.PoisonResistance, 100f / 255 },
            { Stats.IceResistance, 100f / 255 },
            { Stats.LightningResistance, 100f / 255 },
            { Stats.FireResistance, 100f / 255 },
            { Stats.WaterResistance, 80f / 255 },
        };
        monster.AddAttributes(attributes, context, gameConfiguration);

        var box = gameConfiguration.Items.FirstOrDefault(item => item.Group == BoxOfKundunGroup && item.Number == BoxOfKundunNumber);
        if (box is null)
        {
            return;
        }

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
