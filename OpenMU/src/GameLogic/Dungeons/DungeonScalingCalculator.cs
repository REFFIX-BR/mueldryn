// <copyright file="DungeonScalingCalculator.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Dungeons;

/// <summary>
/// Provides scaling calculation formulas for Fortress of Imperial Dungeon monster attributes
/// based on party size and difficulty multiplier.
/// </summary>
public static class DungeonScalingCalculator
{
    /// <summary>
    /// Computes the HP multiplier for dungeon monsters.
    /// Formula: HP_final = HP_base × (1 + 0.70 × (N − 1)) × difficultyMultiplier.
    /// </summary>
    /// <param name="n">The number of participants in the dungeon instance (1-5).</param>
    /// <param name="difficultyMultiplier">The difficulty multiplier (e.g., 1.0 for Normal, 1.5 for Hard, 2.5 for Hell).</param>
    /// <returns>The computed HP multiplier to apply to monster base HP.</returns>
    /// <remarks>
    /// If <paramref name="n"/> is outside the valid range [1, 5], it will be clamped to the nearest boundary.
    /// Callers should validate input and log an error before calling if N is out of range (Req 4.4).
    /// </remarks>
    public static double ComputeHpMultiplier(int n, double difficultyMultiplier)
    {
        var extraMembers = ExtraMembers(n);
        return (1.0 + (0.70 * extraMembers)) * difficultyMultiplier;
    }

    /// <summary>
    /// Computes the damage multiplier for dungeon monsters.
    /// Formula: Dano_final = Dano_base × (1 + 0.35 × (N − 1)) × difficultyMultiplier.
    /// </summary>
    /// <param name="n">The number of participants in the dungeon instance (1-5).</param>
    /// <param name="difficultyMultiplier">The difficulty multiplier (e.g., 1.0 for Normal, 1.3 for Hard, 2.0 for Hell).</param>
    /// <returns>The computed damage multiplier to apply to monster base damage.</returns>
    /// <remarks>
    /// If <paramref name="n"/> is outside the valid range [1, 5], it will be clamped to the nearest boundary.
    /// Callers should validate input and log an error before calling if N is out of range (Req 4.4).
    /// </remarks>
    public static double ComputeDamageMultiplier(int n, double difficultyMultiplier)
    {
        var extraMembers = ExtraMembers(n);
        return (1.0 + (0.35 * extraMembers)) * difficultyMultiplier;
    }

    /// <summary>
    /// Extra attack rate applied on top of the difficulty bonus for larger parties.
    /// </summary>
    public static float ComputeAttackRateBonus(int n, float difficultyBonus)
    {
        var extraMembers = ExtraMembers(n);
        return difficultyBonus * (1f + (0.20f * extraMembers));
    }

    /// <summary>
    /// Scales a wave's monster count with party size. Boss waves stay at 1.
    /// Solo keeps the catalog count; each extra member adds more mobs.
    /// </summary>
    public static int ComputeMonsterCount(int baseCount, int n, bool isBoss)
    {
        if (isBoss)
        {
            return Math.Max(1, baseCount);
        }

        var extraMembers = ExtraMembers(n);
        var extraPerMember = Math.Max(2, (baseCount + 2) / 4);
        return baseCount + (extraPerMember * extraMembers);
    }

    private static int ExtraMembers(int n) => Math.Clamp(n, 1, 5) - 1;
}
