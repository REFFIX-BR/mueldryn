// <copyright file="EnableResetFeatureUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Resets;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Enables the classic character reset feature (level → reset count + bonus points)
/// with test/dev-friendly defaults. Chat commands <c>/reset</c> and <c>/resetinfo</c>
/// are already active; this turns on <see cref="ResetFeaturePlugIn"/>.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("D4E5F6A7-B8C9-4012-8345-6789ABCDEF01")]
public class EnableResetFeatureUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Enable character reset feature";
    internal const string PlugInDescription = "Activates classic character reset (/reset, /re): level 400+, level back to 1, +1500 points per reset (multiplied by reset count), no zen cost.";

    /// <summary>Type id of <see cref="ResetFeaturePlugIn"/>.</summary>
    private static readonly Guid ResetFeatureTypeId = typeof(ResetFeaturePlugIn).GUID;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.EnableResetFeature;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 24, 16, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var plugInConfiguration = gameConfiguration.PlugInConfigurations
            .FirstOrDefault(p => p.TypeId == ResetFeatureTypeId);
        if (plugInConfiguration is null)
        {
            plugInConfiguration = context.CreateNew<PlugInConfiguration>();
            plugInConfiguration.SetGuid(ResetFeatureTypeId);
            plugInConfiguration.TypeId = ResetFeatureTypeId;
            gameConfiguration.PlugInConfigurations.Add(plugInConfiguration);
        }

        plugInConfiguration.IsActive = true;

        // Dev/test defaults: free reset, back to level 1, OpenMU point formula otherwise.
        var resetConfig = new ResetFeaturePlugIn().CreateDefaultConfig();
        plugInConfiguration.SetConfiguration(resetConfig, referenceHandler: null);

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
