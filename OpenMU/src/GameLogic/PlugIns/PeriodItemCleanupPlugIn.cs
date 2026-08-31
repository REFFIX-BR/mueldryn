// <copyright file="PeriodItemCleanupPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Removes expired cash/period items on world enter and syncs remaining expirations to the client.
/// </summary>
[PlugIn]
[Display(Name = "Period Item Cleanup", Description = "Removes expired cash items and syncs expiration tooltips.")]
[Guid("F6071829-A3B4-4C5D-3E4F-5061728395A6")]
public sealed class PeriodItemCleanupPlugIn : IPlayerStateChangedPlugIn
{
    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (previousState != PlayerState.CharacterSelection || currentState != PlayerState.EnteredWorld)
        {
            return;
        }

        if (player.Inventory is null)
        {
            return;
        }

        var expired = player.Inventory.Items
            .Where(i => i.IsExpirationElapsed)
            .ToList();

        foreach (var item in expired)
        {
            var slot = item.ItemSlot;
            await player.Inventory.RemoveItemAsync(item).ConfigureAwait(false);
            await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IItemRemovedPlugIn>(p => p.RemoveItemAsync(slot)).ConfigureAwait(false);
        }

        await player.InvokeViewPlugInAsync<IShowPeriodItemsPlugIn>(p => p.ShowAllPeriodItemsAsync()).ConfigureAwait(false);
    }
}
