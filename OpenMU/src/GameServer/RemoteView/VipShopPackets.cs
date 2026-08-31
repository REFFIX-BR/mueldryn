// <copyright file="VipShopPackets.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Buffers.Binary;
using System.Text;
using MUnique.OpenMU.GameLogic.VipShop;
using MUnique.OpenMU.Network;

/// <summary>
/// Shopping VIP packets (code 0xEE).
/// C→S: EE 00 status, EE 02 buy.
/// S→C: EE 01 status, EE 03 buy result + status.
/// </summary>
internal static class VipShopPackets
{
    public const byte Code = 0xEE;
    public const byte StatusRequestSubCode = 0x00;
    public const byte StatusResponseSubCode = 0x01;
    public const byte BuyRequestSubCode = 0x02;
    public const byte BuyResponseSubCode = 0x03;

    public const int StatusRequestLength = 4;
    public const int BuyRequestLength = 4;

    // header+sub + isVip + remainDays + prices + bonuses + planDays + nameLen + name
    private const int FixedStatusPayload = 1 + 2 + 4 + 4 + 4 + 1 + 1 + 2 + 1;

    public static async ValueTask SendStatusAsync(IConnection connection, VipShopService.VipShopStatus status)
    {
        int Write()
        {
            var nameBytes = Encoding.UTF8.GetBytes(status.CharacterName ?? string.Empty);
            if (nameBytes.Length > 20)
            {
                nameBytes = nameBytes.AsSpan(0, 20).ToArray();
            }

            var size = 4 + FixedStatusPayload + nameBytes.Length;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = Code;
            span[3] = StatusResponseSubCode;
            WriteStatusBody(span[4..], status, nameBytes);
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    public static async ValueTask SendBuyResultAsync(IConnection connection, VipShopService.BuyResult result, VipShopService.VipShopStatus status)
    {
        int Write()
        {
            var nameBytes = Encoding.UTF8.GetBytes(status.CharacterName ?? string.Empty);
            if (nameBytes.Length > 20)
            {
                nameBytes = nameBytes.AsSpan(0, 20).ToArray();
            }

            var size = 5 + FixedStatusPayload + nameBytes.Length;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = Code;
            span[3] = BuyResponseSubCode;
            span[4] = (byte)result;
            WriteStatusBody(span[5..], status, nameBytes);
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    private static void WriteStatusBody(Span<byte> span, VipShopService.VipShopStatus status, byte[] nameBytes)
    {
        span[0] = (byte)(status.IsVip ? 1 : 0);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(1, 2), (ushort)Math.Clamp(status.RemainingDays, 0, ushort.MaxValue));
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(3, 4), (uint)VipShopService.PriceWc);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(7, 4), (uint)VipShopService.PriceWp);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(11, 4), (uint)VipShopService.PriceToken);
        span[15] = (byte)VipShopService.ExpBonusPercent;
        span[16] = (byte)VipShopService.DropBonusPercent;
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(17, 2), (ushort)VipShopService.PlanDays);
        span[19] = (byte)nameBytes.Length;
        nameBytes.CopyTo(span[20..]);
    }
}
