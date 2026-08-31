// <copyright file="AddCashShopItemEffectsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.Skills;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Gives the items of the MU Item Shop their effect: the wearable ones (seals, jewellery, charms,
/// figurines, small wings) get a bonus while they are equipped, the others get a buff which is
/// applied when they are used.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("6F0A9C31-58E4-4B27-9D0C-4A1E5B2D7C08")]
public class AddCashShopItemEffectsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "Add cash shop item effects";
    internal const string PlugInDescription = "Adds the buffs of the shop seals, scrolls and auras and the bonuses of its jewellery, charms and small wings.";

    /// <summary>
    /// Effect numbers which the client does not know. Effects starting at
    /// <c>MagicEffectsList.InvisibleEffectStartIndex</c> (200) are never sent to the client, so they cannot
    /// make it show a wrong buff icon while they still work on the server side.
    /// </summary>
    private const short SilentEffectStart = 210;

    private static readonly BuffItem[] BuffItems =
    [
        // Seals which are not wearable in this client version.
        new(13, 62, "Seal of Healing", SilentEffectStart, 100, 60,
            [new(Stats.HealthRecoveryMultiplier, 2f, AggregateType.Multiplicate), new(Stats.ShieldRecoveryMultiplier, 2f, AggregateType.Multiplicate)]),
        new(13, 63, "Seal of Divinity", SilentEffectStart + 1, 101, 60,
            [new(Stats.DefenseBase, 1.15f, AggregateType.Multiplicate), new(Stats.DefenseRatePvm, 1.15f, AggregateType.Multiplicate)]),
        new(13, 93, "Master Seal of Ascension", SilentEffectStart + 2, 102, 60,
            [new(Stats.BonusExperienceRate, 0.5f, AggregateType.AddRaw)]),
        new(13, 94, "Master Seal of Wealth", SilentEffectStart + 3, 103, 60,
            [new(Stats.MoneyAmountRate, 1.5f, AggregateType.Multiplicate)]),

        // Boost auras.
        new(13, 104, "Max AG Boost Aura", SilentEffectStart + 4, 104, 60,
            [new(Stats.MaximumAbility, 1.3f, AggregateType.Multiplicate)]),
        new(13, 105, "Max SD Boost Aura", SilentEffectStart + 5, 105, 60,
            [new(Stats.MaximumShield, 1.3f, AggregateType.Multiplicate)]),

        new(13, 70, "Talisman of Mobility", SilentEffectStart + 6, 106, 30,
            [new(Stats.MovementSpeed, 2f, AggregateType.AddRaw)]),

        // Buff scrolls. These use the effect numbers of the original client, so the buff icon shows up.
        new(14, 72, "Scroll of Quickness", (short)MagicEffectNumber.ScrollOfQuickness, 110, 60,
            [new(Stats.AttackSpeedAny, 20f, AggregateType.AddRaw)]),
        new(14, 73, "Scroll of Defense", (short)MagicEffectNumber.ScrollOfDefense, 111, 60,
            [new(Stats.DefenseBase, 1.2f, AggregateType.Multiplicate)]),
        new(14, 74, "Scroll of Wrath", (short)MagicEffectNumber.ScrollOfWrath, 112, 60,
            [new(Stats.AttackDamageIncrease, 1.1f, AggregateType.Multiplicate)]),
        new(14, 75, "Scroll of Wizardry", (short)MagicEffectNumber.ScrollOfWizardry, 113, 60,
            [new(Stats.WizardryAttackDamageIncrease, 1.1f, AggregateType.Multiplicate)]),
        new(14, 76, "Scroll of Health", (short)MagicEffectNumber.ScrollOfHealth, 114, 60,
            [new(Stats.MaximumHealth, 1.15f, AggregateType.Multiplicate)]),
        new(14, 77, "Scroll of Mana", (short)MagicEffectNumber.ScrollOfMana, 115, 60,
            [new(Stats.MaximumMana, 1.15f, AggregateType.Multiplicate)]),
        new(14, 97, "Scroll of Battle", SilentEffectStart + 7, 116, 60,
            [new(Stats.AttackRatePvm, 1.2f, AggregateType.Multiplicate)]),
        new(14, 98, "Scroll of Strength", SilentEffectStart + 8, 117, 60,
            [new(Stats.MinimumPhysBaseDmg, 1.1f, AggregateType.Multiplicate), new(Stats.MaximumPhysBaseDmg, 1.1f, AggregateType.Multiplicate)]),
    ];

    private static readonly GearItem[] GearItems =
    [
        // Seals are worn in the ring slot in this client version.
        new(13, 43, [new(Stats.BonusExperienceRate, 0.3f, AggregateType.AddRaw)]),
        new(13, 44, [new(Stats.MoneyAmountRate, 1.3f, AggregateType.Multiplicate)]),
        new(13, 45, [
            new(Stats.HealthRecoveryMultiplier, 1.5f, AggregateType.Multiplicate),
            new(Stats.ManaRecoveryMultiplier, 1.5f, AggregateType.Multiplicate)]),

        // Jewellery.
        new(13, 107, [
            new(Stats.MinimumWizBaseDmg, 1.1f, AggregateType.Multiplicate),
            new(Stats.MaximumWizBaseDmg, 1.1f, AggregateType.Multiplicate),
            new(Stats.AttackSpeedAny, 10f, AggregateType.AddRaw)]),
        new(13, 109, [new(Stats.MaximumMana, 500f, AggregateType.AddRaw)]),
        new(13, 110, [new(Stats.MaximumHealth, 500f, AggregateType.AddRaw)]),
        new(13, 111, [new(Stats.MaximumShield, 500f, AggregateType.AddRaw)]),
        new(13, 112, [new(Stats.MaximumAbility, 500f, AggregateType.AddRaw)]),
        new(13, 113, [new(Stats.AttackDamageIncrease, 1.05f, AggregateType.Multiplicate)]),
        new(13, 114, [new(Stats.DefenseBase, 1.05f, AggregateType.Multiplicate)]),
        new(13, 115, [new(Stats.WizardryAttackDamageIncrease, 1.05f, AggregateType.Multiplicate)]),

        // Figurines and charms.
        new(13, 128, [new(Stats.AttackRatePvm, 1.1f, AggregateType.Multiplicate)]),
        new(13, 129, [new(Stats.DefenseRatePvm, 1.1f, AggregateType.Multiplicate)]),
        new(13, 130, [new(Stats.MoneyAmountRate, 1.1f, AggregateType.Multiplicate)]),
        new(13, 132, [new(Stats.MoneyAmountRate, 1.2f, AggregateType.Multiplicate)]),
        new(13, 134, [new(Stats.MovementSpeed, 2f, AggregateType.AddRaw)]),

        // Small wings, weaker than the first wings which drop in game.
        new(12, 131, [
            new(Stats.AttackDamageIncrease, 1.05f, AggregateType.Multiplicate),
            new(Stats.DamageReceiveDecrement, 0.95f, AggregateType.Multiplicate)]),
        new(12, 132, [
            new(Stats.AttackDamageIncrease, 1.05f, AggregateType.Multiplicate),
            new(Stats.DamageReceiveDecrement, 0.95f, AggregateType.Multiplicate)]),
        new(12, 133, [
            new(Stats.AttackDamageIncrease, 1.05f, AggregateType.Multiplicate),
            new(Stats.DamageReceiveDecrement, 0.95f, AggregateType.Multiplicate)]),
        new(12, 134, [
            new(Stats.AttackDamageIncrease, 1.05f, AggregateType.Multiplicate),
            new(Stats.DamageReceiveDecrement, 0.95f, AggregateType.Multiplicate)]),
        new(12, 135, [
            new(Stats.AttackDamageIncrease, 1.05f, AggregateType.Multiplicate),
            new(Stats.DamageReceiveDecrement, 0.95f, AggregateType.Multiplicate)]),
    ];

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddCashShopItemEffects;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 13, 14, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var buffItem in BuffItems)
        {
            if (FindItem(gameConfiguration, buffItem.Group, buffItem.Number) is not { } definition
                || definition.ConsumeEffect is not null)
            {
                continue;
            }

            definition.ConsumeEffect = CreateEffect(context, gameConfiguration, buffItem);
        }

        foreach (var gearItem in GearItems)
        {
            if (FindItem(gameConfiguration, gearItem.Group, gearItem.Number) is not { } definition
                || definition.BasePowerUpAttributes.Count > 0)
            {
                continue;
            }

            foreach (var boost in gearItem.Boosts)
            {
                var powerUp = context.CreateNew<ItemBasePowerUpDefinition>();
                powerUp.TargetAttribute = boost.Attribute.GetPersistent(gameConfiguration);
                powerUp.BaseValue = boost.Value;
                powerUp.AggregateType = boost.Aggregate;
                definition.BasePowerUpAttributes.Add(powerUp);
            }
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }

    private static ItemDefinition? FindItem(GameConfiguration gameConfiguration, byte group, short number)
    {
        return gameConfiguration.Items.FirstOrDefault(item => item.Group == group && item.Number == number);
    }

    private static MagicEffectDefinition CreateEffect(IContext context, GameConfiguration gameConfiguration, BuffItem buffItem)
    {
        var effect = context.CreateNew<MagicEffectDefinition>();
        gameConfiguration.MagicEffects.Add(effect);
        effect.Name = buffItem.Name;
        effect.Number = buffItem.EffectNumber;
        effect.SubType = buffItem.SubType;
        effect.InformObservers = false;
        effect.SendDuration = true;
        effect.StopByDeath = false;
        effect.Duration = context.CreateNew<PowerUpDefinitionValue>();
        effect.Duration.ConstantValue.Value = (float)TimeSpan.FromMinutes(buffItem.DurationMinutes).TotalSeconds;

        foreach (var boost in buffItem.Boosts)
        {
            var powerUp = context.CreateNew<PowerUpDefinition>();
            effect.PowerUpDefinitions.Add(powerUp);
            powerUp.TargetAttribute = boost.Attribute.GetPersistent(gameConfiguration);
            powerUp.Boost = context.CreateNew<PowerUpDefinitionValue>();
            powerUp.Boost.ConstantValue.Value = boost.Value;
            powerUp.Boost.ConstantValue.AggregateType = boost.Aggregate;
        }

        return effect;
    }

    /// <summary>
    /// One bonus of an item or effect.
    /// </summary>
    /// <param name="Attribute">The boosted attribute.</param>
    /// <param name="Value">The value of the boost.</param>
    /// <param name="Aggregate">How the value is applied to the attribute.</param>
    private sealed record Boost(AttributeDefinition Attribute, float Value, AggregateType Aggregate);

    /// <summary>
    /// An item which applies a buff when it is used.
    /// </summary>
    /// <param name="Group">Item group.</param>
    /// <param name="Number">Item number inside the group.</param>
    /// <param name="Name">Name of the created effect.</param>
    /// <param name="EffectNumber">Effect number, which the client uses to show the buff icon.</param>
    /// <param name="SubType">Effect sub type; effects of the same sub type replace each other.</param>
    /// <param name="DurationMinutes">Duration when the item has no period; period items last until they expire.</param>
    /// <param name="Boosts">The boosts of the buff.</param>
    private sealed record BuffItem(byte Group, short Number, string Name, short EffectNumber, byte SubType, int DurationMinutes, Boost[] Boosts);

    /// <summary>
    /// An item which gives its bonus while it is equipped.
    /// </summary>
    /// <param name="Group">Item group.</param>
    /// <param name="Number">Item number inside the group.</param>
    /// <param name="Boosts">The boosts of the item.</param>
    private sealed record GearItem(byte Group, short Number, Boost[] Boosts);
}
