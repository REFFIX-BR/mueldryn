// -----------------------------------------------------------------------
// <copyright file="MagicBackpackConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Consume handler for the magic backpack of the item shop, which adds one inventory extension.
/// </summary>
[Guid("A0645D3D-26BC-41F1-93CA-94DF6102859B")]
[PlugIn]
[Display(Name = "Magic backpack", Description = "Adds one inventory extension to the character.")]
public class MagicBackpackConsumeHandlerPlugIn : BaseConsumeHandlerPlugIn
{
    private const int MaximumExtensions = 4;

    /// <inheritdoc />
    public override ItemIdentifier Key => new(162, 14);

    /// <inheritdoc />
    public override async ValueTask<bool> ConsumeItemAsync(Player player, Item item, Item? targetItem, FruitUsage fruitUsage)
    {
        if (!this.CheckPreconditions(player, item)
            || player.SelectedCharacter is not { } selectedCharacter)
        {
            return false;
        }

        if (selectedCharacter.InventoryExtensions >= MaximumExtensions)
        {
            await player.ShowBlueMessageAsync("Your inventory is already extended to the maximum.").ConfigureAwait(false);
            return false;
        }

        selectedCharacter.InventoryExtensions++;
        await this.ConsumeSourceItemAsync(player, item).ConfigureAwait(false);

        // The client gets the inventory size when it enters the world, so the new rows show up after a relog.
        await player.ShowBlueMessageAsync("Inventory extended. Log in again to use the new space.").ConfigureAwait(false);
        return true;
    }
}
