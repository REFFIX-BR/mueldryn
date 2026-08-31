// <copyright file="QuestPanelCatalog.cs" company="MUnique">

// Licensed under the MIT License. See LICENSE file in the project root for full license information.

// </copyright>



namespace MUnique.OpenMU.GameLogic.QuestPanel;



/// <summary>

/// Sequential main-quest definitions (Mudream titles + OpenMU kill targets).

/// Player must finish quest N before quest N+1 unlocks.

/// Full list is generated from Mudream QuestSystemText.xml.

/// </summary>

public static partial class QuestPanelCatalog

{

    /// <summary>

    /// Ordered main quest chain (202 Mudream quests).

    /// </summary>

    public static IReadOnlyList<QuestPanelDefinition> Quests { get; } = BuildQuests();



    /// <summary>

    /// Total quests in the chain.

    /// </summary>

    public static int Count => Quests.Count;



    /// <summary>

    /// Returns the definition for <paramref name="stage"/>, or null if the chain is finished.

    /// </summary>

    public static QuestPanelDefinition? Get(int stage)

        => stage >= 0 && stage < Quests.Count ? Quests[stage] : null;

    /// <summary>
    /// Minimum character level to accept the quest at <paramref name="stage"/>.
    /// </summary>
    public static int GetRequiredLevel(int stage)
        => Math.Min(400, Math.Max(1, (int)Math.Round(10 + stage * 1.9)));
}



/// <summary>

/// One sequential main quest.

/// </summary>

/// <param name="Index">Zero-based stage index.</param>

/// <param name="Title">Display title (max ~32 UTF-8 for packet).</param>

/// <param name="TargetLabel">Short monster label for the tracker.</param>

/// <param name="MonsterNumber">OpenMU monster number to kill.</param>

/// <param name="RequiredKills">Kills required after accept.</param>

/// <param name="Experience">Experience granted on claim.</param>

/// <param name="Money">Zen granted on claim.</param>

/// <param name="LevelUpPoints">Free stat points on claim (5 + quest index).</param>

/// <param name="KundunCount">Box of Kundun count (milestones only).</param>

/// <param name="KundunLevel">Box item level (+3=10, +4=11, +5=12).</param>

/// <param name="TcaCount">Talisman of Chaos Assembly count (quest 200).</param>

public sealed record QuestPanelDefinition(

    int Index,

    string Title,

    string TargetLabel,

    ushort MonsterNumber,

    int RequiredKills,

    long Experience,

    int Money,

    int LevelUpPoints,

    int KundunCount,

    byte KundunLevel,

    int TcaCount);


