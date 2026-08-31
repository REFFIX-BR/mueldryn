// <copyright file="QuestPanelProgressPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.QuestPanel;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Tracks kill progress for the sequential main quest panel.
/// </summary>
[PlugIn]
[Display(Name = "Quest Panel Progress", Description = "Counts monster kills for the sequential main quest panel.")]
[Guid("D4E8F1A2-5B7C-4D9E-8A1F-3C6B0E9D2A55")]
public sealed class QuestPanelProgressPlugIn : IAttackableGotKilledPlugIn
{
    /// <inheritdoc />
    public async ValueTask AttackableGotKilledAsync(IAttackable killed, IAttacker? killer)
    {
        if (killer is Player player && killed is Monster monster)
        {
            await QuestPanelService.TryRegisterKillAsync(player, monster).ConfigureAwait(false);
        }
    }
}
