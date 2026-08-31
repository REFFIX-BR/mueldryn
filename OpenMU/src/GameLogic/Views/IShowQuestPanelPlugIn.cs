// <copyright file="IShowQuestPanelPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using MUnique.OpenMU.GameLogic.QuestPanel;

/// <summary>
/// Sends quest panel status / claim results to the client.
/// </summary>
public interface IShowQuestPanelPlugIn : IViewPlugIn
{
    /// <summary>
    /// Sends the current quest panel status.
    /// </summary>
    ValueTask ShowQuestPanelStatusAsync(QuestPanelStatus status);

    /// <summary>
    /// Sends the claim result.
    /// </summary>
    ValueTask ShowQuestPanelClaimResultAsync(QuestPanelClaimResult result, QuestPanelStatus status);

    /// <summary>
    /// Opens the Quest Master NPC list dialog.
    /// </summary>
    ValueTask ShowQuestNpcDialogAsync(QuestPanelStatus status);

    /// <summary>
    /// Sends the full main-quest list for the NPC dialog.
    /// </summary>
    ValueTask ShowQuestNpcListAsync(int stage, int total, IReadOnlyList<QuestPanelNpcListEntry> entries);
}
