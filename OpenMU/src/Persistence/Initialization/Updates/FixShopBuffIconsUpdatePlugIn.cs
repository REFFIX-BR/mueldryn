// <copyright file="FixShopBuffIconsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Two buffs of the item shop still had an effect number which the client does not show as the
/// buff of the bought item: the Seal of Divinity used the number of the Seal of Mobility and the
/// Talisman of Mobility had no number of the client at all.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("B1E7C4A2-0D93-4F58-8A16-2E5D7C90B341")]
public class FixShopBuffIconsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Fix shop buff icons";
    internal const string PlugInDescription = "Gives the Seal of Divinity and the Talisman of Mobility the effect numbers which show their buff icon on the client.";

    /// <summary>
    /// The effect numbers of the client, taken from its buff table (BuffEffect.bmd).
    /// </summary>
    private static readonly (string EffectName, short Number)[] EffectNumbers =
    [
        ("Seal of Divinity", 88),
        ("Talisman of Mobility", 43), // Shown as the Seal of Mobility, which is the same kind of buff.
    ];

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixShopBuffIcons;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 13, 21, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var (effectName, number) in EffectNumbers)
        {
            if (gameConfiguration.MagicEffects.FirstOrDefault(effect => effect.Name == effectName) is not { } effectDefinition
                || effectDefinition.Number == number
                || gameConfiguration.MagicEffects.Any(effect => effect.Number == number))
            {
                continue;
            }

            effectDefinition.Number = number;
            EnsureTimerAttribute(context, gameConfiguration, effectDefinition);
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }

    private static void EnsureTimerAttribute(IContext context, GameConfiguration gameConfiguration, MagicEffectDefinition effectDefinition)
    {
        var id = ShopBuffService.GetTimerAttributeId(effectDefinition.Number);
        if (gameConfiguration.Attributes.Any(attribute => attribute.Id == id))
        {
            return;
        }

        var name = ShopBuffService.GetTimerAttributeName(effectDefinition.Name);
        gameConfiguration.Attributes.Add(context.CreateNew<AttributeDefinition>(id, name, name));
    }
}
