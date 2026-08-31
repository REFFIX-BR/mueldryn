// <copyright file="ShowEventSchedulePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.EventSchedule;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends the event schedule list to the extended client.
/// </summary>
[PlugIn]
[Display(Name = "Show Event Schedule", Description = "Sends invasion/event countdowns for the H-key window.")]
[Guid("A7C4E91B-2F5D-4A8E-9B1C-0D3E6F8A2B45")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class ShowEventSchedulePlugIn : IShowEventSchedulePlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowEventSchedulePlugIn"/> class.
    /// </summary>
    public ShowEventSchedulePlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowEventScheduleAsync(IReadOnlyList<EventScheduleEntry> entries)
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await EventSchedulePackets.SendResponseAsync(connection, entries).ConfigureAwait(false);
    }
}
