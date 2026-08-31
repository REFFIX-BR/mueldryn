// <copyright file="ShowPlayerEquipmentPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends the equipment of another player to the extended client.
/// </summary>
[PlugIn]
[Display(Name = "Show Player Equipment", Description = "Sends the equipped items of another player for the detail window.")]
[Guid("6E1B94C7-5A0F-4D33-8B2E-71C9D0A4F5B8")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class ShowPlayerEquipmentPlugIn : IShowPlayerEquipmentPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowPlayerEquipmentPlugIn"/> class.
    /// </summary>
    public ShowPlayerEquipmentPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowPlayerEquipmentAsync(Player target)
    {
        if (this._player.Connection is not { Connected: true } connection
            || target.SelectedCharacter is null
            || target.Inventory is null)
        {
            return;
        }

        var items = target.Inventory.Items
            .Where(item => item.ItemSlot <= InventoryConstants.LastEquippableItemSlotIndex)
            .OrderBy(item => item.ItemSlot)
            .ToList();

        await EventSchedulePackets.SendPlayerEquipmentAsync(connection, target.SelectedCharacter.Name, items, this._player.ItemSerializer).ConfigureAwait(false);
    }
}
