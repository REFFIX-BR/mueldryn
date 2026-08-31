// <copyright file="PegasusCollectionService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Collections;

using System.Runtime.CompilerServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Handles Pegasus Collections donate / sync (client F3:70/71/72).
/// </summary>
public static class PegasusCollectionService
{
    private static readonly ConditionalWeakTable<Player, StrongBox<IElement?>> AppliedHpBonusElements = new();

    public enum DonateResult : byte
    {
        Ok = 0,
        Failed = 1,
        Already = 2,
        WrongItem = 3,
        NoItem = 4,
        Range = 5,
        NoRoom = 6,
    }

    public static uint[] ReadMask(Player player)
    {
        EnsureDefinitions(player);
        return ReadMaskFromStats(player);
    }

    /// <summary>
    /// Catch up missing set rewards and apply permanent Max HP before the enter-game
    /// reclaim clamp. Must run before <c>SetReclaimableAttributesBeforeEnterGame</c>,
    /// otherwise CurrentHealth is truncated to Max HP without the Collections bonus.
    /// </summary>
    public static async ValueTask PrepareAttributesForEnterWorldAsync(Player player)
    {
        if (player.Attributes is null || player.SelectedCharacter is null)
        {
            return;
        }

        EnsureDefinitions(player);
        var bits = ReadMask(player);
        await CatchUpMissingRewardsAsync(player, bits).ConfigureAwait(false);
        ApplyStoredHpBonus(player);
    }

    public static async ValueTask SendSyncAsync(Player player)
    {
        EnsureDefinitions(player);
        var bits = ReadMask(player);
        await CatchUpMissingRewardsAsync(player, bits).ConfigureAwait(false);
        // Idempotent: safe if PrepareAttributesForEnterWorldAsync already applied.
        ApplyStoredHpBonus(player);
        bits = ReadMask(player);
        await player.InvokeViewPlugInAsync<IShowPegasusCollectionPlugIn>(
            p => p.ShowCollectionSyncAsync(bits)).ConfigureAwait(false);
    }

    public readonly struct DonateOutcome
    {
        public DonateOutcome(DonateResult result, byte setIdx, byte slot, bool completed, uint[] bits, uint rewardHp, uint rewardCoins)
        {
            this.Result = result;
            this.SetIdx = setIdx;
            this.Slot = slot;
            this.Completed = completed;
            this.Bits = bits;
            this.RewardHp = rewardHp;
            this.RewardCoins = rewardCoins;
        }

        public DonateResult Result { get; }
        public byte SetIdx { get; }
        public byte Slot { get; }
        public bool Completed { get; }
        public uint[] Bits { get; }
        public uint RewardHp { get; }
        public uint RewardCoins { get; }
    }

    public static async ValueTask<DonateOutcome> DonateAsync(Player player, byte setIdx, byte slot, byte inventorySlot)
    {
        var bits = player.SelectedCharacter is null ? new uint[PegasusCollectionCatalog.MaskDwordCount] : ReadMask(player);

        if (player.Inventory is null || player.SelectedCharacter is null)
        {
            return new DonateOutcome(DonateResult.Failed, setIdx, slot, false, bits, 0, 0);
        }

        if (setIdx >= PegasusCollectionCatalog.SetCount || slot >= PegasusCollectionCatalog.SlotCount)
        {
            return new DonateOutcome(DonateResult.Range, setIdx, slot, false, bits, 0, 0);
        }

        EnsureDefinitions(player);
        bits = ReadMask(player);
        if (GetBit(bits, setIdx, slot))
        {
            return new DonateOutcome(DonateResult.Already, setIdx, slot, false, bits, 0, 0);
        }

        var item = player.Inventory.GetItem(inventorySlot);
        if (item?.Definition is null)
        {
            return new DonateOutcome(DonateResult.NoItem, setIdx, slot, false, bits, 0, 0);
        }

        if (!ItemMatches(item, setIdx, slot))
        {
            return new DonateOutcome(DonateResult.WrongItem, setIdx, slot, false, bits, 0, 0);
        }

        var wasComplete = IsSetComplete(bits, setIdx);
        await player.DestroyInventoryItemAsync(item).ConfigureAwait(false);

        SetBit(bits, setIdx, slot);
        WriteMask(player, bits);
        // Persist immediately so the next donate reads the full mask (not only the last piece).
        await player.SaveProgressAsync().ConfigureAwait(false);

        var completed = false;
        uint rewardHp = 0;
        uint rewardCoins = 0;
        if (!wasComplete && IsSetComplete(bits, setIdx))
        {
            var granted = await GrantSetRewardAsync(player, setIdx, applyLiveHp: true).ConfigureAwait(false);
            if (granted)
            {
                completed = true;
                rewardHp = (uint)PegasusCollectionCatalog.RewardHp;
                rewardCoins = (uint)PegasusCollectionCatalog.RewardCoins;
                await player.SaveProgressAsync().ConfigureAwait(false);
            }
        }

        return new DonateOutcome(DonateResult.Ok, setIdx, slot, completed, bits, rewardHp, rewardCoins);
    }

