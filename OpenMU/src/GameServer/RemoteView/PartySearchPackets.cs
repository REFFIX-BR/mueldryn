// <copyright file="PartySearchPackets.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Buffers.Binary;
using System.Text;
using MUnique.OpenMU.GameLogic.PartySearch;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets;

/// <summary>
/// Party Search packets (code 0xF9).
/// Note: 0xF9 is also used by legacy NPC dialog (C1 F9 01). Party Search uses
/// C2 F9 01 (list) and C1 F9 03/05 (results), so the client dispatches by header/subcode.
/// C→S: 00 list request, 02 publish, 04 cancel, 06 join.
/// S→C: 01 list, 03 publish result, 05 join result.
/// List response layout (C2):
///   [5]=count [6]=ownActive [7..8]=ownMaxLevel LE [9]=ownFlags [10]=ownClassMask
///   then count × 48-byte entries.
/// Entry: name16, map16, mapNumber2, x, y, count, maxCount, maxLevel2, flags, classMask, leaderLevel2, reserved2.
/// </summary>
internal static class PartySearchPackets
{
    public const byte Code = 0xF9;
    public const byte ListRequestSubCode = 0x00;
    public const byte ListResponseSubCode = 0x01;
    public const byte PublishRequestSubCode = 0x02;
    public const byte PublishResultSubCode = 0x03;
    public const byte CancelRequestSubCode = 0x04;
    public const byte JoinResultSubCode = 0x05;
    public const byte JoinRequestSubCode = 0x06;

    public const int RequestLength = 4;
    public const int NameLength = 16;
    public const int MapNameLength = 16;
    public const int PasswordMaxLength = 10;
    public const int EntrySize = 48;
    public const int ListHeaderSize = 11; // C2(5) + count + ownActive + maxLevel2 + flags + classMask
    public const int PublishMinLength = 9; // C1 F9 02 + active + maxLevel2 + flags + classMask + pwdLen
    public const int JoinMinLength = 21; // C1 F9 06 + name16 + pwdLen

    public static int GetListResponseSize(int entryCount)
        => ListHeaderSize + (entryCount * EntrySize);

    public static async ValueTask SendListAsync(
        IConnection connection,
        IReadOnlyList<PartySearchListEntry> entries,
        bool ownActive,
        PartySearchListing? ownListing)
    {
        var count = Math.Min(entries.Count, byte.MaxValue);
        var size = GetListResponseSize(count);

        int Write()
        {
            var span = connection.Output.GetSpan(size)[..size];
            var header = new C2HeaderWithSubCodeRef(span);
            header.Type = 0xC2;
            header.Length = (ushort)size;
            header.Code = Code;
            header.SubCode = ListResponseSubCode;

            span[5] = (byte)count;
            span[6] = ownActive ? (byte)1 : (byte)0;
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(7, 2), ownListing?.MaxLevel ?? (ushort)400);
            span[9] = (byte)(ownListing?.Flags ?? PartySearchFlags.None);
            span[10] = ownListing?.ClassMask ?? (byte)0xFF;

            var offset = ListHeaderSize;
            for (var i = 0; i < count; i++)
            {
                WriteEntry(span.Slice(offset, EntrySize), entries[i]);
                offset += EntrySize;
            }

            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    public static async ValueTask SendPublishResultAsync(IConnection connection, PartySearchResult result, bool ownActive)
    {
        const int size = 6;

        int Write()
        {
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = size;
            span[2] = Code;
            span[3] = PublishResultSubCode;
            span[4] = (byte)result;
            span[5] = ownActive ? (byte)1 : (byte)0;
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    public static async ValueTask SendJoinResultAsync(IConnection connection, PartySearchResult result)
    {
        const int size = 5;

        int Write()
        {
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = size;
            span[2] = Code;
            span[3] = JoinResultSubCode;
            span[4] = (byte)result;
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    private static void WriteEntry(Span<byte> span, PartySearchListEntry entry)
    {
        span.Clear();
        WriteFixedUtf8(span.Slice(0, NameLength), entry.LeaderName);
        WriteFixedUtf8(span.Slice(16, MapNameLength), entry.MapName);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(32, 2), entry.MapNumber);
        span[34] = entry.X;
        span[35] = entry.Y;
        span[36] = entry.Count;
        span[37] = entry.MaxCount;
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(38, 2), entry.MaxLevel);
        span[40] = (byte)entry.Flags;
        span[41] = entry.ClassMask;
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(42, 2), entry.LeaderLevel);
    }

    private static void WriteFixedUtf8(Span<byte> dest, string value)
    {
        dest.Clear();
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        bytes.AsSpan(0, Math.Min(bytes.Length, dest.Length)).CopyTo(dest);
    }
}
