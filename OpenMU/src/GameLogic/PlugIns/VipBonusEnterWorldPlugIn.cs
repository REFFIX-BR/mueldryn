// <copyright file="VipBonusEnterWorldPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.VipShop;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Applies VIP exp/drop bonuses when a character enters the world.
/// </summary>
[PlugIn]
[Guid("C0D1E2F3-A4B5-4C6D-1E2F-3A4B5C6D7E8F")]
[Display(Name = "VIP Bonus Enter World", Description = "Applies Shopping VIP bonuses on enter world.")]
public sealed class VipBonusEnterWorldPlugIn : IPlayerStateChangedPlugIn
{
    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (previousState != PlayerState.CharacterSelection || currentState != PlayerState.EnteredWorld)
        {
            return;
        }

        VipShopService.ApplyVipBonuses(player);
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
