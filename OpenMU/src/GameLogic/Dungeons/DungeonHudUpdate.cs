// <copyright file="DungeonHudUpdate.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Dungeons;

/// <summary>
/// Payload data for packet 0x14 (S→C) — DungeonHud update.
/// </summary>
/// <remarks>
/// Packet layout (canal 0xFA, SubCode 0x14):
/// <code>
/// Offset | Size | Field
/// -------|------|------
///      4 |    1 | CurrentWave (1–10)
///      5 |    2 | Remaining (LE uint16)
///      7 |    4 | TimeRemainingSeconds (LE uint32)
///     11 |   32 | ObjectiveText (UTF-8, zero-padded)
/// </code>
/// Total packet length: 43 bytes.
/// </remarks>
public readonly struct DungeonHudUpdate
{
    /// <summary>
    /// Maximum byte length of <see cref="ObjectiveText"/> when encoded in UTF-8.
    /// </summary>
    public const int ObjectiveTextMaxBytes = 32;

    /// <summary>
    /// Initializes a new instance of the <see cref="DungeonHudUpdate"/> struct.
    /// </summary>
    public DungeonHudUpdate(
        byte currentWave,
        ushort remaining,
        uint timeRemainingSeconds,
        string objectiveText)
    {
        this.CurrentWave = currentWave;
        this.Remaining = remaining;
        this.TimeRemainingSeconds = timeRemainingSeconds;
        this.ObjectiveText = objectiveText;
    }

    /// <summary>
    /// Gets the current wave number (offset 4). Valid range: 1–10.
    /// </summary>
    public byte CurrentWave { get; }

    /// <summary>
    /// Gets how many monsters are left in the current wave (offsets 5–6, LE uint16).
    /// During intermission this is 0.
    /// </summary>
    public ushort Remaining { get; }

    /// <summary>
    /// Gets the time remaining in the dungeon run, in seconds (offsets 7–10, LE uint32).
    /// </summary>
    public uint TimeRemainingSeconds { get; }

    /// <summary>
    /// Gets the objective description text (offsets 11–42, UTF-8, zero-padded to 32 bytes).
    /// </summary>
    public string ObjectiveText { get; }

    /// <summary>
    /// Packet compatibility alias for <see cref="CurrentWave"/>.
    /// </summary>
    public byte CurrentRoom => this.CurrentWave;

    /// <summary>
    /// Packet compatibility alias for <see cref="Remaining"/>.
    /// </summary>
    public ushort KillCount => this.Remaining;
}
