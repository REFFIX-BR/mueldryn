// <copyright file="QuestPanelStatus.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.QuestPanel;

/// <summary>
/// Client-facing quest panel status (current stage in the sequential chain).
/// </summary>
public sealed class QuestPanelStatus
{
    /// <summary>
    /// Gets the quest display name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the short target label (e.g. Spider).
    /// </summary>
    public string TargetLabel { get; init; } = string.Empty;

    /// <summary>
    /// Gets current kill progress.
    /// </summary>
    public int Kills { get; init; }

    /// <summary>
    /// Gets required kills.
    /// </summary>
    public int Required { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entire chain is finished.
    /// </summary>
    public bool Claimed { get; init; }

    /// <summary>
    /// Gets a value indicating whether the player can claim the current quest.
    /// </summary>
    public bool CanClaim { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current quest was accepted.
    /// </summary>
    public bool Accepted { get; init; }

    /// <summary>
    /// Gets the UI state for NPC list / world markers.
    /// </summary>
    public QuestPanelState State { get; init; }

    /// <summary>
    /// Gets the zero-based stage index of the current quest.
    /// </summary>
    public int Stage { get; init; }

    /// <summary>
    /// Gets total quests in the chain.
    /// </summary>
    public int Total { get; init; }

    /// <summary>
    /// Gets the minimum level required to accept this quest.
    /// </summary>
    public int RequiredLevel { get; init; }
}

/// <summary>
/// Visual / flow state for the quest NPC UI.
/// </summary>
public enum QuestPanelState : byte
{
    /// <summary>Available to pick up (I154).</summary>
    Available = 0,

    /// <summary>In progress (I155).</summary>
    InProgress = 1,

    /// <summary>Complete, ready to claim (I156).</summary>
    Complete = 2,

    /// <summary>Entire chain finished.</summary>
    Claimed = 3,
}

/// <summary>
/// Result of a claim attempt.
/// </summary>
public enum QuestPanelClaimResult : byte
{
    /// <summary>Reward granted (and stage advanced).</summary>
    Success = 0,

    /// <summary>Kill requirement not met.</summary>
    RequirementsNotMet = 1,

    /// <summary>Chain already finished.</summary>
    AlreadyClaimed = 2,

    /// <summary>Inventory full.</summary>
    InventoryFull = 3,

    /// <summary>Unexpected failure.</summary>
    Failed = 4,
}
