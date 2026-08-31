// <copyright file="JewelBankService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.JewelBank;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>
/// Account-bound jewel bank: deposit / withdraw counters shared across all characters.
/// Single jewels use durability as stack count; withdrawing a pack creates the packed jewel
/// item (group 12) which holds 10, 20 or 30 pieces in its item level.
/// </summary>
public static class JewelBankService
{
    /// <summary>
    /// Builds the current bank snapshot (account-wide).
    /// </summary>
    public static async ValueTask<JewelBankStatus> BuildStatusAsync(Player player)
    {
        EnsureAttributeDefinitions(player);
        await MigrateCharacterBankToAccountAsync(player).ConfigureAwait(false);

        var counts = new int[JewelBankCatalog.SlotCount];
        for (var i = 0; i < JewelBankCatalog.SlotCount; i++)
        {
            counts[i] = (int)GetAccountAttributeValue(player, JewelBankCatalog.Attributes[i]);
        }

        return new JewelBankStatus { Counts = counts };
    }

    /// <summary>
    /// Deposits one inventory item (by slot) into the account bank.
    /// Stacked jewels credit <see cref="Item.Durability"/> units, packed jewels their pieces.
    /// </summary>
    public static async ValueTask<JewelBankResult> TryDepositAsync(Player player, byte itemSlot)
    {
        if (player.Inventory is null || player.SelectedCharacter is null || player.Account is null)
        {
            return JewelBankResult.Failed;
        }

        var item = player.Inventory.GetItem(itemSlot);
        if (item?.Definition is null || !JewelBankCatalog.TryGetDeposit(item, out var bankSlot, out var amount))
        {
            return JewelBankResult.InvalidItem;
        }

        EnsureAttributeDefinitions(player);
        await MigrateCharacterBankToAccountAsync(player).ConfigureAwait(false);

        // Credit first: if destroy fails we still have the jewel; never destroy then fail the count.
        var bankStat = GetOrCreateAccountStat(player, JewelBankCatalog.Attributes[(int)bankSlot]);
        if (bankStat.Definition?.MaximumValue is 0f)
        {
            bankStat.Definition.MaximumValue = null;
        }

        bankStat.Value += amount;
        await player.DestroyInventoryItemAsync(item).ConfigureAwait(false);
        return JewelBankResult.Success;
    }

    /// <summary>
    /// Withdraws jewels from the account bank into the inventory.
    /// In <see cref="JewelBankWithdrawMode.Units"/> each jewel gets its own inventory slot, in
    /// <see cref="JewelBankWithdrawMode.Pack"/> the quantity is the size of a single packed jewel.
    /// </summary>
    public static async ValueTask<JewelBankResult> TryWithdrawAsync(Player player, JewelBankSlot bankSlot, byte quantity, JewelBankWithdrawMode mode = JewelBankWithdrawMode.Units)
    {
        if (player.Inventory is null || player.SelectedCharacter is null || player.Account is null)
        {
            return JewelBankResult.Failed;
        }

        if ((int)bankSlot < 0 || (int)bankSlot >= JewelBankCatalog.SlotCount)
        {
            return JewelBankResult.InvalidItem;
        }

        var qty = Math.Clamp((int)quantity, 1, 255);
        EnsureAttributeDefinitions(player);
        await MigrateCharacterBankToAccountAsync(player).ConfigureAwait(false);

        var attr = GetOrCreateAccountStat(player, JewelBankCatalog.Attributes[(int)bankSlot]);
        if (attr.Value < qty)
        {
            return JewelBankResult.NotEnough;
        }

        return mode switch
        {
            JewelBankWithdrawMode.Pack => await WithdrawPackAsync(player, bankSlot, qty, attr).ConfigureAwait(false),
            JewelBankWithdrawMode.Stack => await WithdrawStacksAsync(player, bankSlot, qty, attr).ConfigureAwait(false),
            _ => await WithdrawUnitsAsync(player, bankSlot, qty, attr).ConfigureAwait(false),
        };
    }

