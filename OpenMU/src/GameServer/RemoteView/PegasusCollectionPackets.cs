// <copyright file="PegasusCollectionPackets.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Buffers.Binary;
using MUnique.OpenMU.GameLogic.Collections;
using MUnique.OpenMU.Network;

/// <summary>
/// Packet helpers for Pegasus Collections (F3:70 sync, F3:71 donate, F3:72 result).
/// </summary>
internal static class PegasusCollectionPackets
{
    public const byte GroupCode = 0xF3;
    public const byte SyncSubCode = 0x70;
    public const byte DonateSubCode = 0x71;
    public const byte ResultSubCode = 0x72;

    public const int SyncPacketSize = 16; // C1 size F3 70 + 12 mask
    public const int ResultPacketSize = 28; // + result/set/slot/completed + 12 mask + hp + coins
    public const int DonateRequestLength = 8;

    public static async ValueTask SendSyncAsync(IConnection connection, uint[] maskBits)
    {
        int Write()
        {
            var size = SyncPacketSize;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = GroupCode;
            span[3] = SyncSubCode;
            WriteMask(span[4..], maskBits);
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    public static async ValueTask SendDonateResultAsync(
        IConnection connection,
        PegasusCollectionService.DonateResult result,
        byte setIdx,
        byte slot,
        bool completed,
        uint[] maskBits,
        uint rewardHp,
        uint rewardCoins)
    {
        int Write()
        {
            var size = ResultPacketSize;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = GroupCode;
            span[3] = ResultSubCode;
            span[4] = (byte)result;
            span[5] = setIdx;
            span[6] = slot;
            span[7] = completed ? (byte)1 : (byte)0;
            WriteMask(span[8..], maskBits);
            BinaryPrimitives.WriteUInt32LittleEndian(span[20..], rewardHp);
            BinaryPrimitives.WriteUInt32LittleEndian(span[24..], rewardCoins);
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    private static void WriteMask(Span<byte> span, uint[] maskBits)
    {
        for (var i = 0; i < PegasusCollectionCatalog.MaskDwordCount; i++)
        {
            var value = (maskBits is { Length: > 0 } && i < maskBits.Length) ? maskBits[i] : 0u;
            BinaryPrimitives.WriteUInt32LittleEndian(span[(i * 4)..], value);
        }
    }
}
