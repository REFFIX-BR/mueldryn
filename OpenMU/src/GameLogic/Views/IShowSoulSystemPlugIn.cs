// <copyright file="IShowSoulSystemPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using MUnique.OpenMU.GameLogic.SoulSystem;

/// <summary>
/// Sends Soul System status / results to the client.
/// </summary>
public interface IShowSoulSystemPlugIn : IViewPlugIn
{
    /// <summary>
    /// Sends the current soul system status.
    /// </summary>
    ValueTask ShowSoulSystemStatusAsync(SoulSystemStatus status);

    /// <summary>
    /// Sends an action result with updated status.
    /// </summary>
    ValueTask ShowSoulSystemResultAsync(SoulSystemResult result, SoulSystemStatus status);
}
