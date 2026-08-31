// <copyright file="IProvidesEventSchedule.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.EventSchedule;

/// <summary>
/// Implemented by plugins that can contribute a row to the H-key event schedule.
/// </summary>
public interface IProvidesEventSchedule
{
    /// <summary>
    /// Gets the localized/display name for the schedule list.
    /// </summary>
    string ScheduleDisplayName { get; }

    /// <summary>
    /// Builds the current schedule entry for this event, or <c>null</c> if unavailable.
    /// </summary>
    EventScheduleEntry? TryGetScheduleEntry(IGameContext gameContext);
}
