// <copyright file="QuestPanelRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.QuestPanel;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handles C1 FB 00 (status), C1 FB 02 (claim) and C1 FB 04 (abandon).
/// </summary>
[PlugIn]
[Display(Name = "Quest Panel Request", Description = "Handles side quest panel requests (0xFB).")]
[Guid("A0B1C2D3-E4F5-4678-9ABC-DEF012345678")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
internal sealed class QuestPanelRequestHandlerPlugIn : IPacketHandlerPlugIn
{
    /// <inheritdoc />
    public bool IsEncryptionExpected => false;

    /// <inheritdoc />
    public byte Key => QuestPanelPackets.Code;

    /// <inheritdoc />
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (player.SelectedCharacter is null
            || packet.Length < QuestPanelPackets.RequestLength)
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
            case QuestPanelPackets.StatusRequestSubCode:
            {
                var status = QuestPanelService.BuildStatus(player);
                await player.InvokeViewPlugInAsync<IShowQuestPanelPlugIn>(p => p.ShowQuestPanelStatusAsync(status)).ConfigureAwait(false);
                break;
            }

            case QuestPanelPackets.ClaimRequestSubCode:
            {
                var result = await QuestPanelService.TryClaimAsync(player).ConfigureAwait(false);
                var status = QuestPanelService.BuildStatus(player);
                await player.InvokeViewPlugInAsync<IShowQuestPanelPlugIn>(p => p.ShowQuestPanelClaimResultAsync(result, status)).ConfigureAwait(false);
                break;
            }

            case QuestPanelPackets.AbandonRequestSubCode:
            {
                await QuestPanelService.TryAbandonAsync(player).ConfigureAwait(false);
                break;
            }

            case QuestPanelPackets.AcceptRequestSubCode:
            {
                await QuestPanelService.TryAcceptAsync(player).ConfigureAwait(false);
                break;
            }
        }
    }
}
