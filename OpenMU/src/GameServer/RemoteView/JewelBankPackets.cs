// <copyright file="JewelBankPackets.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Buffers.Binary;
using MUnique.OpenMU.GameLogic.JewelBank;
using MUnique.OpenMU.Network;

/// <summary>
/// Packet helpers for Jewel Bank (code 0xFC).
/// C→S: FC 00 status, FC 02 deposit(slot), FC 04 withdraw(type,qty[,mode]).
/// S→C: FC 01 status, FC 03 action result + status.
/// </summary>
internal static class JewelBankPackets
{
    public const byte Code = 0xFC;
    public const byte StatusRequestSubCode = 0x00;
    public const byte StatusResponseSubCode = 0x01;
    public const byte DepositRequestSubCode = 0x02;
    public const byte ActionResponseSubCode = 0x03;
    public const byte WithdrawRequestSubCode = 0x04;

    public const int StatusRequestLength = 4;
    public const int DepositRequestLength = 5; // + slot
    public const int WithdrawRequestLength = 6; // + type + qty
    public const int WithdrawRequestLengthWithMode = 7; // + mode (0 = units, 1 = pack)

    // C1 header+sub + 10 * u32 counts
    public const int StatusPayloadSize = JewelBankCatalog.SlotCount * 4;
    public const int StatusPacketSize = 4 + StatusPayloadSize;
    public const int ActionPacketSize = StatusPacketSize + 1;

    public static async ValueTask SendStatusAsync(IConnection connection, JewelBankStatus status)
    {
        int Write()
        {
            var size = StatusPacketSize;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = Code;
            span[3] = StatusResponseSubCode;
            WriteCounts(span[4..], status);
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    public static async ValueTask SendActionResultAsync(IConnection connection, JewelBankResult result, JewelBankStatus status)
    {
        int Write()
        {
            var size = ActionPacketSize;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = Code;
            span[3] = ActionResponseSubCode;
            span[4] = (byte)result;
            WriteCounts(span[5..], status);
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    private static void WriteCounts(Span<byte> span, JewelBankStatus status)
    {
        for (var i = 0; i < JewelBankCatalog.SlotCount; i++)
        {
            var value = (status.Counts is { Length: > 0 } && i < status.Counts.Length)
                ? status.Counts[i]
                : 0;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(i * 4, 4), (uint)Math.Clamp(value, 0, int.MaxValue));
        }
    }
}
