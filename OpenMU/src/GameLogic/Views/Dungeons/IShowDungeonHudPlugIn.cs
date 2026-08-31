// <copyright file="IShowDungeonHudPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using MUnique.OpenMU.GameLogic.Dungeons;

/// <summary>
/// Sends the in-instance dungeon HUD update (packet 0xFA/0x14).
/// </summary>
public interface IShowDungeonHudPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the HUD update on the client.
    /// </summary>
    ValueTask ShowDungeonHudUpdateAsync(DungeonHudUpdate update);
}
