// <copyright file="IShowPeriodItemsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// Sends cash/period item expiration info to the client.
/// </summary>
public interface IShowPeriodItemsPlugIn : IViewPlugIn
{
    /// <summary>
    /// Sends expiration info for a single inventory item.
    /// </summary>
    ValueTask ShowPeriodItemAsync(Item item);

    /// <summary>
    /// Sends expiration info for all period items currently in inventory.
    /// </summary>
    ValueTask ShowAllPeriodItemsAsync();
}
