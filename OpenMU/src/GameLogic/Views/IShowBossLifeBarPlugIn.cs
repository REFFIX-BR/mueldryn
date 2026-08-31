// <copyright file="IShowBossLifeBarPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

/// <summary>
/// Shows the life bar of a boss monster on top of the client screen.
/// </summary>
public interface IShowBossLifeBarPlugIn : IViewPlugIn
{
    /// <summary>
    /// Updates the boss life bar.
    /// </summary>
    /// <param name="bossName">The name shown above the bar.</param>
    /// <param name="healthPercentage">The remaining health, from 0 to 100.</param>
    /// <param name="isAlive">When <c>false</c>, the client hides the bar.</param>
    ValueTask ShowBossLifeBarAsync(string bossName, byte healthPercentage, bool isAlive);
}
