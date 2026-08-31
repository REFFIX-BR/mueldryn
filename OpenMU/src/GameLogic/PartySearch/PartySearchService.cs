// <copyright file="PartySearchService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartySearch;

using System.Collections.Concurrent;
using MUnique.OpenMU.GameLogic.PlayerActions.Party;

/// <summary>
/// In-memory party finder registry (Mudream-style Party Search).
/// </summary>
public static class PartySearchService
{
    private static readonly ConcurrentDictionary<string, PartySearchListing> Listings =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly PartyRequestAction PartyRequest = new();

    /// <summary>
    /// Builds the live list of online published parties.
    /// </summary>
    public static IReadOnlyList<PartySearchListEntry> BuildList(IGameContext context)
    {
        var result = new List<PartySearchListEntry>(Listings.Count);
        foreach (var pair in Listings)
        {
            var leader = context.GetPlayerByCharacterName(pair.Key);
            if (leader?.SelectedCharacter is null || leader.CurrentMap is null)
            {
                Listings.TryRemove(pair.Key, out _);
                continue;
            }

            var listing = pair.Value;
            var count = (byte)(leader.Party?.PartyList.Count ?? 1);
            var maxCount = (byte)Math.Clamp((int)context.Configuration.MaximumPartySize, 2, 10);
            result.Add(new PartySearchListEntry
            {
                LeaderName = leader.SelectedCharacter.Name,
                MapName = (string?)leader.CurrentMap.Definition.Name ?? $"Map {leader.CurrentMap.MapId}",
                MapNumber = (ushort)leader.CurrentMap.MapId,
                X = leader.Position.X,
                Y = leader.Position.Y,
                Count = count,
                MaxCount = maxCount,
                MaxLevel = listing.MaxLevel,
                Flags = listing.Flags,
                ClassMask = listing.ClassMask,
                LeaderLevel = (ushort)Math.Clamp(leader.Level, 0, ushort.MaxValue),
            });
        }

        result.Sort((a, b) => string.Compare(a.LeaderName, b.LeaderName, StringComparison.OrdinalIgnoreCase));
        return result;
    }

    /// <summary>
    /// Publishes or updates the player's listing. Pass active=false to cancel.
    /// </summary>
    public static PartySearchResult TryPublish(
        Player player,
        bool active,
        ushort maxLevel,
        PartySearchFlags flags,
        byte classMask,
        string? password)
    {
        if (player.SelectedCharacter is null)
        {
            return PartySearchResult.Failed;
        }

        var name = player.SelectedCharacter.Name;
        if (!active)
        {
            Listings.TryRemove(name, out _);
            return PartySearchResult.Success;
        }

        if (player.Party is { } party && !Equals(party.PartyMaster, player))
        {
            return PartySearchResult.AlreadyInParty;
        }

        var pwd = password?.Trim() ?? string.Empty;
        if (pwd.Length > 10)
        {
            pwd = pwd[..10];
        }

        if (!string.IsNullOrEmpty(pwd))
        {
            flags |= PartySearchFlags.HasPassword;
        }
        else
        {
            flags &= ~PartySearchFlags.HasPassword;
        }

        if (classMask == 0)
        {
            classMask = 0xFF;
        }

        if (maxLevel == 0)
        {
            maxLevel = 400;
        }

        Listings[name] = new PartySearchListing
        {
            LeaderName = name,
            MaxLevel = maxLevel,
            Flags = flags,
            ClassMask = classMask,
            Password = pwd,
            PublishedAt = DateTime.UtcNow,
        };
        return PartySearchResult.Success;
    }

    /// <summary>
    /// Cancels the player's listing if present.
    /// </summary>
    public static PartySearchResult Cancel(Player player)
    {
        if (player.SelectedCharacter is null)
        {
            return PartySearchResult.Failed;
        }

        Listings.TryRemove(player.SelectedCharacter.Name, out _);
        return PartySearchResult.Success;
    }

    /// <summary>
    /// Whether the player currently has an active listing.
    /// </summary>
    public static bool IsListed(Player player)
        => player.SelectedCharacter is not null && Listings.ContainsKey(player.SelectedCharacter.Name);

    /// <summary>
    /// Returns the player's listing settings if published.
    /// </summary>
    public static PartySearchListing? GetOwnListing(Player player)
    {
        if (player.SelectedCharacter is null)
        {
            return null;
        }

        return Listings.TryGetValue(player.SelectedCharacter.Name, out var listing) ? listing : null;
    }

    /// <summary>
    /// Joins a published party (server-side invite from leader to joiner).
    /// </summary>
    public static async ValueTask<PartySearchResult> TryJoinAsync(Player joiner, string leaderName, string? password)
    {
        if (joiner.SelectedCharacter is null || string.IsNullOrWhiteSpace(leaderName))
        {
            return PartySearchResult.InvalidRequest;
        }

        if (string.Equals(joiner.SelectedCharacter.Name, leaderName, StringComparison.OrdinalIgnoreCase))
        {
            return PartySearchResult.CannotAddSelf;
        }

        if (joiner.Party is not null)
        {
            return PartySearchResult.AlreadyInParty;
        }

        if (!Listings.TryGetValue(leaderName, out var listing))
        {
            return PartySearchResult.NotListed;
        }

        var leader = joiner.GameContext.GetPlayerByCharacterName(leaderName);
        if (leader?.SelectedCharacter is null)
        {
            Listings.TryRemove(leaderName, out _);
            return PartySearchResult.LeaderOffline;
        }

        if ((listing.Flags & PartySearchFlags.HasPassword) != 0)
        {
            var pwd = password?.Trim() ?? string.Empty;
            if (!string.Equals(pwd, listing.Password, StringComparison.Ordinal))
            {
                return PartySearchResult.WrongPassword;
            }
        }

        if (joiner.Level > listing.MaxLevel)
        {
            return PartySearchResult.LevelTooHigh;
        }

        var classNumber = joiner.SelectedCharacter.CharacterClass?.Number ?? 0;
        if (listing.ClassMask != 0xFF && (listing.ClassMask & (1 << (classNumber & 7))) == 0)
        {
            return PartySearchResult.ClassNotAllowed;
        }

        if ((listing.Flags & PartySearchFlags.OnlyOneClass) != 0
            && leader.SelectedCharacter.CharacterClass?.Number is { } leaderClass
            && classNumber != leaderClass)
        {
            return PartySearchResult.ClassNotAllowed;
        }

        if ((listing.Flags & (PartySearchFlags.OnlyGuild | PartySearchFlags.OnlyAlliance)) != 0)
        {
            var leaderGuild = leader.GuildStatus?.GuildId;
            var joinerGuild = joiner.GuildStatus?.GuildId;
            if (leaderGuild is null or 0 || joinerGuild is null or 0 || leaderGuild != joinerGuild)
            {
                return PartySearchResult.WrongGuild;
            }
        }

        var maxSize = joiner.GameContext.Configuration.MaximumPartySize;
        if (leader.Party is { } existing && existing.PartyList.Count >= maxSize)
        {
            return PartySearchResult.PartyFull;
        }

        if (leader.Party is { } party && !Equals(party.PartyMaster, leader))
        {
            return PartySearchResult.Failed;
        }

        await PartyRequest.HandlePartyRequestAsync(leader, joiner).ConfigureAwait(false);
        return PartySearchResult.Success;
    }
}
