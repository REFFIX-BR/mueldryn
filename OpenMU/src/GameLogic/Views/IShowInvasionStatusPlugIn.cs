// <copyright file="IShowInvasionStatusPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using MUnique.OpenMU.GameLogic.EventSchedule;

/// <summary>
/// Sends live invasion monster counts to the client.
/// </summary>
public interface IShowInvasionStatusPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the invasion dropdown snapshot.
    /// </summary>
    /// <param name="snapshot">Title, timer and monster rows.</param>
    ValueTask ShowInvasionStatusAsync(InvasionStatusSnapshot snapshot);
}
