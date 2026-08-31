// <copyright file="DungeonPanelService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Dungeons;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Views;

/// <summary>
/// Builds and shows the Fortress of Imperial dungeon selection window.
/// </summary>
public static class DungeonPanelService
{
    /// <summary>
    /// Identifier used by the custom client packet (0xFA/0x11).
    /// </summary>
    public const byte DungeonId = 1;

    /// <summary>
    /// Shows the dungeon window for the given player.
    /// </summary>
    public static async ValueTask ShowWindowAsync(Player player, DungeonDifficulty difficulty = DungeonDifficulty.Normal)
    {
        if (player.SelectedCharacter is null)
        {
            return;
        }

        var payload = await BuildPayloadAsync(player, difficulty).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IShowDungeonWindowPlugIn>(p => p.ShowDungeonWindowAsync(payload)).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the window payload for the selected difficulty.
    /// </summary>
    public static async ValueTask<DungeonWindowPayload> BuildPayloadAsync(Player player, DungeonDifficulty difficulty)
    {
        var repository = new CharacterAttributeEntryLimitRepository(player);
        var limit = await repository.GetOrCreateAsync(player.SelectedCharacter!).ConfigureAwait(false);
        var remaining = (byte)Math.Clamp(await limit.GetAvailableEntriesAsync().ConfigureAwait(false), 0, DungeonKeyItems.MaxDailyEntries);
        var freeSlots = (byte)Math.Min(byte.MaxValue, player.Inventory?.FreeSlots.Count() ?? 0);
        var definition = FindDefinition(player, difficulty);
        var minLevel = (ushort)(definition?.MinimumCharacterLevel ?? GetMinimumLevel(difficulty));
        var minResets = GetMinimumResets(difficulty);
        return new DungeonWindowPayload(DungeonId, difficulty, minLevel, minResets, remaining, freeSlots);
    }

    /// <summary>
    /// Finds the mini game definition for a difficulty.
    /// </summary>
    public static MiniGameDefinition? FindDefinition(Player player, DungeonDifficulty difficulty)
        => player.GameContext.Configuration.MiniGameDefinitions
            .FirstOrDefault(d => d.Type == MiniGameType.ImperialFortress && d.GameLevel == (byte)difficulty);

    /// <summary>
    /// Gets the configured minimum character level for a difficulty.
    /// </summary>
    public static int GetMinimumLevel(DungeonDifficulty difficulty) => difficulty switch
    {
        DungeonDifficulty.Hard => 250,
        DungeonDifficulty.Hell => 400,
        _ => 100,
    };

    /// <summary>
    /// Gets the configured minimum reset count for a difficulty.
    /// </summary>
    public static byte GetMinimumResets(DungeonDifficulty difficulty) => difficulty switch
    {
        DungeonDifficulty.Hard => 5,
        DungeonDifficulty.Hell => 15,
        _ => 0,
    };
}
