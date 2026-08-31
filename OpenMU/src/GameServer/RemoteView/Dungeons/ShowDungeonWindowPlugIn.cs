// <copyright file="ShowDungeonWindowPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Dungeons;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Dungeons;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The default implementation of the <see cref="IShowDungeonWindowPlugIn"/> which sends the dungeon window packet (0xFA/0x11) to the client.
/// </summary>
[Guid("E4F5A6B7-C8D9-0E1F-2345-6789ABCDEF01")]
[PlugIn]
[Display(Name = "Show Dungeon Window", Description = "Sends the dungeon window packet (0xFA/0x11) to the client.")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public class ShowDungeonWindowPlugIn : IShowDungeonWindowPlugIn
{
    private readonly RemotePlayer player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowDungeonWindowPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public ShowDungeonWindowPlugIn(RemotePlayer player)
    {
        this.player = player;
    }

    /// <inheritdoc />
    public async ValueTask ShowDungeonWindowAsync(DungeonWindowPayload payload)
    {
        if (this.player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await EventSchedulePackets.SendDungeonWindowAsync(connection, payload).ConfigureAwait(false);
    }
}
