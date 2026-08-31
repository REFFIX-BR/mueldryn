// <copyright file="SoulSystemTypes.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.SoulSystem;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Snapshot of soul system state for the client.
/// </summary>
public sealed class SoulSystemStatus
{
    /// <summary>Remaining points available to spend.</summary>
    public required int Remaining { get; init; }

    /// <summary>16 allocations: tab*4+col, each 0..100.</summary>
    public required byte[] Allocations { get; init; }
}

/// <summary>
/// Result of a soul system action.
/// </summary>
public enum SoulSystemResult : byte
{
    Success = 0,
    Failed = 1,
    NotEnoughPoints = 2,
    InvalidRequest = 3,
}

/// <summary>
/// How a soul bonus is applied to OpenMU attributes.
/// </summary>
public enum SoulBonusKind : byte
{
    /// <summary>AddRaw absolute value.</summary>
    Flat = 0,

    /// <summary>AddRaw chance fraction (percent / 100).</summary>
    ChancePercent = 1,

    /// <summary>Multiplicate by (1 + percent/100).</summary>
    MultiplicativePercent = 2,

    /// <summary>AddRaw to BonusExperienceRate / similar (percent / 100).</summary>
    BonusRatePercent = 3,

    /// <summary>Multiplicate MoneyAmountRate by (1 + percent/100).</summary>
    MoneyRatePercent = 4,

    /// <summary>AddRaw to all primary totals.</summary>
    AllStatsFlat = 5,

    /// <summary>Damage receive multiplier: Multiplicate by (1 - percent/100).</summary>
    DamageDecreasePercent = 6,
}

/// <summary>
/// One main element (column) definition.
/// </summary>
public sealed class SoulElementDefinition
{
    public required string Name { get; init; }

    public required float ValuePerPoint { get; init; }

    public required bool IsPercentValue { get; init; }

    public required SoulBonusKind Kind { get; init; }

    public required AttributeDefinition[] Targets { get; init; }

    public required SoulSubDefinition[] Subs { get; init; }
}

/// <summary>
/// Sub reward unlocked at ReqPoints.
/// </summary>
public sealed class SoulSubDefinition
{
    public required int ReqPoints { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required float Value { get; init; }

    public required SoulBonusKind Kind { get; init; }

    public required AttributeDefinition[] Targets { get; init; }
}

/// <summary>
/// Catalog matching SoulSystem.xml (Season 6 Pegasus).
/// </summary>
public static class SoulSystemCatalog
{
    public const int TabCount = 4;
    public const int ColumnCount = 4;
    public const int SlotCount = TabCount * ColumnCount;
    public const int MaxPointsPerColumn = 100;
    public const int ResetRewardCount = 5;
    public const int GrandResetRewardCount = 75;

    public static readonly int[] SubReqPoints = [40, 70, 100];

    /// <summary>Remaining points attribute.</summary>
    public static AttributeDefinition RemainingAttribute { get; } = Stats.SoulPointsRemaining;

    /// <summary>Allocation attributes indexed by tab*4+col.</summary>
    public static readonly AttributeDefinition[] AllocAttributes =
    [
        Stats.SoulAlloc00, Stats.SoulAlloc01, Stats.SoulAlloc02, Stats.SoulAlloc03,
        Stats.SoulAlloc10, Stats.SoulAlloc11, Stats.SoulAlloc12, Stats.SoulAlloc13,
        Stats.SoulAlloc20, Stats.SoulAlloc21, Stats.SoulAlloc22, Stats.SoulAlloc23,
        Stats.SoulAlloc30, Stats.SoulAlloc31, Stats.SoulAlloc32, Stats.SoulAlloc33,
    ];

    private static readonly AttributeDefinition[] PhysWizMinMax =
    [
        Stats.MinimumPhysBaseDmg,
        Stats.MaximumPhysBaseDmg,
        Stats.MinimumWizBaseDmg,
        Stats.MaximumWizBaseDmg,
    ];

    private static readonly AttributeDefinition[] AllTotals =
    [
        Stats.TotalStrength,
        Stats.TotalAgility,
        Stats.TotalVitality,
        Stats.TotalEnergy,
    ];

