// <copyright file="CashShopRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Cash Shop (0xD2) handler for Season 6 InGameShop UI: open, points, buy, empty storage.
/// </summary>
[PlugIn]
[Display(Name = "Cash Shop Request", Description = "Handles cash shop requests (0xD2) for Season 6 InGameShop UI.")]
[Guid("7F3A9C2E-4B18-4D6A-9E51-C8D0F2A4B6E1")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
internal sealed class CashShopRequestHandlerPlugIn : IPacketHandlerPlugIn
{
    private static readonly AttributeDefinition[] CashAttributes =
    [
        Stats.WCoinC,
        Stats.WCoinP,
        Stats.GoblinPoints,
    ];

    /// <inheritdoc />
    public bool IsEncryptionExpected => false;

    /// <inheritdoc />
    public byte Key => CashShopPackets.Code;

    /// <inheritdoc />
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (player is not RemotePlayer { Connection: { Connected: true } connection })
        {
            return;
        }

        if (player.SelectedCharacter is null || packet.Length < 4)
        {
            return;
        }

        var span = packet.Span;
        if (span[0] is not (0xC1 or 0xC3))
        {
            return;
        }

        switch (span[3])
        {
            case CashShopPackets.PointInfoSubCode:
                await SendPointsAsync(player, connection).ConfigureAwait(false);
                break;

            case CashShopPackets.OpenStateSubCode:
            {
                if (packet.Length < 5)
                {
                    return;
                }

                var isClosed = span[4] != 0;
                if (isClosed)
                {
                    return;
                }

                await CashShopPackets.SendOpenResultAsync(connection, success: true).ConfigureAwait(false);
                break;
            }

            case 0x03: // buy
            {
                if (packet.Length < CashShopItemBuyRequest.Length)
                {
                    await CashShopPackets.SendBuyResultAsync(connection, (byte)CashShopService.BuyResult.CannotBuy).ConfigureAwait(false);
                    break;
                }

                CashShopItemBuyRequest request = packet;
                var (result, remaining) = await CashShopService.TryBuyAsync(
                    player,
                    request.PackageMainIndex,
                    request.ProductMainIndex,
                    request.ItemIndex,
                    request.CoinIndex).ConfigureAwait(false);

                await CashShopPackets.SendBuyResultAsync(connection, (byte)result, remaining).ConfigureAwait(false);
                if (result == CashShopService.BuyResult.Success)
                {
                    await SendPointsAsync(player, connection).ConfigureAwait(false);
                }

                break;
            }

            case 0x05: // storage list
            {
                ushort page = 1;
                if (packet.Length >= 8)
                {
                    page = (ushort)BinaryPrimitives.ReadInt32LittleEndian(span[4..]);
                    if (page == 0)
                    {
                        page = 1;
                    }
                }

                await CashShopPackets.SendEmptyStorageAsync(connection, page).ConfigureAwait(false);
                break;
            }

            case 0x0A: // delete storage item
            case 0x0B: // consume storage item
                break;

            case 0x13: // event item list
                await CashShopPackets.SendEventItemCountAsync(connection, 0).ConfigureAwait(false);
                break;
        }
    }

    private static async ValueTask SendPointsAsync(Player player, Network.IConnection connection)
    {
        EnsureCashAttributeDefinitions(player);
        var wCoinC = GetAccountAttributeValue(player, Stats.WCoinC);
        var wCoinP = GetAccountAttributeValue(player, Stats.WCoinP);
        var goblin = GetAccountAttributeValue(player, Stats.GoblinPoints);
        await CashShopPackets.SendPointInfoAsync(
            connection,
            totalCash: wCoinC + wCoinP,
            cashCredit: wCoinC,
            cashPrepaid: wCoinP,
            totalPoint: 0,
            totalMileage: goblin).ConfigureAwait(false);
    }

    private static void EnsureCashAttributeDefinitions(Player player)
    {
        foreach (var attr in CashAttributes)
        {
            EnsureDefinition(player, attr);
        }
    }

    private static void EnsureDefinition(Player player, AttributeDefinition template)
    {
        var config = player.GameContext.Configuration;
        var existing = config.Attributes.FirstOrDefault(a => a.Id == template.Id);
        if (existing is not null)
        {
            if (existing.MaximumValue is 0f)
            {
                existing.MaximumValue = null;
            }

            return;
        }

        try
        {
            var persistent = player.PersistenceContext.CreateNew<AttributeDefinition>(
                template.Id,
                template.Designation,
                template.Description);
            persistent.MaximumValue = null;
            config.Attributes.Add(persistent);
        }
        catch (InvalidOperationException)
        {
            template.MaximumValue = null;
            config.Attributes.Add(template);
        }
    }

    private static double GetAccountAttributeValue(Player player, AttributeDefinition template)
    {
        var attr = player.Account?.Attributes
            .FirstOrDefault(a => a.Definition is not null && a.Definition.Id == template.Id);
        return attr?.Value ?? 0d;
    }
}
