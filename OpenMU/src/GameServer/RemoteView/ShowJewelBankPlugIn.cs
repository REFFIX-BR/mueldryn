// <copyright file="ShowJewelBankPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.JewelBank;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends jewel bank packets to the extended client.
/// </summary>
[PlugIn]
[Display(Name = "Show Jewel Bank", Description = "Sends jewel bank status and action results.")]
[Guid("B2C3D4E5-6F70-4819-9A0B-1C2D3E4F5061")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class ShowJewelBankPlugIn : IShowJewelBankPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowJewelBankPlugIn"/> class.
    /// </summary>
    public ShowJewelBankPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowJewelBankStatusAsync(JewelBankStatus status)
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await JewelBankPackets.SendStatusAsync(connection, status).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowJewelBankResultAsync(JewelBankResult result, JewelBankStatus status)
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await JewelBankPackets.SendActionResultAsync(connection, result, status).ConfigureAwait(false);
    }
}
