// <copyright file="AddMudreamT4AncientSetsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Registers Mudream T4 ancient sets on existing Season 6 databases.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("B7E2C914-6F5A-4D38-9C1E-8A0D4F2B7E31")]
public class AddMudreamT4AncientSetsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add Mudream T4 Ancient Sets";
    internal const string PlugInDescription = "Registers Umbral, Silent, Shattered, Luminous, Celestial, Blazing and Stormborn ancient sets for Ashcrow/Eclipse/Iris/Valiant/Glorious.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddMudreamT4AncientSets;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 09, 18, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        new MudreamT4AncientSets(context, gameConfiguration).Initialize();
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
