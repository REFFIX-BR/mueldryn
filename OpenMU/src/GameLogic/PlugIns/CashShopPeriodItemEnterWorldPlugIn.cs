// <copyright file="CashShopPeriodItemEnterWorldPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Applies the running buffs of the item shop when a character enters the world and saves their
/// remaining time when it leaves, so only the time spent online is consumed.
/// </summary>
[PlugIn]
[Guid("A6F4B0C7-3E58-4D91-B2A5-7C1E9D48F033")]
[Display(Name = "Cash Shop Buffs", Description = "Restores the bought seals, scrolls and auras on enter world and saves their remaining time on logout.")]
public sealed class CashShopPeriodItemEnterWorldPlugIn : IPlayerStateChangedPlugIn
{
    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (previousState == PlayerState.CharacterSelection && currentState == PlayerState.EnteredWorld)
        {
            await ShopBuffService.RestoreAsync(player).ConfigureAwait(false);

            // Items which were bought before the buffs got applied directly are turned into a buff here.
            await CashShopPeriodItemService.RefreshAsync(player).ConfigureAwait(false);
            return;
        }

        if (currentState == PlayerState.Disconnected || currentState == PlayerState.CharacterSelection)
        {
            await ShopBuffService.SaveAsync(player).ConfigureAwait(false);
        }
    }
}
