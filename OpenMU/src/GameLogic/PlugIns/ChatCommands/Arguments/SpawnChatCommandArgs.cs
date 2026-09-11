// <copyright file="SpawnChatCommandArgs.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands.Arguments;

/// <summary>
/// Arguments used by <see cref="SpawnChatCommandPlugIn"/>.
/// </summary>
public class SpawnChatCommandArgs : ArgumentsBase
{
    /// <summary>
    /// Gets or sets the monster number (e.g. 604 = Golden Kundun).
    /// </summary>
    [Argument("id", false)]
    public short Id { get; set; }

    /// <summary>
    /// Gets or sets a short alias (e.g. goldenkundun, kundun, jack).
    /// </summary>
    [Argument("alias", false)]
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the map X coordinate. Use together with <see cref="Y"/>. Default -1 = at GM feet.
    /// </summary>
    [Argument("x", false)]
    public int X { get; set; } = -1;

    /// <summary>
    /// Gets or sets the map Y coordinate. Use together with <see cref="X"/>. Default -1 = at GM feet.
    /// </summary>
    [Argument("y", false)]
    public int Y { get; set; } = -1;
}
