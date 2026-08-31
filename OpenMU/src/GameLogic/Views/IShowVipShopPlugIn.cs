// <copyright file="IShowVipShopPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using MUnique.OpenMU.GameLogic.VipShop;

/// <summary>
/// Sends Shopping VIP packets to the client.
/// </summary>
public interface IShowVipShopPlugIn : IViewPlugIn
{
    /// <summary>Sends current VIP shop status.</summary>
    ValueTask ShowVipShopStatusAsync(VipShopService.VipShopStatus status);

    /// <summary>Sends buy result and refreshed status.</summary>
    ValueTask ShowVipShopBuyResultAsync(VipShopService.BuyResult result, VipShopService.VipShopStatus status);
}