    /// <summary>
    /// Delivers one item per jewel, so the player can use or move them individually.
    /// </summary>
    private static async ValueTask<JewelBankResult> WithdrawUnitsAsync(Player player, JewelBankSlot bankSlot, int quantity, StatAttribute attr)
    {
        var id = JewelBankCatalog.Items[(int)bankSlot];
        var definition = player.GameContext.Configuration.Items
            .FirstOrDefault(i => i.Group == id.Group && i.Number == id.Number);
        if (definition is null)
        {
            return JewelBankResult.Failed;
        }

        var freeSlots = player.Inventory!.FreeSlots.Take(quantity).ToList();
        if (freeSlots.Count == 0)
        {
            return JewelBankResult.InventoryFull;
        }

        var delivered = 0;
        foreach (var freeSlot in freeSlots)
        {
            var item = player.PersistenceContext.CreateNew<Item>();
            item.Definition = definition;
            item.Durability = 1;
            item.Level = 0;

            if (!await player.Inventory.AddItemAsync(freeSlot, item).ConfigureAwait(false))
            {
                await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
                break;
            }

            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
            delivered++;
        }

        if (delivered == 0)
        {
            return JewelBankResult.InventoryFull;
        }

        attr.Value -= delivered;
        return delivered < quantity ? JewelBankResult.InventoryFull : JewelBankResult.Success;
    }

    /// <summary>
    /// Delivers the jewels in as few inventory stacks as possible, topping up existing stacks first.
    /// </summary>
    private static async ValueTask<JewelBankResult> WithdrawStacksAsync(Player player, JewelBankSlot bankSlot, int quantity, StatAttribute attr)
    {
        var id = JewelBankCatalog.Items[(int)bankSlot];
        var definition = player.GameContext.Configuration.Items
            .FirstOrDefault(i => i.Group == id.Group && i.Number == id.Number);
        if (definition is null)
        {
            return JewelBankResult.Failed;
        }

        var maxStack = definition.Durability > 1 ? (int)definition.Durability : 1;
        var remaining = quantity;
        var delivered = 0;

        foreach (var existing in player.Inventory!.Items
                     .Where(i => i.Definition == definition && i.Level == 0 && i.Durability < maxStack)
                     .OrderByDescending(i => i.Durability)
                     .ToList())
        {
            if (remaining <= 0)
            {
                break;
            }

            var add = Math.Min(maxStack - (int)existing.Durability, remaining);
            if (add <= 0)
            {
                continue;
            }

            existing.Durability += add;
            remaining -= add;
            delivered += add;
            await player.InvokeViewPlugInAsync<IItemDurabilityChangedPlugIn>(p => p.ItemDurabilityChangedAsync(existing, false)).ConfigureAwait(false);
        }

        while (remaining > 0)
        {
            var stackAmount = Math.Min(maxStack, remaining);
            var item = player.PersistenceContext.CreateNew<Item>();
            item.Definition = definition;
            item.Durability = stackAmount;
            item.Level = 0;

            if (!await player.Inventory.AddItemAsync(item).ConfigureAwait(false))
            {
                await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
                break;
            }

            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
            remaining -= stackAmount;
            delivered += stackAmount;
        }

        if (delivered == 0)
        {
            return JewelBankResult.InventoryFull;
        }

        attr.Value -= delivered;
        return remaining > 0 ? JewelBankResult.InventoryFull : JewelBankResult.Success;
    }

