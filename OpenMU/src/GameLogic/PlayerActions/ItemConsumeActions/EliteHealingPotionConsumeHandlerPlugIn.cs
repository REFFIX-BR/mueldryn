// -----------------------------------------------------------------------
// <copyright file="EliteHealingPotionConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Consume handler for the elite healing potion of the item shop, which recovers the whole health.
/// </summary>
[Guid("1C7B6A54-9D22-4E68-8A31-0B5D7E4F9C21")]
[PlugIn]
[Display(Name = "Elite healing potion", Description = "Recovers the complete health.")]
public class EliteHealingPotionConsumeHandlerPlugIn : HealthPotionConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => ItemConstants.EliteHealingPotion;

    /// <inheritdoc/>
    protected override int Multiplier => 10;
}
