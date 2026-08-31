// <copyright file="EventScheduleTiming.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.EventSchedule;

using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

/// <summary>
/// Shared helpers to compute next start / remaining duration from a timetable.
/// </summary>
public static class EventScheduleTiming
{
    /// <summary>
    /// Computes schedule status from a periodic-task configuration and state.
    /// </summary>
    /// <param name="configuration">The periodic task configuration.</param>
    /// <param name="state">The current task state.</param>
    /// <param name="lastRunUtc">When the last run started.</param>
    /// <param name="nextRunUtc">When the next state transition is scheduled.</param>
    /// <param name="utcNow">Current UTC time.</param>
    public static (bool IsActive, int SecondsRemaining)? TryCompute(
        PeriodicTaskConfiguration? configuration,
        PeriodicTaskState state,
        DateTime lastRunUtc,
        DateTime nextRunUtc,
        DateTime utcNow)
    {
        if (configuration is null)
        {
            return null;
        }

        if (state is PeriodicTaskState.Prepared)
        {
            // Pre-start window: countdown until the actual start (NextRunUtc).
            var untilStart = (int)Math.Max(0, (nextRunUtc - utcNow).TotalSeconds);
            return (false, untilStart);
        }

        if (state is PeriodicTaskState.Started
            || (lastRunUtc != DateTime.MinValue && lastRunUtc.Add(configuration.TaskDuration) > utcNow))
        {
            var endUtc = lastRunUtc == DateTime.MinValue
                ? nextRunUtc
                : lastRunUtc.Add(configuration.TaskDuration);
            if (endUtc < utcNow && nextRunUtc > utcNow)
            {
                endUtc = nextRunUtc;
            }

            var remaining = (int)Math.Max(0, (endUtc - utcNow).TotalSeconds);
            return (true, remaining);
        }

        if (configuration.Timetable is not { Count: > 0 })
        {
            return null;
        }

        var timeNow = TimeOnly.FromDateTime(utcNow);
        var laterToday = configuration.Timetable.Where(t => t > timeNow).Order().ToList();
        DateTime nextUtc;
        if (laterToday.Count > 0)
        {
            nextUtc = utcNow.Date.Add(laterToday[0].ToTimeSpan());
        }
        else
        {
            var firstTomorrow = configuration.Timetable.Order().First();
            nextUtc = utcNow.Date.AddDays(1).Add(firstTomorrow.ToTimeSpan());
        }

        return (false, (int)Math.Max(0, (nextUtc - utcNow).TotalSeconds));
    }

    /// <summary>
    /// Computes from a <see cref="TimeSpan"/> until next opening (mini-games).
    /// </summary>
    /// <param name="duration">Duration until next start, or zero when open.</param>
    public static (bool IsActive, int SecondsRemaining)? FromDurationUntilStart(TimeSpan? duration)
    {
        if (duration is null)
        {
            return null;
        }

        // Open / entrance window (see MiniGameStartBasePlugIn).
        if (duration.Value == TimeSpan.Zero)
        {
            return (true, 0);
        }

        // FirstOrDefault on today's remaining slots can yield a negative span past midnight;
        // wrap to the next day's equivalent slot.
        var until = duration.Value < TimeSpan.Zero
            ? duration.Value + TimeSpan.FromDays(1)
            : duration.Value;

        return (false, (int)Math.Max(0, until.TotalSeconds));
    }
}
