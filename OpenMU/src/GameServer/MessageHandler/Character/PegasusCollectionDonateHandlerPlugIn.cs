// <copyright file="PegasusCollectionDonateHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Character;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Collections;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handles F3:71 Collections donate requests from MuMain.
/// </summary>
[PlugIn]
[Display(Name = "Pegasus Collection Donate", Description = "Handles Collections donate (0xF3, 0x71).")]
[Guid("C011EC70-71A1-4B02-9C03-D4E5F6071830")]
[BelongsToGroup(CharacterGroupHandlerPlugIn.GroupKey)]
internal sealed class PegasusCollectionDonateHandlerPlugIn : ISubPacketHandlerPlugIn
{
    public bool IsEncryptionExpected => false;

    public byte Key => PegasusCollectionPackets.DonateSubCode;

    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (player.SelectedCharacter is null || packet.Length < PegasusCollectionPackets.DonateRequestLength)
        {
            return;
        }

        var span = packet.Span;
        var setIdx = span[4];
        var slot = span[5];
        var invSlot = span[6];
        var outcome = await PegasusCollectionService.DonateAsync(player, setIdx, slot, invSlot).ConfigureAwait(false);

        // Always send F3:72 on the wire (view plug-in may be inactive for this client version).
        if (player is RemotePlayer { Connection.Connected: true } remote)
        {
            await PegasusCollectionPackets.SendDonateResultAsync(
                remote.Connection,
                outcome.Result,
                outcome.SetIdx,
                outcome.Slot,
                outcome.Completed,
                outcome.Bits,
                outcome.RewardHp,
                outcome.RewardCoins).ConfigureAwait(false);
        }
    }
}