    /// <summary>
    /// Delivers a single packed jewel holding all the requested pieces.
    /// </summary>
    private static async ValueTask<JewelBankResult> WithdrawPackAsync(Player player, JewelBankSlot bankSlot, int size, StatAttribute attr)
    {
        if (!JewelBankCatalog.TryGetPackLevel(size, out var level))
        {
            return JewelBankResult.Failed;
        }

        var id = JewelBankCatalog.PackedItems[(int)bankSlot];
        var definition = player.GameContext.Configuration.Items
            .FirstOrDefault(i => i.Group == id.Group && i.Number == id.Number);
        if (definition is null)
        {
            return JewelBankResult.Failed;
        }

        var pack = player.PersistenceContext.CreateNew<Item>();
        pack.Definition = definition;
        pack.Level = level;
        pack.Durability = 1;

        if (!await player.Inventory!.AddItemAsync(pack).ConfigureAwait(false))
        {
            await player.PersistenceContext.DeleteAsync(pack).ConfigureAwait(false);
            return JewelBankResult.InventoryFull;
        }

        await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(pack)).ConfigureAwait(false);
        attr.Value -= size;
        return JewelBankResult.Success;
    }

    /// <summary>Counts physical jewel units in the player's inventory.</summary>
    public static int CountInventoryUnits(Player player, JewelBankSlot bankSlot)
    {
        if (player.Inventory is null || (int)bankSlot < 0 || (int)bankSlot >= JewelBankCatalog.SlotCount)
        {
            return 0;
        }

        var id = JewelBankCatalog.Items[(int)bankSlot];
        return player.Inventory.Items
            .Where(i => i.Definition?.Group == id.Group && i.Definition.Number == id.Number)
            .Sum(i => Math.Max(1, (int)i.Durability));
    }

    /// <summary>Consumes an exact number of physical jewel units from inventory stacks.</summary>
    public static async ValueTask<bool> TryConsumeInventoryUnitsAsync(Player player, JewelBankSlot bankSlot, int amount)
    {
        if (amount <= 0 || CountInventoryUnits(player, bankSlot) < amount || player.Inventory is null)
        {
            return false;
        }

        var id = JewelBankCatalog.Items[(int)bankSlot];
        var remaining = amount;
        foreach (var item in player.Inventory.Items
                     .Where(i => i.Definition?.Group == id.Group && i.Definition.Number == id.Number)
                     .OrderBy(i => i.ItemSlot)
                     .ToList())
        {
            if (remaining <= 0)
            {
                break;
            }

            var stack = Math.Max(1, (int)item.Durability);
            if (stack <= remaining)
            {
                remaining -= stack;
                await player.DestroyInventoryItemAsync(item).ConfigureAwait(false);
            }
            else
            {
                item.Durability = stack - remaining;
                remaining = 0;
                await player.InvokeViewPlugInAsync<IItemDurabilityChangedPlugIn>(
                    p => p.ItemDurabilityChangedAsync(item, false)).ConfigureAwait(false);
            }
        }

        return remaining == 0;
    }

    /// <summary>Credits jewel units directly to the account-bound Item Bank.</summary>
    public static async ValueTask CreditAccountAsync(Player player, JewelBankSlot bankSlot, int amount)
    {
        if (amount <= 0 || player.Account is null)
        {
            return;
        }

        EnsureAttributeDefinitions(player);
        await MigrateCharacterBankToAccountAsync(player).ConfigureAwait(false);
        var stat = GetOrCreateAccountStat(player, JewelBankCatalog.Attributes[(int)bankSlot]);
        stat.Value += amount;
    }

    /// <summary>Debits the account-bound Item Bank, used only for transaction rollback.</summary>
    public static async ValueTask<bool> TryDebitAccountAsync(Player player, JewelBankSlot bankSlot, int amount)
    {
        if (amount <= 0 || player.Account is null)
        {
            return false;
        }

        EnsureAttributeDefinitions(player);
        await MigrateCharacterBankToAccountAsync(player).ConfigureAwait(false);
        var stat = GetOrCreateAccountStat(player, JewelBankCatalog.Attributes[(int)bankSlot]);
        if (stat.Value < amount)
        {
            return false;
        }

        stat.Value -= amount;
        return true;
    }

    /// <summary>Restores physical jewel units to inventory after a rolled-back shop purchase.</summary>
    public static async ValueTask<bool> RestoreInventoryUnitsAsync(Player player, JewelBankSlot bankSlot, int amount)
    {
        if (amount <= 0 || player.Inventory is null)
        {
            return false;
        }

        var id = JewelBankCatalog.Items[(int)bankSlot];
        var definition = player.GameContext.Configuration.Items
            .FirstOrDefault(i => i.Group == id.Group && i.Number == id.Number);
        if (definition is null)
        {
            return false;
        }

        var maxStack = definition.Durability > 1 ? (int)definition.Durability : 1;
        var remaining = amount;
        foreach (var existing in player.Inventory.Items
                     .Where(i => i.Definition == definition && i.Level == 0 && i.Durability < maxStack)
                     .OrderByDescending(i => i.Durability)
                     .ToList())
        {
            var add = Math.Min(maxStack - (int)existing.Durability, remaining);
            if (add <= 0)
            {
                continue;
            }

            existing.Durability += add;
            remaining -= add;
            await player.InvokeViewPlugInAsync<IItemDurabilityChangedPlugIn>(
                p => p.ItemDurabilityChangedAsync(existing, false)).ConfigureAwait(false);
            if (remaining == 0)
            {
                return true;
            }
        }

        while (remaining > 0)
        {
            var stack = Math.Min(maxStack, remaining);
            var item = player.PersistenceContext.CreateNew<Item>();
            item.Definition = definition;
            item.Durability = stack;
            item.Level = 0;
            if (!await player.Inventory.AddItemAsync(item).ConfigureAwait(false))
            {
                await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
                return false;
            }

            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
            remaining -= stack;
        }

        return true;
    }

    /// <summary>
    /// Moves jewel-bank counters from the current character onto the account
    /// (same AttributeDefinition cannot live on both).
    /// </summary>
    private static async ValueTask MigrateCharacterBankToAccountAsync(Player player)
    {
        var character = player.SelectedCharacter;
        if (character is null || player.Account is null)
        {
            return;
        }

        foreach (var template in JewelBankCatalog.Attributes)
        {
            var onChar = character.Attributes
                .FirstOrDefault(a => a.Definition is not null && a.Definition.Id == template.Id);
            if (onChar is null)
            {
                continue;
            }

            var amount = onChar.Value;
            character.Attributes.Remove(onChar);
            await player.PersistenceContext.DeleteAsync(onChar).ConfigureAwait(false);

            if (amount <= 0)
            {
                continue;
            }

            var accountStat = GetOrCreateAccountStat(player, template);
            accountStat.Value += amount;
        }
    }

    private static void EnsureAttributeDefinitions(Player player)
    {
        foreach (var attr in JewelBankCatalog.Attributes)
        {
            EnsureDefinition(player, attr);
        }
    }

    private static void EnsureDefinition(Player player, AttributeDefinition template)
    {
        var config = player.GameContext.Configuration;
        var existing = config.Attributes.FirstOrDefault(a => a.Id == template.Id);
        if (existing is not null)
        {
            if (existing.MaximumValue is 0f)
            {
                existing.MaximumValue = null;
            }

            return;
        }

        try
        {
            var persistent = player.PersistenceContext.CreateNew<AttributeDefinition>(
                template.Id,
                template.Designation,
                template.Description);
            persistent.MaximumValue = null;
            config.Attributes.Add(persistent);
        }
        catch (InvalidOperationException)
        {
            template.MaximumValue = null;
            config.Attributes.Add(template);
        }
    }

    private static float GetAccountAttributeValue(Player player, AttributeDefinition template)
    {
        var attr = player.Account?.Attributes
            .FirstOrDefault(a => a.Definition is not null && a.Definition.Id == template.Id);
        return attr?.Value ?? 0f;
    }

    private static StatAttribute GetOrCreateAccountStat(Player player, AttributeDefinition template)
    {
        var account = player.Account
            ?? throw new InvalidOperationException("No account.");
        var existing = account.Attributes
            .FirstOrDefault(a => a.Definition is not null && a.Definition.Id == template.Id);
        if (existing is not null)
        {
            if (existing.Definition?.MaximumValue is 0f)
            {
                existing.Definition.MaximumValue = null;
            }

            return existing;
        }

        var definition = player.GameContext.Configuration.Attributes.First(a => a.Id == template.Id);
        var created = player.PersistenceContext.CreateNew<StatAttribute>(definition, 0);
        account.Attributes.Add(created);
        return created;
    }
}
