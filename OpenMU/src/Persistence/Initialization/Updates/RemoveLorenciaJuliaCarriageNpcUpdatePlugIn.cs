// <copyright file="RemoveLorenciaJuliaCarriageNpcUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Maps;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Removes Market Union Member Julia (547) from Lorencia (~139,138), where she stands
/// next to the decorative carriage / shirtless cart-puller prop. Julia remains in Loren Market.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("A7B8C9D0-E1F2-4031-9456-789ABCDEF132")]
public class RemoveLorenciaJuliaCarriageNpcUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Remove Lorencia Julia carriage NPC";
    internal const string PlugInDescription = "Removes Market Union Member Julia (547) spawn from Lorencia near the town carriage (~139,138).";

    private const short JuliaNpcNumber = 547;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.RemoveLorenciaJuliaCarriageNpc;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 24, 14, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var lorencia = gameConfiguration.Maps.FirstOrDefault(m => m.Number == Lorencia.Number);
        if (lorencia is null)
        {
            return;
        }

        var juliaSpawns = lorencia.MonsterSpawns
            .Where(s => s.MonsterDefinition?.Number == JuliaNpcNumber)
            .ToList();

        foreach (var spawn in juliaSpawns)
        {
            lorencia.MonsterSpawns.Remove(spawn);
            await context.DeleteAsync(spawn).ConfigureAwait(false);
        }
    }
}