    /// <summary>
    /// Grants HP / WCoinC / jewels for a completed set (once). Returns false if already claimed.
    /// </summary>
    /// <param name="applyLiveHp">When true, also pushes Max HP into the live attribute system (mid-session complete).</param>
    private static async ValueTask<bool> GrantSetRewardAsync(Player player, int setIdx, bool applyLiveHp)
    {
        if (player.SelectedCharacter is null || player.Inventory is null)
        {
            return false;
        }

        EnsureDefinitions(player);
        EnsureDefinition(player, Stats.WCoinC);
        EnsureDefinition(player, PegasusCollectionCatalog.BonusHpAttribute);
        EnsureDefinition(player, PegasusCollectionCatalog.RewardClaimedAttribute);

        var claimed = GetOrCreateStat(player, PegasusCollectionCatalog.RewardClaimedAttribute);
        var claimedBits = (uint)Math.Max(0, Math.Round(claimed.Value));
        var flag = 1u << setIdx;
        if ((claimedBits & flag) != 0)
        {
            return false;
        }

        // Jewels first (Soul + Bless). Drop at feet if inventory is full.
        foreach (var jewelId in PegasusCollectionCatalog.RewardJewels)
        {
            var definition = player.GameContext.Configuration.Items
                .FirstOrDefault(i => i.Group == jewelId.Group && i.Number == jewelId.Number);
            if (definition is null)
            {
                continue;
            }

            var item = player.PersistenceContext.CreateNew<Item>();
            item.Definition = definition;
            item.Durability = 1;
            item.Level = 0;

            if (await player.Inventory.AddItemAsync(item).ConfigureAwait(false))
            {
                await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
            }
            else if (player.CurrentMap is { } map)
            {
                await map.AddAsync(new DroppedItem(item, player.Position, map, player, player.GetAsEnumerable())).ConfigureAwait(false);
            }
            else
            {
                await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
            }
        }

        // WCoinC on account
        var wcoin = GetOrCreateAccountStat(player, Stats.WCoinC);
        wcoin.Value += PegasusCollectionCatalog.RewardCoins;

        // Permanent Max HP (stored; applied live below or on enter-world)
        var bonusHp = GetOrCreateStat(player, PegasusCollectionCatalog.BonusHpAttribute);
        bonusHp.Value += PegasusCollectionCatalog.RewardHp;
        if (applyLiveHp && player.Attributes is not null)
        {
            player.Attributes.AddElement(
                new SimpleElement(PegasusCollectionCatalog.RewardHp, AggregateType.AddRaw),
                Stats.MaximumHealth);
            player.Attributes[Stats.CurrentHealth] = Math.Min(
                player.Attributes[Stats.CurrentHealth] + PegasusCollectionCatalog.RewardHp,
                player.Attributes[Stats.MaximumHealth]);
            await player.InvokeViewPlugInAsync<IUpdateStatsPlugIn>(
                p => p.UpdateStatsAsync(Stats.MaximumHealth, player.Attributes[Stats.MaximumHealth])).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IUpdateStatsPlugIn>(
                p => p.UpdateStatsAsync(Stats.CurrentHealth, player.Attributes[Stats.CurrentHealth])).ConfigureAwait(false);
        }

        claimed.Value = claimedBits | flag;
        return true;
    }

