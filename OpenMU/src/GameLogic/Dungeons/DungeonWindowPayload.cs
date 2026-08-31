// <copyright file="DungeonWindowPayload.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Dungeons;

/// <summary>
/// Payload data for packet 0x11 (S→C) — DungeonWindow response.
/// </summary>
/// <remarks>
/// Packet layout (canal 0xFA, SubCode 0x11):
/// <code>
/// Offset | Size | Field
/// -------|------|------
///      4 |    1 | DungeonId
///      5 |    1 | Difficulty (0=Normal, 1=Hard, 2=Hell)
///      6 |    2 | MinLevel (LE uint16)
///      8 |    1 | MinResets
///      9 |    1 | RemainingEntries
///     10 |    1 | FreeInventorySlots
///     11 |    1 | (reserved)
/// </code>
/// Total packet length: 12 bytes.
/// </remarks>
public readonly struct DungeonWindowPayload
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DungeonWindowPayload"/> struct.
    /// </summary>
    /// <param name="dungeonId">The dungeon identifier.</param>
    /// <param name="difficulty">The dungeon difficulty.</param>
    /// <param name="minLevel">The minimum character level required.</param>
    /// <param name="minResets">The minimum number of resets required.</param>
    /// <param name="remainingEntries">The number of daily entries remaining for the player.</param>
    /// <param name="freeInventorySlots">The number of free inventory slots the player currently has.</param>
    public DungeonWindowPayload(
        byte dungeonId,
        DungeonDifficulty difficulty,
        ushort minLevel,
        byte minResets,
        byte remainingEntries,
        byte freeInventorySlots)
    {
        this.DungeonId = dungeonId;
        this.Difficulty = difficulty;
        this.MinLevel = minLevel;
        this.MinResets = minResets;
        this.RemainingEntries = remainingEntries;
        this.FreeInventorySlots = freeInventorySlots;
    }

    /// <summary>
    /// Gets the dungeon identifier (offset 4).
    /// </summary>
    public byte DungeonId { get; }

    /// <summary>
    /// Gets the difficulty level (offset 5). 0 = Normal, 1 = Hard, 2 = Hell.
    /// </summary>
    public DungeonDifficulty Difficulty { get; }

    /// <summary>
    /// Gets the minimum character level required to enter (offsets 6–7, LE uint16).
    /// </summary>
    public ushort MinLevel { get; }

    /// <summary>
    /// Gets the minimum number of resets required to enter (offset 8).
    /// </summary>
    public byte MinResets { get; }

    /// <summary>
    /// Gets the number of daily entries remaining for this character (offset 9).
    /// </summary>
    public byte RemainingEntries { get; }

    /// <summary>
    /// Gets the number of free inventory slots the player currently has (offset 10).
    /// </summary>
    public byte FreeInventorySlots { get; }
}
