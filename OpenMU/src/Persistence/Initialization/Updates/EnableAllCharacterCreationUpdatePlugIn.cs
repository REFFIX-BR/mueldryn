// <copyright file="EnableAllCharacterCreationUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Re-enables Summoner and Rage Fighter creation after
/// <see cref="DisableSummonerAndRageFighterUpdatePlugIn"/>, and restores their unlock plug-ins.
/// Magic Gladiator / Dark Lord remain creatable server-side; the GameServer always sends
/// unlock flags so the client shows every Season 6 base class for new accounts.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("B1C2D3E4-F5A6-4789-9012-3456789ABCDE")]
public class EnableAllCharacterCreationUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Enable all character creation";
    internal const string PlugInDescription = "Re-enables Summoner and Rage Fighter character creation and restores their unlock plug-ins.";

    private static readonly byte[] PreviouslyDisabledCreatableClasses =
    [
        (byte)CharacterClassNumber.Summoner,
        (byte)CharacterClassNumber.RageFighter,
    ];

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
    public override UpdateVersion Version => UpdateVersion.EnableAllCharacterCreation;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 24, 15, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var characterClass in gameConfiguration.CharacterClasses.Where(c => PreviouslyDisabledCreatableClasses.Contains(c.Number)))
        {
            characterClass.CanGetCreated = true;
        }

        foreach (var plugInId in UnlockPlugInIds)
        {
            if (gameConfiguration.PlugInConfigurations.FirstOrDefault(p => p.TypeId == plugInId) is { } plugInConfiguration)
            {
                plugInConfiguration.IsActive = true;
            }
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
