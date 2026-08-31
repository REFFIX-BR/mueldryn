// <copyright file="DungeonEntryService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Dungeons;

using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.GameLogic.PlayerActions.MiniGames;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Interfaces;
using MiniGameEnterResult = MUnique.OpenMU.GameLogic.PlayerActions.MiniGames.EnterResult;

/// <summary>
/// Validates entry requirements and warps players into the Fortress of Imperial Dungeon.
/// </summary>
public static class DungeonEntryService
{
    /// <summary>
    /// Attempts to validate all entry requirements and warp the player (and party) into the specified dungeon difficulty.
    /// </summary>
    public static async ValueTask<EntryResult> TryEnterAsync(Player player, DungeonDifficulty difficulty)
    {
        if (player.SelectedCharacter is not { } || player.Attributes is null)
        {
            return player.SelectedCharacter is null ? EntryResult.CharacterNotSelected : EntryResult.AttributesNotInitialized;
        }

        var dungeonDefinition = DungeonPanelService.FindDefinition(player, difficulty);
        if (dungeonDefinition is null)
        {
            await SendEntryFailedMessageAsync(player, "Dungeon configuration was not found.").ConfigureAwait(false);
            return EntryResult.DefinitionNotFound;
        }

        if (player.Party is { } party && !Equals(party.PartyMaster, player))
        {
            await SendEntryFailedMessageAsync(player, "Só o líder da party pode iniciar a dungeon.").ConfigureAwait(false);
            return EntryResult.NotPartyLeader;
        }

        var participants = GetParticipants(player);
        if (participants.Count > dungeonDefinition.MaximumPlayerCount)
        {
            await SendEntryFailedMessageAsync(player, "A dungeon aceita no máximo 5 jogadores.").ConfigureAwait(false);
            return EntryResult.InstanceFull;
        }

        var keyName = DungeonTicketItem.GetDisplayName(difficulty);
        var missingTicket = participants.Where(member => !DungeonTicketItem.HasTicket(member, difficulty)).ToList();
        if (missingTicket.Count > 0)
        {
            var names = string.Join(", ", missingTicket.Select(member => member.Name));
            var message = missingTicket.Count == 1
                ? $"{names} precisa da {keyName} +{DungeonKeyItems.KeyLevel}."
                : $"Os jogadores {names} precisam da {keyName} +{DungeonKeyItems.KeyLevel}.";
            await NotifyPartyAsync(player, missingTicket, message).ConfigureAwait(false);
            return EntryResult.MissingRequiredItem;
        }

        foreach (var member in participants)
        {
            var (result, message) = await ValidateMemberAsync(member, dungeonDefinition, difficulty).ConfigureAwait(false);
            if (result != EntryResult.Success)
            {
                await NotifyPartyAsync(player, [member], message).ConfigureAwait(false);
                return result;
            }
        }

        var miniGame = await player.GameContext.GetMiniGameAsync(dungeonDefinition, player).ConfigureAwait(false);
        if (miniGame is not DungeonContext context)
        {
            await SendEntryFailedMessageAsync(player, "Could not create the dungeon instance.").ConfigureAwait(false);
            return EntryResult.DefinitionNotFound;
        }

        if (context.PlayerCount + participants.Count > context.Definition.MaximumPlayerCount)
        {
            await SendEntryFailedMessageAsync(player, "The dungeon instance is full. Please try again later.").ConfigureAwait(false);
            return EntryResult.InstanceFull;
        }

        var entered = new List<Player>(participants.Count);
        foreach (var member in participants)
        {
            var enterResult = await context.TryEnterAsync(member).ConfigureAwait(false);
            if (enterResult != MiniGameEnterResult.Success)
            {
                var mapped = enterResult switch
                {
                    MiniGameEnterResult.Full => EntryResult.InstanceFull,
                    MiniGameEnterResult.NotOpen => EntryResult.AlreadyRunning,
                    _ => EntryResult.AlreadyRunning,
                };
                await SendEntryFailedMessageAsync(player, $"{member.Name} não conseguiu entrar na dungeon.").ConfigureAwait(false);
                return mapped;
            }

            entered.Add(member);
        }

        foreach (var member in entered)
        {
            var repository = new CharacterAttributeEntryLimitRepository(member);
            if (member.SelectedCharacter is { } character)
            {
                var entryLimit = await repository.GetOrCreateAsync(character).ConfigureAwait(false);
                if (await entryLimit.TryConsumeEntryAsync().ConfigureAwait(false))
                {
                    await repository.SaveAsync(character, entryLimit).ConfigureAwait(false);
                }
            }

            await DungeonTicketItem.ConsumeKeyAsync(member, difficulty).ConfigureAwait(false);
            await member.WarpToAsync(CreateDungeonEntryGate(context)).ConfigureAwait(false);
        }

        return EntryResult.Success;
    }

    /// <summary>
    /// Leaves the dungeon and warps the player to Lorencia.
    /// Remaining party members stay in the instance.
    /// </summary>
    public static async ValueTask TryLeaveAsync(Player player)
    {
        if (player.CurrentMiniGame is DungeonContext dungeon)
        {
            await dungeon.TryLeaveAsync(player).ConfigureAwait(false);
        }
    }

    private static IReadOnlyList<Player> GetParticipants(Player requester)
    {
        if (requester.Party is not { } party)
        {
            return [requester];
        }

        return party.PartyList
            .OfType<Player>()
            .Where(member => member.IsConnected && member.SelectedCharacter is not null)
            .ToList();
    }

