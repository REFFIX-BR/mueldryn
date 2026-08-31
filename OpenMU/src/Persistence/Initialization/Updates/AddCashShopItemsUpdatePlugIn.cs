// <copyright file="AddCashShopItemsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the item definitions offered by the MU Item Shop scripts (IBSPackage/IBSProduct), which the
/// default season 6 data does not contain. Without them the shop shows the entries but the server has
/// nothing to hand out, so every purchase fails with "item not available".
/// Sizes, slots and durability are taken from the client item table, so both sides agree.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("B3D71F42-8C05-4E19-9A6D-1F72C4E8A530")]
public class AddCashShopItemsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add cash shop items";
    internal const string PlugInDescription = "Adds the seals, scrolls, tickets, jewellery, potions and cards which the MU Item Shop offers.";

    /// <summary>Client value of <c>m_byItemSlot</c> for items which cannot be equipped.</summary>
    private const byte NoSlot = 255;

    private static readonly CashShopItem[] Items =
    [
        new(12, 131, "Small Wing of Curse", 3, 2, 7, 200),
        new(12, 132, "Small Wings of Elf", 3, 2, 7, 200),
        new(12, 133, "Small Wings of Heaven", 3, 2, 7, 200),
        new(12, 134, "Small Wings of Satan", 3, 2, 7, 200),
        new(12, 135, "Little Warrior's Cloak", 2, 2, 7, 200),
        new(13, 43, "Seal of Ascension", 1, 1, 10, 255),
        new(13, 44, "Seal of Wealth", 1, 1, 10, 255),
        new(13, 45, "Seal of Sustenance", 1, 1, 10, 255),
        new(13, 46, "Devil Square Ticket", 1, 1, NoSlot, 255),
        new(13, 47, "Blood Castle Ticket", 1, 1, NoSlot, 255),
        new(13, 48, "Kalima Ticket", 1, 1, NoSlot, 255),
        new(13, 54, "Reset Fruit Strength", 1, 1, NoSlot, 255),
        new(13, 55, "Reset Fruit Quickness", 1, 1, NoSlot, 255),
        new(13, 56, "Reset Fruit Health", 1, 1, NoSlot, 255),
        new(13, 57, "Reset Fruit Energy", 1, 1, NoSlot, 255),
        new(13, 58, "Reset Fruit Control", 1, 1, NoSlot, 255),
        new(13, 61, "Illusion Temple Ticket", 1, 1, NoSlot, 255),
        new(13, 62, "Seal of Healing", 1, 1, NoSlot, 1),
        new(13, 63, "Seal of Divinity", 1, 1, NoSlot, 1),
        new(13, 69, "Talisman of Resurrection", 1, 1, NoSlot, 255),
        new(13, 70, "Talisman of Mobility", 1, 1, NoSlot, 255),
        new(13, 93, "Master Seal of Ascension", 1, 1, NoSlot, 1),
        new(13, 94, "Master Seal of Wealth", 1, 1, NoSlot, 1),
        new(13, 104, "Max AG Boost Aura", 1, 1, NoSlot, 1),
        new(13, 105, "Max SD Boost Aura", 1, 1, NoSlot, 1),
        new(13, 107, "Lethal Wizard's Ring", 1, 1, 10, 100),
        new(13, 109, "Sapphire Ring", 1, 1, 10, 255),
        new(13, 110, "Ruby Ring", 1, 1, 10, 255),
        new(13, 111, "Topaz Ring", 1, 1, 10, 255),
        new(13, 112, "Amethyst Ring", 1, 1, 10, 255),
        new(13, 113, "Ruby Necklace", 1, 1, 9, 255),
        new(13, 114, "Emerald Necklace", 1, 1, 9, 255),
        new(13, 115, "Sapphire Necklace", 1, 1, 9, 255),
        new(13, 124, "Paid Channel Access Ticket", 1, 1, NoSlot, 1),
        new(13, 127, "Open Access Ticket to Varka", 1, 1, NoSlot, 255),
        new(13, 128, "Hawk Figurine", 1, 1, 10, 255),
        new(13, 129, "Goat Figurine", 1, 1, 10, 255),
        new(13, 130, "Oak Charm", 1, 1, 10, 255),
        new(13, 132, "Golden Oak Charm", 1, 2, 10, 255),
        new(13, 134, "Worn Horseshoe", 1, 1, 10, 255),
        new(13, 135, "1st Lucky Armor Ticket", 1, 1, NoSlot, 255),
        new(13, 136, "1st Lucky Pants Ticket", 1, 1, NoSlot, 255),
        new(13, 137, "1st Lucky Helm Ticket", 1, 1, NoSlot, 255),
        new(13, 138, "1st Lucky Gloves Ticket", 1, 1, NoSlot, 255),
        new(13, 139, "1st Lucky Boots Ticket", 1, 1, NoSlot, 255),
        new(13, 140, "2nd Lucky Armor Ticket", 1, 1, NoSlot, 255),
        new(13, 141, "2nd Lucky Pants Ticket", 1, 1, NoSlot, 255),
        new(13, 142, "2nd Lucky Helm Ticket", 1, 1, NoSlot, 255),
        new(13, 143, "2nd Lucky Gloves Ticket", 1, 1, NoSlot, 255),
        new(13, 144, "2nd Lucky Boots Ticket", 1, 1, NoSlot, 255),
        new(14, 53, "Talisman of Luck", 1, 1, NoSlot, 255),
        new(14, 70, "Elite Healing Potion", 1, 1, NoSlot, 255),
        new(14, 71, "Elite Mana Potion", 1, 1, NoSlot, 255),
        new(14, 72, "Scroll of Quickness", 2, 2, NoSlot, 1),
        new(14, 73, "Scroll of Defense", 2, 2, NoSlot, 1),
        new(14, 74, "Scroll of Wrath", 2, 2, NoSlot, 1),
        new(14, 75, "Scroll of Wizardry", 2, 2, NoSlot, 1),
        new(14, 76, "Scroll of Health", 2, 2, NoSlot, 1),
        new(14, 77, "Scroll of Mana", 2, 2, NoSlot, 1),
        new(14, 91, "Summoner Character Card", 1, 1, NoSlot, 255),
        new(14, 92, "Chaos Card Gold", 1, 1, NoSlot, 255),
        new(14, 95, "Chaos Card Mini", 1, 1, NoSlot, 255),
        new(14, 97, "Scroll of Battle", 2, 2, NoSlot, 1),
        new(14, 98, "Scroll of Strength", 2, 2, NoSlot, 1),
        new(14, 112, "Silver Key", 1, 1, NoSlot, 255),
        new(14, 113, "Gold Key", 1, 1, NoSlot, 255),
        new(14, 120, "Goblin Gold Coin", 1, 1, NoSlot, 255),
        new(14, 133, "Elite SD Potion", 1, 1, NoSlot, 255),
        new(14, 137, "Package Box D", 1, 1, NoSlot, 255),
        new(14, 146, "Rare Item Ticket 8", 1, 1, NoSlot, 255),
        new(14, 149, "Rare Item Ticket 11", 1, 1, NoSlot, 255),
        new(14, 162, "Magic Backpack", 1, 1, NoSlot, 255),
        new(14, 163, "Vault Expansion Certificate", 1, 1, NoSlot, 255),
        new(14, 169, "RageFighter Character Card", 1, 1, NoSlot, 255),
    ];

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddCashShopItems;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 13, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var item in Items)
        {
            if (gameConfiguration.Items.Any(i => i.Group == item.Group && i.Number == item.Number))
            {
                continue;
            }

            var definition = context.CreateNew<ItemDefinition>();
            gameConfiguration.Items.Add(definition);
            definition.Group = item.Group;
            definition.Number = item.Number;
            definition.Name = item.Name;
            definition.Width = item.Width;
            definition.Height = item.Height;

            // For everything which is not worn, the durability holds the maximum stack size.
            definition.Durability = item.Durability;
            definition.MaximumItemLevel = 0;
            definition.DropsFromMonsters = false;
            definition.DropLevel = 0;
            definition.Value = 0;
            definition.MaximumSockets = 0;

            if (item.Slot != NoSlot)
            {
                definition.ItemSlot = gameConfiguration.ItemSlotTypes.First(t => t.ItemSlots.Contains(item.Slot));
            }

            foreach (var characterClass in gameConfiguration.CharacterClasses)
            {
                definition.QualifiedCharacters.Add(characterClass);
            }
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// One item of the shop catalog, mirroring the client item table.
    /// </summary>
    /// <param name="Group">Item group.</param>
    /// <param name="Number">Item number inside the group.</param>
    /// <param name="Name">Display name.</param>
    /// <param name="Width">Inventory width.</param>
    /// <param name="Height">Inventory height.</param>
    /// <param name="Slot">Client item slot, <see cref="NoSlot"/> when it cannot be equipped.</param>
    /// <param name="Durability">Durability when worn, maximum stack size otherwise.</param>
    private sealed record CashShopItem(byte Group, short Number, string Name, byte Width, byte Height, byte Slot, byte Durability);
}
