// <copyright file="EventSchedulePackets.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Buffers.Binary;
using System.Text;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Dungeons;
using MUnique.OpenMU.GameLogic.EventSchedule;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets;

/// <summary>
/// Manual packet helpers for schedule / invasion status / player equipment (code 0xFA).
/// C→S: C1 FA 00 schedule request; C1 FA 02 invasion status request; C1 FA 04 player equipment request.
/// S→C: C2 FA 01 schedule; C2 FA 03 invasion status; C2 FA 05 player equipment.
/// </summary>
internal static class EventSchedulePackets
{
    public const byte Code = 0xFA;
    public const byte RequestSubCode = 0x00;
    public const byte ResponseSubCode = 0x01;
    public const byte InvasionStatusRequestSubCode = 0x02;
    public const byte InvasionStatusResponseSubCode = 0x03;
    public const byte PlayerEquipmentRequestSubCode = 0x04;
    public const byte PlayerEquipmentResponseSubCode = 0x05;
    public const byte BossLifeBarSubCode = 0x06;
    public const byte DungeonWindowRequestSubCode = 0x10; // C→S
    public const byte DungeonWindowResponseSubCode = 0x11; // S→C
    public const byte DungeonEnterRequestSubCode = 0x12; // C→S
    public const byte DungeonEnterResultSubCode = 0x13; // S→C
    public const byte DungeonHudUpdateSubCode = 0x14; // S→C
    public const byte DungeonLeaveRequestSubCode = 0x15; // C→S
    public const int BossLifeBarPacketSize = 6 + NameLength; // header(4) + percent(1) + alive(1) + name(32)
    public const int DungeonWindowResponsePacketSize = 12;
    public const int DungeonHudUpdatePacketSize = 43;
    public const int DungeonObjectiveTextLength = 32;
    public const int RequestLength = 4;
    public const int PlayerEquipmentRequestLength = 6; // header(4) + player id(2)
    public const int EntrySize = 38; // status(1) + category(1) + seconds(4) + name(32)
    public const int InvasionStatusEntrySize = 68; // invasion(32) + monster(32) + alive(2) + total(2)
    public const int NameLength = 32;
    public const int InvasionStatusHeaderSize = 36; // seconds(4) + title(32)

    public static int GetResponseSize(int entryCount)
        => 6 + (entryCount * EntrySize); // C2 header+sub + count

    public static int GetInvasionStatusResponseSize(int entryCount)
        => 6 + InvasionStatusHeaderSize + (entryCount * InvasionStatusEntrySize);

