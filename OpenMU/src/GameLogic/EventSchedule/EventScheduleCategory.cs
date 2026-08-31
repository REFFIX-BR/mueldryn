// <copyright file="EventScheduleCategory.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.EventSchedule;

/// <summary>
/// Client H-key window page for a schedule row.
/// </summary>
public enum EventScheduleCategory : byte
{
    /// <summary>
    /// Invasion events (Golden, Red Dragon, White Wizard, …).
    /// </summary>
    Invasion = 0,

    /// <summary>
    /// Mini-games / events (Blood Castle, Devil Square, Chaos Castle, …).
    /// </summary>
    MiniGame = 1,
}
