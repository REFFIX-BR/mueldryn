// <copyright file="InvasionStatusService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.EventSchedule;

using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Aggregates live invasion monster counts for the client Invasões dropdown.
/// </summary>
public static class InvasionStatusService
{
    /// <summary>
    /// Builds the dropdown snapshot from all active invasion plugins.
    /// </summary>
    public static InvasionStatusSnapshot Build(IGameContext gameContext)
    {
        var entries = new List<InvasionMonsterStatusEntry>();
        string title = "Invasoes";
        var seconds = 0;

        foreach (var provider in gameContext.PlugInManager
                     .GetActivePlugInsOf<IPeriodicTaskPlugIn>()
                     .OfType<IProvidesInvasionMonsterStatus>())
        {
            var rows = provider.GetMonsterStatusEntries(gameContext);
            if (rows.Count == 0)
            {
                continue;
            }

            if (entries.Count == 0)
            {
                title = provider.ScheduleDisplayName;
                seconds = provider.TryGetActiveSecondsRemaining(gameContext) ?? 0;
            }

            entries.AddRange(rows);
        }

        return new InvasionStatusSnapshot
        {
            Title = title,
            SecondsRemaining = seconds,
            Entries = entries,
        };
    }
}
