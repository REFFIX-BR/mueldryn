// <copyright file="ShopBuffService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.CashShop;

using System.Globalization;
using System.Runtime.CompilerServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Handles the buffs which are sold in the item shop (seals, scrolls and auras). They are applied
/// right away instead of being delivered as an item, so the remaining time is kept in the character
/// attributes to survive a logout. Only the time the character spends online is consumed.
/// </summary>
public static class ShopBuffService
{
    /// <summary>
    /// Prefix of the identifiers of the attributes which keep the remaining seconds of a buff.
    /// The effect number is encoded in the last group of the identifier.
    /// </summary>
    private const string TimerAttributeIdPrefix = "8f1d6e20-4c7b-4a19-9e52-";

    /// <summary>
    /// A buff which lasts even longer is applied in slices, because neither the timer of a
    /// <see cref="MagicEffect"/> nor the countdown of the game client can hold more than that.
    /// </summary>
    private static readonly TimeSpan MaximumSlice = TimeSpan.FromDays(45);

    private static readonly ConditionalWeakTable<Player, Dictionary<short, RunningBuff>> RunningBuffs = new();

    /// <summary>
    /// Applies a bought buff. When the same buff is already running, the times are added up.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="effectDefinition">The effect of the bought item.</param>
    /// <param name="duration">The bought duration.</param>
    /// <returns><see langword="true"/>, if the buff is active afterwards.</returns>
    public static async ValueTask<bool> AddAsync(Player player, MagicEffectDefinition effectDefinition, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return false;
        }

        var total = duration + GetRemaining(player, effectDefinition.Number);
        if (!await ApplyAsync(player, effectDefinition, total).ConfigureAwait(false))
        {
            return false;
        }

