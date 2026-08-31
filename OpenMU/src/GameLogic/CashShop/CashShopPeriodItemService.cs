// <copyright file="CashShopPeriodItemService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.CashShop;

/// <summary>
/// Takes care of the items which were bought with a period: expired ones are removed and the ones
/// which are actually a buff (seals, scrolls and auras bought before they became direct buffs) are
/// turned into the buff and removed from the inventory.
/// </summary>
public static class CashShopPeriodItemService
{
    /// <summary>
    /// Cleans up the period items of the inventory.
    /// </summary>
    /// <param name="player">The player.</param>
    public static async ValueTask RefreshAsync(Player player)
    {
        if (player.Inventory is null || player.Attributes is null)
        {
            return;
        }

        foreach (var item in player.Inventory.Items.ToList())
        {
            if (!item.HasExpiration)
            {
                continue;
            }

            var remainingPeriod = item.ExpirationDate!.Value - DateTime.UtcNow;
            if (remainingPeriod <= TimeSpan.Zero)
            {
                await player.DestroyInventoryItemAsync(item).ConfigureAwait(false);
                continue;
            }

            if (item.Definition?.ConsumeEffect is { } effectDefinition)
            {
                await ShopBuffService.AddAsync(player, effectDefinition, remainingPeriod).ConfigureAwait(false);
                await player.DestroyInventoryItemAsync(item).ConfigureAwait(false);
            }
        }
    }
}
