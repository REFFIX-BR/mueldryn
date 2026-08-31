// <copyright file="FixMudreamCosmeticClassRestrictionsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Restricts Mudream cosmetic items to the correct character classes (bow→ELF, scepter→DL, claw→RF, …).
/// First import allowed every class — DL could equip bows, etc.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("C7A1E4F2-6B8D-4C9E-A1F3-2D4E5F6A7B8C")]
public class FixMudreamCosmeticClassRestrictionsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Fix Mudream cosmetic class restrictions";
    internal const string PlugInDescription = "Applies vanilla class rules to Mudream ITEM VISUAL skins (QualifiedCharacters).";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixMudreamCosmeticClassRestrictions;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 27, 5, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var item in MudreamCosmeticItemCatalog.Items)
        {
            var definition = gameConfiguration.Items.FirstOrDefault(i => i.Group == item.Group && i.Number == item.Number);
            if (definition is null)
            {
                continue;
            }

            MudreamCosmeticClassRules.ApplyQualifiedCharacters(definition, gameConfiguration, item.Group, item.Name);
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
