// <copyright file="InvasionMonsterStatusEntry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.EventSchedule;

/// <summary>
/// One monster row for the invasion status panel (alive/total).
/// </summary>
public sealed class InvasionMonsterStatusEntry
{
    /// <summary>
    /// Gets the invasion display name (e.g. Golden Invasion).
    /// </summary>
    public required string InvasionName { get; init; }

    /// <summary>
    /// Gets the monster display name.
    /// </summary>
    public required string MonsterName { get; init; }

    /// <summary>
    /// Gets how many of this monster are still alive.
    /// </summary>
    public int Alive { get; init; }

    /// <summary>
    /// Gets how many of this monster were spawned for the run.
    /// </summary>
    public int Total { get; init; }
}
