// <copyright file="DungeonNpcTalkPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Dungeons;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Opens the dungeon window when a player talks to the Fortress of Imperial NPC.
/// </summary>
[Guid("D1E2F3A4-B5C6-7D8E-9F01-234567890ABC")]
[PlugIn]
[Display(Name = "Dungeon NPC Talk Handler", Description = "Opens the dungeon window when a player interacts with the Fortress of Imperial Dungeon NPC.")]
public class DungeonNpcTalkPlugIn : IPlayerTalkToNpcPlugIn
{
    /// <summary>
    /// NPC number used for the Imperial Dungeon entry NPC.
    /// </summary>
    public static short DungeonNpcNumber => 690;

    /// <inheritdoc />
    public async ValueTask PlayerTalksToNpcAsync(Player player, NonPlayerCharacter npc, NpcTalkEventArgs eventArgs)
    {
        if (npc.Definition.Number != DungeonNpcNumber)
        {
            return;
        }

        if (player.SelectedCharacter is null)
        {
            return;
        }

        // Must set before any await — TalkNpcAction does not await this call.
        eventArgs.HasBeenHandled = true;
        eventArgs.LeavesDialogOpen = true;
        await DungeonPanelService.ShowWindowAsync(player).ConfigureAwait(false);
    }
}
