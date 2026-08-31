// <copyright file="ShowQuestPanelPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.QuestPanel;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends quest panel packets to the extended client.
/// </summary>
[PlugIn]
[Display(Name = "Show Quest Panel", Description = "Sends side quest panel status and claim results.")]
[Guid("F1A2B3C4-5D6E-4F7A-8B9C-0D1E2F3A4B5C")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class ShowQuestPanelPlugIn : IShowQuestPanelPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowQuestPanelPlugIn"/> class.
    /// </summary>
    public ShowQuestPanelPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowQuestPanelStatusAsync(QuestPanelStatus status)
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await QuestPanelPackets.SendStatusAsync(connection, status).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowQuestPanelClaimResultAsync(QuestPanelClaimResult result, QuestPanelStatus status)
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await QuestPanelPackets.SendClaimResultAsync(connection, result, status).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowQuestNpcDialogAsync(QuestPanelStatus status)
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await QuestPanelPackets.SendOpenNpcDialogAsync(connection, status).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowQuestNpcListAsync(int stage, int total, IReadOnlyList<QuestPanelNpcListEntry> entries)
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await QuestPanelPackets.SendNpcQuestListAsync(connection, stage, total, entries).ConfigureAwait(false);
    }
}
