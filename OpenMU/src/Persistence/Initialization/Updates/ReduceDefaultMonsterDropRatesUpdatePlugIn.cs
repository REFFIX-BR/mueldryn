// <copyright file="ReduceDefaultMonsterDropRatesUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Lowers the default monster drop chances. Vanilla OpenMU uses 50 % zen + 30 % random item (~80 %
/// of kills drop something). With farm spots at Quantity 6 the ground fills quickly.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("4C1E9A73-2B5F-4D08-9E61-7F3A2D908C14")]
public class ReduceDefaultMonsterDropRatesUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Reduce default monster drop rates";
    internal const string PlugInDescription =
        "Lowers default zen/item drop chances (50/30 % → 12/8 %) so hunt maps are not littered with loot.";

    /// <summary>Default money drop group id from <see cref="GameConfigurationInitializerBase"/>.</summary>
    private const short MoneyDropGroupId = 1;

    /// <summary>Default random-item drop group id.</summary>
    private const short RandomItemDropGroupId = 2;

    /// <summary>Default excellent drop group id.</summary>
    private const short ExcellentDropGroupId = 3;

    /// <summary>Default jewel drop group id.</summary>
    private const short JewelDropGroupId = 4;

    /// <summary>Zen drop chance (was 0.5).</summary>
    private const double MoneyDropChance = 0.12;

    /// <summary>Random equipment drop chance (was 0.3).</summary>
    private const double RandomItemDropChance = 0.08;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.ReduceDefaultMonsterDropRates;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 01, 14, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        SetChance(gameConfiguration, MoneyDropGroupId, MoneyDropChance, "money");
        SetChance(gameConfiguration, RandomItemDropGroupId, RandomItemDropChance, "random item");
        SetChance(gameConfiguration, ExcellentDropGroupId, 0.0001, "excellent");
        SetChance(gameConfiguration, JewelDropGroupId, 0.0008, "jewel");

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }

    private static void SetChance(GameConfiguration gameConfiguration, short groupId, double chance, string label)
    {
        var id = GuidHelper.CreateGuid<DropItemGroup>(groupId);
        var group = gameConfiguration.DropItemGroups.FirstOrDefault(g => g.GetId() == id);
        if (group is null)
        {
            return;
        }

        group.Chance = chance;
        group.Description = $"Default {label} drop ({chance * 100:0.##} % chance)";
    }
}
