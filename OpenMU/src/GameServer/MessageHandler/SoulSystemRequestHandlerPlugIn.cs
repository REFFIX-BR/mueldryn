// <copyright file="SoulSystemRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.SoulSystem;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handles C1 FE 00/02/04 Soul System requests.
/// </summary>
[PlugIn]
[Display(Name = "Soul System Request", Description = "Handles Soul System requests (0xFE).")]
[Guid("F6A7B8C9-0314-4C5D-2E3F-405162738495")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
internal sealed class SoulSystemRequestHandlerPlugIn : IPacketHandlerPlugIn
{
    /// <inheritdoc />
    public bool IsEncryptionExpected => false;

    /// <inheritdoc />
    public byte Key => SoulSystemPackets.Code;

    /// <inheritdoc />
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (player.SelectedCharacter is null || packet.Length < SoulSystemPackets.StatusRequestLength)
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
            case SoulSystemPackets.StatusRequestSubCode:
            {
                SoulSystemService.ApplyBonuses(player);
                var status = SoulSystemService.BuildStatus(player);
                await player.InvokeViewPlugInAsync<IShowSoulSystemPlugIn>(p => p.ShowSoulSystemStatusAsync(status)).ConfigureAwait(false);
                break;
            }

            case SoulSystemPackets.SetRequestSubCode:
            {
                if (packet.Length < SoulSystemPackets.SetRequestLength)
                {
                    return;
                }

                var result = SoulSystemService.TrySetAllocation(player, span[4], span[5], span[6]);
                var status = SoulSystemService.BuildStatus(player);
                await player.InvokeViewPlugInAsync<IShowSoulSystemPlugIn>(p => p.ShowSoulSystemResultAsync(result, status)).ConfigureAwait(false);
                break;
            }

            case SoulSystemPackets.ResetRequestSubCode:
            {
                var result = SoulSystemService.TryResetAllocations(player);
                var status = SoulSystemService.BuildStatus(player);
                await player.InvokeViewPlugInAsync<IShowSoulSystemPlugIn>(p => p.ShowSoulSystemResultAsync(result, status)).ConfigureAwait(false);
                break;
            }
        }
    }
}
