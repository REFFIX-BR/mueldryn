// <copyright file="SoulSystemService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.SoulSystem;

using System.Runtime.CompilerServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Character soul points: allocate, reset, and apply combat bonuses.
/// </summary>
public static class SoulSystemService
{
    private static readonly ConditionalWeakTable<Player, List<(IElement Element, AttributeDefinition Target)>> ActiveBonuses = new();

    /// <summary>
    /// Builds the status snapshot for the client.
    /// </summary>
    public static SoulSystemStatus BuildStatus(Player player)
    {
        EnsureDefinitions(player);
        var alloc = new byte[SoulSystemCatalog.SlotCount];
        for (var i = 0; i < SoulSystemCatalog.SlotCount; i++)
        {
            alloc[i] = (byte)Math.Clamp(GetStatValue(player, SoulSystemCatalog.AllocAttributes[i]), 0, SoulSystemCatalog.MaxPointsPerColumn);
        }

        return new SoulSystemStatus
        {
            Remaining = (int)Math.Max(0, GetStatValue(player, SoulSystemCatalog.RemainingAttribute)),
            Allocations = alloc,
        };
    }

    /// <summary>
    /// Sets one column allocation to an absolute value (0..100), spending/refunding remaining.
    /// </summary>
    public static SoulSystemResult TrySetAllocation(Player player, byte tab, byte col, byte value)
    {
        if (player.SelectedCharacter is null || player.Attributes is null)
        {
            return SoulSystemResult.Failed;
        }

        if (tab >= SoulSystemCatalog.TabCount || col >= SoulSystemCatalog.ColumnCount)
        {
            return SoulSystemResult.InvalidRequest;
        }

        EnsureDefinitions(player);
        var index = SoulSystemCatalog.Index(tab, col);
        var allocStat = GetOrCreateStat(player, SoulSystemCatalog.AllocAttributes[index]);
        var remainingStat = GetOrCreateStat(player, SoulSystemCatalog.RemainingAttribute);

        BaseAttribute allocBase = allocStat;
        BaseAttribute remainingBase = remainingStat;
        var oldValue = (int)Math.Clamp(allocBase.Value, 0, SoulSystemCatalog.MaxPointsPerColumn);
        var newValue = Math.Clamp((int)value, 0, SoulSystemCatalog.MaxPointsPerColumn);
        var delta = newValue - oldValue;
        if (delta == 0)
        {
            return SoulSystemResult.Success;
        }

        var remaining = (int)Math.Max(0, remainingBase.Value);
        if (delta > 0 && remaining < delta)
        {
            return SoulSystemResult.NotEnoughPoints;
        }

        remainingStat.Value = remaining - delta;
        allocStat.Value = newValue;
        ApplyBonuses(player);
        return SoulSystemResult.Success;
    }

    /// <summary>
    /// Refunds all allocated points back to remaining.
    /// </summary>
    public static SoulSystemResult TryResetAllocations(Player player)
    {
        if (player.SelectedCharacter is null)
        {
            return SoulSystemResult.Failed;
        }

        EnsureDefinitions(player);
        var remainingStat = GetOrCreateStat(player, SoulSystemCatalog.RemainingAttribute);
        BaseAttribute remainingBase = remainingStat;
        var refund = 0;
        for (var i = 0; i < SoulSystemCatalog.SlotCount; i++)
        {
            var alloc = GetOrCreateStat(player, SoulSystemCatalog.AllocAttributes[i]);
            BaseAttribute allocBase = alloc;
            refund += (int)Math.Clamp(allocBase.Value, 0, SoulSystemCatalog.MaxPointsPerColumn);
            alloc.Value = 0;
        }

        remainingStat.Value = Math.Max(0, remainingBase.Value) + refund;
        ApplyBonuses(player);
        return SoulSystemResult.Success;
    }

    /// <summary>
    /// Grants soul points after a character reset.
    /// </summary>
    public static void GrantResetReward(Player player, int amount = SoulSystemCatalog.ResetRewardCount)
    {
        if (player.SelectedCharacter is null || amount <= 0)
        {
            return;
        }

        EnsureDefinitions(player);
        var remaining = GetOrCreateStat(player, SoulSystemCatalog.RemainingAttribute);
        BaseAttribute remainingBase = remaining;
        remaining.Value = Math.Max(0, remainingBase.Value) + amount;
    }

    /// <summary>
    /// Clears previous soul power-ups and re-applies from current allocations.
    /// </summary>
    public static void ApplyBonuses(Player player)
    {
        if (player.Attributes is null || player.SelectedCharacter is null)
        {
            return;
        }

        EnsureDefinitions(player);
        ClearBonuses(player);

        var applied = ActiveBonuses.GetOrCreateValue(player);
        for (var tab = 0; tab < SoulSystemCatalog.TabCount; tab++)
        {
            for (var col = 0; col < SoulSystemCatalog.ColumnCount; col++)
            {
                var points = (int)Math.Clamp(
                    GetStatValue(player, SoulSystemCatalog.AllocAttributes[SoulSystemCatalog.Index(tab, col)]),
                    0,
                    SoulSystemCatalog.MaxPointsPerColumn);
                if (points <= 0)
                {
                    continue;
                }

                var element = SoulSystemCatalog.GetElement(tab, col);
                var mainValue = points * element.ValuePerPoint;
                ApplyBonus(player, applied, element.Kind, mainValue, element.Targets);

                foreach (var sub in element.Subs)
                {
                    if (points >= sub.ReqPoints)
                    {
                        ApplyBonus(player, applied, sub.Kind, sub.Value, sub.Targets);
                    }
                }
            }
        }
    }

