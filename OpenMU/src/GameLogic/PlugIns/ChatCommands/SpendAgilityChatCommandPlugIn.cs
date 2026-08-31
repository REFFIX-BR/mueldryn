// <copyright file="SpendAgilityChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Spends free level-up points on Agility: <c>/a</c> or <c>/a N</c>.
/// </summary>
[Guid("B084C868-A349-43AA-88C1-04B591D2EE2B")]
[PlugIn]
[Display(Name = "Agilidade (/a)", Description = "Gasta pontos livres em Agilidade. Uso: /a ou /a N")]
[ChatCommandHelp(Command, CharacterStatus.Normal)]
public class SpendAgilityChatCommandPlugIn : SpendStatPointsChatCommandBase
{
    private const string Command = "/a";

    /// <summary>
    /// Initializes a new instance of the <see cref="SpendAgilityChatCommandPlugIn"/> class.
    /// </summary>
    public SpendAgilityChatCommandPlugIn()
        : base(Stats.BaseAgility, "Agilidade", "Uso: /a ou /a N")
    {
    }

    /// <inheritdoc />
    public override string Key => Command;
}
