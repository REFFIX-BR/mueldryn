// <copyright file="SoulPointsChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.SoulSystem;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// GM chat command: /soulpoints amount — grants remaining Soul System points.
/// </summary>
[Guid("C9D0E1F2-3647-4F80-5162-738495A6B7C8")]
[PlugIn]
[Display(Name = "Soul Points", Description = "Grants Soul System remaining points. Usage: /soulpoints <amount>")]
[ChatCommandHelp(Command, typeof(Arguments), MinimumStatus)]
public class SoulPointsChatCommandPlugIn : ChatCommandPlugInBase<SoulPointsChatCommandPlugIn.Arguments>
{
    private const string Command = "/soulpoints";
    private const CharacterStatus MinimumStatus = CharacterStatus.GameMaster;

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => MinimumStatus;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player player, Arguments arguments)
    {
        if (player.SelectedCharacter is null)
        {
            return;
        }

        var amount = arguments?.Amount ?? 0;
        if (amount <= 0)
        {
            await player.ShowBlueMessageAsync("Usage: /soulpoints <amount>").ConfigureAwait(false);
            return;
        }

        SoulSystemService.GrantResetReward(player, amount);
        var status = SoulSystemService.BuildStatus(player);
        await player.ShowBlueMessageAsync($"Soul points remaining: {status.Remaining}").ConfigureAwait(false);
    }

    /// <summary>
    /// Arguments for /soulpoints.
    /// </summary>
    public class Arguments : ArgumentsBase
    {
        /// <summary>
        /// Gets or sets how many remaining points to grant.
        /// </summary>
        public int Amount { get; set; }
    }
}
