// <copyright file="SpendStrengthChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Spends free level-up points on Strength: <c>/f</c> or <c>/f N</c>.
/// </summary>
[Guid("03AAF613-43F4-47AD-ABBE-83A1F7250D5D")]
[PlugIn]
[Display(Name = "Força (/f)", Description = "Gasta pontos livres em Força. Uso: /f ou /f N")]
[ChatCommandHelp(Command, CharacterStatus.Normal)]
public class SpendStrengthChatCommandPlugIn : SpendStatPointsChatCommandBase
{
    private const string Command = "/f";

    /// <summary>
    /// Initializes a new instance of the <see cref="SpendStrengthChatCommandPlugIn"/> class.
    /// </summary>
    public SpendStrengthChatCommandPlugIn()
        : base(Stats.BaseStrength, "Força", "Uso: /f ou /f N")
    {
    }

    /// <inheritdoc />
    public override string Key => Command;
}
