// <copyright file="IProvidesInvasionMonsterStatus.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.EventSchedule;

/// <summary>
/// Implemented by invasion plugins that can report live monster counts.
/// </summary>
public interface IProvidesInvasionMonsterStatus
{
    /// <summary>
    /// Gets the schedule/display name of the invasion.
    /// </summary>
    string ScheduleDisplayName { get; }

    /// <summary>
    /// Builds status rows for the active invasion run, or an empty list when idle.
    /// </summary>
    IReadOnlyList<InvasionMonsterStatusEntry> GetMonsterStatusEntries(IGameContext gameContext);

    /// <summary>
    /// Gets countdown seconds for the active/preparing run, or <c>null</c> when idle.
    /// </summary>
    int? TryGetActiveSecondsRemaining(IGameContext gameContext);
}
