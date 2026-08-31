// <copyright file="IShowEventSchedulePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using MUnique.OpenMU.GameLogic.EventSchedule;

/// <summary>
/// Sends the event/invasion schedule list to the client (H-key window).
/// </summary>
public interface IShowEventSchedulePlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the schedule entries.
    /// </summary>
    ValueTask ShowEventScheduleAsync(IReadOnlyList<EventScheduleEntry> entries);
}
