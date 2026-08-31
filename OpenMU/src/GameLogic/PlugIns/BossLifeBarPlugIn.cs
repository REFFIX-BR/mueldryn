// <copyright file="BossLifeBarPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Feeds the boss life bar of the extended client: whenever a boss monster gets hit, everybody who
/// sees it receives the remaining health, and the bar is hidden again when the boss dies.
/// </summary>
[PlugIn]
[Display(Name = "Boss life bar", Description = "Sends the remaining health of boss monsters to the clients which see them.")]
[Guid("A4F1C8D2-6B93-4E57-8C10-59D3A7E0B641")]
public class BossLifeBarPlugIn : IAttackableGotHitPlugIn, IAttackableGotKilledPlugIn
{
    /// <summary>
    /// Monsters which get a life bar. Invasion bosses first, then the classic ones.
    /// </summary>
    private static readonly HashSet<int> BossMonsterNumbers =
    [
        InvasionMonsters.GoldenBudgeDragon,
        InvasionMonsters.GoldenSoldier,
        InvasionMonsters.GoldenTitan,
        InvasionMonsters.GoldenGoblin,
        InvasionMonsters.GoldenDragon,
        InvasionMonsters.GoldenLizardKing,
        InvasionMonsters.GoldenVepar,
        InvasionMonsters.GoldenTantallos,
        InvasionMonsters.GoldenWheel,
        InvasionMonsters.GoldenDarkKnight,
        InvasionMonsters.GoldenDevil,
        InvasionMonsters.GoldenStoneGolem,
        InvasionMonsters.GoldenCrust,
        InvasionMonsters.GoldenSatyros,
        InvasionMonsters.GoldenTwinTail,
        InvasionMonsters.GoldenIronKnight,
        InvasionMonsters.GoldenNapin,
        InvasionMonsters.GreatGoldenDragon,
        InvasionMonsters.GoldenRabbit,
        InvasionMonsters.RedDragon,
        InvasionMonsters.WhiteWizard,
        InvasionMonsters.DestructiveOgreSoldier,
        InvasionMonsters.DestructiveOgreArcher,
        66, // Cursed King
        295, // Erohim
        459, // Selupan
        161, // Illusion of Kundun 1
        181, // Illusion of Kundun 2
        189, // Illusion of Kundun 3
        197, // Illusion of Kundun 4
        267, // Illusion of Kundun 5
        338, // Illusion of Kundun 6
        275, // Illusion of Kundun 7
    ];

    /// <summary>
    /// A boss can be hit several times per second by a whole party, so updates are rate limited.
    /// The percentage is always sent when it actually changed, so the bar never lags behind visibly.
    /// </summary>
    private static readonly TimeSpan MinimumUpdateInterval = TimeSpan.FromMilliseconds(250);

    private static readonly ConditionalWeakTable<IAttackable, UpdateState> States = new();

    /// <inheritdoc />
    public void AttackableGotHit(IAttackable attackable, IAttacker attacker, HitInfo hitInfo)
    {
        if (attackable is not AttackableNpcBase npc || !IsBoss(npc))
        {
            return;
        }

        var percentage = GetHealthPercentage(npc);
        var state = States.GetOrCreateValue(attackable);
        if (!state.ShouldSend(percentage, MinimumUpdateInterval))
        {
            return;
        }

        _ = npc.ForEachWorldObserverAsync<IShowBossLifeBarPlugIn>(
            p => p.ShowBossLifeBarAsync(npc.Definition.Designation, percentage, npc.IsAlive && percentage > 0),
            false);
    }

    /// <inheritdoc />
    public async ValueTask AttackableGotKilledAsync(IAttackable killed, IAttacker? killer)
    {
        if (killed is not AttackableNpcBase npc || !IsBoss(npc))
        {
            return;
        }

        States.Remove(killed);
        await npc.ForEachWorldObserverAsync<IShowBossLifeBarPlugIn>(
            p => p.ShowBossLifeBarAsync(npc.Definition.Designation, 0, false),
            false).ConfigureAwait(false);
    }

    private static bool IsBoss(AttackableNpcBase npc) => BossMonsterNumbers.Contains(npc.Definition.Number);

    private static byte GetHealthPercentage(AttackableNpcBase npc)
    {
        var health = npc.Health;

        // The spawn area can override the maximum health, so the current value is a lower bound for it.
        var maximumHealth = Math.Max(npc.Attributes[Stats.MaximumHealth], health);
        if (maximumHealth <= 0)
        {
            return 0;
        }

        var percentage = (int)Math.Ceiling(health * 100.0 / maximumHealth);
        return (byte)Math.Clamp(percentage, 0, 100);
    }

    private sealed class UpdateState
    {
        private int _lastPercentage = -1;
        private DateTime _lastUpdate = DateTime.MinValue;

        public bool ShouldSend(byte percentage, TimeSpan minimumInterval)
        {
            lock (this)
            {
                var now = DateTime.UtcNow;
                if (percentage == this._lastPercentage && now - this._lastUpdate < minimumInterval)
                {
                    return false;
                }

                this._lastPercentage = percentage;
                this._lastUpdate = now;
                return true;
            }
        }
    }
}
