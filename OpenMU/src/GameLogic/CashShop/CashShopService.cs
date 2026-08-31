// <copyright file="CashShopService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.CashShop;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>
/// Handles cash shop purchases: deduct account coins and deliver the items to the inventory.
/// </summary>
public static class CashShopService
{
    private const int ClientItemIndexStride = 512;

    /// <summary>
    /// Result codes matching MuMain ReceiveIGS_BuyItem.
    /// </summary>
    public enum BuyResult : byte
    {
        Success = 0,
        NotEnoughPoints = 1,
        InventoryFull = 2,
        SoldOut = 3,
        NotAvailable = 4,
        NoLongerAvailable = 5,
        CannotBuy = 6,
    }

    /// <summary>
    /// Attempts to buy a cash shop offer.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="packageSeq">Package sequence of the shop entry.</param>
    /// <param name="priceSeq">Selected price row, 0 for single price packages.</param>
    /// <param name="itemCode">Item code the client displayed (group*512+number).</param>
    /// <param name="coinType">Coin type the client asked to pay with (508/509/510).</param>
    /// <returns>Buy result and remaining coin balance for the used currency.</returns>
    public static async ValueTask<(BuyResult Result, int Remaining)> TryBuyAsync(
        Player player,
        uint packageSeq,
        uint priceSeq,
        ushort itemCode,
        uint coinType)
    {
        if (player.SelectedCharacter is null || player.Account is null || player.Inventory is null)
        {
            return (BuyResult.CannotBuy, 0);
        }

        if (!CashShopCatalog.TryGetOffer(packageSeq, priceSeq, itemCode, out var offer))
        {
            return (BuyResult.NotAvailable, 0);
        }

        if (offer.Price < 0)
        {
            return (BuyResult.CannotBuy, 0);
        }

        var coinAttr = ResolveCoinAttribute(coinType, offer.CashType);
        EnsureAccountStat(player, coinAttr);
        var balanceAttr = GetOrCreateAccountStat(player, coinAttr);
        if (balanceAttr.Value < offer.Price)
        {
            return (BuyResult.NotEnoughPoints, (int)balanceAttr.Value);
        }

        var deliveries = new List<(CashShopCatalog.ShopItem Line, ItemDefinition Definition)>();
        var rewardPoints = 0;
        foreach (var line in offer.Items)
        {
            rewardPoints += line.RewardPoints;
            if (line.ItemCode == 0)
            {
                continue;
            }

            var definition = FindDefinition(player, line.ItemCode);
            if (definition is null)
            {
                return (BuyResult.NotAvailable, (int)balanceAttr.Value);
            }

            deliveries.Add((line, definition));
        }

        if (deliveries.Count == 0 && rewardPoints == 0)
        {
            return (BuyResult.NotAvailable, (int)balanceAttr.Value);
        }

        var created = new List<Item>();
        var buffs = new List<(MagicEffectDefinition Effect, TimeSpan Duration)>();
        foreach (var (line, definition) in deliveries)
        {
            if (line.DurationSeconds > 0 && definition.ConsumeEffect is { } buffEffect)
            {
                // Buffs are not carried around in the inventory, they start right away and run for
                // the bought period.
                buffs.Add((buffEffect, TimeSpan.FromSeconds((double)line.DurationSeconds * Math.Max(1, line.Quantity))));
                continue;
            }

            if (!await TryDeliverAsync(player, line, definition, created).ConfigureAwait(false))
            {
                // A bundle must arrive complete, otherwise the player pays for items he never got.
                await RollbackAsync(player, created).ConfigureAwait(false);
                return (BuyResult.InventoryFull, (int)balanceAttr.Value);
            }
        }

        balanceAttr.Value -= offer.Price;

        if (rewardPoints > 0)
        {
            EnsureAccountStat(player, Stats.GoblinPoints);
            var goblin = GetOrCreateAccountStat(player, Stats.GoblinPoints);
            goblin.Value += rewardPoints;
        }

        foreach (var item in created)
        {
            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
            if (item.HasExpiration)
            {
                await player.InvokeViewPlugInAsync<IShowPeriodItemsPlugIn>(p => p.ShowPeriodItemAsync(item)).ConfigureAwait(false);
            }
        }

        foreach (var (effect, duration) in buffs)
        {
            await ShopBuffService.AddAsync(player, effect, duration).ConfigureAwait(false);
        }

        return (BuyResult.Success, (int)balanceAttr.Value);
    }

    private static async ValueTask<bool> TryDeliverAsync(
        Player player,
        CashShopCatalog.ShopItem line,
        ItemDefinition definition,
        List<Item> created)
    {
        // Stackable goods keep the piece count in Durability; everything else needs one slot each.
        var isWearable = definition.ItemSlot is not null;
        var maxStack = isWearable ? 1 : Math.Max(1, (int)definition.Durability);
        var remaining = Math.Max(1, line.Quantity);

        while (remaining > 0)
        {
            var amount = Math.Min(maxStack, remaining);
            var item = player.PersistenceContext.CreateNew<Item>();
            item.Definition = definition;
            item.Level = 0;
            item.Durability = isWearable ? definition.Durability : amount;
            if (line.DurationSeconds > 0)
            {
                item.ExpirationDate = DateTime.UtcNow.AddSeconds(line.DurationSeconds);
            }

            if (!await player.Inventory!.AddItemAsync(item).ConfigureAwait(false))
            {
                await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
                return false;
            }

            created.Add(item);
            remaining -= amount;
        }

        return true;
    }

    private static async ValueTask RollbackAsync(Player player, List<Item> created)
    {
        foreach (var item in created)
        {
            await player.Inventory!.RemoveItemAsync(item).ConfigureAwait(false);
            await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
        }

        created.Clear();
    }

    private static ItemDefinition? FindDefinition(Player player, ushort itemCode)
    {
        var group = (byte)(itemCode / ClientItemIndexStride);
        var number = (short)(itemCode % ClientItemIndexStride);
        return player.GameContext.Configuration.Items
            .FirstOrDefault(i => i.Group == group && i.Number == number);
    }

    private static AttributeDefinition ResolveCoinAttribute(uint requested, uint scriptCashType)
    {
        // The client echoes the currency of the script; fall back to it when the request is unset.
        var coinType = requested is CashShopCatalog.CoinTypeWCoinC
            or CashShopCatalog.CoinTypeWCoinP
            or CashShopCatalog.CoinTypeGoblin
            ? requested
            : scriptCashType;

        return coinType switch
        {
            CashShopCatalog.CoinTypeWCoinP => Stats.WCoinP,
            CashShopCatalog.CoinTypeGoblin => Stats.GoblinPoints,
            _ => Stats.WCoinC,
        };
    }

    private static void EnsureAccountStat(Player player, AttributeDefinition template)
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
                template.Id,
                template.Designation,
                template.Description);
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
        var account = player.Account
            ?? throw new InvalidOperationException("No account.");
        var existing = account.Attributes
            .FirstOrDefault(a => a.Definition is not null && a.Definition.Id == template.Id);
        if (existing is not null)
        {
            return existing;
        }

        var definition = player.GameContext.Configuration.Attributes.First(a => a.Id == template.Id);
        var created = player.PersistenceContext.CreateNew<StatAttribute>(definition, 0);
        account.Attributes.Add(created);
        return created;
    }
}
