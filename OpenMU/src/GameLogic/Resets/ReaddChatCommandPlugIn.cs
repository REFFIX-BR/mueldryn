// <copyright file="ReaddChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Brazilian private-server alias for <see cref="ResetStatsChatCommandPlugIn"/> (<c>/readd</c>).
/// Resets invested stats and refunds points; cost comes from <see cref="StatResetConfiguration.RequiredMoney"/>.
/// </summary>
[Guid("7C2E91A4-5B8D-4F01-9E36-A4D8C1B0F572")]
[PlugIn]
[Display(Name = "Readd stats /readd", Description = "Redistribui pontos de status (custa zen configurado no Stat Reset).")]
[ChatCommandHelp(Command, "Reseta os atributos e devolve os pontos para redistribuir (custa 5kk).", null)]
public class ReaddChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/readd";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        var statResetFeature = player.GameContext.FeaturePlugIns.GetPlugIn<StatResetFeaturePlugIn>();
        if (statResetFeature?.Configuration is { } configuration && !configuration.ChatCommandEnabled)
        {
            await player.ShowLocalizedBlueMessageAsync(PlayerMessage.StatResetChatCommandDisabled).ConfigureAwait(false);
            return;
        }

        var resetAction = new ResetStatsAction(player);
        await resetAction.ResetStatsAsync().ConfigureAwait(false);
    }
}
