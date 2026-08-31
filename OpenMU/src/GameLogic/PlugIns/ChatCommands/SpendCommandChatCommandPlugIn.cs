// <copyright file="SpendCommandChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Spends free level-up points on Command/Leadership (Dark Lord): <c>/cmd</c> or <c>/cmd N</c>.
/// </summary>
[Guid("2E27F82E-F2C1-45A8-AE12-9637A7816B4D")]
[PlugIn]
[Display(Name = "Comando (/cmd)", Description = "Gasta pontos livres em Comando (Dark Lord). Uso: /cmd ou /cmd N")]
[ChatCommandHelp(Command, CharacterStatus.Normal)]
public class SpendCommandChatCommandPlugIn : SpendStatPointsChatCommandBase
{
    private const string Command = "/cmd";

    /// <summary>
    /// Initializes a new instance of the <see cref="SpendCommandChatCommandPlugIn"/> class.
    /// </summary>
    public SpendCommandChatCommandPlugIn()
        : base(Stats.BaseLeadership, "Comando", "Uso: /cmd ou /cmd N")
    {
    }

    /// <inheritdoc />
    public override string Key => Command;
}
