// <copyright file="MakeShopBuffsDirectUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The seals of the item shop were worn in the ring slot, which was wrong: they are buffs. They are
/// turned into buff items here, the effects of the other shop buffs get the effect numbers of the
/// original client so their icon shows up, and every buff gets the attribute which keeps its
/// remaining time over a logout.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("2C9E4A18-7B63-4D05-9F41-A0D3E58B7C12")]
public class MakeShopBuffsDirectUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Make shop buffs direct";
    internal const string PlugInDescription = "Turns the shop seals into buffs instead of rings and keeps the remaining time of all shop buffs on the character.";

    private const byte HelperGroup = 13;

    private static readonly SealItem[] Seals =
    [
        new(43, "Seal of Ascension", 40, 118, [new(Stats.BonusExperienceRate, 0.3f, AggregateType.AddRaw)]),
        new(44, "Seal of Wealth", 41, 119, [new(Stats.MoneyAmountRate, 1.3f, AggregateType.Multiplicate)]),
        new(45, "Seal of Sustenance", 42, 120, [
            new(Stats.HealthRecoveryMultiplier, 1.5f, AggregateType.Multiplicate),
            new(Stats.ManaRecoveryMultiplier, 1.5f, AggregateType.Multiplicate)]),
    ];

    /// <summary>
    /// The effect numbers of the original client, so the client shows the buff icon of the item.
    /// </summary>
    private static readonly (string EffectName, short Number)[] ClientEffectNumbers =
    [
        ("Seal of Healing", 87),
        ("Seal of Divinity", 43),
        ("Master Seal of Ascension", 101),
        ("Master Seal of Wealth", 102),
        ("Max AG Boost Aura", 113),
        ("Max SD Boost Aura", 114),
        ("Scroll of Battle", 89),
        ("Scroll of Strength", 90),
    ];

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.MakeShopBuffsDirect;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 13, 18, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var seal in Seals)
        {
            if (gameConfiguration.Items.FirstOrDefault(item => item.Group == HelperGroup && item.Number == seal.Number) is not { } definition)
            {
                continue;
            }

            definition.ItemSlot = null;
            definition.Durability = 1;
            definition.BasePowerUpAttributes.Clear();
            definition.ConsumeEffect ??= CreateEffect(context, gameConfiguration, seal);
        }

        foreach (var (effectName, number) in ClientEffectNumbers)
        {
            if (gameConfiguration.MagicEffects.FirstOrDefault(effect => effect.Name == effectName) is { } effectDefinition
                && gameConfiguration.MagicEffects.All(effect => effect.Number != number))
            {
                effectDefinition.Number = number;
            }
        }

        var buffEffects = gameConfiguration.Items
            .Select(item => item.ConsumeEffect)
            .Where(effect => effect is not null)
            .Distinct()
            .ToList();
        foreach (var effect in buffEffects)
        {
            EnsureTimerAttribute(context, gameConfiguration, effect!);
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

    private static MagicEffectDefinition CreateEffect(IContext context, GameConfiguration gameConfiguration, SealItem seal)
    {
        var effect = context.CreateNew<MagicEffectDefinition>();
        gameConfiguration.MagicEffects.Add(effect);
        effect.Name = seal.Name;
        effect.Number = seal.EffectNumber;
        effect.SubType = seal.SubType;
        effect.InformObservers = false;
        effect.SendDuration = true;
        effect.StopByDeath = false;
        effect.Duration = context.CreateNew<PowerUpDefinitionValue>();
        effect.Duration.ConstantValue.Value = (float)TimeSpan.FromHours(1).TotalSeconds;

        foreach (var (attribute, value, aggregateType) in seal.Boosts)
        {
            var powerUp = context.CreateNew<PowerUpDefinition>();
            effect.PowerUpDefinitions.Add(powerUp);
            powerUp.TargetAttribute = attribute.GetPersistent(gameConfiguration);
            powerUp.Boost = context.CreateNew<PowerUpDefinitionValue>();
            powerUp.Boost.ConstantValue.Value = value;
            powerUp.Boost.ConstantValue.AggregateType = aggregateType;
        }

        return effect;
    }

    /// <summary>
    /// A seal which is turned from a ring into a buff item.
    /// </summary>
    /// <param name="Number">Item number in the helper group.</param>
    /// <param name="Name">Name of the created effect.</param>
    /// <param name="EffectNumber">Effect number, which the client uses to show the buff icon.</param>
    /// <param name="SubType">Effect sub type; effects of the same sub type replace each other.</param>
    /// <param name="Boosts">The boosts of the buff.</param>
    private sealed record SealItem(short Number, string Name, short EffectNumber, byte SubType, (AttributeDefinition Attribute, float Value, AggregateType AggregateType)[] Boosts);
}
