// <copyright file="PartySearchResult.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartySearch;

/// <summary>
/// Result codes for publish / cancel / join (sent to client).
/// </summary>
public enum PartySearchResult : byte
{
    Success = 0,
    Failed = 1,
    AlreadyInParty = 2,
    WrongPassword = 3,
    WrongGuild = 4,
    LevelTooHigh = 5,
    ClassNotAllowed = 6,
    PartyFull = 7,
    LeaderOffline = 8,
    NotListed = 9,
    CannotAddSelf = 10,
    InvalidRequest = 11,
}
