// <copyright file="DungeonHellKeyCraftingHandler.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Craftings;

using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;

/// <summary>
/// Hell key mix: the sacrificed gear must be excellent +9 with at least two excellent options.
/// </summary>
public class DungeonHellKeyCraftingHandler : SimpleItemCraftingHandler
{
    private const int MinimumExcellentOptions = 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="DungeonHellKeyCraftingHandler"/> class.
    /// </summary>
    /// <param name="settings">The settings.</param>
    public DungeonHellKeyCraftingHandler(SimpleCraftingSettings settings)
        : base(settings)
    {
    }

    /// <inheritdoc />
    protected override bool RequiredItemMatches(Item item, ItemCraftingRequiredItem requiredItem)
    {
        if (!base.RequiredItemMatches(item, requiredItem))
        {
            return false;
        }

        if (requiredItem.RequiredItemOptions.Contains(ItemOptionTypes.Excellent)
            && requiredItem.MinimumItemLevel >= 9)
        {
            var excellentCount = item.ItemOptions.Count(option => option.ItemOption?.OptionType == ItemOptionTypes.Excellent);
            return excellentCount >= MinimumExcellentOptions;
        }

        return true;
    }
}
