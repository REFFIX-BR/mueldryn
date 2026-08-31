// -----------------------------------------------------------------------
// <copyright file="VaultExpansionConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Consume handler for the vault expansion certificate of the item shop, which doubles the vault size
/// of the account.
/// </summary>
[Guid("B1756E4E-37CD-4202-A4DB-A5E07213960C")]
[PlugIn]
[Display(Name = "Vault expansion certificate", Description = "Doubles the vault size of the account.")]
public class VaultExpansionConsumeHandlerPlugIn : BaseConsumeHandlerPlugIn
{
    /// <inheritdoc />
    public override ItemIdentifier Key => new(163, 14);

    /// <inheritdoc />
    public override async ValueTask<bool> ConsumeItemAsync(Player player, Item item, Item? targetItem, FruitUsage fruitUsage)
    {
        if (!this.CheckPreconditions(player, item)
            || player.Account is not { } account)
        {
            return false;
        }

        if (account.IsVaultExtended)
        {
            await player.ShowBlueMessageAsync("Your vault is already extended.").ConfigureAwait(false);
            return false;
        }

        account.IsVaultExtended = true;
        await this.ConsumeSourceItemAsync(player, item).ConfigureAwait(false);
        await player.ShowBlueMessageAsync("Vault extended. Log in again to see the second page.").ConfigureAwait(false);
        return true;
    }
}
