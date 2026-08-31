// <copyright file="SpendEnergyChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Spends free level-up points on Energy: <c>/e</c> or <c>/e N</c>.
/// </summary>
[Guid("71CD3D22-431F-4115-AC26-F1165CF22185")]
[PlugIn]
[Display(Name = "Energia (/e)", Description = "Gasta pontos livres em Energia. Uso: /e ou /e N")]
[ChatCommandHelp(Command, CharacterStatus.Normal)]
public class SpendEnergyChatCommandPlugIn : SpendStatPointsChatCommandBase
{
    private const string Command = "/e";

    /// <summary>
    /// Initializes a new instance of the <see cref="SpendEnergyChatCommandPlugIn"/> class.
    /// </summary>
    public SpendEnergyChatCommandPlugIn()
        : base(Stats.BaseEnergy, "Energia", "Uso: /e ou /e N")
    {
    }

    /// <inheritdoc />
    public override string Key => Command;
}
