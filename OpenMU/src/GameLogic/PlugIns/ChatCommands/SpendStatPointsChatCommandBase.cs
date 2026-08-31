// <copyright file="SpendStatPointsChatCommandBase.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Globalization;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.PlayerActions.Character;

/// <summary>
/// Shared logic for short chat commands that spend free <see cref="Character.LevelUpPoints"/>
/// on a single attribute (e.g. <c>/f</c>, <c>/a</c>).
/// </summary>
public abstract class SpendStatPointsChatCommandBase : IChatCommandPlugIn
{
    private readonly IncreaseStatsAction _action = new();
    private readonly AttributeDefinition _attribute;
    private readonly string _statDisplayName;
    private readonly string _usageHint;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpendStatPointsChatCommandBase"/> class.
    /// </summary>
    /// <param name="attribute">The base attribute to increase.</param>
    /// <param name="statDisplayName">Portuguese display name for chat feedback.</param>
    /// <param name="usageHint">Usage string shown on invalid amount.</param>
    protected SpendStatPointsChatCommandBase(AttributeDefinition attribute, string statDisplayName, string usageHint)
    {
        this._attribute = attribute;
        this._statDisplayName = statDisplayName;
        this._usageHint = usageHint;
    }

    /// <inheritdoc />
    public abstract string Key { get; }

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        if (player.SelectedCharacter is null || player.Attributes is null)
        {
            return;
        }

        if (player.CurrentMap is null)
        {
            return;
        }

        if (!player.IsAlive)
        {
            await player.ShowBlueMessageAsync("Você está morto.").ConfigureAwait(false);
            return;
        }

        if (!TryParseAmount(command, out var amount))
        {
            await player.ShowBlueMessageAsync(this._usageHint).ConfigureAwait(false);
            return;
        }

        var selectedCharacter = player.SelectedCharacter;
        if (!selectedCharacter.CanIncreaseStats(amount))
        {
            await player.ShowBlueMessageAsync("Pontos insuficientes.").ConfigureAwait(false);
            return;
        }

        var attributeDef = selectedCharacter.CharacterClass?.GetStatAttribute(this._attribute);
        if (attributeDef is not { IncreasableByPlayer: true })
        {
            await player.ShowBlueMessageAsync($"Sua classe não possui {this._statDisplayName}.").ConfigureAwait(false);
            return;
        }

        if (player.CurrentMiniGame is not null && amount > 1)
        {
            await player.ShowBlueMessageAsync("Não é permitido adicionar vários pontos durante um mini-game.").ConfigureAwait(false);
            return;
        }

        var valueBefore = player.Attributes[this._attribute];
        await this._action.IncreaseStatsAsync(player, this._attribute, amount).ConfigureAwait(false);
        var added = (int)(player.Attributes[this._attribute] - valueBefore);
        if (added > 0)
        {
            await player.ShowBlueMessageAsync($"{this._statDisplayName} +{added}").ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Parses an optional amount; missing amount means 1.
    /// </summary>
    private static bool TryParseAmount(string command, out ushort amount)
    {
        amount = 1;
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return true;
        }

        if (!ushort.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out amount) || amount < 1)
        {
            amount = 0;
            return false;
        }

        return true;
    }
}
