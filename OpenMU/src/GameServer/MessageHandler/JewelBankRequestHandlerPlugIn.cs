// <copyright file="JewelBankRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.JewelBank;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handles C1 FC 00/02/04 jewel bank requests.
/// </summary>
[PlugIn]
[Display(Name = "Jewel Bank Request", Description = "Handles jewel bank requests (0xFC).")]
[Guid("C3D4E5F6-7081-492A-0B1C-2D3E4F506172")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
internal sealed class JewelBankRequestHandlerPlugIn : IPacketHandlerPlugIn
{
    /// <inheritdoc />
    public bool IsEncryptionExpected => false;

    /// <inheritdoc />
    public byte Key => JewelBankPackets.Code;

    /// <inheritdoc />
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (player.SelectedCharacter is null || packet.Length < JewelBankPackets.StatusRequestLength)
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
            case JewelBankPackets.StatusRequestSubCode:
            {
                var status = await JewelBankService.BuildStatusAsync(player).ConfigureAwait(false);
                await player.InvokeViewPlugInAsync<IShowJewelBankPlugIn>(p => p.ShowJewelBankStatusAsync(status)).ConfigureAwait(false);
                break;
            }

            case JewelBankPackets.DepositRequestSubCode:
            {
                if (packet.Length < JewelBankPackets.DepositRequestLength)
                {
                    return;
                }

                var result = await JewelBankService.TryDepositAsync(player, span[4]).ConfigureAwait(false);
                var status = await JewelBankService.BuildStatusAsync(player).ConfigureAwait(false);
                await player.InvokeViewPlugInAsync<IShowJewelBankPlugIn>(p => p.ShowJewelBankResultAsync(result, status)).ConfigureAwait(false);
                break;
            }

            case JewelBankPackets.WithdrawRequestSubCode:
            {
                if (packet.Length < JewelBankPackets.WithdrawRequestLength)
                {
                    return;
                }

                var mode = packet.Length >= JewelBankPackets.WithdrawRequestLengthWithMode
                    ? (JewelBankWithdrawMode)span[6]
                    : JewelBankWithdrawMode.Units;
                var result = await JewelBankService.TryWithdrawAsync(player, (JewelBankSlot)span[4], span[5], mode).ConfigureAwait(false);
                var status = await JewelBankService.BuildStatusAsync(player).ConfigureAwait(false);
                await player.InvokeViewPlugInAsync<IShowJewelBankPlugIn>(p => p.ShowJewelBankResultAsync(result, status)).ConfigureAwait(false);
                break;
            }
        }
    }
}
