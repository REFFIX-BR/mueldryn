// <copyright file="AddMudreamT5ToT8ArmorsAndSetsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Registers Mudream T5–T8 armor item definitions and ancient sets on existing Season 6 databases.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("C8F3A1B2-4D5E-6F70-8A91-B2C3D4E5F607")]
public class AddMudreamT5ToT8ArmorsAndSetsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add Mudream T5-T8 Armors And Sets";
    internal const string PlugInDescription = "Adds Mudream T5–T8 armor definitions (Ravager…Holyangel) and ancient sets (Dread…Azrion) so /item group=7 number=77 anc=1 works.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddMudreamT5ToT8ArmorsAndSets;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 10, 14, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        new MudreamT5ToT8ArmorsAndSets(context, gameConfiguration).Initialize();
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
