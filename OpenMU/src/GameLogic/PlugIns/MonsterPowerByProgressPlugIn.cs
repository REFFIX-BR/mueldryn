// <copyright file="MonsterPowerByProgressPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Scales monster combat stats (and level for EXP) based on the attacking player's resets and levels,
/// so each reset/level keeps city farm spots worthwhile.
/// </summary>
[PlugIn]
[Display(Name = "Monster power by progress", Description = "Scales monster HP/damage/defense/level from the attacker's resets and levels.")]
[Guid("B7C8D9E0-F1A2-4B3C-8D9E-0F1A2B3C4D5E")]
public class MonsterPowerByProgressPlugIn : IAttackableGotHitPlugIn, IObjectRemovedFromMapPlugIn,
    ISupportCustomConfiguration<MonsterPowerByProgressConfiguration>,
    ISupportDefaultCustomConfiguration
{
    private readonly ConcurrentDictionary<ushort, ScaleState> _scaledMonsters = new();

    /// <inheritdoc />
    public MonsterPowerByProgressConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new MonsterPowerByProgressConfiguration();

    /// <inheritdoc />
    public void AttackableGotHit(IAttackable attackable, IAttacker attacker, HitInfo hitInfo)
    {
        if (attackable is not AttackableNpcBase monster || attackable is Monster { SummonedBy: not null })
        {
            return;
        }

        var player = attacker as Player ?? (attacker as Monster)?.SummonedBy;
        if (player?.Attributes is null)
        {
            return;
        }

        var config = this.Configuration ?? new MonsterPowerByProgressConfiguration();
        var multiplier = CalculateMultiplier(player, config);
        if (multiplier < config.MinimumMultiplier)
        {
            return;
        }

        var state = this._scaledMonsters.GetOrAdd(monster.Id, _ => CreateScaleState(monster));
        if (multiplier <= state.AppliedMultiplier + 0.001f)
        {
            return;
        }

        var previous = state.AppliedMultiplier;
        state.AppliedMultiplier = multiplier;
        state.CombatMultiplier.Value = multiplier;
        state.LevelMultiplier.Value = MathF.Sqrt(multiplier); // EXP grows, but softer than raw HP/dmg

        if (previous <= 1.001f)
        {
            monster.Attributes[Stats.CurrentHealth] = monster.Attributes[Stats.MaximumHealth];
        }
        else
        {
            var ratio = multiplier / previous;
            monster.Attributes[Stats.CurrentHealth] = MathF.Min(
                monster.Attributes[Stats.MaximumHealth],
                monster.Attributes[Stats.CurrentHealth] * ratio);
        }
    }

    /// <inheritdoc />
    public ValueTask ObjectRemovedFromMapAsync(GameMap map, ILocateable removedObject)
    {
        if (removedObject is AttackableNpcBase monster
            && this._scaledMonsters.TryRemove(monster.Id, out var state))
        {
            RemoveScale(monster, state);
        }

        return ValueTask.CompletedTask;
    }

    private static float CalculateMultiplier(Player player, MonsterPowerByProgressConfiguration config)
    {
        var resets = player.Attributes![Stats.Resets];
        var level = player.Attributes[Stats.Level];
        var masterLevel = player.Attributes[Stats.MasterLevel];

        var multiplier = 1f
            + (resets * (config.PercentPerReset / 100f))
            + (level * (config.PercentPerLevel / 100f))
            + (masterLevel * (config.PercentPerMasterLevel / 100f));

        return Math.Clamp(multiplier, 1f, config.MaximumMultiplier);
    }

    private static ScaleState CreateScaleState(AttackableNpcBase monster)
    {
        var combat = new SimpleElement(1f, AggregateType.Multiplicate);
        var level = new SimpleElement(1f, AggregateType.Multiplicate);

        monster.Attributes.AddElement(combat, Stats.MinimumPhysBaseDmg);
        monster.Attributes.AddElement(combat, Stats.MaximumPhysBaseDmg);
        monster.Attributes.AddElement(combat, Stats.MinimumWizBaseDmg);
        monster.Attributes.AddElement(combat, Stats.MaximumWizBaseDmg);
        monster.Attributes.AddElement(combat, Stats.AttackRatePvm);
        monster.Attributes.AddElement(combat, Stats.DefenseRatePvm);
        monster.Attributes.AddElement(combat, Stats.DefenseBase);
        monster.Attributes.AddElement(combat, Stats.MaximumHealth);
        monster.Attributes.AddElement(level, Stats.Level);

        return new ScaleState(combat, level);
    }

    private static void RemoveScale(AttackableNpcBase monster, ScaleState state)
    {
        monster.Attributes.RemoveElement(state.CombatMultiplier, Stats.MinimumPhysBaseDmg);
        monster.Attributes.RemoveElement(state.CombatMultiplier, Stats.MaximumPhysBaseDmg);
        monster.Attributes.RemoveElement(state.CombatMultiplier, Stats.MinimumWizBaseDmg);
        monster.Attributes.RemoveElement(state.CombatMultiplier, Stats.MaximumWizBaseDmg);
        monster.Attributes.RemoveElement(state.CombatMultiplier, Stats.AttackRatePvm);
        monster.Attributes.RemoveElement(state.CombatMultiplier, Stats.DefenseRatePvm);
        monster.Attributes.RemoveElement(state.CombatMultiplier, Stats.DefenseBase);
        monster.Attributes.RemoveElement(state.CombatMultiplier, Stats.MaximumHealth);
        monster.Attributes.RemoveElement(state.LevelMultiplier, Stats.Level);
    }

    private sealed class ScaleState
    {
        public ScaleState(SimpleElement combatMultiplier, SimpleElement levelMultiplier)
        {
            this.CombatMultiplier = combatMultiplier;
            this.LevelMultiplier = levelMultiplier;
        }

        public SimpleElement CombatMultiplier { get; }

        public SimpleElement LevelMultiplier { get; }

        public float AppliedMultiplier { get; set; } = 1f;
    }
}
