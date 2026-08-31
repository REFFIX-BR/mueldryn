// <copyright file="DungeonDifficulty.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Dungeons;

/// <summary>
/// The difficulty level of a Fortress of Imperial Dungeon instance.
/// </summary>
public enum DungeonDifficulty : byte
{
    /// <summary>
    /// Normal difficulty — minimum level 100, no resets required.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Hard difficulty — minimum level 250, 5 resets required.
    /// </summary>
    Hard = 1,

    /// <summary>
    /// Hell difficulty — minimum level 400, 15 resets required.
    /// </summary>
    Hell = 2,
}

/// <summary>
/// The current room (phase) of a Fortress of Imperial Dungeon run.
/// </summary>
public enum DungeonRoomPhase
{
    /// <summary>
    /// Room 1 — initial monster wave.
    /// </summary>
    Room1 = 1,

    /// <summary>
    /// Room 2 — elite monster wave.
    /// </summary>
    Room2 = 2,

    /// <summary>
    /// Room 3 — boss fight.
    /// </summary>
    Room3 = 3,
}
