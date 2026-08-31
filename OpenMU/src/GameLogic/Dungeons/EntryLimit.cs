// <copyright file="EntryLimit.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Dungeons;

using System.Threading;

/// <summary>
/// Tracks the daily entry limit for a character in the Fortress of Imperial Dungeon.
/// Each character is allowed up to <see cref="MaxDailyEntries"/> entries per UTC day.
/// </summary>
/// <remarks>
/// Thread-safety is ensured via a <see cref="SemaphoreSlim"/> so that concurrent entry
/// requests for the same character are serialised and only the correct number of
/// requests succeed (Requirements 6.1, 6.2, 6.3, 6.5).
/// </remarks>
public sealed class EntryLimit
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Gets the UTC date on which the entry counter was last reset.
    /// </summary>
    public DateOnly LastResetDate { get; private set; }

    /// <summary>
    /// Gets the number of dungeon entries already consumed on <see cref="LastResetDate"/>.
    /// </summary>
    public int EntriesConsumed { get; private set; }

    /// <summary>
    /// Restores an <see cref="EntryLimit"/> from persisted character attributes.
    /// </summary>
    public static EntryLimit FromPersisted(DateOnly lastResetDate, int entriesConsumed)
    {
        return new EntryLimit
        {
            LastResetDate = lastResetDate,
            EntriesConsumed = Math.Clamp(entriesConsumed, 0, DungeonKeyItems.MaxDailyEntries),
        };
    }

    /// <summary>
    /// Gets the maximum number of dungeon entries allowed per UTC day.
    /// </summary>
    public int MaxDailyEntries => DungeonKeyItems.MaxDailyEntries;

    /// <summary>
    /// Returns the number of entries still available for the current UTC day.
    /// </summary>
    /// <remarks>
    /// If the current UTC date differs from <see cref="LastResetDate"/>, the counter is
    /// reset to zero and <see cref="LastResetDate"/> is updated before computing the
    /// remaining entries (Requirement 6.3).
    /// </remarks>
    /// <returns>The number of remaining entries (0–<see cref="MaxDailyEntries"/>).</returns>
    public async ValueTask<int> GetAvailableEntriesAsync()
    {
        await this._lock.WaitAsync().ConfigureAwait(false);
        try
        {
            this.ResetIfNewDay();
            return this.MaxDailyEntries - this.EntriesConsumed;
        }
        finally
        {
            this._lock.Release();
        }
    }

    /// <summary>
    /// Attempts to consume one entry for the current UTC day.
    /// </summary>
    /// <remarks>
    /// The <see cref="SemaphoreSlim"/> guarantees that at most <see cref="MaxDailyEntries"/>
    /// out of any number of concurrent calls succeed (Requirement 6.5).
    /// </remarks>
    /// <returns>
    /// <c>true</c> if an entry was successfully consumed; <c>false</c> if the daily limit
    /// has already been reached.
    /// </returns>
    public async ValueTask<bool> TryConsumeEntryAsync()
    {
        await this._lock.WaitAsync().ConfigureAwait(false);
        try
        {
            this.ResetIfNewDay();

            if (this.EntriesConsumed >= this.MaxDailyEntries)
            {
                return false;
            }

            this.EntriesConsumed++;
            return true;
        }
        finally
        {
            this._lock.Release();
        }
    }

    /// <summary>
    /// Resets the entry counter when the UTC date has changed since the last reset.
    /// Must be called while holding <see cref="_lock"/>.
    /// </summary>
    private void ResetIfNewDay()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (today != this.LastResetDate)
        {
            this.EntriesConsumed = 0;
            this.LastResetDate = today;
        }
    }
}
