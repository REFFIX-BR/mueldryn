// <copyright file="VipShopService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.VipShop;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Account VIP purchase (Shopping VIP): WP cost, 30 days, +15% exp/drop display.
/// </summary>
public static class VipShopService
{
    /// <summary>Plan duration in days.</summary>
    public const int PlanDays = 30;

    /// <summary>WCoinC price (0 = not used).</summary>
    public const int PriceWc = 0;

    /// <summary>WCoinP / WP price.</summary>
    public const int PriceWp = 3000;

    /// <summary>Token Event price (0 = not used).</summary>
    public const int PriceToken = 0;

    /// <summary>Displayed experience bonus percent.</summary>
    public const int ExpBonusPercent = 15;

    /// <summary>Displayed drop bonus percent.</summary>
    public const int DropBonusPercent = 15;

    /// <summary>Buy result codes for the client.</summary>
    public enum BuyResult : byte
    {
        Success = 0,
        Failed = 1,
        NotEnoughPoints = 2,
        AlreadyActive = 3,
    }

    /// <summary>Snapshot for the Shopping VIP UI.</summary>
    public sealed class VipShopStatus
    {
        public bool IsVip { get; init; }

        public int RemainingDays { get; init; }

        public string CharacterName { get; init; } = string.Empty;
    }

    /// <summary>Builds UI status for the logged-in character.</summary>
    public static VipShopStatus BuildStatus(Player player)
    {
        EnsureDefinitions(player);
        RefreshVipFlag(player);
        var remain = GetRemainingDays(player);
        return new VipShopStatus
        {
            IsVip = remain > 0,
            RemainingDays = remain,
            CharacterName = player.SelectedCharacter?.Name ?? string.Empty,
        };
    }

    /// <summary>Purchases VIP with WP and extends account VIP by <see cref="PlanDays"/>.</summary>
    public static BuyResult TryBuy(Player player)
    {
        if (player.Account is null || player.SelectedCharacter is null)
        {
            return BuyResult.Failed;
        }

        EnsureDefinitions(player);
        RefreshVipFlag(player);

        var wp = GetOrCreateAccountStat(player, Stats.WCoinP);
        if (PriceWp > 0 && wp.Value < PriceWp)
        {
            return BuyResult.NotEnoughPoints;
        }

        if (PriceWc > 0)
        {
            var wc = GetOrCreateAccountStat(player, Stats.WCoinC);
            if (wc.Value < PriceWc)
            {
                return BuyResult.NotEnoughPoints;
            }

            wc.Value -= PriceWc;
        }

        if (PriceWp > 0)
        {
            wp.Value -= PriceWp;
        }

        var nowDays = CurrentDayNumber();
        var expire = GetOrCreateAccountStat(player, Stats.VipExpireDay);
        var baseDay = Math.Max(expire.Value, nowDays);
        expire.Value = baseDay + PlanDays;

        var isVip = GetOrCreateAccountStat(player, Stats.IsVip);
        isVip.Value = 1;

        // Live attribute system (account Stats.IsVip is already linked).
        if (player.Attributes is not null)
        {
            player.Attributes[Stats.IsVip] = 1;
        }

        ApplyVipBonuses(player);
        return BuyResult.Success;
    }

    /// <summary>True when account VIP is still valid.</summary>
    public static bool IsVipActive(Player player) => GetRemainingDays(player) > 0;

    /// <summary>Applies +15% bonus experience while VIP is active.</summary>
    public static void ApplyVipBonuses(Player player)
    {
        if (player.Attributes is null || !IsVipActive(player))
        {
            return;
        }

        // Additive 0.15 on BonusExperienceRate (1.0 base → +15%).
        const float bonus = ExpBonusPercent / 100f;
        player.Attributes[Stats.BonusExperienceRate] =
            Math.Max(player.Attributes[Stats.BonusExperienceRate], bonus);
        player.Attributes[Stats.MoneyAmountRate] =
            Math.Max(player.Attributes[Stats.MoneyAmountRate], 1f + bonus);
    }

    private static int GetRemainingDays(Player player)
    {
        var expire = player.Account?.Attributes
            .FirstOrDefault(a => a.Definition is not null && a.Definition.Id == Stats.VipExpireDay.Id);
        if (expire is null || expire.Value <= 0)
        {
            return 0;
        }

        var remain = (int)Math.Ceiling(expire.Value - CurrentDayNumber());
        return Math.Max(0, remain);
    }

    private static void RefreshVipFlag(Player player)
    {
        var isVip = GetOrCreateAccountStat(player, Stats.IsVip);
        var active = GetRemainingDays(player) > 0;
        isVip.Value = active ? 1 : 0;
        if (player.Attributes is not null)
        {
            player.Attributes[Stats.IsVip] = isVip.Value;
        }
    }

    private static float CurrentDayNumber()
        => (float)(DateTime.UtcNow - DateTime.UnixEpoch).TotalDays;

    private static void EnsureDefinitions(Player player)
    {
        EnsureDefinition(player, Stats.IsVip);
        EnsureDefinition(player, Stats.VipExpireDay);
        EnsureDefinition(player, Stats.WCoinC);
        EnsureDefinition(player, Stats.WCoinP);
    }

    private static void EnsureDefinition(Player player, AttributeDefinition template)
    {
        var config = player.GameContext.Configuration;
        if (config.Attributes.Any(a => a.Id == template.Id))
        {
            var existing = config.Attributes.First(a => a.Id == template.Id);
            if (existing.MaximumValue is 0f)
            {
                existing.MaximumValue = null;
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
        catch (InvalidOperationException)
        {
            template.MaximumValue = null;
            config.Attributes.Add(template);
        }
    }

    private static StatAttribute GetOrCreateAccountStat(Player player, AttributeDefinition template)
    {
        var account = player.Account ?? throw new InvalidOperationException("No account.");
        var existing = account.Attributes
            .FirstOrDefault(a => a.Definition is not null && a.Definition.Id == template.Id);
        if (existing is not null)
        {
            if (existing.Definition?.MaximumValue is 0f)
            {
                existing.Definition.MaximumValue = null;
            }

            return existing;
        }

        var definition = player.GameContext.Configuration.Attributes.First(a => a.Id == template.Id);
        var created = player.PersistenceContext.CreateNew<StatAttribute>(definition, 0);
        account.Attributes.Add(created);
        return created;
    }
}
