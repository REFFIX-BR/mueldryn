// <copyright file="PartySearchRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PartySearch;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handles C1 F9 00/02/04/06 Party Search requests.
/// </summary>
[PlugIn]
[Display(Name = "Party Search Request", Description = "Handles Party Search requests (0xF9).")]
[Guid("B2C3D4E5-F6A7-4890-BCDE-1234567890FA")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
internal sealed class PartySearchRequestHandlerPlugIn : IPacketHandlerPlugIn
{
    /// <inheritdoc />
    public bool IsEncryptionExpected => false;

    /// <inheritdoc />
    public byte Key => PartySearchPackets.Code;

    /// <inheritdoc />
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (player.SelectedCharacter is null || packet.Length < PartySearchPackets.RequestLength)
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
            case PartySearchPackets.ListRequestSubCode:
                await this.HandleListAsync(player).ConfigureAwait(false);
                break;

            case PartySearchPackets.PublishRequestSubCode:
                await this.HandlePublishAsync(player, packet).ConfigureAwait(false);
                break;

            case PartySearchPackets.CancelRequestSubCode:
            {
                var result = PartySearchService.Cancel(player);
                await player.InvokeViewPlugInAsync<IShowPartySearchPlugIn>(
                    p => p.ShowPartySearchPublishResultAsync(result, false)).ConfigureAwait(false);
                await this.HandleListAsync(player).ConfigureAwait(false);
                break;
            }

            case PartySearchPackets.JoinRequestSubCode:
                await this.HandleJoinAsync(player, packet).ConfigureAwait(false);
                break;
        }
    }

    private async ValueTask HandleListAsync(Player player)
    {
        var entries = PartySearchService.BuildList(player.GameContext);
        var own = PartySearchService.GetOwnListing(player);
        await player.InvokeViewPlugInAsync<IShowPartySearchPlugIn>(
            p => p.ShowPartySearchListAsync(entries, own is not null, own)).ConfigureAwait(false);
    }

    private async ValueTask HandlePublishAsync(Player player, Memory<byte> packet)
    {
        var span = packet.Span;
        if (span.Length < PartySearchPackets.PublishMinLength)
        {
            return;
        }

        var active = span[4] != 0;
        var maxLevel = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(5, 2));
        var flags = (PartySearchFlags)span[7];
        var classMask = span[8];
        var pwdLen = span[9];
        string password = string.Empty;
        if (pwdLen > 0 && span.Length >= 10 + pwdLen)
        {
            var len = Math.Min((int)pwdLen, PartySearchPackets.PasswordMaxLength);
            password = Encoding.UTF8.GetString(span.Slice(10, len));
        }

        var result = PartySearchService.TryPublish(player, active, maxLevel, flags, classMask, password);
        var ownActive = PartySearchService.IsListed(player);
        await player.InvokeViewPlugInAsync<IShowPartySearchPlugIn>(
            p => p.ShowPartySearchPublishResultAsync(result, ownActive)).ConfigureAwait(false);
        await this.HandleListAsync(player).ConfigureAwait(false);
    }

    private async ValueTask HandleJoinAsync(Player player, Memory<byte> packet)
    {
        var span = packet.Span;
        if (span.Length < PartySearchPackets.JoinMinLength)
        {
            return;
        }

        var nameBytes = span.Slice(4, PartySearchPackets.NameLength);
        var zero = nameBytes.IndexOf((byte)0);
        var nameLen = zero >= 0 ? zero : PartySearchPackets.NameLength;
        var leaderName = Encoding.UTF8.GetString(nameBytes[..nameLen]).TrimEnd('\0');

        var pwdLen = span[20];
        string password = string.Empty;
        if (pwdLen > 0 && span.Length >= 21 + pwdLen)
        {
            var len = Math.Min((int)pwdLen, PartySearchPackets.PasswordMaxLength);
            password = Encoding.UTF8.GetString(span.Slice(21, len));
        }

        var result = await PartySearchService.TryJoinAsync(player, leaderName, password).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IShowPartySearchPlugIn>(
            p => p.ShowPartySearchJoinResultAsync(result)).ConfigureAwait(false);
    }
}
