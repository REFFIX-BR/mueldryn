// <copyright file="IEntryLimitRepository.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Dungeons;

/// <summary>
/// Provides persistence operations for <see cref="EntryLimit"/> records scoped to
/// individual characters in the Fortress of Imperial Dungeon (Requirement 6.4).
/// </summary>
public interface IEntryLimitRepository
{
    /// <summary>
    /// Retrieves the <see cref="EntryLimit"/> for the given <paramref name="character"/>,
    /// or creates and returns a new one if none exists yet.
    /// </summary>
    /// <param name="character">The character whose entry limit should be loaded or initialised.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that resolves to the existing or newly created
    /// <see cref="EntryLimit"/> for <paramref name="character"/>.
    /// </returns>
    ValueTask<EntryLimit> GetOrCreateAsync(Character character);

    /// <summary>
    /// Persists the current state of the given <paramref name="limit"/> for
    /// <paramref name="character"/>.
    /// </summary>
    /// <param name="character">The character to whom the entry limit belongs.</param>
    /// <param name="limit">The entry limit state to persist.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the save operation is done.</returns>
    ValueTask SaveAsync(Character character, EntryLimit limit);
}
