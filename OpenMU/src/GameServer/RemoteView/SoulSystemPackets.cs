// <copyright file="SoulSystemPackets.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Buffers.Binary;
using MUnique.OpenMU.GameLogic.SoulSystem;
using MUnique.OpenMU.Network;

/// <summary>
/// Packet helpers for Soul System (code 0xFE).
/// C→S: FE 00 status, FE 02 set(tab,col,value), FE 04 reset.
/// S→C: FE 01 status, FE 03 result + status.
/// Status payload: u16 remaining + 16×u8 alloc.
/// </summary>
internal static class SoulSystemPackets
{
    public const byte Code = 0xFE;
    public const byte StatusRequestSubCode = 0x00;
    public const byte StatusResponseSubCode = 0x01;
    public const byte SetRequestSubCode = 0x02;
    public const byte ActionResponseSubCode = 0x03;
    public const byte ResetRequestSubCode = 0x04;

    public const int StatusRequestLength = 4;
    public const int SetRequestLength = 7; // + tab + col + value
    public const int ResetRequestLength = 4;

    public const int StatusPayloadSize = 2 + SoulSystemCatalog.SlotCount;
    public const int StatusPacketSize = 4 + StatusPayloadSize;
    public const int ActionPacketSize = StatusPacketSize + 1;

    public static async ValueTask SendStatusAsync(IConnection connection, SoulSystemStatus status)
    {
        int Write()
        {
            var size = StatusPacketSize;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = Code;
            span[3] = StatusResponseSubCode;
            WriteStatus(span[4..], status);
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    public static async ValueTask SendActionResultAsync(IConnection connection, SoulSystemResult result, SoulSystemStatus status)
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
            WriteStatus(span[5..], status);
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    private static void WriteStatus(Span<byte> span, SoulSystemStatus status)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)Math.Clamp(status.Remaining, 0, ushort.MaxValue));
        for (var i = 0; i < SoulSystemCatalog.SlotCount; i++)
        {
            span[2 + i] = (status.Allocations is { Length: > 0 } && i < status.Allocations.Length)
                ? status.Allocations[i]
                : (byte)0;
        }
    }
}
