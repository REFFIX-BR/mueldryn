// <copyright file="ShowVipShopPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.VipShop;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <inheritdoc />
[PlugIn]
[Guid("A8B9C0D1-E2F3-4A5B-9C0D-1E2F3A4B5C6D")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class ShowVipShopPlugIn : IShowVipShopPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowVipShopPlugIn"/> class.
    /// </summary>
    public ShowVipShopPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowVipShopStatusAsync(VipShopService.VipShopStatus status)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        await VipShopPackets.SendStatusAsync(connection, status).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowVipShopBuyResultAsync(VipShopService.BuyResult result, VipShopService.VipShopStatus status)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        await VipShopPackets.SendBuyResultAsync(connection, result, status).ConfigureAwait(false);
    }
}
