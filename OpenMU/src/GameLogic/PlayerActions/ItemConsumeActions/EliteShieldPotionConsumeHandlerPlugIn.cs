// -----------------------------------------------------------------------
// <copyright file="EliteShieldPotionConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Consume handler for the elite shield potion of the item shop, which recovers the whole shield.
/// </summary>
[Guid("3E9D8C76-BF44-4A8A-8C53-2D7F9A6B1E43")]
[PlugIn]
[Display(Name = "Elite shield potion", Description = "Recovers the complete shield.")]
public class EliteShieldPotionConsumeHandlerPlugIn : ShieldPotionConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => ItemConstants.EliteShieldPotion;

    /// <inheritdoc />
    protected override double RecoverPercentage => 100;
}
