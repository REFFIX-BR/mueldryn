// <copyright file="PartySearchListing.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartySearch;

/// <summary>
/// Stored party search listing (leader publishes looking-for-members).
/// </summary>
public sealed class PartySearchListing
{
    public required string LeaderName { get; init; }

    public ushort MaxLevel { get; set; } = 400;

    public PartySearchFlags Flags { get; set; }

    /// <summary>
    /// Bitmask of allowed CharacterClass.Number values (bit N = class number N).
    /// 0xFF = all classes.
    /// </summary>
    public byte ClassMask { get; set; } = 0xFF;

    public string Password { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Snapshot row sent to clients.
/// </summary>
public sealed class PartySearchListEntry
{
    public required string LeaderName { get; init; }

    public required string MapName { get; init; }

    public ushort MapNumber { get; init; }

    public byte X { get; init; }

    public byte Y { get; init; }

    public byte Count { get; init; }

    public byte MaxCount { get; init; }

    public ushort MaxLevel { get; init; }

    public PartySearchFlags Flags { get; init; }

    public byte ClassMask { get; init; }

    public ushort LeaderLevel { get; init; }
}
