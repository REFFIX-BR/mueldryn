// <copyright file="PersonalShopPriceTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

/// <summary>Tests the personal-shop price wire encoding.</summary>
[TestFixture]
public class PersonalShopPriceTest
{
    /// <summary>Ensures every supported currency round-trips through the existing price field.</summary>
    [TestCase(PersonalShopCurrency.Zen)]
    [TestCase(PersonalShopCurrency.Bless)]
    [TestCase(PersonalShopCurrency.Soul)]
    [TestCase(PersonalShopCurrency.Chaos)]
    [TestCase(PersonalShopCurrency.WCoinC)]
    [TestCase(PersonalShopCurrency.WCoinP)]
    [TestCase(PersonalShopCurrency.GoblinPoint)]
    public void SupportedCurrencyRoundTrips(PersonalShopCurrency currency)
    {
        const int amount = 12345678;
        var encoded = ((int)currency << 28) | amount;

        var success = PersonalShopPrice.TryDecode(encoded, out var decoded);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(decoded.Currency, Is.EqualTo(currency));
            Assert.That(decoded.Amount, Is.EqualTo(amount));
        });
    }

    /// <summary>Ensures invalid and zero prices are rejected.</summary>
    [TestCase(0)]
    [TestCase(0x70000001)]
    [TestCase(-1)]
    public void InvalidPriceIsRejected(int encoded)
    {
        Assert.That(PersonalShopPrice.TryDecode(encoded, out _), Is.False);
    }

    /// <summary>Ensures the maximum 28-bit amount remains valid.</summary>
    [Test]
    public void MaximumAmountIsAccepted()
    {
        const int maximum = 0x0FFFFFFF;
        var encoded = ((int)PersonalShopCurrency.GoblinPoint << 28) | maximum;

        Assert.That(PersonalShopPrice.TryDecode(encoded, out var decoded), Is.True);
        Assert.That(decoded.Amount, Is.EqualTo(maximum));
    }
}
