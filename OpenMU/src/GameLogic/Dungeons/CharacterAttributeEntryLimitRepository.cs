// <copyright file="CharacterAttributeEntryLimitRepository.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Dungeons;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Persists <see cref="EntryLimit"/> on the character using existing stat attributes.
/// </summary>
public sealed class CharacterAttributeEntryLimitRepository : IEntryLimitRepository
{
    private readonly Player _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="CharacterAttributeEntryLimitRepository"/> class.
    /// </summary>
    public CharacterAttributeEntryLimitRepository(Player player)
    {
        this._player = player;
    }

    /// <inheritdoc />
    public ValueTask<EntryLimit> GetOrCreateAsync(Character character)
    {
        this.EnsureAttributeDefinitions();
        var dayNumber = (int)this.GetAttributeValue(Stats.DungeonEntryDateAttribute);
        var consumed = (int)this.GetAttributeValue(Stats.DungeonEntriesConsumedAttribute);
        var lastReset = dayNumber > 0
            ? DateOnly.FromDayNumber(dayNumber)
            : default;
        return ValueTask.FromResult(EntryLimit.FromPersisted(lastReset, consumed));
    }

    /// <inheritdoc />
    public ValueTask SaveAsync(Character character, EntryLimit limit)
    {
        this.EnsureAttributeDefinitions();
        this.GetOrCreateStat(Stats.DungeonEntryDateAttribute).Value = limit.LastResetDate == default
            ? 0
            : limit.LastResetDate.DayNumber;
        this.GetOrCreateStat(Stats.DungeonEntriesConsumedAttribute).Value = limit.EntriesConsumed;
        return ValueTask.CompletedTask;
    }

    private void EnsureAttributeDefinitions()
    {
        this.EnsureDefinition(Stats.DungeonEntryDateAttribute);
        this.EnsureDefinition(Stats.DungeonEntriesConsumedAttribute);
    }

    private void EnsureDefinition(AttributeDefinition template)
    {
        var config = this._player.GameContext.Configuration;
        if (config.Attributes.Any(a => a.Id == template.Id))
        {
            return;
        }

        try
        {
            var persistent = this._player.PersistenceContext.CreateNew<AttributeDefinition>(
                template.Id,
                template.Designation,
                template.Description);
            config.Attributes.Add(persistent);
        }
        catch (InvalidOperationException)
        {
            config.Attributes.Add(template);
        }
    }

    private float GetAttributeValue(AttributeDefinition template)
    {
        var attr = this._player.SelectedCharacter?.Attributes
            .FirstOrDefault(a => a.Definition is not null && a.Definition.Id == template.Id);
        return attr?.Value ?? 0f;
    }

    private StatAttribute GetOrCreateStat(AttributeDefinition template)
    {
        var character = this._player.SelectedCharacter
            ?? throw new InvalidOperationException("No character selected.");
        var existing = character.Attributes
            .FirstOrDefault(a => a.Definition is not null && a.Definition.Id == template.Id);
        if (existing is not null)
        {
            return existing;
        }

        var definition = this._player.GameContext.Configuration.Attributes.First(a => a.Id == template.Id);
        var created = this._player.PersistenceContext.CreateNew<StatAttribute>(definition, 0);
        character.Attributes.Add(created);
        return created;
    }
}
