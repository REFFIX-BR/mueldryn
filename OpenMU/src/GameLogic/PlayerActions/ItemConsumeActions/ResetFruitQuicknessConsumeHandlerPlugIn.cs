// -----------------------------------------------------------------------
// <copyright file="ResetFruitQuicknessConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Consume handler of the reset fruit which gives the invested agility points back.
/// </summary>
[Guid("6C202F09-E277-4DBD-9F86-509B2D9E4176")]
[PlugIn]
[Display(Name = "Reset fruit (quickness)", Description = "Resets the agility and gives the points back.")]
public class ResetFruitQuicknessConsumeHandlerPlugIn : ResetFruitConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => new(55, 13);

    /// <inheritdoc />
    protected override AttributeDefinition StatAttribute => Stats.BaseAgility;
}