    /// <summary>Elements[tab][col].</summary>
    public static readonly SoulElementDefinition[][] Elements =
    [
        // Attack
        [
            new SoulElementDefinition
            {
                Name = "Damage",
                ValuePerPoint = 0.15f,
                IsPercentValue = true,
                Kind = SoulBonusKind.MultiplicativePercent,
                Targets = PhysWizMinMax,
                Subs =
                [
                    Sub(40, "Add Physical and Wizardry", "Increases Min. Max. Damage +100", 100f, SoulBonusKind.Flat, PhysWizMinMax),
                    Sub(70, "Add Physical and Wizardry", "Increases Min. Max. Damage +200", 200f, SoulBonusKind.Flat, PhysWizMinMax),
                    Sub(100, "Add Physical and Wizardry", "Increases Min. Max. Damage +300", 300f, SoulBonusKind.Flat, PhysWizMinMax),
                ],
            },
            new SoulElementDefinition
            {
                Name = "Excellent Damage Rate",
                ValuePerPoint = 0.1f,
                IsPercentValue = true,
                Kind = SoulBonusKind.ChancePercent,
                Targets = [Stats.ExcellentDamageChance],
                Subs =
                [
                    Sub(40, "Add Excellent Damage", "Increases Excellent Damage +500", 500f, SoulBonusKind.Flat, [Stats.ExcellentDamageBonus]),
                    Sub(70, "Add Excellent Damage", "Increases Excellent Damage +1000", 1000f, SoulBonusKind.Flat, [Stats.ExcellentDamageBonus]),
                    Sub(100, "Add Excellent Damage", "Increases Excellent Damage +1500", 1500f, SoulBonusKind.Flat, [Stats.ExcellentDamageBonus]),
                ],
            },
            new SoulElementDefinition
            {
                Name = "Critical Damage Rate",
                ValuePerPoint = 0.1f,
                IsPercentValue = true,
                Kind = SoulBonusKind.ChancePercent,
                Targets = [Stats.CriticalDamageChance],
                Subs =
                [
                    Sub(40, "Add Critical Damage", "Increases Critical Damage +500", 500f, SoulBonusKind.Flat, [Stats.CriticalDamageBonus]),
                    Sub(70, "Add Critical Damage", "Increases Critical Damage +1000", 1000f, SoulBonusKind.Flat, [Stats.CriticalDamageBonus]),
                    Sub(100, "Add Critical Damage", "Increases Critical Damage +1500", 1500f, SoulBonusKind.Flat, [Stats.CriticalDamageBonus]),
                ],
            },
            new SoulElementDefinition
            {
                Name = "Attack Speed",
                ValuePerPoint = 0.5f,
                IsPercentValue = false,
                Kind = SoulBonusKind.Flat,
                Targets = [Stats.AttackSpeedAny],
                Subs =
                [
                    Sub(40, "Add Attack Speed", "Increases Attack Speed +10", 10f, SoulBonusKind.Flat, [Stats.AttackSpeedAny]),
                    Sub(70, "Add Attack Speed", "Increases Attack Speed +20", 20f, SoulBonusKind.Flat, [Stats.AttackSpeedAny]),
                    Sub(100, "Add Attack Speed", "Increases Attack Speed +30", 30f, SoulBonusKind.Flat, [Stats.AttackSpeedAny]),
                ],
            },
        ],
        // Defense
        [
            new SoulElementDefinition
            {
                Name = "Defense",
                ValuePerPoint = 0.15f,
                IsPercentValue = true,
                Kind = SoulBonusKind.MultiplicativePercent,
                Targets = [Stats.DefenseBase],
                Subs =
                [
                    Sub(40, "Add Defense", "Increases Defense +200", 200f, SoulBonusKind.Flat, [Stats.DefenseBase]),
                    Sub(70, "Add Defense", "Increases Defense +400", 400f, SoulBonusKind.Flat, [Stats.DefenseBase]),
                    Sub(100, "Add Defense", "Increases Defense +600", 600f, SoulBonusKind.Flat, [Stats.DefenseBase]),
                ],
            },
            new SoulElementDefinition
            {
                Name = "Reflect Damage",
                ValuePerPoint = 0.1f,
                IsPercentValue = true,
                Kind = SoulBonusKind.ChancePercent,
                Targets = [Stats.DamageReflection],
                Subs =
                [
                    Sub(40, "Add Reflect Damage", "Increases Reflect Damage +1%", 1f, SoulBonusKind.ChancePercent, [Stats.DamageReflection]),
                    Sub(70, "Add Reflect Damage", "Increases Reflect Damage +2%", 2f, SoulBonusKind.ChancePercent, [Stats.DamageReflection]),
                    Sub(100, "Add Reflect Damage", "Increases Reflect Damage +3%", 3f, SoulBonusKind.ChancePercent, [Stats.DamageReflection]),
                ],
            },
            new SoulElementDefinition
            {
                Name = "Health",
                ValuePerPoint = 0.2f,
                IsPercentValue = true,
                Kind = SoulBonusKind.MultiplicativePercent,
                Targets = [Stats.MaximumHealth],
                Subs =
                [
                    Sub(40, "Add Max. Life", "Increases Max. Life +1000", 1000f, SoulBonusKind.Flat, [Stats.MaximumHealth]),
                    Sub(70, "Add Max. Life", "Increases Max. Life +2000", 2000f, SoulBonusKind.Flat, [Stats.MaximumHealth]),
                    Sub(100, "Add Max. Life", "Increases Max. Life +3000", 3000f, SoulBonusKind.Flat, [Stats.MaximumHealth]),
                ],
            },
            new SoulElementDefinition
            {
                Name = "Damage Decrease",
                ValuePerPoint = 0.03f,
                IsPercentValue = true,
                Kind = SoulBonusKind.DamageDecreasePercent,
                Targets = [Stats.DamageReceiveDecrement],
                Subs =
                [
                    Sub(40, "Add Damage Decrease", "Increases Damage Decrease +0.5%", 0.5f, SoulBonusKind.DamageDecreasePercent, [Stats.DamageReceiveDecrement]),
                    Sub(70, "Add Damage Decrease", "Increases Damage Decrease +1%", 1f, SoulBonusKind.DamageDecreasePercent, [Stats.DamageReceiveDecrement]),
                    Sub(100, "Add Damage Decrease", "Increases Damage Decrease +1.5%", 1.5f, SoulBonusKind.DamageDecreasePercent, [Stats.DamageReceiveDecrement]),
                ],
            },
        ],
        // Support
        [
            new SoulElementDefinition
            {
                Name = "Excellent DMG Resist",
                ValuePerPoint = 0.1f,
                IsPercentValue = true,
                Kind = SoulBonusKind.DamageDecreasePercent,
                Targets = [Stats.DamageReceiveDecrement],
                Subs =
                [
                    Sub(40, "Add Excellent Resistance", "Increases Excellent Damage Resistance +1%", 1f, SoulBonusKind.DamageDecreasePercent, [Stats.DamageReceiveDecrement]),
                    Sub(70, "Add Excellent Resistance", "Increases Excellent Damage Resistance +3%", 3f, SoulBonusKind.DamageDecreasePercent, [Stats.DamageReceiveDecrement]),
                    Sub(100, "Add Excellent Resistance", "Increases Excellent Damage Resistance +6%", 6f, SoulBonusKind.DamageDecreasePercent, [Stats.DamageReceiveDecrement]),
                ],
            },
            new SoulElementDefinition
            {
                Name = "Critical DMG Resist",
                ValuePerPoint = 0.1f,
                IsPercentValue = true,
                Kind = SoulBonusKind.DamageDecreasePercent,
                Targets = [Stats.DamageReceiveDecrement],
                Subs =
                [
                    Sub(40, "Add Critical Resistance", "Increases Critical Damage Resistance +1%", 1f, SoulBonusKind.DamageDecreasePercent, [Stats.DamageReceiveDecrement]),
                    Sub(70, "Add Critical Resistance", "Increases Critical Damage Resistance +3%", 3f, SoulBonusKind.DamageDecreasePercent, [Stats.DamageReceiveDecrement]),
                    Sub(100, "Add Critical Resistance", "Increases Critical Damage Resistance +6%", 6f, SoulBonusKind.DamageDecreasePercent, [Stats.DamageReceiveDecrement]),
                ],
            },
            new SoulElementDefinition
            {
                Name = "Shield Defense (SD)",
                ValuePerPoint = 0.2f,
                IsPercentValue = true,
                Kind = SoulBonusKind.MultiplicativePercent,
                Targets = [Stats.MaximumShield],
                Subs =
                [
                    Sub(40, "Add Shield Defense", "Increases Shield Defense +3%", 3f, SoulBonusKind.MultiplicativePercent, [Stats.MaximumShield]),
                    Sub(70, "Add Shield Defense", "Increases Shield Defense +5%", 5f, SoulBonusKind.MultiplicativePercent, [Stats.MaximumShield]),
                    Sub(100, "Add Shield Defense", "Increases Shield Defense +7%", 7f, SoulBonusKind.MultiplicativePercent, [Stats.MaximumShield]),
                ],
            },
            new SoulElementDefinition
            {
                Name = "All Stats",
                ValuePerPoint = 2f,
                IsPercentValue = false,
                Kind = SoulBonusKind.AllStatsFlat,
                Targets = AllTotals,
                Subs =
                [
                    Sub(40, "Add All Stats", "Increases All Stats +30", 30f, SoulBonusKind.AllStatsFlat, AllTotals),
                    Sub(70, "Add All Stats", "Increases All Stats +60", 60f, SoulBonusKind.AllStatsFlat, AllTotals),
                    Sub(100, "Add All Stats", "Increases All Stats +90", 90f, SoulBonusKind.AllStatsFlat, AllTotals),
                ],
            },
        ],
        // Misc
        [
            new SoulElementDefinition
            {
                Name = "Zen Drop",
                ValuePerPoint = 0.1f,
                IsPercentValue = true,
                Kind = SoulBonusKind.MoneyRatePercent,
                Targets = [Stats.MoneyAmountRate],
                Subs =
                [
                    Sub(40, "Add Zen Drop Rate", "Increases Zen Drop Rate +3%", 3f, SoulBonusKind.MoneyRatePercent, [Stats.MoneyAmountRate]),
                    Sub(70, "Add Zen Drop Rate", "Increases Zen Drop Rate +6%", 6f, SoulBonusKind.MoneyRatePercent, [Stats.MoneyAmountRate]),
                    Sub(100, "Add Zen Drop Rate", "Increases Zen Drop Rate +9%", 9f, SoulBonusKind.MoneyRatePercent, [Stats.MoneyAmountRate]),
                ],
            },
            new SoulElementDefinition
            {
                Name = "Normal Experience",
                ValuePerPoint = 0.025f,
                IsPercentValue = true,
                Kind = SoulBonusKind.BonusRatePercent,
                Targets = [Stats.BonusExperienceRate],
                Subs =
                [
                    Sub(40, "Add Normal Experience Rate", "Increases Normal Experience Rate +0.5%", 0.5f, SoulBonusKind.BonusRatePercent, [Stats.BonusExperienceRate]),
                    Sub(70, "Add Normal Experience Rate", "Increases Normal Experience Rate +1%", 1f, SoulBonusKind.BonusRatePercent, [Stats.BonusExperienceRate]),
                    Sub(100, "Add Normal Experience Rate", "Increases Normal Experience Rate +1.5%", 1.5f, SoulBonusKind.BonusRatePercent, [Stats.BonusExperienceRate]),
                ],
            },
            new SoulElementDefinition
            {
                Name = "Master Experience",
                ValuePerPoint = 0.025f,
                IsPercentValue = true,
                Kind = SoulBonusKind.BonusRatePercent,
                Targets = [Stats.MasterExperienceRate],
                Subs =
                [
                    Sub(40, "Add Master Experience Rate", "Increases Master Experience Rate +1%", 1f, SoulBonusKind.BonusRatePercent, [Stats.MasterExperienceRate]),
                    Sub(70, "Add Master Experience Rate", "Increases Master Experience Rate +1.5%", 1.5f, SoulBonusKind.BonusRatePercent, [Stats.MasterExperienceRate]),
                    Sub(100, "Add Master Experience Rate", "Increases Master Experience Rate +2%", 2f, SoulBonusKind.BonusRatePercent, [Stats.MasterExperienceRate]),
                ],
            },
            new SoulElementDefinition
            {
                Name = "Automatic HP Recovery",
                ValuePerPoint = 0.05f,
                IsPercentValue = true,
                Kind = SoulBonusKind.ChancePercent,
                Targets = [Stats.HealthRecoveryMultiplier],
                Subs =
                [
                    Sub(40, "Add Automatic HP Recovery", "Increases Automatic HP Recovery +2%", 2f, SoulBonusKind.ChancePercent, [Stats.HealthRecoveryMultiplier]),
                    Sub(70, "Add Automatic HP Recovery", "Increases Automatic HP Recovery +4%", 4f, SoulBonusKind.ChancePercent, [Stats.HealthRecoveryMultiplier]),
                    Sub(100, "Add Automatic HP Recovery", "Increases Automatic HP Recovery +6%", 6f, SoulBonusKind.ChancePercent, [Stats.HealthRecoveryMultiplier]),
                ],
            },
        ],
    ];

    public static int Index(int tab, int col) => (tab * ColumnCount) + col;

    public static SoulElementDefinition GetElement(int tab, int col) => Elements[tab][col];

    private static SoulSubDefinition Sub(
        int req,
        string name,
        string description,
        float value,
        SoulBonusKind kind,
        AttributeDefinition[] targets)
        => new()
        {
            ReqPoints = req,
            Name = name,
            Description = description,
            Value = value,
            Kind = kind,
            Targets = targets,
        };
}
