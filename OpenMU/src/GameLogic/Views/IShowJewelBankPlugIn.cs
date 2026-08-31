// <copyright file="IShowJewelBankPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using MUnique.OpenMU.GameLogic.JewelBank;

/// <summary>
/// Sends jewel bank status / action results to the client.
/// </summary>
public interface IShowJewelBankPlugIn : IViewPlugIn
{
    /// <summary>
    /// Sends the current jewel bank status.
    /// </summary>
    ValueTask ShowJewelBankStatusAsync(JewelBankStatus status);

    /// <summary>
    /// Sends a deposit/withdraw result with updated status.
    /// </summary>
    ValueTask ShowJewelBankResultAsync(JewelBankResult result, JewelBankStatus status);
}
