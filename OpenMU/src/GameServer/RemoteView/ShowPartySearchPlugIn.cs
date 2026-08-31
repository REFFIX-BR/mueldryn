// <copyright file="ShowPartySearchPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PartySearch;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends Party Search packets to the extended client.
/// </summary>
[PlugIn]
[Display(Name = "Show Party Search", Description = "Sends Party Search list and action results (0xF9).")]
[Guid("A1B2C3D4-E5F6-4789-ABCD-0123456789EF")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class ShowPartySearchPlugIn : IShowPartySearchPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowPartySearchPlugIn"/> class.
    /// </summary>
    public ShowPartySearchPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowPartySearchListAsync(
        IReadOnlyList<PartySearchListEntry> entries,
        bool ownActive,
        PartySearchListing? ownListing)
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await PartySearchPackets.SendListAsync(connection, entries, ownActive, ownListing).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowPartySearchPublishResultAsync(PartySearchResult result, bool ownActive)
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await PartySearchPackets.SendPublishResultAsync(connection, result, ownActive).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowPartySearchJoinResultAsync(PartySearchResult result)
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await PartySearchPackets.SendJoinResultAsync(connection, result).ConfigureAwait(false);
    }
}