        await player.ShowBlueMessageAsync($"{effectDefinition.Name} is active for {Format(total)}.").ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Applies the buffs which are still running, called when a character enters the world.
    /// </summary>
    /// <param name="player">The player.</param>
    public static async ValueTask RestoreAsync(Player player)
    {
        if (player.SelectedCharacter is null)
        {
            return;
        }

        foreach (var stat in player.SelectedCharacter.Attributes.ToList())
        {
            if (stat.Definition is null
                || !TryGetEffectNumber(stat.Definition.Id, out var effectNumber)
                || stat.Value <= 0)
            {
                continue;
            }

            if (player.GameContext.Configuration.MagicEffects.FirstOrDefault(e => e.Number == effectNumber) is not { } effectDefinition)
            {
                continue;
            }

            var remaining = TimeSpan.FromSeconds(stat.Value);
            if (await ApplyAsync(player, effectDefinition, remaining).ConfigureAwait(false))
            {
                await player.ShowBlueMessageAsync($"{effectDefinition.Name} is still active for {Format(remaining)}.").ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Saves the remaining times, called when the character leaves the game.
    /// </summary>
    /// <param name="player">The player.</param>
    public static ValueTask SaveAsync(Player player)
    {
        if (!RunningBuffs.TryGetValue(player, out var buffs))
        {
            return ValueTask.CompletedTask;
        }

        foreach (var buff in buffs.Values)
        {
            SetRemaining(player, buff.Definition, buff.GetRemaining());
        }

        // Without the entries, the effects which are disposed together with the player cannot
        // be mistaken for buffs which ran out of time.
        buffs.Clear();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Gets the remaining time of a buff of the selected character.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="effectNumber">The number of the effect.</param>
    /// <returns>The remaining time.</returns>
    public static TimeSpan GetRemaining(Player player, short effectNumber)
    {
        if (RunningBuffs.TryGetValue(player, out var buffs) && buffs.TryGetValue(effectNumber, out var running))
        {
            return running.GetRemaining();
        }

        var stat = FindStat(player, effectNumber);
        return stat is null || stat.Value <= 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(stat.Value);
    }

    /// <summary>
    /// Gets the identifier of the attribute which keeps the remaining seconds of the specified effect.
    /// </summary>
    /// <param name="effectNumber">The number of the effect.</param>
    /// <returns>The identifier of the attribute.</returns>
    public static Guid GetTimerAttributeId(short effectNumber)
    {
        return Guid.Parse(TimerAttributeIdPrefix + effectNumber.ToString("X12", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Gets the designation of the attribute which keeps the remaining seconds of the specified effect.
    /// </summary>
    /// <param name="effectName">The name of the effect.</param>
    /// <returns>The designation of the attribute.</returns>
    public static string GetTimerAttributeName(string effectName)
    {
        return $"Remaining seconds of {effectName}";
    }

    private static async ValueTask<bool> ApplyAsync(Player player, MagicEffectDefinition effectDefinition, TimeSpan totalRemaining)
    {
        if (player.Attributes is null || player.SelectedCharacter is null || totalRemaining <= TimeSpan.Zero)
        {
            return false;
        }

        var boosts = effectDefinition.PowerUpDefinitions
            .Where(def => def.Boost is not null && def.TargetAttribute is not null)
            .Select(def => new MagicEffect.ElementWithTarget(player.Attributes.CreateElement(def), def.TargetAttribute!))
            .ToArray();
        if (boosts.Length == 0)
        {
            return false;
        }

        if (player.MagicEffectList.ActiveEffects.TryGetValue(effectDefinition.Number, out var sameEffect))
        {
            await sameEffect.DisposeAsync().ConfigureAwait(false);
        }

        if (effectDefinition.SubType != 0
            && await player.MagicEffectList.TryGetActiveEffectOfSubTypeAsync(effectDefinition.SubType).ConfigureAwait(false) is { } sameSubType)
        {
            await sameSubType.DisposeAsync().ConfigureAwait(false);
        }

        var slice = totalRemaining > MaximumSlice ? MaximumSlice : totalRemaining;
        var effect = new MagicEffect(slice, effectDefinition, boosts);
        var running = new RunningBuff(effectDefinition, DateTime.UtcNow, totalRemaining);
        GetRunningBuffs(player)[effectDefinition.Number] = running;
        SetRemaining(player, effectDefinition, totalRemaining);

        effect.EffectTimeOut += _ => OnBuffTimeOutAsync(player, running);
        await player.MagicEffectList.AddEffectAsync(effect).ConfigureAwait(false);
        return true;
    }

    private static async ValueTask OnBuffTimeOutAsync(Player player, RunningBuff running)
    {
        if (!RunningBuffs.TryGetValue(player, out var buffs)
            || !buffs.TryGetValue(running.Definition.Number, out var current)
            || !ReferenceEquals(current, running))
        {
            // Either the player left the game or the buff was replaced by a newer one.
            return;
        }

        buffs.Remove(running.Definition.Number);
        var remaining = running.GetRemaining();
        if (remaining > TimeSpan.FromSeconds(1))
        {
            // Long buffs run in slices, so the next one continues where this one stopped.
            await ApplyAsync(player, running.Definition, remaining).ConfigureAwait(false);
            return;
        }

        SetRemaining(player, running.Definition, TimeSpan.Zero);
        await player.ShowBlueMessageAsync($"{running.Definition.Name} has ended.").ConfigureAwait(false);
    }

    private static Dictionary<short, RunningBuff> GetRunningBuffs(Player player)
    {
        return RunningBuffs.GetValue(player, _ => new Dictionary<short, RunningBuff>());
    }

    private static StatAttribute? FindStat(Player player, short effectNumber)
    {
        var id = GetTimerAttributeId(effectNumber);
        return player.SelectedCharacter?.Attributes
            .FirstOrDefault(a => a.Definition is not null && a.Definition.Id == id);
    }

    private static void SetRemaining(Player player, MagicEffectDefinition effectDefinition, TimeSpan remaining)
    {
        if (GetOrCreateStat(player, effectDefinition) is { } stat)
        {
            stat.Value = (float)Math.Max(0, Math.Round(remaining.TotalSeconds));
        }
    }

    private static StatAttribute? GetOrCreateStat(Player player, MagicEffectDefinition effectDefinition)
    {
        if (player.SelectedCharacter is null)
        {
            return null;
        }

        if (FindStat(player, effectDefinition.Number) is { } existing)
        {
            return existing;
        }

        if (GetOrCreateDefinition(player, effectDefinition) is not { } definition)
        {
            return null;
        }

        var created = player.PersistenceContext.CreateNew<StatAttribute>(definition, 0);
        player.SelectedCharacter.Attributes.Add(created);
        return created;
    }

    private static AttributeDefinition? GetOrCreateDefinition(Player player, MagicEffectDefinition effectDefinition)
    {
        var id = GetTimerAttributeId(effectDefinition.Number);
        var configuration = player.GameContext.Configuration;
        if (configuration.Attributes.FirstOrDefault(a => a.Id == id) is { } existing)
        {
            return existing;
        }

        try
        {
            var created = player.PersistenceContext.CreateNew<AttributeDefinition>(
                id,
                GetTimerAttributeName(effectDefinition.Name),
                GetTimerAttributeName(effectDefinition.Name));
            created.MaximumValue = null;
            configuration.Attributes.Add(created);
            return created;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static bool TryGetEffectNumber(Guid attributeId, out short effectNumber)
    {
        effectNumber = 0;
        var text = attributeId.ToString();
        return text.StartsWith(TimerAttributeIdPrefix, StringComparison.OrdinalIgnoreCase)
               && short.TryParse(text.AsSpan(TimerAttributeIdPrefix.Length), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out effectNumber);
    }

    private static string Format(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
        {
            return $"{(int)duration.TotalDays}d {duration.Hours}h";
        }

        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}h {duration.Minutes}min"
            : $"{Math.Max(1, (int)duration.TotalMinutes)}min";
    }

    private sealed class RunningBuff(MagicEffectDefinition definition, DateTime appliedAt, TimeSpan totalRemaining)
    {
        public MagicEffectDefinition Definition { get; } = definition;

        public TimeSpan GetRemaining()
        {
            var remaining = totalRemaining - (DateTime.UtcNow - appliedAt);
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }
}
