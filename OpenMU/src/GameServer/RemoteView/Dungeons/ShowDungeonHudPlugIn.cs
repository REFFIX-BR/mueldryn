// <copyright file="ShowDungeonHudPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Dungeons;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Dungeons;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends dungeon HUD updates (0xFA/0x14) to the client.
/// </summary>
[Guid("B7C8D9E0-F1A2-4345-9678-9ABCDEF01234")]
[PlugIn]
[Display(Name = "Show Dungeon HUD", Description = "Sends the dungeon HUD packet (0xFA/0x14) to the client.")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public class ShowDungeonHudPlugIn : IShowDungeonHudPlugIn
{
    private readonly RemotePlayer player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowDungeonHudPlugIn"/> class.
    /// </summary>
    public ShowDungeonHudPlugIn(RemotePlayer player)
    {
        this.player = player;
    }

    /// <inheritdoc />
    public async ValueTask ShowDungeonHudUpdateAsync(DungeonHudUpdate update)
    {
        if (this.player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await EventSchedulePackets.SendDungeonHudUpdateAsync(connection, update).ConfigureAwait(false);
    }
}
