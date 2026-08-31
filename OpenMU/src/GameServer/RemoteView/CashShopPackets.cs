// <copyright file="CashShopPackets.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Buffers.Binary;
using MUnique.OpenMU.Network;

/// <summary>
/// Minimal Season 6 cash shop (0xD2) S→C packets so the MuMain InGameShop UI can open.
/// Matches client structs in WSclient.h (PMSG_CASHSHOP_*).
/// Script folder expected on client: Data\InGameShopScript\512.2012.084\
/// </summary>
internal static class CashShopPackets
{
    public const byte Code = 0xD2;

    public const byte PointInfoSubCode = 0x01;
    public const byte OpenStateSubCode = 0x02;
    public const byte BuyResultSubCode = 0x03;
    public const byte StorageCountSubCode = 0x06;
    public const byte ScriptVersionSubCode = 0x0C;
    public const byte EventItemCountSubCode = 0x13;

    /// <summary>Matches local client scripts <c>512.2012.084</c>.</summary>
    public const ushort ScriptSaleZone = 512;
    public const ushort ScriptYear = 2012;
    public const ushort ScriptYearId = 84;

    public static async ValueTask SendScriptVersionAsync(IConnection connection)
    {
        int Write()
        {
            const int size = 10; // C1 + len + D2 + 0C + 3x WORD
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = Code;
            span[3] = ScriptVersionSubCode;
            BinaryPrimitives.WriteUInt16LittleEndian(span[4..], ScriptSaleZone);
            BinaryPrimitives.WriteUInt16LittleEndian(span[6..], ScriptYear);
            BinaryPrimitives.WriteUInt16LittleEndian(span[8..], ScriptYearId);
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    public static async ValueTask SendOpenResultAsync(IConnection connection, bool success)
    {
        int Write()
        {
            const int size = 5;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = Code;
            span[3] = OpenStateSubCode;
            span[4] = success ? (byte)1 : (byte)0;
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    public static async ValueTask SendPointInfoAsync(
        IConnection connection,
        double totalCash = 0,
        double cashCredit = 0,
        double cashPrepaid = 0,
        double totalPoint = 0,
        double totalMileage = 0)
    {
        int Write()
        {
            // header(4) + viewType(1) + 5 doubles
            const int size = 4 + 1 + (5 * 8);
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = Code;
            span[3] = PointInfoSubCode;
            span[4] = 0; // btViewType
            WriteDouble(span[5..], totalCash);
            WriteDouble(span[13..], cashCredit);
            WriteDouble(span[21..], cashPrepaid);
            WriteDouble(span[29..], totalPoint);
            WriteDouble(span[37..], totalMileage);
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    public static async ValueTask SendEmptyStorageAsync(IConnection connection, ushort pageIndex = 1)
    {
        int Write()
        {
            const int size = 12; // header + 4 WORDs
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = Code;
            span[3] = StorageCountSubCode;
            BinaryPrimitives.WriteUInt16LittleEndian(span[4..], 0); // total items
            BinaryPrimitives.WriteUInt16LittleEndian(span[6..], 0); // current page items
            BinaryPrimitives.WriteUInt16LittleEndian(span[8..], pageIndex);
            BinaryPrimitives.WriteUInt16LittleEndian(span[10..], 1); // total pages
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    public static async ValueTask SendBuyResultAsync(IConnection connection, byte resultCode, int leftCount = 0)
    {
        int Write()
        {
            const int size = 9; // header + result + leftCount(long)
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = Code;
            span[3] = BuyResultSubCode;
            span[4] = resultCode;
            BinaryPrimitives.WriteInt32LittleEndian(span[5..], leftCount);
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    public static async ValueTask SendEventItemCountAsync(IConnection connection, ushort count = 0)
    {
        int Write()
        {
            const int size = 6;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = Code;
            span[3] = EventItemCountSubCode;
            BinaryPrimitives.WriteUInt16LittleEndian(span[4..], count);
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    private static void WriteDouble(Span<byte> span, double value)
    {
        BinaryPrimitives.WriteInt64LittleEndian(span, BitConverter.DoubleToInt64Bits(value));
    }
}
