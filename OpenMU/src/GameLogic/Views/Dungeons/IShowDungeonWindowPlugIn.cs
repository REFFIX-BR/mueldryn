// <copyright file="IShowDungeonWindowPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using MUnique.OpenMU.GameLogic.Dungeons;

/// <summary>
/// Interface for a view plugin that displays the dungeon selection window to the player.
/// </summary>
public interface IShowDungeonWindowPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the dungeon window with the specified payload data.
    /// </summary>
    /// <param name="payload">The dungeon window payload containing difficulty info, requirements, and entry limits.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask ShowDungeonWindowAsync(DungeonWindowPayload payload);
}
