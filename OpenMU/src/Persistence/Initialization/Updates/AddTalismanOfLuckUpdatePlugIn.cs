// <copyright file="AddTalismanOfLuckUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Makes the talisman of luck of the item shop work: adding it to a chaos machine mix raises the
/// success rate by 25 percent, like in the original game.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("2C8B4A16-95D7-4E03-B1F8-6D0A93E5C712")]
public class AddTalismanOfLuckUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add talisman of luck";
    internal const string PlugInDescription = "The talisman of luck raises the success rate of the chaos machine mixes by 25 percent.";

    private const byte TalismanGroup = 14;
    private const short TalismanNumber = 53;
    private const byte SuccessRateAddition = 25;

    /// <summary>Mixes which accept the talisman, by the number of the crafting.</summary>
    private static readonly byte[] SupportedCraftingNumbers =
    [
        1, // Chaos Weapon
        11, // 1st Level Wings
        7, // 2nd Level Wings
        38, // 3rd Level Wings, Stage 1
        39, // 3rd Level Wings, Stage 2
        24, // Cape of Lord/Fighter
        3, 4, 22, 23, 49, 50, // +10 to +15 item combinations
    ];

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddTalismanOfLuck;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 13, 15, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (gameConfiguration.Items.FirstOrDefault(item => item.Group == TalismanGroup && item.Number == TalismanNumber) is not { } talisman)
        {
            return;
        }

        foreach (var monster in gameConfiguration.Monsters)
        {
            foreach (var crafting in monster.ItemCraftings.Where(c => SupportedCraftingNumbers.Contains(c.Number)))
            {
                if (crafting.SimpleCraftingSettings is not { } settings
                    || settings.RequiredItems.Any(required => required.PossibleItems.Contains(talisman)))
                {
                    continue;
                }

                var requiredItem = context.CreateNew<ItemCraftingRequiredItem>();
                requiredItem.MinimumAmount = 0;
                requiredItem.MaximumAmount = 1;
                requiredItem.AddPercentage = SuccessRateAddition;
                requiredItem.SuccessResult = MixResult.Disappear;
                requiredItem.FailResult = MixResult.Disappear;
                requiredItem.PossibleItems.Add(talisman);
                settings.RequiredItems.Add(requiredItem);
            }
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
