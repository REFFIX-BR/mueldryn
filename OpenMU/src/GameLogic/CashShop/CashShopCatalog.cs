// <copyright file="CashShopCatalog.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.CashShop;

using System.Globalization;
using System.IO;

/// <summary>
/// Reads the MuMain InGameShop scripts (IBSPackage.txt and IBSProduct.txt), so a purchase is
/// validated against the exact same catalog the client renders in its grid.
/// </summary>
public static class CashShopCatalog
{
    /// <summary>Official WCoin(C) coin type id used by Season 6 scripts.</summary>
    public const uint CoinTypeWCoinC = 508;

    /// <summary>Official WCoin(P) coin type id.</summary>
    public const uint CoinTypeWCoinP = 509;

    /// <summary>Goblin Points / mileage style coin type.</summary>
    public const uint CoinTypeGoblin = 510;

    private const char FieldSeparator = '@';
    private const char ListSeparator = '|';

    private static readonly Dictionary<uint, List<PriceRow>> RowsByProduct = new();
    private static readonly Dictionary<uint, PriceRow> RowsByPriceSeq = new();
    private static readonly Dictionary<uint, PackageRow> PackagesBySeq = new();
    private static readonly object LoadLock = new();

    private static bool _loaded;

    /// <summary>
    /// One deliverable line of an offer.
    /// </summary>
    /// <param name="ItemCode">Client item code (group * 512 + number).</param>
    /// <param name="Name">Display name from the script.</param>
    /// <param name="Quantity">Number of pieces to hand out.</param>
    /// <param name="DurationSeconds">Lifetime of the item, 0 when permanent.</param>
    /// <param name="RewardPoints">Goblin points to credit instead of an item (GP products).</param>
    public sealed record ShopItem(ushort ItemCode, string Name, int Quantity, int DurationSeconds, int RewardPoints);

    /// <summary>
    /// What the player is paying for: one price, one currency, one or more items.
    /// </summary>
    /// <param name="Name">Display name.</param>
    /// <param name="Price">Price in the currency below.</param>
    /// <param name="CashType">Currency id of the script (508/509/510).</param>
    /// <param name="Items">Items handed out on success.</param>
    public sealed record ShopOffer(string Name, int Price, uint CashType, IReadOnlyList<ShopItem> Items);

    /// <summary>
    /// Resolves what the client asked to buy.
    /// </summary>
    /// <param name="packageSeq">Package sequence of the shop grid entry.</param>
    /// <param name="priceSeq">
    /// Selected price row. Single price packages send 0, because the client only fills this field
    /// when the player picks one of several durations.
    /// </param>
    /// <param name="clientItemCode">Item code the client displayed, used as last resort.</param>
    /// <param name="offer">The resolved offer.</param>
    /// <returns><see langword="true"/> if the offer could be resolved.</returns>
    public static bool TryGetOffer(uint packageSeq, uint priceSeq, ushort clientItemCode, out ShopOffer offer)
    {
        EnsureLoaded();
        offer = null!;

        PackagesBySeq.TryGetValue(packageSeq, out var package);

        PriceRow? selected = null;
        if (priceSeq != 0)
        {
            RowsByPriceSeq.TryGetValue(priceSeq, out selected);
        }
        else if (package is not null && package.PriceSeqs.Count == 1)
        {
            RowsByPriceSeq.TryGetValue(package.PriceSeqs[0], out selected);
        }

        var items = BuildItems(package, selected, clientItemCode);
        if (items.Count == 0)
        {
            return false;
        }

        int price;
        if (priceSeq != 0 && selected is not null)
        {
            // The player picked a duration, so that row holds the price shown in the dialog.
            price = selected.Price;
        }
        else if (package is not null)
        {
            price = package.Price;
        }
        else if (selected is not null)
        {
            price = selected.Price;
        }
        else
        {
            return false;
        }

        var name = package?.Name ?? selected?.Name ?? string.Empty;
        offer = new ShopOffer(name, price, package?.CashType ?? CoinTypeWCoinC, items);
        return true;
    }

    private static List<ShopItem> BuildItems(PackageRow? package, PriceRow? selected, ushort clientItemCode)
    {
        var items = new List<ShopItem>();

        if (package is not null)
        {
            foreach (var productSeq in package.ProductSeqs)
            {
                if (!RowsByProduct.TryGetValue(productSeq, out var rows) || rows.Count == 0)
                {
                    continue;
                }

                var row = (selected is not null ? rows.FirstOrDefault(r => r.PriceSeq == selected.PriceSeq) : null)
                    ?? rows.OrderBy(r => r.Price).ThenBy(DurationOrder).First();
                AddItem(items, row);
            }
        }

        if (items.Count == 0 && selected is not null)
        {
            AddItem(items, selected);
        }

        if (items.Count == 0)
        {
            // Packages without usable product rows still carry the item code the grid shows.
            var code = package?.ItemCode ?? 0;
            if (code == 0)
            {
                code = clientItemCode;
            }

            if (code != 0 && code != ushort.MaxValue)
            {
                items.Add(new ShopItem(code, package?.Name ?? string.Empty, 1, 0, 0));
            }
        }

        return items;
    }

    private static void AddItem(List<ShopItem> items, PriceRow row)
    {
        if (row.ItemCode == 0 && row.RewardPoints == 0)
        {
            return;
        }

        items.Add(new ShopItem(row.ItemCode, row.Name, Math.Max(1, row.Quantity), row.DurationSeconds, row.RewardPoints));
    }

    private static int DurationOrder(PriceRow row) => row.DurationSeconds == 0 ? int.MaxValue : row.DurationSeconds;

