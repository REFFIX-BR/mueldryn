// -----------------------------------------------------------------------
// <copyright file="ResetFruitHealthConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Consume handler of the reset fruit which gives the invested vitality points back.
/// </summary>
[Guid("7D31300A-F388-4ECE-8097-61AC3EAF5287")]
[PlugIn]
[Display(Name = "Reset fruit (health)", Description = "Resets the vitality and gives the points back.")]
public class ResetFruitHealthConsumeHandlerPlugIn : ResetFruitConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => new(56, 13);

    /// <inheritdoc />
    protected override AttributeDefinition StatAttribute => Stats.BaseVitality;
}
