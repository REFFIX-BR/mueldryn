// <copyright file="ItemPostRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handles C1 FD 00 item sell/post announce requests.
/// </summary>
[PlugIn]
[Display(Name = "Item Post Request", Description = "Handles item sell post announce requests (0xFD).")]
[Guid("D4E5F607-8192-4A3B-1C2D-3E4F50617283")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
internal sealed class ItemPostRequestHandlerPlugIn : IPacketHandlerPlugIn
{
    private static readonly Dictionary<Guid, DateTime> LastAnnounceUtc = new();
    private static readonly object CooldownLock = new();

    /// <inheritdoc />
    public bool IsEncryptionExpected => false;

    /// <inheritdoc />
    public byte Key => ItemPostPackets.Code;

    /// <inheritdoc />
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (player.SelectedCharacter is null || player.Inventory is null || packet.Length < ItemPostPackets.AnnounceRequestLength)
        {
            return;
        }

        var span = packet.Span;
        if (span[0] is not (0xC1 or 0xC3) || span[3] != ItemPostPackets.AnnounceRequestSubCode)
        {
            return;
        }

        var characterId = player.SelectedCharacter.Id;
        lock (CooldownLock)
        {
            if (LastAnnounceUtc.TryGetValue(characterId, out var last) && DateTime.UtcNow - last < TimeSpan.FromSeconds(3))
            {
                return;
            }

            LastAnnounceUtc[characterId] = DateTime.UtcNow;
        }

        var slot = span[4];
        var item = player.Inventory.GetItem(slot);
        if (item?.Definition is null)
        {
            return;
        }

        if (player is not RemotePlayer remotePlayer)
        {
            return;
        }

        var itemData = new byte[remotePlayer.ItemSerializer.NeededSpace];
        var written = remotePlayer.ItemSerializer.SerializeItem(itemData, item);
        var itemPayload = itemData.AsMemory(0, written);

        var seller = player.SelectedCharacter.Name ?? "???";
        var itemName = item.Definition.Name.ToString();
        if (string.IsNullOrWhiteSpace(itemName))
        {
            itemName = "Item";
        }

        var chatMessage = $"{seller}: [SELL] {itemName}";

        await player.GameContext.SendGlobalChatMessageAsync("[POST]", chatMessage, ChatMessageType.Gens).ConfigureAwait(false);

        var players = await player.GameContext.GetPlayersAsync().ConfigureAwait(false);
        foreach (var target in players)
        {
            if (target is not RemotePlayer rp || rp.Connection is not { Connected: true } connection)
            {
                continue;
            }

            await ItemPostPackets.SendAnnounceAsync(connection, seller, itemName, itemPayload).ConfigureAwait(false);
        }
    }
}
