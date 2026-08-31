// <copyright file="PersonalShopPaymentService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

using System.Collections.Concurrent;
using System.Threading;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.JewelBank;
using MUnique.OpenMU.GameLogic.Views;

/// <summary>Currency encoded into bits 28-30 of the existing 32-bit store price.</summary>
public enum PersonalShopCurrency : byte
{
    Zen = 0,
    Bless = 1,
    Soul = 2,
    Chaos = 3,
    WCoinC = 4,
    WCoinP = 5,
    GoblinPoint = 6,
}

/// <summary>Decoded personal-shop price.</summary>
public readonly record struct PersonalShopPrice(PersonalShopCurrency Currency, int Amount)
{
    private const int AmountMask = 0x0FFFFFFF;

    /// <summary>Decodes and validates the value stored in <see cref="Item.StorePrice"/>.</summary>
    public static bool TryDecode(int encoded, out PersonalShopPrice price)
    {
        var currency = (PersonalShopCurrency)((encoded >> 28) & 0x7);
        var amount = encoded & AmountMask;
        price = new(currency, amount);
        return encoded > 0 && amount > 0 && currency <= PersonalShopCurrency.GoblinPoint;
    }
}

/// <summary>Result of transferring a personal-shop payment.</summary>
public enum PersonalShopPaymentResult
{
    Success,
    InvalidPrice,
    NotEnough,
    SellerCapacity,
    Failed,
}

/// <summary>A committed payment which can be explicitly rolled back.</summary>
public sealed class PersonalShopPaymentReceipt
{
    private readonly Func<ValueTask> _rollback;
    private bool _rolledBack;

    internal PersonalShopPaymentReceipt(Func<ValueTask> rollback) => this._rollback = rollback;

    /// <summary>Rolls the payment back once.</summary>
    public async ValueTask RollbackAsync()
    {
        if (this._rolledBack)
        {
            return;
        }

        this._rolledBack = true;
        await this._rollback().ConfigureAwait(false);
    }
}

