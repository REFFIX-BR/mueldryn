// <copyright file="ShowSoulSystemPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.SoulSystem;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends Soul System packets to the extended client.
/// </summary>
[PlugIn]
[Display(Name = "Show Soul System", Description = "Sends Soul System status and action results.")]
[Guid("E5F6A7B8-9203-4B4C-1D2E-3F4051627384")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class ShowSoulSystemPlugIn : IShowSoulSystemPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowSoulSystemPlugIn"/> class.
    /// </summary>
    public ShowSoulSystemPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowSoulSystemStatusAsync(SoulSystemStatus status)
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await SoulSystemPackets.SendStatusAsync(connection, status).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowSoulSystemResultAsync(SoulSystemResult result, SoulSystemStatus status)
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await SoulSystemPackets.SendActionResultAsync(connection, result, status).ConfigureAwait(false);
    }
}
