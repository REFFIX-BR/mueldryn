// -----------------------------------------------------------------------
// <copyright file="EliteManaPotionConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Consume handler for the elite mana potion of the item shop, which recovers the whole mana.
/// </summary>
[Guid("2D8C7B65-AE33-4F79-9B42-1C6E8F5A0D32")]
[PlugIn]
[Display(Name = "Elite mana potion", Description = "Recovers the complete mana.")]
public class EliteManaPotionConsumeHandlerPlugIn : ManaPotionConsumeHandler
{
    /// <inheritdoc />
    public override ItemIdentifier Key => ItemConstants.EliteManaPotion;

    /// <inheritdoc/>
    protected override int Multiplier => 10;
}
