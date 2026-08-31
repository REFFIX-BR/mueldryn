// <copyright file="SoulSystemEnterWorldPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.SoulSystem;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Re-applies Soul System bonuses when a character enters the world.
/// </summary>
[PlugIn]
[Guid("A7B8C9D0-1425-4D6E-3F40-5162738495A6")]
[Display(Name = "Soul System Enter World", Description = "Applies Soul System bonuses on enter world.")]
public sealed class SoulSystemEnterWorldPlugIn : IPlayerStateChangedPlugIn
{
    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (previousState != PlayerState.CharacterSelection || currentState != PlayerState.EnteredWorld)
        {
            return;
        }

        SoulSystemService.ApplyBonuses(player);
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
