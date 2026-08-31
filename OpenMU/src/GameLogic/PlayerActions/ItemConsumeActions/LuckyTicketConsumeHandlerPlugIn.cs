// -----------------------------------------------------------------------
// <copyright file="LuckyTicketConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Opens the lucky tickets of the item shop, which give a random piece of equipment.
/// </summary>
[Guid("C2867F5F-48DE-4313-B5EC-B6F18324A71D")]
[PlugIn]
[Display(Name = "Lucky tickets", Description = "Gives the random equipment of the lucky tickets of the item shop.")]
public class LuckyTicketConsumeHandlerPlugIn : ItemShopBoxConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => new(null, 13);

    /// <inheritdoc />
    protected override short[] RewardItemNumbers =>
    [
        135, 136, 137, 138, 139, // 1st lucky armor, pants, helm, gloves and boots ticket
        140, 141, 142, 143, 144, // 2nd lucky armor, pants, helm, gloves and boots ticket
    ];
}
