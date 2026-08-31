// <copyright file="ShowPeriodItemsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends 0xD2 period-item packets to the extended client.
/// </summary>
[PlugIn]
[Display(Name = "Show Period Items", Description = "Sends cash item expiration timestamps to the client.")]
[Guid("E5F60718-92A3-4B4C-2D3E-4F5061728394")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class ShowPeriodItemsPlugIn : IShowPeriodItemsPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowPeriodItemsPlugIn"/> class.
    /// </summary>
    public ShowPeriodItemsPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowPeriodItemAsync(Item item)
    {
        if (this._player.Connection is not { Connected: true } connection || !item.HasExpiration)
        {
            return;
        }

        await PeriodItemPackets.SendItemAsync(connection, item).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowAllPeriodItemsAsync()
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        var items = this._player.Inventory?.Items?
            .Where(i => i.HasExpiration && !i.IsExpirationElapsed)
            .ToList() ?? [];

        await PeriodItemPackets.SendCountAsync(connection, (byte)Math.Min(items.Count, 255)).ConfigureAwait(false);
        foreach (var item in items)
        {
            await PeriodItemPackets.SendItemAsync(connection, item).ConfigureAwait(false);
        }
    }
}