    private static void ClearBonuses(Player player)
    {
        if (!ActiveBonuses.TryGetValue(player, out var list) || player.Attributes is null)
        {
            return;
        }

        foreach (var (element, target) in list)
        {
            player.Attributes.RemoveElement(element, target);
        }

        list.Clear();
    }

    private static void ApplyBonus(
        Player player,
        List<(IElement Element, AttributeDefinition Target)> applied,
        SoulBonusKind kind,
        float value,
        AttributeDefinition[] targets)
    {
        if (player.Attributes is null || value == 0f || targets.Length == 0)
        {
            return;
        }

        switch (kind)
        {
            case SoulBonusKind.Flat:
            case SoulBonusKind.AllStatsFlat:
                foreach (var target in targets)
                {
                    Add(player, applied, new SimpleElement(value, AggregateType.AddRaw), target);
                }

                break;

            case SoulBonusKind.ChancePercent:
                foreach (var target in targets)
                {
                    Add(player, applied, new SimpleElement(value / 100f, AggregateType.AddRaw), target);
                }

                break;

            case SoulBonusKind.BonusRatePercent:
                foreach (var target in targets)
                {
                    Add(player, applied, new SimpleElement(value / 100f, AggregateType.AddRaw), target);
                }

                break;

            case SoulBonusKind.MultiplicativePercent:
                foreach (var target in targets)
                {
                    Add(player, applied, new SimpleElement(1f + (value / 100f), AggregateType.Multiplicate), target);
                }

                break;

            case SoulBonusKind.MoneyRatePercent:
                foreach (var target in targets)
                {
                    Add(player, applied, new SimpleElement(1f + (value / 100f), AggregateType.Multiplicate), target);
                }

                break;

            case SoulBonusKind.DamageDecreasePercent:
                foreach (var target in targets)
                {
                    var factor = Math.Max(0.01f, 1f - (value / 100f));
                    Add(player, applied, new SimpleElement(factor, AggregateType.Multiplicate), target);
                }

                break;
        }
    }

    private static void Add(
        Player player,
        List<(IElement Element, AttributeDefinition Target)> applied,
        IElement element,
        AttributeDefinition target)
    {
        player.Attributes!.AddElement(element, target);
        applied.Add((element, target));
    }

    private static float GetStatValue(Player player, AttributeDefinition template)
    {
        var attr = player.SelectedCharacter?.Attributes
            .FirstOrDefault(a => a.Definition is not null && a.Definition.Id == template.Id);
        if (attr is null)
        {
            return 0f;
        }

        // StatAttribute.Value is `new` and may clamp via MaximumValue; read the raw stored value.
        BaseAttribute asBase = attr;
        return Math.Max(0f, asBase.Value);
    }

    private static StatAttribute GetOrCreateStat(Player player, AttributeDefinition template)
    {
        var character = player.SelectedCharacter
            ?? throw new InvalidOperationException("No character selected.");
        var existing = character.Attributes
            .FirstOrDefault(a => a.Definition is not null && a.Definition.Id == template.Id);
        if (existing is not null)
        {
            if (existing.Definition?.MaximumValue is not null)
            {
                existing.Definition.MaximumValue = null;
            }

            return existing;
        }

        var definition = player.GameContext.Configuration.Attributes.First(a => a.Id == template.Id);
        if (definition.MaximumValue is not null)
        {
            definition.MaximumValue = null;
        }

        var created = player.PersistenceContext.CreateNew<StatAttribute>(definition, 0);
        character.Attributes.Add(created);
        return created;
    }

    private static void EnsureDefinitions(Player player)
    {
        EnsureDefinition(player, SoulSystemCatalog.RemainingAttribute);
        foreach (var attr in SoulSystemCatalog.AllocAttributes)
        {
            EnsureDefinition(player, attr);
        }
    }

    private static void EnsureDefinition(Player player, AttributeDefinition template)
    {
        var config = player.GameContext.Configuration;
        var existing = config.Attributes.FirstOrDefault(a => a.Id == template.Id);
        if (existing is not null)
        {
            existing.MaximumValue = null;
            if (string.IsNullOrEmpty(existing.Designation))
            {
                existing.Designation = template.Designation;
            }

            return;
        }

        try
        {
            var persistent = player.PersistenceContext.CreateNew<AttributeDefinition>(
                template.Id, template.Designation, template.Description);
            persistent.MaximumValue = null;
            config.Attributes.Add(persistent);
        }
        catch
        {
            // Already created concurrently.
        }
    }
}
