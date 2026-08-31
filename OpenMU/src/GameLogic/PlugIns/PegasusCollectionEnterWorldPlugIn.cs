// <copyright file="PegasusCollectionEnterWorldPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Collections;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Syncs Collections mask when the character enters the world.
/// </summary>
[PlugIn]
[Guid("C011EC70-E071-4B02-9C03-D4E5F6071831")]
[Display(Name = "Pegasus Collection Enter World", Description = "Sends Collections sync on enter world.")]
public sealed class PegasusCollectionEnterWorldPlugIn : IPlayerStateChangedPlugIn
{
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (previousState != PlayerState.CharacterSelection || currentState != PlayerState.EnteredWorld)
        {
            return;
        }

        await PegasusCollectionService.SendSyncAsync(player).ConfigureAwait(false);
    }
}
