// <copyright file="EventScheduleRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Dungeons;
using MUnique.OpenMU.GameLogic.EventSchedule;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handles C1 FA 00 (schedule) and C1 FA 02 (invasion monster status).
/// </summary>
[PlugIn]
[Display(Name = "Event Schedule Request", Description = "Handles schedule and invasion status requests (0xFA).")]
[Guid("C8F2A01D-4B7E-4D9A-8C3F-1E5A9B0D6F22")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
internal sealed class EventScheduleRequestHandlerPlugIn : IPacketHandlerPlugIn
{
    /// <inheritdoc />
    public bool IsEncryptionExpected => false;

    /// <inheritdoc />
    public byte Key => EventSchedulePackets.Code;

    /// <inheritdoc />
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (player.SelectedCharacter is null
            || packet.Length < EventSchedulePackets.RequestLength)
        {
            return;
        }

        var span = packet.Span;
        if (span[0] is not (0xC1 or 0xC3))
        {
            return;
        }

        switch (span[3])
        {
            case EventSchedulePackets.RequestSubCode:
            {
                var entries = await EventScheduleService.BuildAsync(player.GameContext).ConfigureAwait(false);
                await player.InvokeViewPlugInAsync<IShowEventSchedulePlugIn>(p => p.ShowEventScheduleAsync(entries)).ConfigureAwait(false);
                break;
            }

            case EventSchedulePackets.InvasionStatusRequestSubCode:
            {
                var snapshot = InvasionStatusService.Build(player.GameContext);
                await player.InvokeViewPlugInAsync<IShowInvasionStatusPlugIn>(p => p.ShowInvasionStatusAsync(snapshot)).ConfigureAwait(false);
                break;
            }

            case EventSchedulePackets.PlayerEquipmentRequestSubCode:
            {
                if (packet.Length < EventSchedulePackets.PlayerEquipmentRequestLength)
                {
                    break;
                }

                var targetId = (ushort)((span[4] << 8) | span[5]);
                var target = await player.GetObservingPlayerWithIdAsync(targetId).ConfigureAwait(false);
                if (target is null)
                {
                    break;
                }

                await player.InvokeViewPlugInAsync<IShowPlayerEquipmentPlugIn>(p => p.ShowPlayerEquipmentAsync(target)).ConfigureAwait(false);
                break;
            }

            case EventSchedulePackets.DungeonWindowRequestSubCode:
            {
                await this.HandleDungeonWindowRequestAsync(player).ConfigureAwait(false);
                break;
            }

            case EventSchedulePackets.DungeonEnterRequestSubCode:
            {
                if (packet.Length < 6)
                {
                    break;
                }

                await this.HandleDungeonEnterRequestAsync(player, packet).ConfigureAwait(false);
                break;
            }

            case EventSchedulePackets.DungeonLeaveRequestSubCode:
            {
                await DungeonEntryService.TryLeaveAsync(player).ConfigureAwait(false);
                break;
            }
        }
    }

    /// <summary>
    /// Handles the dungeon window open request (0x10).
    /// Fetches the player's entry limit, builds the dungeon window payload, and sends it to the client.
    /// </summary>
    /// <param name="player">The player requesting the dungeon window.</param>
    private async ValueTask HandleDungeonWindowRequestAsync(Player player)
    {
        await DungeonPanelService.ShowWindowAsync(player).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles the dungeon enter request (0x12).
    /// Deserializes the dungeon ID and difficulty, then forwards to DungeonEntryService.
    /// </summary>
    /// <param name="player">The player requesting entry.</param>
    /// <param name="packet">The packet data.</param>
    private async ValueTask HandleDungeonEnterRequestAsync(Player player, Memory<byte> packet)
    {
        if (player.SelectedCharacter is null || player is not RemoteView.RemotePlayer remotePlayer)
        {
            return;
        }

        if (remotePlayer.Connection is not { Connected: true } connection)
        {
            return;
        }

        var span = packet.Span;
        var difficultyValue = span[5];
        var difficulty = Enum.IsDefined(typeof(DungeonDifficulty), difficultyValue)
            ? (DungeonDifficulty)difficultyValue
            : DungeonDifficulty.Normal;

        var result = await DungeonEntryService.TryEnterAsync(player, difficulty).ConfigureAwait(false);
        var message = result switch
        {
            EntryResult.Success => "Entrando na Imperial Fortress.",
            EntryResult.LevelTooLow => "Nível insuficiente para esta dificuldade.",
            EntryResult.InsufficientResets => "Resets insuficientes para esta dificuldade.",
            EntryResult.PlayerKillerNotAllowed => "Player Killers não podem entrar nesta dungeon.",
            EntryResult.InventoryFull => "É necessário pelo menos 1 slot livre no inventário.",
            EntryResult.DailyLimitReached => "Limite diário de entradas atingido.",
            EntryResult.AlreadyRunning => "Você já está em uma dungeon ou evento.",
            EntryResult.InstanceFull => "A instância da dungeon está cheia.",
            EntryResult.NotPartyLeader => "Só o líder da party pode iniciar a dungeon.",
            EntryResult.MissingRequiredItem => "Um jogador da party não está com o item no inventário.",
            EntryResult.PartyMemberCannotEnter => "Um membro da party não pode entrar nesta dungeon.",
            _ => "Não foi possível entrar na dungeon.",
        };

        await EventSchedulePackets.SendDungeonEnterResultAsync(connection, (byte)result, message).ConfigureAwait(false);
    }
}
