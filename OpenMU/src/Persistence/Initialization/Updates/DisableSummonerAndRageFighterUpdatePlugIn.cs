// <copyright file="DisableSummonerAndRageFighterUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Takes the Summoner and Rage Fighter classes out of the game: they can't be created anymore and
/// their exclusive equipment stops dropping from monsters and boxes.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("C5D8E1F3-7A46-4B92-8E05-2C71B9D34A68")]
public class DisableSummonerAndRageFighterUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Disable Summoner and Rage Fighter";
    internal const string PlugInDescription = "Blocks the creation of Summoner and Rage Fighter characters and removes their exclusive items from monster and box drops.";

    private static readonly byte[] DisabledClassNumbers =
    [
        (byte)CharacterClassNumber.Summoner,
        (byte)CharacterClassNumber.BloodySummoner,
        (byte)CharacterClassNumber.DimensionMaster,
        (byte)CharacterClassNumber.RageFighter,
        (byte)CharacterClassNumber.FistMaster,
    ];

    /// <summary>
    /// The plugins which unlock these classes for an account while leveling up. Without deactivating
    /// them, the client would keep offering the classes in the character creation dialog.
    /// </summary>
    private static readonly Guid[] UnlockPlugInIds =
    [
        new("2DFFD751-7651-4FA1-93D1-9890CA0F04F1"), // UnlockSummonerAtLevel1
        new("2DFFD752-7652-4FA2-93D2-9890CA0F04F2"), // UnlockRageFighterAtLevel150
    ];

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.DisableSummonerAndRageFighter;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 13, 17, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var characterClass in gameConfiguration.CharacterClasses.Where(c => DisabledClassNumbers.Contains(c.Number)))
        {
            characterClass.CanGetCreated = false;
        }

        foreach (var plugInId in UnlockPlugInIds)
        {
            if (gameConfiguration.PlugInConfigurations.FirstOrDefault(p => p.TypeId == plugInId) is { } plugInConfiguration)
            {
                plugInConfiguration.IsActive = false;
            }
        }

        // Items which only these classes can use. Shared items (e.g. staffs of wizard and summoner)
        // keep dropping, since the other classes still need them.
        var exclusiveItems = gameConfiguration.Items
            .Where(item => item.QualifiedCharacters.Count > 0
                           && item.QualifiedCharacters.All(c => DisabledClassNumbers.Contains(c.Number)))
            .ToHashSet();

        foreach (var item in exclusiveItems)
        {
            item.DropsFromMonsters = false;
        }

        foreach (var dropItemGroup in GetAllDropItemGroups(gameConfiguration))
        {
            foreach (var item in dropItemGroup.PossibleItems.Where(exclusiveItems.Contains).ToList())
            {
                dropItemGroup.PossibleItems.Remove(item);
            }
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }

    private static IEnumerable<DropItemGroup> GetAllDropItemGroups(GameConfiguration gameConfiguration)
    {
        foreach (var dropItemGroup in gameConfiguration.DropItemGroups)
        {
            yield return dropItemGroup;
        }

        foreach (var monster in gameConfiguration.Monsters)
        {
            foreach (var dropItemGroup in monster.DropItemGroups)
            {
                yield return dropItemGroup;
            }
        }

        // Boxes (Box of Kundun, event boxes, ...) carry their loot table on the box item itself.
        foreach (var item in gameConfiguration.Items)
        {
            foreach (var dropItemGroup in item.DropItems)
            {
                yield return dropItemGroup;
            }
        }
    }
}
