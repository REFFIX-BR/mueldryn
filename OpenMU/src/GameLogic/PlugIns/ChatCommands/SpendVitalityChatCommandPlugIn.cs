// <copyright file="SpendVitalityChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Spends free level-up points on Vitality: <c>/v</c> or <c>/v N</c>.
/// </summary>
[Guid("1D6ABD8E-EA7D-4091-B760-78BFB85BD14A")]
[PlugIn]
[Display(Name = "Vitalidade (/v)", Description = "Gasta pontos livres em Vitalidade. Uso: /v ou /v N")]
[ChatCommandHelp(Command, CharacterStatus.Normal)]
public class SpendVitalityChatCommandPlugIn : SpendStatPointsChatCommandBase
{
    private const string Command = "/v";

    /// <summary>
    /// Initializes a new instance of the <see cref="SpendVitalityChatCommandPlugIn"/> class.
    /// </summary>
    public SpendVitalityChatCommandPlugIn()
        : base(Stats.BaseVitality, "Vitalidade", "Uso: /v ou /v N")
    {
    }

    /// <inheritdoc />
    public override string Key => Command;
}
