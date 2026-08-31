// <copyright file="EventScheduleService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.EventSchedule;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Aggregates invasion and mini-game schedule rows for the client H-key window.
/// </summary>
public static class EventScheduleService
{
    private static readonly MiniGameType[] MiniGameTypes =
    [
        MiniGameType.BloodCastle,
        MiniGameType.DevilSquare,
        MiniGameType.ChaosCastle,
    ];

    private static readonly Dictionary<MiniGameType, string> MiniGameNames = new()
    {
        [MiniGameType.BloodCastle] = "Blood Castle",
        [MiniGameType.DevilSquare] = "Devil Square",
        [MiniGameType.ChaosCastle] = "Chaos Castle",
    };

    /// <summary>
    /// Builds the full schedule list for the given game context.
    /// </summary>
    public static async ValueTask<IReadOnlyList<EventScheduleEntry>> BuildAsync(IGameContext gameContext)
    {
        var result = new List<EventScheduleEntry>();

        foreach (var provider in gameContext.PlugInManager
                     .GetActivePlugInsOf<IPeriodicTaskPlugIn>()
                     .OfType<IProvidesEventSchedule>())
        {
            if (provider.TryGetScheduleEntry(gameContext) is { } entry)
            {
                result.Add(entry);
            }
        }

        foreach (var miniGameType in MiniGameTypes)
        {
            var entry = await TryBuildMiniGameEntryAsync(gameContext, miniGameType).ConfigureAwait(false);
            if (entry is not null)
            {
                result.Add(entry);
            }
        }

        return result;
    }

    private static async ValueTask<EventScheduleEntry?> TryBuildMiniGameEntryAsync(IGameContext gameContext, MiniGameType miniGameType)
    {
        var startPlugIn = gameContext.PlugInManager.GetStrategy<MiniGameType, IPeriodicMiniGameStartPlugIn>(miniGameType);
        if (startPlugIn is null)
        {
            return null;
        }

        var definition = gameContext.Configuration.MiniGameDefinitions
            .FirstOrDefault(d => d.Type == miniGameType);
        if (definition is null)
        {
            return null;
        }

        var duration = await startPlugIn.GetDurationUntilNextStartAsync(gameContext, definition).ConfigureAwait(false);
        var computed = EventScheduleTiming.FromDurationUntilStart(duration);
        if (computed is null)
        {
            return null;
        }

        var (isActive, seconds) = computed.Value;
        return new EventScheduleEntry
        {
            Name = MiniGameNames.GetValueOrDefault(miniGameType, miniGameType.ToString()),
            Category = EventScheduleCategory.MiniGame,
            IsActive = isActive,
            SecondsRemaining = seconds,
        };
    }
}
