// <copyright file="MonsterPowerByProgressConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// Configuration for <see cref="MonsterPowerByProgressPlugIn"/>.
/// Values are percentages (e.g. 12 = +12% per reset).
/// </summary>
public class MonsterPowerByProgressConfiguration
{
    /// <summary>
    /// Gets or sets the extra power percent applied per character reset.
    /// </summary>
    public float PercentPerReset { get; set; } = 12f;

    /// <summary>
    /// Gets or sets the extra power percent applied per character level.
    /// </summary>
    public float PercentPerLevel { get; set; } = 0.12f;

    /// <summary>
    /// Gets or sets the extra power percent applied per master level.
    /// </summary>
    public float PercentPerMasterLevel { get; set; } = 0.04f;

    /// <summary>
    /// Gets or sets the minimum multiplier required before scaling is applied.
    /// </summary>
    public float MinimumMultiplier { get; set; } = 1.05f;

    /// <summary>
    /// Gets or sets the maximum power multiplier cap.
    /// </summary>
    public float MaximumMultiplier { get; set; } = 25f;
}
