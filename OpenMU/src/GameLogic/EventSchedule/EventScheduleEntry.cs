// <copyright file="EventScheduleEntry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.EventSchedule;

/// <summary>
/// One row in the in-game event/invasion schedule window (H key).
/// </summary>
public sealed class EventScheduleEntry
{
    /// <summary>
    /// Gets the display name shown in the client.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the UI page category (invasions vs mini-games).
    /// </summary>
    public EventScheduleCategory Category { get; init; }

    /// <summary>
    /// Gets a value indicating whether the event is currently running.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Gets seconds until start (waiting) or until end (active).
    /// </summary>
    public int SecondsRemaining { get; init; }
}
