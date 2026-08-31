// -----------------------------------------------------------------------
// <copyright file="ResetFruitControlConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Consume handler of the reset fruit which gives the invested command points back.
/// </summary>
[Guid("9F53522C-15AB-40E0-82B9-83CE50C174A9")]
[PlugIn]
[Display(Name = "Reset fruit (control)", Description = "Resets the command stat and gives the points back.")]
public class ResetFruitControlConsumeHandlerPlugIn : ResetFruitConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => new(58, 13);

    /// <inheritdoc />
    protected override AttributeDefinition StatAttribute => Stats.BaseLeadership;
}
