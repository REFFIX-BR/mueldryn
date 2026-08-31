// <copyright file="ShowInvasionStatusPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.EventSchedule;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends live invasion monster counts to the extended client.
/// </summary>
[PlugIn]
[Display(Name = "Show Invasion Status", Description = "Sends alive/total monster counts for active invasions.")]
[Guid("B1D8E4F2-7A3C-4E9B-9F1D-2C5A8B0E6D44")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class ShowInvasionStatusPlugIn : IShowInvasionStatusPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowInvasionStatusPlugIn"/> class.
    /// </summary>
    public ShowInvasionStatusPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowInvasionStatusAsync(InvasionStatusSnapshot snapshot)
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await EventSchedulePackets.SendInvasionStatusAsync(connection, snapshot).ConfigureAwait(false);
    }
}
