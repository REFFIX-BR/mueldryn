// <copyright file="PeriodItemPackets.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Buffers.Binary;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Network;

/// <summary>
/// Cash-shop period item packets (code 0xD2).
/// S→C: D2 11 count, D2 12 itemCode+slot+expireUnix.
/// </summary>
internal static class PeriodItemPackets
{
    public const byte Code = 0xD2;
    public const byte CountSubCode = 0x11;
    public const byte ListSubCode = 0x12;

    private const int ClientItemIndexStride = 512;

    public static async ValueTask SendCountAsync(IConnection connection, byte count)
    {
        int Write()
        {
            const int size = 5;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = size;
            span[2] = Code;
            span[3] = CountSubCode;
            span[4] = count;
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    public static async ValueTask SendItemAsync(IConnection connection, Item item)
    {
        if (item.Definition is null || item.ExpirationDate is not { } expire)
        {
            return;
        }

        var itemCode = (ushort)((item.Definition.Group * ClientItemIndexStride) + item.Definition.Number);
        var expireUnix = (int)new DateTimeOffset(DateTime.SpecifyKind(expire, DateTimeKind.Utc)).ToUnixTimeSeconds();

        int Write()
        {
            const int size = 12;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = size;
            span[2] = Code;
            span[3] = ListSubCode;
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(4, 2), itemCode);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(6, 2), item.ItemSlot);
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(8, 4), expireUnix);
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }
}
