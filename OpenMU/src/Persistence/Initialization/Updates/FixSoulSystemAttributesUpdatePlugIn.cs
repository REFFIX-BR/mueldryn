// <copyright file="FixSoulSystemAttributesUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.SoulSystem;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Ensures Soul Points Remaining exists and renames legacy "Soul Element N" alloc rows.
/// Update 122 created c0de5000 alloc definitions; Remaining (B1C2D3E4-2000) was missing.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("D0E1F2A3-3647-4F80-9A1B-2C3D4E5F6071")]
public class FixSoulSystemAttributesUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Fix Soul System attributes";
    internal const string PlugInDescription = "Adds Soul Points Remaining and aligns Soul Element 0..15 designations with Soul Alloc names.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixSoulSystemAttributes;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 22, 17, 40, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        Ensure(context, gameConfiguration, SoulSystemCatalog.RemainingAttribute);
        foreach (var attr in SoulSystemCatalog.AllocAttributes)
        {
            Ensure(context, gameConfiguration, attr);
        }

        return ValueTask.CompletedTask;
    }

    private static void Ensure(IContext context, GameConfiguration gameConfiguration, AttributeDefinition attribute)
    {
        var existing = gameConfiguration.Attributes.FirstOrDefault(a => a.Id == attribute.Id);
        if (existing is not null)
        {
            existing.Designation = attribute.Designation;
            existing.Description = attribute.Description;
            existing.MaximumValue = null;
            return;
        }

        var persistent = context.CreateNew<AttributeDefinition>(attribute.Id, attribute.Designation, attribute.Description);
        persistent.MaximumValue = null;
        gameConfiguration.Attributes.Add(persistent);
    }
}
