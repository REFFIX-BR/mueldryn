// <copyright file="ShowPegasusCollectionPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Collections;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends Pegasus Collections packets to MuMain.
/// </summary>
[PlugIn]
[Display(Name = "Show Pegasus Collection", Description = "Sends Collections sync and donate results.")]
[Guid("C011EC70-5A01-4B02-9C03-D4E5F6071829")]
public sealed class ShowPegasusCollectionPlugIn : IShowPegasusCollectionPlugIn
{
    private readonly RemotePlayer _player;

    public ShowPegasusCollectionPlugIn(RemotePlayer player) => this._player = player;

    public async ValueTask ShowCollectionSyncAsync(uint[] maskBits)
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await PegasusCollectionPackets.SendSyncAsync(connection, maskBits).ConfigureAwait(false);
    }

    public async ValueTask ShowCollectionDonateResultAsync(
        PegasusCollectionService.DonateResult result,
        byte setIdx,
        byte slot,
        bool completed,
        uint[] maskBits,
        uint rewardHp,
        uint rewardCoins)
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await PegasusCollectionPackets.SendDonateResultAsync(
            connection, result, setIdx, slot, completed, maskBits, rewardHp, rewardCoins).ConfigureAwait(false);
    }
}
