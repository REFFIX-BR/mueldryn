// <copyright file="CashShopScriptVersionOnEnterPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends cash shop script version (0xD2/0x0C) when the player enters the world,
/// unlocking MuMain InGameShop (<c>ShopOpenUnLock</c> + local scripts).
/// </summary>
[PlugIn]
[Display(Name = "Cash Shop Script Version", Description = "Sends InGameShop script version on enter world so the client unlocks the shop.")]
[Guid("8E4B0D3F-5C29-4E7B-AF62-D9E1A3B5C7F2")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class CashShopScriptVersionOnEnterPlugIn : IPlayerStateChangedPlugIn
{
    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (currentState != PlayerState.EnteredWorld)
        {
            return;
        }

        if (player is not RemotePlayer { Connection: { Connected: true } connection })
        {
            return;
        }

        await CashShopPackets.SendScriptVersionAsync(connection).ConfigureAwait(false);
    }
}
