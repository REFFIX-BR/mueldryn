// <copyright file="IShowPartySearchPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using MUnique.OpenMU.GameLogic.PartySearch;

/// <summary>
/// Sends Party Search data to the client (code 0xF9).
/// </summary>
public interface IShowPartySearchPlugIn : IViewPlugIn
{
    /// <summary>
    /// Sends the current party search list.
    /// </summary>
    ValueTask ShowPartySearchListAsync(IReadOnlyList<PartySearchListEntry> entries, bool ownActive, PartySearchListing? ownListing);

    /// <summary>
    /// Sends a publish/cancel result.
    /// </summary>
    ValueTask ShowPartySearchPublishResultAsync(PartySearchResult result, bool ownActive);

    /// <summary>
    /// Sends a join result.
    /// </summary>
    ValueTask ShowPartySearchJoinResultAsync(PartySearchResult result);
}
