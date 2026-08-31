// <copyright file="PartySearchFlags.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartySearch;

/// <summary>
/// Listing restriction flags (Mudream Party Search Settings).
/// </summary>
[Flags]
public enum PartySearchFlags : byte
{
    None = 0,
    HasPassword = 0x01,
    OnlyGuild = 0x02,
    OnlyAlliance = 0x04,
    OnlyOneClass = 0x08,
}
