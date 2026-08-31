// <copyright file="VipShopRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.VipShop;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handles C1 EE Shopping VIP requests.
/// </summary>
[PlugIn]
[Display(Name = "VIP Shop Request", Description = "Handles Shopping VIP requests (0xEE).")]
[Guid("B9C0D1E2-F3A4-4B5C-0D1E-2F3A4B5C6D7E")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
internal sealed class VipShopRequestHandlerPlugIn : IPacketHandlerPlugIn
{
    /// <inheritdoc />
    public bool IsEncryptionExpected => false;

    /// <inheritdoc />
    public byte Key => VipShopPackets.Code;

    /// <inheritdoc />
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (player.SelectedCharacter is null || packet.Length < VipShopPackets.StatusRequestLength)
        {
            return;
        }

        var span = packet.Span;
        if (span[0] is not (0xC1 or 0xC3))
        {
            return;
        }

        switch (span[3])
        {
            case VipShopPackets.StatusRequestSubCode:
            {
                var status = VipShopService.BuildStatus(player);
                await player.InvokeViewPlugInAsync<IShowVipShopPlugIn>(p => p.ShowVipShopStatusAsync(status)).ConfigureAwait(false);
                break;
            }

            case VipShopPackets.BuyRequestSubCode:
            {
                var result = VipShopService.TryBuy(player);
                var status = VipShopService.BuildStatus(player);
                await player.InvokeViewPlugInAsync<IShowVipShopPlugIn>(p => p.ShowVipShopBuyResultAsync(result, status)).ConfigureAwait(false);
                break;
            }
        }
    }
}
