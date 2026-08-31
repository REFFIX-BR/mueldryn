// -----------------------------------------------------------------------
// <copyright file="ResetFruitEnergyConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Consume handler of the reset fruit which gives the invested energy points back.
/// </summary>
[Guid("8E42411B-049A-4FDF-91A8-72BD4FB06398")]
[PlugIn]
[Display(Name = "Reset fruit (energy)", Description = "Resets the energy and gives the points back.")]
public class ResetFruitEnergyConsumeHandlerPlugIn : ResetFruitConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => new(57, 13);

    /// <inheritdoc />
    protected override AttributeDefinition StatAttribute => Stats.BaseEnergy;
}