    private static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        lock (LoadLock)
        {
            if (_loaded)
            {
                return;
            }

            foreach (var path in CandidatePaths("IBSProduct.txt"))
            {
                if (File.Exists(path))
                {
                    LoadProducts(path);
                    break;
                }
            }

            foreach (var path in CandidatePaths("IBSPackage.txt"))
            {
                if (File.Exists(path))
                {
                    LoadPackages(path);
                    break;
                }
            }

            if (RowsByPriceSeq.Count == 0)
            {
                // Fallback so Pet Panda still works without the script files.
                AddFallback(99, 150, 6736, "Pet Panda", 86400);
                AddFallback(100, 700, 6736, "Pet Panda", 604800);
            }

            _loaded = true;
        }
    }

    private static void AddFallback(uint priceSeq, int price, ushort itemCode, string name, int durationSeconds)
    {
        var row = new PriceRow
        {
            ProductSeq = priceSeq,
            PriceSeq = priceSeq,
            Price = price,
            ItemCode = itemCode,
            Name = name,
            DurationSeconds = durationSeconds,
        };
        RowsByPriceSeq[priceSeq] = row;
        RowsByProduct[priceSeq] = [row];
    }

    private static IEnumerable<string> CandidatePaths(string fileName)
    {
        var baseDir = AppContext.BaseDirectory;
        yield return Path.Combine(baseDir, "Data", "InGameShop", fileName);
        yield return Path.Combine(baseDir, fileName);
        yield return Path.Combine(baseDir, "Data", "InGameShopScript", "512.2012.084", fileName);
    }

    private static void LoadProducts(string path)
    {
        foreach (var raw in File.ReadLines(path))
        {
            var parts = SplitFields(raw, 15);
            if (parts is null
                || !uint.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var productSeq)
                || !int.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var price)
                || !uint.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out var priceSeq))
            {
                continue;
            }

            ushort.TryParse(parts[13], NumberStyles.Integer, CultureInfo.InvariantCulture, out var itemCode);

            if (!RowsByPriceSeq.TryGetValue(priceSeq, out var row))
            {
                row = new PriceRow
                {
                    ProductSeq = productSeq,
                    PriceSeq = priceSeq,
                    Price = price,
                    ItemCode = itemCode,
                    Name = parts[1],
                };
                RowsByPriceSeq[priceSeq] = row;
                if (!RowsByProduct.TryGetValue(productSeq, out var list))
                {
                    list = new List<PriceRow>();
                    RowsByProduct[productSeq] = list;
                }

                list.Add(row);
            }

            // Each attribute of a product is a separate line; the unit is the only reliable
            // discriminator, since the property names come localized in the official scripts.
            ApplyAttribute(row, parts[3], parts[4]);
        }
    }

    private static void ApplyAttribute(PriceRow row, string value, string unit)
    {
        switch (unit.Trim())
        {
            case "Sec.":
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) && seconds > 0)
                {
                    row.DurationSeconds = seconds;
                }

                break;

            case "EA":
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity) && quantity > 0)
                {
                    row.Quantity = quantity;
                }

                break;

            case "WReward":
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var reward) && reward > 0)
                {
                    row.RewardPoints = reward;
                }

                break;
        }
    }

    private static void LoadPackages(string path)
    {
        foreach (var raw in File.ReadLines(path))
        {
            var parts = SplitFields(raw, 26);
            if (parts is null
                || !uint.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var packageSeq)
                || !int.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var price))
            {
                continue;
            }

            ushort.TryParse(parts[20], NumberStyles.Integer, CultureInfo.InvariantCulture, out var itemCode);
            uint.TryParse(parts[25], NumberStyles.Integer, CultureInfo.InvariantCulture, out var cashType);

            PackagesBySeq[packageSeq] = new PackageRow
            {
                PackageSeq = packageSeq,
                Name = parts[3],
                Price = price,
                CashType = ResolveCashType(cashType, parts[14]),
                ItemCode = itemCode,
                ProductSeqs = ParseList(parts[19]),
                PriceSeqs = ParseList(parts[23]),
            };
        }
    }

    private static uint ResolveCashType(uint cashType, string cashName)
    {
        if (cashType is CoinTypeWCoinC or CoinTypeWCoinP or CoinTypeGoblin)
        {
            return cashType;
        }

        // Goblin point products leave the numeric type at 0 and only name the currency.
        return cashName.Contains("Goblin", StringComparison.OrdinalIgnoreCase)
            ? CoinTypeGoblin
            : CoinTypeWCoinC;
    }

    private static List<uint> ParseList(string field)
    {
        var result = new List<uint>();
        foreach (var part in field.Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (uint.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                result.Add(value);
            }
        }

        return result;
    }

    private static string[]? SplitFields(string raw, int minimumFields)
    {
        var line = raw.Trim();
        if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith("end", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parts = line.Split(FieldSeparator);
        return parts.Length < minimumFields ? null : parts;
    }

    private sealed class PriceRow
    {
        public uint ProductSeq { get; init; }

        public uint PriceSeq { get; init; }

        public int Price { get; init; }

        public ushort ItemCode { get; init; }

        public string Name { get; init; } = string.Empty;

        public int DurationSeconds { get; set; }

        public int Quantity { get; set; } = 1;

        public int RewardPoints { get; set; }
    }

    private sealed class PackageRow
    {
        public uint PackageSeq { get; init; }

        public string Name { get; init; } = string.Empty;

        public int Price { get; init; }

        public uint CashType { get; init; }

        public ushort ItemCode { get; init; }

        public List<uint> ProductSeqs { get; init; } = new();

        public List<uint> PriceSeqs { get; init; } = new();
    }
}
