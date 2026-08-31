// <copyright file="IShowPlayerEquipmentPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

/// <summary>
/// Shows the equipment of another player in the player detail window.
/// </summary>
public interface IShowPlayerEquipmentPlugIn : IViewPlugIn
{
    /// <summary>
    /// Sends the equipped items of the target player.
    /// </summary>
    /// <param name="target">The observed player whose equipment should be shown.</param>
    ValueTask ShowPlayerEquipmentAsync(Player target);
}