    private static async ValueTask CatchUpMissingRewardsAsync(Player player, uint[] bits)
    {
        var any = false;
        for (var setIdx = 0; setIdx < PegasusCollectionCatalog.SetCount; setIdx++)
        {
            if (!IsSetComplete(bits, setIdx))
            {
                continue;
            }

            // applyLiveHp:false — enter-world will ApplyStoredHpBonus for the total.
            if (await GrantSetRewardAsync(player, setIdx, applyLiveHp: false).ConfigureAwait(false))
            {
                any = true;
            }
        }

        if (any)
        {
            await player.SaveProgressAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Applies the permanent Collections Max HP bonus to the live attribute system.
    /// Idempotent per player attribute system instance.
    /// </summary>
    public static void ApplyStoredHpBonus(Player player)
    {
        if (player.Attributes is null || player.SelectedCharacter is null)
        {
            return;
        }

        EnsureDefinition(player, PegasusCollectionCatalog.BonusHpAttribute);
        var bonus = GetStatValue(player, PegasusCollectionCatalog.BonusHpAttribute);
        var slot = AppliedHpBonusElements.GetOrCreateValue(player);

        if (slot.Value is { } previous)
        {
            player.Attributes.RemoveElement(previous, Stats.MaximumHealth);
            slot.Value = null;
        }

        if (bonus <= 0)
        {
            return;
        }

        var maxBefore = player.Attributes[Stats.MaximumHealth];
        var current = player.Attributes[Stats.CurrentHealth];
        var element = new SimpleElement(bonus, AggregateType.AddRaw);
        player.Attributes.AddElement(element, Stats.MaximumHealth);
        slot.Value = element;

        // Previous logins clamped CurrentHealth to Max without this bonus and then saved it.
        // If HP sits exactly on the pre-bonus max, restore the missing bonus amount.
        if (Math.Abs(current - maxBefore) < 0.5f)
        {
            player.Attributes[Stats.CurrentHealth] = Math.Min(
                current + bonus,
                player.Attributes[Stats.MaximumHealth]);
        }
    }

    private static StatAttribute GetOrCreateAccountStat(Player player, AttributeDefinition template)
    {
        var account = player.Account ?? throw new InvalidOperationException("No account.");
        var existing = account.Attributes
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
        definition.MaximumValue = null;
        var created = player.PersistenceContext.CreateNew<StatAttribute>(definition, 0);
        account.Attributes.Add(created);
        return created;
    }

    private static bool ItemMatches(Item item, byte setIdx, byte slot)
    {
        var want = PegasusCollectionCatalog.Pieces[setIdx, slot];
        if (item.Definition!.Group != want.Group || item.Definition.Number != want.Number)
        {
            return false;
        }

        if (item.Level < PegasusCollectionCatalog.RequiredLevel)
        {
            return false;
        }

        if (!item.ItemOptions.Any(o => o.ItemOption?.OptionType == ItemOptionTypes.Luck))
        {
            return false;
        }

        var option = item.ItemOptions.FirstOrDefault(o => o.ItemOption?.OptionType == ItemOptionTypes.Option);
        if (option is null || option.Level < PegasusCollectionCatalog.RequiredOptionLevel)
        {
            return false;
        }

        var hasReflect = item.ItemOptions.Any(o =>
            o.ItemOption?.OptionType == ItemOptionTypes.Excellent
            && (ReferenceEquals(o.ItemOption.PowerUpDefinition?.TargetAttribute, PegasusCollectionCatalog.RequiredExcellentStat)
                || o.ItemOption.PowerUpDefinition?.TargetAttribute?.Id == PegasusCollectionCatalog.RequiredExcellentStat.Id
                || o.ItemOption.LevelDependentOptions.Any(l =>
                    l.PowerUpDefinition?.TargetAttribute?.Id == PegasusCollectionCatalog.RequiredExcellentStat.Id)));

        return hasReflect;
    }

    private static bool GetBit(uint[] bits, int setIdx, int slot)
    {
        var bit = setIdx * PegasusCollectionCatalog.SlotCount + slot;
        return ((bits[bit / 32] >> (bit % 32)) & 1u) != 0;
    }

    private static void SetBit(uint[] bits, int setIdx, int slot)
    {
        var bit = setIdx * PegasusCollectionCatalog.SlotCount + slot;
        bits[bit / 32] |= 1u << (bit % 32);
    }

    private static bool IsSetComplete(uint[] bits, int setIdx)
    {
        for (var i = 0; i < PegasusCollectionCatalog.SlotCount; i++)
        {
            if (!GetBit(bits, setIdx, i))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Persist as small integer floats (0-1023 per attr). Bit-reinterpreted floats are rejected by
    /// <see cref="StatAttribute.Value"/> when |delta| &lt; 0.01, which broke multi-donate progress.
    /// Layout: 6 sets × 5 slots = 30 bits, packed into 3×10-bit chunks.
    /// </summary>
    private static void WriteMask(Player player, uint[] bits)
    {
        uint all = 0;
        for (var setIdx = 0; setIdx < PegasusCollectionCatalog.SetCount; setIdx++)
        {
            for (var slot = 0; slot < PegasusCollectionCatalog.SlotCount; slot++)
            {
                if (GetBit(bits, setIdx, slot))
                {
                    all |= 1u << (setIdx * PegasusCollectionCatalog.SlotCount + slot);
                }
            }
        }

        for (var i = 0; i < PegasusCollectionCatalog.MaskDwordCount; i++)
        {
            var chunk = (float)((all >> (i * 10)) & 0x3FFu);
            var stat = GetOrCreateStat(player, PegasusCollectionCatalog.MaskAttributes[i]);
            // Integer steps (>=1) always pass StatAttribute's |delta|>0.01 guard.
            if (Math.Abs(stat.Value - chunk) > 0.01f)
                stat.Value = chunk;
            else if ((uint)Math.Round(stat.Value) != (uint)chunk)
            {
                // Break out of near-zero denormals left by the old bit-reinterpret storage.
                stat.Value = chunk + 1f;
                stat.Value = chunk;
            }
        }

        // Keep wire format consistent: bits[0] holds packed 30 flags, others clear.
        bits[0] = all;
        if (bits.Length > 1)
        {
            bits[1] = 0;
        }

        if (bits.Length > 2)
        {
            bits[2] = 0;
        }
    }

    private static uint[] ReadMaskFromStats(Player player)
    {
        uint all = 0;
        for (var i = 0; i < PegasusCollectionCatalog.MaskDwordCount; i++)
        {
            var raw = GetStatValue(player, PegasusCollectionCatalog.MaskAttributes[i]);
            var chunk = (uint)Math.Clamp(Math.Round(raw), 0, 1023);
            all |= chunk << (i * 10);
        }

        return [all, 0u, 0u];
    }

    private static float GetStatValue(Player player, AttributeDefinition template)
    {
        var attr = player.SelectedCharacter?.Attributes
            .FirstOrDefault(a => a.Definition is not null && a.Definition.Id == template.Id);
        return attr?.Value ?? 0f;
    }

    private static StatAttribute GetOrCreateStat(Player player, AttributeDefinition template)
    {
        var character = player.SelectedCharacter
            ?? throw new InvalidOperationException("No character selected.");
        var existing = character.Attributes
            .FirstOrDefault(a => a.Definition is not null && a.Definition.Id == template.Id);
        if (existing is not null)
        {
            // Collection masks must never be clamped (MaximumValue would drop bits).
            if (existing.Definition?.MaximumValue is not null)
            {
                existing.Definition.MaximumValue = null;
            }

            return existing;
        }

        var definition = player.GameContext.Configuration.Attributes.FirstOrDefault(a => a.Id == template.Id)
            ?? throw new InvalidOperationException($"Missing collection attribute definition {template.Id}. Apply AddPegasusCollectionAttributes update.");
        definition.MaximumValue = null;
        var created = player.PersistenceContext.CreateNew<StatAttribute>(definition, 0);
        character.Attributes.Add(created);
        return created;
    }

    private static void EnsureDefinitions(Player player)
    {
        foreach (var attr in PegasusCollectionCatalog.MaskAttributes)
        {
            EnsureDefinition(player, attr);
        }

        EnsureDefinition(player, PegasusCollectionCatalog.BonusHpAttribute);
        EnsureDefinition(player, PegasusCollectionCatalog.RewardClaimedAttribute);
        EnsureDefinition(player, Stats.WCoinC);
    }

    private static void EnsureDefinition(Player player, AttributeDefinition template)
    {
        var config = player.GameContext.Configuration;
        var existing = config.Attributes.FirstOrDefault(a => a.Id == template.Id);
        if (existing is not null)
        {
            existing.MaximumValue = null;
            return;
        }

        // Player persistence context can CreateNew AttributeDefinition, but only EF model
        // instances may be added to GameConfiguration.Attributes (CollectionAdapter).
        // Never Add(template) — plain AttributeSystem definitions throw ArgumentException
        // and abort character enter (client stuck on NOW LOADING).
        try
        {
            var persistent = player.PersistenceContext.CreateNew<AttributeDefinition>(
                template.Id, template.Designation, template.Description);
            persistent.MaximumValue = null;
            config.Attributes.Add(persistent);
        }
        catch
        {
            // Missing rows must come from FixPegasusCollectionAttributes update plugin.
            // Concurrent create or wrong context — leave config unchanged.
        }
    }
}
