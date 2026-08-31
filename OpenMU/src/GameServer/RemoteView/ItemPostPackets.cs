// <copyright file="ItemPostPackets.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Text;
using MUnique.OpenMU.Network;

/// <summary>
/// Packet helpers for Item Sell Post (code 0xFD).
/// C→S: FD 00 announce(slot).
/// S→C: FD 01 broadcast announce (seller + item name + item data).
/// </summary>
internal static class ItemPostPackets
{
    public const byte Code = 0xFD;
    public const byte AnnounceRequestSubCode = 0x00;
    public const byte AnnounceBroadcastSubCode = 0x01;

    public const int AnnounceRequestLength = 5;
    public const int SellerNameBytes = 10;
    public const int MaxItemData = 32;
    public const int MaxItemNameBytes = 48;

    public static async ValueTask SendAnnounceAsync(IConnection connection, string sellerName, string itemName, ReadOnlyMemory<byte> itemData)
    {
        var nameBytes = Encoding.UTF8.GetBytes(itemName ?? string.Empty);
        if (nameBytes.Length > MaxItemNameBytes)
        {
            nameBytes = nameBytes.AsSpan(0, MaxItemNameBytes).ToArray();
        }

        var dataLen = Math.Min(itemData.Length, MaxItemData);
        var size = 4 + SellerNameBytes + 1 + nameBytes.Length + 1 + dataLen;

        int Write()
        {
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = Code;
            span[3] = AnnounceBroadcastSubCode;

            span.Slice(4, SellerNameBytes).Clear();
            var seller = Encoding.UTF8.GetBytes(sellerName ?? string.Empty);
            seller.AsSpan(0, Math.Min(seller.Length, SellerNameBytes)).CopyTo(span.Slice(4, SellerNameBytes));

            var offset = 4 + SellerNameBytes;
            span[offset++] = (byte)nameBytes.Length;
            nameBytes.CopyTo(span.Slice(offset, nameBytes.Length));
            offset += nameBytes.Length;

            span[offset++] = (byte)dataLen;
            itemData.Span[..dataLen].CopyTo(span.Slice(offset, dataLen));
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }
}
