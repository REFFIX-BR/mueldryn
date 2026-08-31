// <copyright file="DungeonChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Dungeons;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Opens the Fortress of Imperial dungeon window.
/// </summary>
[Guid("9C8B7A65-4321-4FED-90AB-CDEF12345678")]
[PlugIn]
[Display(Name = "Dungeon", Description = "Opens the Fortress of Imperial Guardian dungeon window.")]
[ChatCommandHelp(CommandKey, CharacterStatus.Normal)]
public class DungeonChatCommandPlugIn : IChatCommandPlugIn
{
    private const string CommandKey = "/dungeon";

    /// <inheritdoc />
    public string Key => CommandKey;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        await DungeonPanelService.ShowWindowAsync(player).ConfigureAwait(false);
    }
}
