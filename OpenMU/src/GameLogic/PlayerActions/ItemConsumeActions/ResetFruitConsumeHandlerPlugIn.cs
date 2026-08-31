// -----------------------------------------------------------------------
// <copyright file="ResetFruitConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Views.Character;

/// <summary>
/// Base class of the reset fruits of the item shop. Using one of them sets its stat back to the base
/// value of the character class and gives all the invested points back as level up points.
/// </summary>
public abstract class ResetFruitConsumeHandlerPlugIn : BaseConsumeHandlerPlugIn
{
    /// <summary>
    /// Gets the stat which is reset by this fruit.
    /// </summary>
    protected abstract AttributeDefinition StatAttribute { get; }

    /// <inheritdoc />
    public override async ValueTask<bool> ConsumeItemAsync(Player player, Item item, Item? targetItem, FruitUsage fruitUsage)
    {
        if (!this.CheckPreconditions(player, item)
            || player.Attributes is null
            || player.SelectedCharacter is not { CharacterClass: { } characterClass } selectedCharacter)
        {
            return false;
        }

        // Giving points back can break the requirements of what the character is wearing.
        if (player.Inventory!.EquippedItems.Any())
        {
            await player.InvokeViewPlugInAsync<IFruitConsumptionResponsePlugIn>(
                p => p.ShowResponseAsync(FruitConsumptionResult.PreventedByEquippedItems, 0, this.StatAttribute)).ConfigureAwait(false);
            return false;
        }

        if (characterClass.StatAttributes.FirstOrDefault(s => s.IncreasableByPlayer && s.Attribute == this.StatAttribute) is not { } statDefinition)
        {
            await player.InvokeViewPlugInAsync<IFruitConsumptionResponsePlugIn>(
                p => p.ShowResponseAsync(FruitConsumptionResult.MinusPrevented, 0, this.StatAttribute)).ConfigureAwait(false);
            return false;
        }

        var baseValue = (int)statDefinition.BaseValue;
        var investedPoints = (int)player.Attributes[this.StatAttribute] - baseValue;
        if (investedPoints <= 0)
        {
            await player.InvokeViewPlugInAsync<IFruitConsumptionResponsePlugIn>(
                p => p.ShowResponseAsync(FruitConsumptionResult.MinusPreventedByDefault, 0, this.StatAttribute)).ConfigureAwait(false);
            return false;
        }

        player.Attributes[this.StatAttribute] = baseValue;
        selectedCharacter.LevelUpPoints += investedPoints;

        await this.ConsumeSourceItemAsync(player, item).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IUpdateCharacterBaseStatsPlugIn>(p => p.UpdateCharacterBaseStatsAsync()).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IUpdateCharacterStatsPlugIn>(p => p.UpdateCharacterStatsAsync()).ConfigureAwait(false);
        return true;
    }
}
