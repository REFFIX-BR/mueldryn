// <copyright file="ExcellentOptionRarityValues.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Dream-style excellent option magnitudes by rarity (Normal / Uncommon / Rare / Epic).
/// Higher rarity = stronger percentage / flat value.
/// </summary>
public static class ExcellentOptionRarityValues
{
    // Armor / defense: Life & Mana (Multiplicate 1.0X)
    private static readonly float[] LifeMana = [1.01f, 1.02f, 1.03f, 1.05f];

    // Zen drop (Multiplicate)
    private static readonly float[] Zen = [1.05f, 1.10f, 1.15f, 1.25f];

    // Defense success rate PvM (Multiplicate)
    private static readonly float[] DefenseRate = [1.05f, 1.06f, 1.10f, 1.15f];

    // Reflect damage (AddRaw fraction)
    private static readonly float[] Reflect = [0.01f, 0.02f, 0.03f, 0.05f];

    // Damage decrease (AddRaw fraction)
    private static readonly float[] DamageDecrease = [0.01f, 0.02f, 0.03f, 0.04f];

    // Weapon / wizardry damage % (Multiplicate)
    private static readonly float[] DamagePercent = [1.01f, 1.02f, 1.03f, 1.05f];

    // Excellent damage chance (AddRaw fraction)
    private static readonly float[] ExcellentChance = [0.05f, 0.08f, 0.10f, 0.15f];

    // Attack speed (AddRaw)
    private static readonly float[] AttackSpeed = [3f, 5f, 7f, 10f];

    // After-kill life/mana restore (AddRaw, classic 1/8)
    private static readonly float[] AfterKill = [1f / 12f, 1f / 10f, 1f / 8f, 1f / 6f];

    /// <summary>
    /// Resolves the effective boost constant for an excellent option at the given rarity.
    /// Returns <c>null</c> when the option should keep its configured definition value
    /// (e.g. related level-based damage).
    /// </summary>
    public static float? TryGetBoostValue(ItemOption option, ExcellentOptionRarity rarity)
    {
        var target = option.PowerUpDefinition?.TargetAttribute;
        if (target is null)
        {
            return null;
        }

        var index = Math.Clamp((int)rarity, 0, 3);

        if (target == Stats.MaximumHealth || target == Stats.MaximumMana)
        {
            return LifeMana[index];
        }

        if (target == Stats.MoneyAmountRate)
        {
            return Zen[index];
        }

        if (target == Stats.DefenseRatePvm)
        {
            return DefenseRate[index];
        }

        if (target == Stats.DamageReflection)
        {
            return Reflect[index];
        }

        if (target == Stats.ArmorDamageDecrease)
        {
            return DamageDecrease[index];
        }

        if (target == Stats.PhysicalBaseDmgIncrease || target == Stats.WizardryBaseDmgIncrease)
        {
            return DamagePercent[index];
        }

        if (target == Stats.ExcellentDamageChance)
        {
            return ExcellentChance[index];
        }

        if (target == Stats.AttackSpeedAny)
        {
            return AttackSpeed[index];
        }

        if (target == Stats.ManaAfterMonsterKillMultiplier || target == Stats.HealthAfterMonsterKillMultiplier)
        {
            return AfterKill[index];
        }

        return null;
    }
}
