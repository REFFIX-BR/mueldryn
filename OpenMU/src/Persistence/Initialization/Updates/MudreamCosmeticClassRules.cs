// <copyright file="MudreamCosmeticClassRules.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;

/// <summary>
/// Vanilla MU class eligibility for Mudream cosmetic weapons/armor (matches MuMain RequireClass).
/// </summary>
internal static class MudreamCosmeticClassRules
{
    internal static void ApplyQualifiedCharacters(ItemDefinition definition, GameConfiguration gameConfiguration, byte group, string name)
    {
        definition.QualifiedCharacters.Clear();
        var (wizard, knight, elf, magicGladiator, darkLord, summoner, ragefighter) = GetClassLevels(group, name);
        foreach (var characterClass in gameConfiguration.DetermineCharacterClasses(
                     wizard, knight, elf, magicGladiator, darkLord, summoner, ragefighter))
        {
            definition.QualifiedCharacters.Add(characterClass);
        }
    }

    private static (int Wizard, int Knight, int Elf, int MagicGladiator, int DarkLord, int Summoner, int Ragefighter)
        GetClassLevels(byte group, string name)
    {
        const int yes = 1;
        const int no = 0;
        name ??= string.Empty;

        bool Has(string token) => name.Contains(token, StringComparison.OrdinalIgnoreCase);

        return group switch
        {
            4 => (no, no, yes, no, no, no, no), // bow / crossbow — ELF
            5 => (yes, no, no, yes, no, yes, no), // staff — DW / MG / SUM
            2 => (no, no, no, no, yes, no, no), // scepter — DL
            3 or 1 or 6 => (no, yes, no, yes, yes, no, yes), // spear / axe / shield
            7 or 8 or 9 or 10 or 11 or 12 or 13 => (yes, yes, yes, yes, yes, yes, yes), // armor / wings / pets
            0 when Has("Claw") => (no, no, no, no, no, no, yes),
            0 when Has("Dagger") => (yes, no, no, yes, no, yes, no),
            0 when Has("Two-Hand") || Has("Two Hand") => (no, yes, no, yes, yes, no, yes),
            0 when Has("Spear") => (no, yes, no, yes, yes, no, yes),
            0 => (yes, yes, no, yes, yes, no, yes), // 1H sword / blade / hammer
            _ => (yes, yes, yes, yes, yes, yes, yes),
        };
    }
}
