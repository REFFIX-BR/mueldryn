// <copyright file="DungeonTicketItem.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Dungeons;

/// <summary>
/// Resolves and consumes the difficulty key required to enter the dungeon.
/// Normal uses Silver Key +9, Hard uses Red Key +9 and Hell uses Purple Key +9.
/// </summary>
public static class DungeonTicketItem
{
    /// <summary>
    /// Entry consumes the matching dungeon key.
    /// </summary>
    public const bool RequireTicket = true;

    /// <summary>
    /// Item group of dungeon keys.
    /// </summary>
    public const byte ItemGroup = DungeonKeyItems.Group;

    /// <summary>
    /// Gets the inventory item number of the key required for <paramref name="difficulty"/>.
    /// </summary>
    public static short GetItemNumber(DungeonDifficulty difficulty)
        => DungeonKeyItems.GetRequiredKeyNumber(difficulty);

    /// <summary>
    /// Gets the display name of the key required for <paramref name="difficulty"/>.
    /// </summary>
    public static string GetDisplayName(DungeonDifficulty difficulty)
        => DungeonKeyItems.GetRequiredKeyName(difficulty);

    /// <summary>
    /// Finds the first matching dungeon key in the player's inventory.
    /// </summary>
    public static Item? FindKey(Player player, DungeonDifficulty difficulty)
    {
        var number = GetItemNumber(difficulty);
        return player.Inventory?.Items.FirstOrDefault(item =>
            item.Definition is { } definition
            && definition.Group == ItemGroup
            && definition.Number == number
            && item.Level >= DungeonKeyItems.KeyLevel
            && item.Durability > 0);
    }

    /// <summary>
    /// Returns whether the player has the dungeon key for <paramref name="difficulty"/>.
    /// </summary>
    public static bool HasTicket(Player player, DungeonDifficulty difficulty)
        => FindKey(player, difficulty) is not null;

    /// <summary>
    /// Removes the matching dungeon key from the inventory, if present.
    /// </summary>
    public static async ValueTask ConsumeKeyAsync(Player player, DungeonDifficulty difficulty)
    {
        var key = FindKey(player, difficulty);
        if (key is null)
        {
            return;
        }

        await player.DestroyInventoryItemAsync(key).ConfigureAwait(false);
    }
}
