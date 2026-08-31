// <copyright file="IShowPegasusCollectionPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using MUnique.OpenMU.GameLogic.Collections;

/// <summary>
/// Sends Pegasus Collections sync / donate results to the client.
/// </summary>
public interface IShowPegasusCollectionPlugIn : IViewPlugIn
{
    ValueTask ShowCollectionSyncAsync(uint[] maskBits);

    ValueTask ShowCollectionDonateResultAsync(
        PegasusCollectionService.DonateResult result,
        byte setIdx,
        byte slot,
        bool completed,
        uint[] maskBits,
        uint rewardHp,
        uint rewardCoins);
}
