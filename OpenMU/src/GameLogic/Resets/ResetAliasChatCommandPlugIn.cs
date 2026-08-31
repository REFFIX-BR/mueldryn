// <copyright file="ResetAliasChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Brazilian private-server alias for <see cref="ResetChatCommandPlugIn"/> (<c>/re</c>).
/// Kept as a separate plug-in class so it does not conflict with other chat-command edits.
/// </summary>
[Guid("DB5F7327-C451-4B9B-BB3F-2EAB27E30374")]
[PlugIn]
[Display(Name = "Reset alias /re", Description = "Atalho /re para o comando de reset de personagem.")]
[ChatCommandHelp(Command, "Faz o reset do personagem, se disponivel.", null)]
public class ResetAliasChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/re";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        var resetAction = new ResetCharacterAction(player);
        await resetAction.ResetCharacterAsync().ConfigureAwait(false);
    }
}
