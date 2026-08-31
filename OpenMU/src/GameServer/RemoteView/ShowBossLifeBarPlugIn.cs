// <copyright file="ShowBossLifeBarPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends the boss health to the extended client, which draws the boss life bar.
/// </summary>
[PlugIn]
[Display(Name = "Show Boss Life Bar", Description = "Sends the remaining health of boss monsters.")]
[Guid("2F7A6C31-8D45-4E62-9A0B-3D5C7E1F84A9")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class ShowBossLifeBarPlugIn : IShowBossLifeBarPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowBossLifeBarPlugIn"/> class.
    /// </summary>
    public ShowBossLifeBarPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowBossLifeBarAsync(string bossName, byte healthPercentage, bool isAlive)
    {
        if (this._player.Connection is not { Connected: true } connection)
        {
            return;
        }

        await EventSchedulePackets.SendBossLifeBarAsync(connection, bossName, healthPercentage, isAlive).ConfigureAwait(false);
    }
}
