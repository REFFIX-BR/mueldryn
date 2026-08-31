// <copyright file="QuestPanelNpcListEntry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.QuestPanel;

/// <summary>
/// One row in the Quest Master NPC list.
/// </summary>
/// <param name="Index">Quest stage index.</param>
/// <param name="ListState">0 locked, 1 available, 2 in progress, 3 complete, 4 done.</param>
/// <param name="Title">Display title.</param>
public sealed record QuestPanelNpcListEntry(int Index, byte ListState, string Title);