    public static async ValueTask SendResponseAsync(IConnection connection, IReadOnlyList<EventScheduleEntry> entries)
    {
        var count = Math.Min(entries.Count, byte.MaxValue);
        var size = GetResponseSize(count);

        int Write()
        {
            var span = connection.Output.GetSpan(size)[..size];
            var header = new C2HeaderWithSubCodeRef(span);
            header.Type = 0xC2;
            header.Length = (ushort)size;
            header.Code = Code;
            header.SubCode = ResponseSubCode;

            span[5] = (byte)count;
            var offset = 6;
            for (var i = 0; i < count; i++)
            {
                var entry = entries[i];
                span[offset] = entry.IsActive ? (byte)1 : (byte)0;
                span[offset + 1] = (byte)entry.Category;
                BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset + 2, 4), (uint)Math.Max(0, entry.SecondsRemaining));
                span.Slice(offset + 6, NameLength).Clear();
                var nameBytes = Encoding.UTF8.GetBytes(entry.Name);
                nameBytes.AsSpan(0, Math.Min(nameBytes.Length, NameLength)).CopyTo(span.Slice(offset + 6, NameLength));
                offset += EntrySize;
            }

            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    public static async ValueTask SendInvasionStatusAsync(IConnection connection, InvasionStatusSnapshot snapshot)
    {
        var count = Math.Min(snapshot.Entries.Count, byte.MaxValue);
        var size = GetInvasionStatusResponseSize(count);

        int Write()
        {
            var span = connection.Output.GetSpan(size)[..size];
            var header = new C2HeaderWithSubCodeRef(span);
            header.Type = 0xC2;
            header.Length = (ushort)size;
            header.Code = Code;
            header.SubCode = InvasionStatusResponseSubCode;

            span[5] = (byte)count;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(6, 4), (uint)Math.Max(0, snapshot.SecondsRemaining));
            span.Slice(10, NameLength).Clear();
            var titleBytes = Encoding.UTF8.GetBytes(snapshot.Title ?? "Invasoes");
            titleBytes.AsSpan(0, Math.Min(titleBytes.Length, NameLength)).CopyTo(span.Slice(10, NameLength));

            var offset = 6 + InvasionStatusHeaderSize;
            for (var i = 0; i < count; i++)
            {
                var entry = snapshot.Entries[i];
                span.Slice(offset, NameLength).Clear();
                var invasionBytes = Encoding.UTF8.GetBytes(entry.InvasionName ?? string.Empty);
                invasionBytes.AsSpan(0, Math.Min(invasionBytes.Length, NameLength)).CopyTo(span.Slice(offset, NameLength));

                span.Slice(offset + NameLength, NameLength).Clear();
                var nameBytes = Encoding.UTF8.GetBytes(entry.MonsterName ?? string.Empty);
                nameBytes.AsSpan(0, Math.Min(nameBytes.Length, NameLength)).CopyTo(span.Slice(offset + NameLength, NameLength));

                BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(offset + NameLength * 2, 2), (ushort)Math.Clamp(entry.Alive, 0, ushort.MaxValue));
                BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(offset + NameLength * 2 + 2, 2), (ushort)Math.Clamp(entry.Total, 0, ushort.MaxValue));
                offset += InvasionStatusEntrySize;
            }

            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the health of a boss monster, so the client can show its life bar.
    /// </summary>
    public static async ValueTask SendBossLifeBarAsync(IConnection connection, string bossName, byte healthPercentage, bool isAlive)
    {
        int Write()
        {
            var span = connection.Output.GetSpan(BossLifeBarPacketSize)[..BossLifeBarPacketSize];
            span.Clear();
            span[0] = 0xC1;
            span[1] = BossLifeBarPacketSize;
            span[2] = Code;
            span[3] = BossLifeBarSubCode;
            span[4] = healthPercentage;
            span[5] = isAlive ? (byte)1 : (byte)0;

            var nameBytes = Encoding.UTF8.GetBytes(bossName ?? string.Empty);
            nameBytes.AsSpan(0, Math.Min(nameBytes.Length, NameLength)).CopyTo(span.Slice(6, NameLength));

            return BossLifeBarPacketSize;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the equipment of another player, so the client can show it in the player detail window.
    /// Each entry is slot(1) + length(1) + item data, because the serialized size varies per item.
    /// </summary>
    public static async ValueTask SendPlayerEquipmentAsync(IConnection connection, string playerName, IReadOnlyList<Item> items, IItemSerializer itemSerializer)
    {
        var count = Math.Min(items.Count, byte.MaxValue);
        var maxSize = 6 + NameLength + (count * (2 + itemSerializer.NeededSpace));

        int Write()
        {
            var span = connection.Output.GetSpan(maxSize)[..maxSize];
            span.Clear();
            var header = new C2HeaderWithSubCodeRef(span);
            header.Type = 0xC2;
            header.Code = Code;
            header.SubCode = PlayerEquipmentResponseSubCode;

            span[5] = (byte)count;
            var nameBytes = Encoding.UTF8.GetBytes(playerName ?? string.Empty);
            nameBytes.AsSpan(0, Math.Min(nameBytes.Length, NameLength)).CopyTo(span.Slice(6, NameLength));

            var offset = 6 + NameLength;
            for (var i = 0; i < count; i++)
            {
                var item = items[i];
                var itemSize = itemSerializer.SerializeItem(span.Slice(offset + 2, itemSerializer.NeededSpace), item);
                span[offset] = item.ItemSlot;
                span[offset + 1] = (byte)itemSize;
                offset += 2 + itemSize;
            }

            header.Length = (ushort)offset;
            return offset;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the dungeon window data to the client (packet 0x11).
    /// Packet structure: C1 [len] FA 11 [dungeonId] [difficulty] [minLevel: 2B LE] [minResets: 1B] [remainingEntries] [freeSlots]
    /// </summary>
    public static async ValueTask SendDungeonWindowAsync(IConnection connection, DungeonWindowPayload payload)
    {
        int Write()
        {
            var span = connection.Output.GetSpan(DungeonWindowResponsePacketSize)[..DungeonWindowResponsePacketSize];
            span[0] = 0xC1;
            span[1] = DungeonWindowResponsePacketSize;
            span[2] = Code;
            span[3] = DungeonWindowResponseSubCode;
            span[4] = payload.DungeonId;
            span[5] = (byte)payload.Difficulty;
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(6, 2), payload.MinLevel);
            span[8] = payload.MinResets;
            span[9] = payload.RemainingEntries;
            span[10] = payload.FreeInventorySlots;
            span[11] = 0; // reserved

            return DungeonWindowResponsePacketSize;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the dungeon entry result to the client (packet 0x13).
    /// Packet structure: C1 [len] FA 13 [result] [msgLen] [message: UTF-8]
    /// </summary>
    public static async ValueTask SendDungeonEnterResultAsync(IConnection connection, byte resultCode, string? message)
    {
        var messageBytes = message != null ? Encoding.UTF8.GetBytes(message) : Array.Empty<byte>();
        var messageLen = Math.Min(messageBytes.Length, 64);
        var size = 6 + messageLen;

        int Write()
        {
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = Code;
            span[3] = DungeonEnterResultSubCode;
            span[4] = resultCode;
            span[5] = (byte)messageLen;

            if (messageLen > 0)
            {
                messageBytes.AsSpan(0, messageLen).CopyTo(span.Slice(6, messageLen));
            }

            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the dungeon HUD update to the client (packet 0x14).
    /// Packet structure: C1 [len] FA 14 [room] [kills: 2B LE] [timeLeft: 4B LE] [objText: 32B]
    /// </summary>
    public static async ValueTask SendDungeonHudUpdateAsync(IConnection connection, DungeonHudUpdate update)
    {
        int Write()
        {
            var span = connection.Output.GetSpan(DungeonHudUpdatePacketSize)[..DungeonHudUpdatePacketSize];
            span[0] = 0xC1;
            span[1] = DungeonHudUpdatePacketSize;
            span[2] = Code;
            span[3] = DungeonHudUpdateSubCode;
            span[4] = (byte)update.CurrentRoom;
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(5, 2), update.KillCount);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(7, 4), update.TimeRemainingSeconds);
            
            // Clear the objective text area and copy UTF-8 bytes
            span.Slice(11, DungeonObjectiveTextLength).Clear();
            var textBytes = Encoding.UTF8.GetBytes(update.ObjectiveText ?? string.Empty);
            textBytes.AsSpan(0, Math.Min(textBytes.Length, DungeonObjectiveTextLength)).CopyTo(span.Slice(11, DungeonObjectiveTextLength));

            return DungeonHudUpdatePacketSize;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }
}