    private static async ValueTask<(EntryResult Result, string Message)> ValidateMemberAsync(
        Player member,
        MiniGameDefinition dungeonDefinition,
        DungeonDifficulty difficulty)
    {
        if (member.SelectedCharacter is not { } character)
        {
            return (EntryResult.CharacterNotSelected, $"{member.Name} não tem um personagem selecionado.");
        }

        if (member.Attributes is not { } attributes)
        {
            return (EntryResult.AttributesNotInitialized, $"{member.Name} ainda não está pronto para entrar.");
        }

        var currentLevel = (int)attributes[Stats.Level];
        var minimumLevel = dungeonDefinition.MinimumCharacterLevel > 0
            ? dungeonDefinition.MinimumCharacterLevel
            : DungeonPanelService.GetMinimumLevel(difficulty);
        if (currentLevel < minimumLevel)
        {
            return (EntryResult.LevelTooLow, $"{member.Name} precisa ser pelo menos nível {minimumLevel} para esta dungeon.");
        }

        var currentResets = (int)attributes[Stats.Resets];
        var minimumResets = DungeonPanelService.GetMinimumResets(difficulty);
        if (currentResets < minimumResets)
        {
            return (EntryResult.InsufficientResets, $"{member.Name} precisa de pelo menos {minimumResets} resets para esta dungeon.");
        }

        if (character.State >= HeroState.PlayerKiller1stStage)
        {
            return (EntryResult.PlayerKillerNotAllowed, $"{member.Name} está PK e não pode entrar nesta dungeon.");
        }

        if (!HasAtLeastOneFreeInventorySlot(member))
        {
            return (EntryResult.InventoryFull, $"{member.Name} precisa de pelo menos 1 slot livre no inventário.");
        }

        var repository = new CharacterAttributeEntryLimitRepository(member);
        var entryLimit = await repository.GetOrCreateAsync(character).ConfigureAwait(false);
        var availableEntries = await entryLimit.GetAvailableEntriesAsync().ConfigureAwait(false);
        if (availableEntries <= 0)
        {
            return (EntryResult.DailyLimitReached, $"{member.Name} já usou o limite diário de entradas.");
        }

        if (member.CurrentMiniGame is not null)
        {
            return (EntryResult.AlreadyRunning, $"{member.Name} já está em uma dungeon ou evento.");
        }

        return (EntryResult.Success, string.Empty);
    }

    private static bool HasAtLeastOneFreeInventorySlot(Player player)
        => player.Inventory?.FreeSlots.Any() == true;

    private static async ValueTask NotifyPartyAsync(Player requester, IEnumerable<Player> members, string message)
    {
        await SendEntryFailedMessageAsync(requester, message).ConfigureAwait(false);
        foreach (var member in members)
        {
            if (!Equals(member, requester))
            {
                await SendEntryFailedMessageAsync(member, message).ConfigureAwait(false);
            }
        }
    }

    private static async ValueTask SendEntryFailedMessageAsync(Player player, string message)
    {
        await player.InvokeViewPlugInAsync<IShowMessagePlugIn>(
            p => p.ShowMessageAsync(message, MessageType.BlueNormal)).ConfigureAwait(false);
    }

    private static ExitGate CreateDungeonEntryGate(DungeonContext context)
    {
        return new ExitGate
        {
            Map = context.Map.Definition,
            X1 = DungeonWaveCatalog.ArenaX,
            X2 = DungeonWaveCatalog.ArenaX,
            Y1 = DungeonWaveCatalog.ArenaY,
            Y2 = DungeonWaveCatalog.ArenaY,
            Direction = Direction.South,
        };
    }
}

/// <summary>
/// Represents the result of a dungeon entry attempt.
/// </summary>
public enum EntryResult : byte
{
    /// <summary>Entry was successful and the player has been warped.</summary>
    Success = 0,

    /// <summary>The player's level is below the minimum required.</summary>
    LevelTooLow = 1,

    /// <summary>The player's reset count is below the minimum required.</summary>
    InsufficientResets = 2,

    /// <summary>The player has PK status and is not allowed to enter.</summary>
    PlayerKillerNotAllowed = 3,

    /// <summary>The player does not have at least 1 free inventory slot.</summary>
    InventoryFull = 4,

    /// <summary>The player has consumed all 2 daily entries.</summary>
    DailyLimitReached = 5,

    /// <summary>The player is already in an active dungeon run or minigame.</summary>
    AlreadyRunning = 6,

    /// <summary>The dungeon instance has reached maximum capacity.</summary>
    InstanceFull = 7,

    /// <summary>Internal error: player has no selected character.</summary>
    CharacterNotSelected = 8,

    /// <summary>Internal error: player's attributes are not initialized.</summary>
    AttributesNotInitialized = 9,

    /// <summary>Internal error: dungeon definition not found.</summary>
    DefinitionNotFound = 10,

    /// <summary>A party member tried to start the dungeon instead of the leader.</summary>
    NotPartyLeader = 11,

    /// <summary>A participant is missing the required dungeon ticket.</summary>
    MissingRequiredItem = 12,

    /// <summary>A party member failed a requirement (level, resets, inventory, daily limit, etc.).</summary>
    PartyMemberCannotEnter = 13,
}
