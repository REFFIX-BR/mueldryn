// -----------------------------------------------------------------------
// <copyright file="ItemShopBoxConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Opens the reward items of the item shop (lucky tickets, chaos cards, keys and boxes). The reward is
/// taken from the drop groups of the item definition and goes directly into the inventory.
/// Items of the same group which are not reward items keep their normal behaviour.
/// </summary>
public abstract class ItemShopBoxConsumeHandlerPlugIn : BaseConsumeHandlerPlugIn
{
    private const int GenerationAttempts = 8;

    private static readonly ApplyMagicEffectConsumeHandlerPlugIn MagicEffectHandler = new();

    /// <summary>
    /// Gets the item numbers of this group which hold a reward.
    /// </summary>
    protected abstract short[] RewardItemNumbers { get; }

    /// <inheritdoc />
    public override async ValueTask<bool> ConsumeItemAsync(Player player, Item item, Item? targetItem, FruitUsage fruitUsage)
    {
        var definition = item.Definition;
        if (definition is null || !this.RewardItemNumbers.Contains(definition.Number))
        {
            // This handler is registered for the whole item group, so everything else has to keep working.
            return definition?.ConsumeEffect is not null
                   && await MagicEffectHandler.ConsumeItemAsync(player, item, targetItem, fruitUsage).ConfigureAwait(false);
        }

        if (!this.CheckPreconditions(player, item) || player.Inventory is not { } inventory)
        {
            return false;
        }

        var dropGroups = definition.DropItems.Where(group => group.SourceItemLevel == item.Level).ToList();
        if (dropGroups.Count == 0)
        {
            return false;
        }

        Item? reward = null;
        uint money = 0;
        for (var attempt = 0; attempt < GenerationAttempts && reward is null; attempt++)
        {
            var (generatedItem, generatedMoney, _) = player.GameContext.DropGenerator.GenerateItemDrop(dropGroups);
            reward = generatedItem;
            money = generatedMoney ?? 0;
            if (money > 0)
            {
                break;
            }
        }

        if (reward is null && money == 0)
        {
            return false;
        }

        if (reward is not null)
        {
            var slot = inventory.CheckInvSpace(reward);
            if (slot is null || slot < InventoryConstants.LastEquippableItemSlotIndex)
            {
                await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.InventoryFull)).ConfigureAwait(false);
                return false;
            }

            if (reward is not TemporaryItem)
            {
                player.PersistenceContext.Attach(reward);
            }

            if (!await inventory.AddItemAsync(slot.Value, reward).ConfigureAwait(false))
            {
                return false;
            }

            // A temporary item is replaced by a persistent one when it enters the inventory.
            var storedItem = inventory.GetItem(slot.Value);
            if (storedItem is not null)
            {
                await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(storedItem)).ConfigureAwait(false);
            }
        }

        if (money > 0)
        {
            inventory.TryAddMoney((int)money);
        }

        await this.ConsumeSourceItemAsync(player, item).ConfigureAwait(false);
        return true;
    }
}