/// <summary>Validates, transfers and rolls back personal-shop payments.</summary>
public static class PersonalShopPaymentService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> AccountLocks = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Transfers the encoded price from buyer to seller.</summary>
    public static async ValueTask<(PersonalShopPaymentResult Result, PersonalShopPaymentReceipt? Receipt)> TryTransferAsync(
        Player buyer,
        Player seller,
        int encodedPrice)
    {
        if (!PersonalShopPrice.TryDecode(encodedPrice, out var price)
            || buyer.Account is null
            || seller.Account is null)
        {
            return (PersonalShopPaymentResult.InvalidPrice, null);
        }

        var locks = await AcquireLocksAsync(buyer.Account.LoginName, seller.Account.LoginName).ConfigureAwait(false);
        try
        {
            var result = await TransferCoreAsync(buyer, seller, price).ConfigureAwait(false);
            if (result != PersonalShopPaymentResult.Success)
            {
                return (result, null);
            }

            return (result, new PersonalShopPaymentReceipt(
                () => RollbackAsync(buyer, seller, price)));
        }
        finally
        {
            ReleaseLocks(locks);
        }
    }

    private static async ValueTask<PersonalShopPaymentResult> TransferCoreAsync(
        Player buyer,
        Player seller,
        PersonalShopPrice price)
    {
        switch (price.Currency)
        {
            case PersonalShopCurrency.Zen:
                if (buyer.Money < price.Amount)
                {
                    return PersonalShopPaymentResult.NotEnough;
                }

                if (!buyer.TryRemoveMoney(price.Amount))
                {
                    return PersonalShopPaymentResult.NotEnough;
                }

                if (!seller.TryAddMoney(price.Amount))
                {
                    buyer.TryAddMoney(price.Amount);
                    return PersonalShopPaymentResult.SellerCapacity;
                }

                return PersonalShopPaymentResult.Success;

            case PersonalShopCurrency.Bless:
            case PersonalShopCurrency.Soul:
            case PersonalShopCurrency.Chaos:
            {
                var slot = ToJewelSlot(price.Currency);
                if (JewelBankService.CountInventoryUnits(buyer, slot) < price.Amount)
                {
                    return PersonalShopPaymentResult.NotEnough;
                }

                if (!await JewelBankService.TryConsumeInventoryUnitsAsync(buyer, slot, price.Amount).ConfigureAwait(false))
                {
                    return PersonalShopPaymentResult.Failed;
                }

                await JewelBankService.CreditAccountAsync(seller, slot, price.Amount).ConfigureAwait(false);
                var status = await JewelBankService.BuildStatusAsync(seller).ConfigureAwait(false);
                await seller.InvokeViewPlugInAsync<IShowJewelBankPlugIn>(
                    p => p.ShowJewelBankStatusAsync(status)).ConfigureAwait(false);
                return PersonalShopPaymentResult.Success;
            }

            case PersonalShopCurrency.WCoinC:
            case PersonalShopCurrency.WCoinP:
            case PersonalShopCurrency.GoblinPoint:
            {
                var definition = ToAccountStat(price.Currency);
                EnsureDefinition(buyer, definition);
                EnsureDefinition(seller, definition);
                var buyerStat = GetOrCreateAccountStat(buyer, definition);
                if (buyerStat.Value < price.Amount)
                {
                    return PersonalShopPaymentResult.NotEnough;
                }

                var sellerStat = GetOrCreateAccountStat(seller, definition);
                buyerStat.Value -= price.Amount;
                sellerStat.Value += price.Amount;
                return PersonalShopPaymentResult.Success;
            }

            default:
                return PersonalShopPaymentResult.InvalidPrice;
        }
    }

    private static async ValueTask RollbackAsync(Player buyer, Player seller, PersonalShopPrice price)
    {
        if (buyer.Account is null || seller.Account is null)
        {
            return;
        }

        var locks = await AcquireLocksAsync(buyer.Account.LoginName, seller.Account.LoginName).ConfigureAwait(false);
        try
        {
            switch (price.Currency)
            {
                case PersonalShopCurrency.Zen:
                    seller.TryRemoveMoney(price.Amount);
                    buyer.TryAddMoney(price.Amount);
                    break;
                case PersonalShopCurrency.Bless:
                case PersonalShopCurrency.Soul:
                case PersonalShopCurrency.Chaos:
                {
                    var slot = ToJewelSlot(price.Currency);
                    if (await JewelBankService.TryDebitAccountAsync(seller, slot, price.Amount).ConfigureAwait(false))
                    {
                        await JewelBankService.RestoreInventoryUnitsAsync(buyer, slot, price.Amount).ConfigureAwait(false);
                        var status = await JewelBankService.BuildStatusAsync(seller).ConfigureAwait(false);
                        await seller.InvokeViewPlugInAsync<IShowJewelBankPlugIn>(
                            p => p.ShowJewelBankStatusAsync(status)).ConfigureAwait(false);
                    }

                    break;
                }

                case PersonalShopCurrency.WCoinC:
                case PersonalShopCurrency.WCoinP:
                case PersonalShopCurrency.GoblinPoint:
                {
                    var definition = ToAccountStat(price.Currency);
                    var sellerStat = GetOrCreateAccountStat(seller, definition);
                    var buyerStat = GetOrCreateAccountStat(buyer, definition);
                    sellerStat.Value -= price.Amount;
                    buyerStat.Value += price.Amount;
                    break;
                }
            }
        }
        finally
        {
            ReleaseLocks(locks);
        }
    }

    private static JewelBankSlot ToJewelSlot(PersonalShopCurrency currency)
        => currency switch
        {
            PersonalShopCurrency.Bless => JewelBankSlot.Bless,
            PersonalShopCurrency.Soul => JewelBankSlot.Soul,
            _ => JewelBankSlot.Chaos,
        };

    private static AttributeDefinition ToAccountStat(PersonalShopCurrency currency)
        => currency switch
        {
            PersonalShopCurrency.WCoinP => Stats.WCoinP,
            PersonalShopCurrency.GoblinPoint => Stats.GoblinPoints,
            _ => Stats.WCoinC,
        };

    private static void EnsureDefinition(Player player, AttributeDefinition template)
    {
        var config = player.GameContext.Configuration;
        var existing = config.Attributes.FirstOrDefault(a => a.Id == template.Id);
        if (existing is not null)
        {
            existing.MaximumValue = null;
            return;
        }

        try
        {
            var created = player.PersistenceContext.CreateNew<AttributeDefinition>(
                template.Id,
                template.Designation,
                template.Description);
            created.MaximumValue = null;
            config.Attributes.Add(created);
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
        var existing = account.Attributes.FirstOrDefault(a => a.Definition?.Id == template.Id);
        if (existing is not null)
        {
            return existing;
        }

        var definition = player.GameContext.Configuration.Attributes.First(a => a.Id == template.Id);
        var created = player.PersistenceContext.CreateNew<StatAttribute>(definition, 0);
        account.Attributes.Add(created);
        return created;
    }

    private static async ValueTask<List<SemaphoreSlim>> AcquireLocksAsync(string firstAccount, string secondAccount)
    {
        var names = new[] { firstAccount, secondAccount }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);
        var acquired = new List<SemaphoreSlim>(2);
        foreach (var name in names)
        {
            var accountLock = AccountLocks.GetOrAdd(name, _ => new SemaphoreSlim(1, 1));
            await accountLock.WaitAsync().ConfigureAwait(false);
            acquired.Add(accountLock);
        }

        return acquired;
    }

    private static void ReleaseLocks(List<SemaphoreSlim> locks)
    {
        for (var i = locks.Count - 1; i >= 0; i--)
        {
            locks[i].Release();
        }
    }
}
