// <copyright file="EnableReaddAndClearInvUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.GameLogic.Resets;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Enables /readd (stat redistribute, 5kk zen) and /clearinv for normal players,
/// and restores Fortress walkmesh after the Mudream World82 remap experiment.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("A41F8C2E-9D57-4B13-8E06-5C7A2F91D0B4")]
public class EnableReaddAndClearInvUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Enable /readd and /clearinv";
    internal const string PlugInDescription =
        "Activates Stat Reset (/readd, 5.000.000 zen) and Clear Inventory (/clearinv). Also reloads Fortress terrain 69–72 to the previous walkmesh.";

    private static readonly Guid StatResetFeatureTypeId = typeof(StatResetFeaturePlugIn).GUID;
    private static readonly Guid ClearInventoryTypeId = typeof(ClearInventoryChatCommandPlugIn).GUID;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.EnableReaddAndClearInv;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 09, 11, 19, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        this.EnableStatReset(context, gameConfiguration);
        this.EnableClearInventory(context, gameConfiguration);

        foreach (var mapNumber in new short[] { 69, 70, 71, 72 })
        {
            gameConfiguration.Maps.FirstOrDefault(m => m.Number == mapNumber)?.UpdateTerrainFromResources();
        }

        return ValueTask.CompletedTask;
    }

    private void EnableStatReset(IContext context, GameConfiguration gameConfiguration)
    {
        var plugInConfiguration = gameConfiguration.PlugInConfigurations
            .FirstOrDefault(p => p.TypeId == StatResetFeatureTypeId);
        if (plugInConfiguration is null)
        {
            plugInConfiguration = context.CreateNew<PlugInConfiguration>();
            plugInConfiguration.SetGuid(StatResetFeatureTypeId);
            plugInConfiguration.TypeId = StatResetFeatureTypeId;
            gameConfiguration.PlugInConfigurations.Add(plugInConfiguration);
        }

        plugInConfiguration.IsActive = true;
        var config = new StatResetConfiguration
        {
            RequiredLevel = 1,
            RequiredMoney = 5_000_000,
            ChatCommandEnabled = true,
            MoveHome = false,
            LogOut = false,
        };
        plugInConfiguration.SetConfiguration(config, referenceHandler: null);
    }

    private void EnableClearInventory(IContext context, GameConfiguration gameConfiguration)
    {
        var plugInConfiguration = gameConfiguration.PlugInConfigurations
            .FirstOrDefault(p => p.TypeId == ClearInventoryTypeId);
        if (plugInConfiguration is null)
        {
            plugInConfiguration = context.CreateNew<PlugInConfiguration>();
            plugInConfiguration.SetGuid(ClearInventoryTypeId);
            plugInConfiguration.TypeId = ClearInventoryTypeId;
            gameConfiguration.PlugInConfigurations.Add(plugInConfiguration);
        }

        plugInConfiguration.IsActive = true;
        var config = new ClearInventoryChatCommandPlugIn.ClearInventoryConfiguration
        {
            MoneyCost = 0,
            RequireConfirmation = true,
            ConfirmationMessage = "Confirmacao: digite /clearinv de novo em 10s para limpar o inventario.",
            NotEnoughMoneyMessage = "Zen insuficiente.",
            InventoryClearedMessage = "Inventario limpo.",
        };
        plugInConfiguration.SetConfiguration(config, referenceHandler: null);
    }
}
