// <copyright file="QuestPanelPackets.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Buffers.Binary;
using System.Text;
using MUnique.OpenMU.GameLogic.QuestPanel;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets;

/// <summary>
/// Manual packet helpers for the side quest panel (code 0xFB).
/// Payload: kills u32, required u32, claimed, canClaim, accepted, state,
/// stage u16, total u16, name[32], target[24], requiredLevel u16.
/// </summary>
internal static class QuestPanelPackets
{
    public const byte Code = 0xFB;
    public const byte StatusRequestSubCode = 0x00;
    public const byte StatusResponseSubCode = 0x01;
    public const byte ClaimRequestSubCode = 0x02;
    public const byte ClaimResponseSubCode = 0x03;
    public const byte AbandonRequestSubCode = 0x04;
    public const byte AcceptRequestSubCode = 0x05;
    public const byte OpenNpcDialogSubCode = 0x06;
    public const byte NpcQuestListSubCode = 0x07;
    public const int RequestLength = 4;
    public const int NameLength = 32;
    public const int TargetLength = 24;
    public const int NpcListEntrySize = 2 + 1 + NameLength; // 35

    public const int StatusPayloadSize = 4 + 4 + 1 + 1 + 1 + 1 + 2 + 2 + NameLength + TargetLength + 2; // 74
    public const int StatusPacketSize = 4 + StatusPayloadSize; // 78
    public const int ClaimPacketSize = StatusPacketSize + 1; // 79
    public const int OpenNpcPacketSize = StatusPacketSize;

    public static async ValueTask SendStatusAsync(IConnection connection, QuestPanelStatus status)
    {
        int Write()
        {
            var size = StatusPacketSize;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = Code;
            span[3] = StatusResponseSubCode;
            WriteStatusPayload(span[4..], status);
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    public static async ValueTask SendClaimResultAsync(IConnection connection, QuestPanelClaimResult result, QuestPanelStatus status)
    {
        int Write()
        {
            var size = ClaimPacketSize;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = Code;
            span[3] = ClaimResponseSubCode;
            span[4] = (byte)result;
            WriteStatusPayload(span[5..], status);
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    public static async ValueTask SendOpenNpcDialogAsync(IConnection connection, QuestPanelStatus status)
    {
        int Write()
        {
            var size = OpenNpcPacketSize;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = Code;
            span[3] = OpenNpcDialogSubCode;
            WriteStatusPayload(span[4..], status);
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    public static async ValueTask SendNpcQuestListAsync(IConnection connection, int stage, int total, IReadOnlyList<QuestPanelNpcListEntry> entries)
    {
        var count = Math.Clamp(entries.Count, 0, byte.MaxValue);
        var packetSize = 6 + 4 + (count * NpcListEntrySize);

        int Write()
        {
            var span = connection.Output.GetSpan(packetSize)[..packetSize];
            span[0] = 0xC2;
            span[1] = (byte)(packetSize >> 8);
            span[2] = (byte)(packetSize & 0xFF);
            span[3] = Code;
            span[4] = NpcQuestListSubCode;
            span[5] = (byte)count;
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(6, 2), (ushort)Math.Clamp(stage, 0, ushort.MaxValue));
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(8, 2), (ushort)Math.Clamp(total, 0, ushort.MaxValue));
            var offset = 10;
            for (var i = 0; i < count; i++)
            {
                var entry = entries[i];
                BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(offset, 2), (ushort)Math.Clamp(entry.Index, 0, ushort.MaxValue));
                span[offset + 2] = entry.ListState;
                WriteFixedUtf8(span.Slice(offset + 3, NameLength), entry.Title);
                offset += NpcListEntrySize;
            }

            return packetSize;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    private static void WriteStatusPayload(Span<byte> span, QuestPanelStatus status)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(span[..4], (uint)Math.Clamp(status.Kills, 0, uint.MaxValue));
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(4, 4), (uint)Math.Clamp(status.Required, 0, uint.MaxValue));
        span[8] = status.Claimed ? (byte)1 : (byte)0;
        span[9] = status.CanClaim ? (byte)1 : (byte)0;
        span[10] = status.Accepted ? (byte)1 : (byte)0;
        span[11] = (byte)status.State;
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(12, 2), (ushort)Math.Clamp(status.Stage, 0, ushort.MaxValue));
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(14, 2), (ushort)Math.Clamp(status.Total, 0, ushort.MaxValue));
        WriteFixedUtf8(span.Slice(16, NameLength), status.Name);
        WriteFixedUtf8(span.Slice(16 + NameLength, TargetLength), status.TargetLabel);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(16 + NameLength + TargetLength, 2), (ushort)Math.Clamp(status.RequiredLevel, 0, ushort.MaxValue));
    }

    private static void WriteFixedUtf8(Span<byte> dest, string? text)
    {
        dest.Clear();
        var bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
        bytes.AsSpan(0, Math.Min(bytes.Length, dest.Length)).CopyTo(dest);
    }
}
