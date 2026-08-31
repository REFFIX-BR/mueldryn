// -----------------------------------------------------------------------
// <copyright file="ResetFruitStrengthConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Consume handler of the reset fruit which gives the invested strength points back.
/// </summary>
[Guid("5B1F2E98-D166-4CAC-8E75-4F9A1C8D3065")]
[PlugIn]
[Display(Name = "Reset fruit (strength)", Description = "Resets the strength and gives the points back.")]
public class ResetFruitStrengthConsumeHandlerPlugIn : ResetFruitConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => new(54, 13);

    /// <inheritdoc />
    protected override AttributeDefinition StatAttribute => Stats.BaseStrength;
}
