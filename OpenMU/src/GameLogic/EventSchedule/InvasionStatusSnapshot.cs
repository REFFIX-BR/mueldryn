// <copyright file="InvasionStatusSnapshot.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.EventSchedule;

/// <summary>
/// Full payload for the client invasion dropdown (title, timer, monster rows).
/// </summary>
public sealed class InvasionStatusSnapshot
{
    /// <summary>
    /// Gets the invasion display title (e.g. Golden Invasion).
    /// </summary>
    public string Title { get; init; } = "Invasoes";

    /// <summary>
    /// Gets seconds remaining until the active invasion ends (or until start if preparing).
    /// </summary>
    public int SecondsRemaining { get; init; }

    /// <summary>
    /// Gets the monster alive/total rows.
    /// </summary>
    public IReadOnlyList<InvasionMonsterStatusEntry> Entries { get; init; } = [];
}
