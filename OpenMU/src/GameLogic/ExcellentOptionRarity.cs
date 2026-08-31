// <copyright file="ExcellentOptionRarity.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

/// <summary>
/// Rarity of a single excellent option (add), shown as [Normal]/[Uncommon]/[Rare]/[Epic] in the client tooltip.
/// Stored in <see cref="MUnique.OpenMU.DataModel.Entities.ItemOptionLink.Level"/> for excellent options.
/// </summary>
public enum ExcellentOptionRarity : byte
{
    /// <summary>White [Normal].</summary>
    Normal = 0,

    /// <summary>Green [Uncommon].</summary>
    Uncommon = 1,

    /// <summary>Pink [Rare].</summary>
    Rare = 2,

    /// <summary>Red/pink [Epic].</summary>
    Epic = 3,
}
