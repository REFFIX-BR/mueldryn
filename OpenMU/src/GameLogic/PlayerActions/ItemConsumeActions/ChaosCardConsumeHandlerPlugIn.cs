// -----------------------------------------------------------------------
// <copyright file="ChaosCardConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Opens the chaos cards, keys, rare item tickets and boxes of the item shop, which give a random reward.
/// </summary>
[Guid("D3978060-59EF-4424-C6FD-C702943AB82E")]
[PlugIn]
[Display(Name = "Item shop cards and boxes", Description = "Gives the random reward of the chaos cards, keys, rare item tickets and boxes of the item shop.")]
public class ChaosCardConsumeHandlerPlugIn : ItemShopBoxConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => new(null, 14);

    /// <inheritdoc />
    protected override short[] RewardItemNumbers =>
    [
        92, // Chaos Card Gold
        95, // Chaos Card Mini
        112, // Silver Key
        113, // Gold Key
        137, // Package Box D
        146, // Rare Item Ticket 8
        149, // Rare Item Ticket 11
    ];
}
