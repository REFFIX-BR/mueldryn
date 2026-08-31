// <copyright file="QuestNpcTalkPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.QuestPanel;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Opens the quest NPC list when a player talks to the Quest Master.
/// </summary>
[Guid("B2C3D4E5-F6A7-8901-2345-6789ABCDEF01")]
[PlugIn]
[Display(Name = "Quest NPC Talk Handler", Description = "Opens the quest NPC dialog when talking to the Quest Master.")]
public class QuestNpcTalkPlugIn : IPlayerTalkToNpcPlugIn
{
    /// <inheritdoc />
    public async ValueTask PlayerTalksToNpcAsync(Player player, NonPlayerCharacter npc, NpcTalkEventArgs eventArgs)
    {
        if (npc.Definition.Number != QuestPanelService.QuestNpcNumber)
        {
            return;
        }

        if (player.SelectedCharacter is null)
        {
            return;
        }

        // Must set before any await — TalkNpcAction does not await this call.
        // Do not leave NpcDialogOpened: our UI is client-side and needs re-talk to work.
        eventArgs.HasBeenHandled = true;
        eventArgs.LeavesDialogOpen = false;
        await QuestPanelService.ShowNpcDialogAsync(player).ConfigureAwait(false);
    }
}
