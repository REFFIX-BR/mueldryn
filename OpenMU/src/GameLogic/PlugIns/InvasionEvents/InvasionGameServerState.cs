// <copyright file="InvasionGameServerState.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;

using System.Collections.Concurrent;
using System.Linq;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

/// <summary>
/// Invasion state that is created for every periodic invasion run.
/// </summary>
public class InvasionGameServerState : PeriodicTaskGameServerState
{
    private readonly HashSet<ushort> _mapIds = [];
    private readonly Dictionary<ushort, ushort> _selectedMaps = [];
    private readonly List<ushort> _announcedMapIds = [];
    private readonly ConcurrentDictionary<Monster, byte> _monsters = new();
    private readonly ConcurrentDictionary<ushort, int> _plannedTotals = new();
    private readonly ConcurrentDictionary<ushort, string> _monsterNames = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InvasionGameServerState"/> class.
    /// </summary>
    /// <param name="context">The game context.</param>
    public InvasionGameServerState(IGameContext context)
        : base(context)
    {
    }

    /// <summary>
    /// Gets a value indicating whether this invasion currently has tracked monsters or planned totals.
    /// </summary>
    public bool HasMonsterStatus => !this._plannedTotals.IsEmpty || !this._monsters.IsEmpty;

    /// <summary>
    /// Gets or sets the map identifier used for UI display / map-event state broadcasts.
    /// <c>null</c> means no event is active or the display map has not been selected yet.
    /// </summary>
    public ushort? MapId { get; set; }

    /// <summary>
    /// Gets the set of map identifiers on which monsters will spawn this run.
    /// </summary>
    public IReadOnlySet<ushort> MapIds => this._mapIds;

    /// <summary>
    /// Gets the read-only mapping of monster ID to selected map identifier.
    /// Populated for spawns whose <see cref="SpawnMapStrategy"/> is <see cref="SpawnMapStrategy.RandomMap"/>.
    /// </summary>
    public IReadOnlyDictionary<ushort, ushort> SelectedMaps => this._selectedMaps;

    /// <summary>
    /// Gets the list of map identifiers that are named in the invasion start/end broadcast.
    /// These are the maps on which the announced (featured) monster actually spawns this run,
    /// so the message always points players at a map that really contains it.
    /// </summary>
    public IReadOnlyList<ushort> AnnouncedMapIds => this._announcedMapIds;

    /// <summary>
    /// Sets the maps named in the broadcast message and the single <see cref="MapId"/> used
    /// for the map-event UI state.
    /// </summary>
    /// <param name="mapIds">The maps on which the announced monster spawns this run.</param>
    internal void SetAnnouncedMaps(IReadOnlyCollection<ushort> mapIds)
    {
        this._announcedMapIds.Clear();
        this._announcedMapIds.AddRange(mapIds);
        this.MapId = this._announcedMapIds.Count > 0 ? this._announcedMapIds[0] : null;
    }

    /// <summary>
    /// Registers a map as active for this run and optionally records which map was
    /// randomly chosen for a particular monster type.
    /// </summary>
    /// <param name="mapId">The map identifier to register.</param>
    /// <param name="monsterId">
    /// When provided, records the <paramref name="mapId"/> as the chosen map for this monster.
    /// Pass <c>null</c> for "spawn-on-all-maps" entries.
    /// </param>
    internal void RegisterMap(ushort mapId, ushort? monsterId = null)
    {
        this._mapIds.Add(mapId);
        if (monsterId.HasValue)
        {
            this._selectedMaps[monsterId.Value] = mapId;
        }
    }

    /// <summary>
    /// Clears all state accumulated from a previous run so the object can be reused.
    /// </summary>
    internal void Reset()
    {
        this.MapId = null;
        this._mapIds.Clear();
        this._selectedMaps.Clear();
        this._announcedMapIds.Clear();
        this._plannedTotals.Clear();
        this._monsterNames.Clear();
    }

    /// <summary>
    /// Records how many monsters of a type were planned for this run (alive/total denominator).
    /// </summary>
    internal void AddPlannedSpawn(ushort monsterId, string monsterName, int count)
    {
        if (count <= 0)
        {
            return;
        }

        this._plannedTotals.AddOrUpdate(monsterId, count, (_, existing) => existing + count);
        this._monsterNames[monsterId] = monsterName;
    }

    /// <summary>
    /// Tracks a monster spawned by this invasion and handles its cleanup on death.
    /// </summary>
    /// <param name="monster">The monster to track.</param>
    internal void AddMonster(Monster monster)
    {
        if (this._monsters.TryAdd(monster, 0))
        {
            monster.Died += this.OnMonsterDied;
            var id = (ushort)monster.Definition.Number;
            this._monsterNames.TryAdd(id, monster.Definition.Designation.ToString() ?? $"Monster {id}");
        }
    }

    /// <summary>
    /// Builds alive/total rows for the invasion status UI.
    /// </summary>
    internal IReadOnlyList<(string MonsterName, int Alive, int Total)> GetMonsterStatusRows()
    {
        var aliveById = new Dictionary<ushort, int>();
        foreach (var monster in this._monsters.Keys)
        {
            if (monster.IsDisposed)
            {
                continue;
            }

            var id = (ushort)monster.Definition.Number;
            aliveById[id] = aliveById.GetValueOrDefault(id) + 1;
        }

        var ids = this._plannedTotals.Keys
            .Concat(aliveById.Keys)
            .Distinct()
            .OrderBy(id => id);

        var rows = new List<(string, int, int)>();
        foreach (var id in ids)
        {
            var total = this._plannedTotals.GetValueOrDefault(id);
            var alive = aliveById.GetValueOrDefault(id);
            if (total <= 0)
            {
                total = alive;
            }

            var name = this._monsterNames.GetValueOrDefault(id, $"Monster {id}");
            rows.Add((name, alive, total));
        }

        return rows;
    }

    /// <summary>
    /// Despawns and disposes all active monsters tracked by this invasion state.
    /// </summary>
    internal async ValueTask CleanUpMonstersAsync()
    {
        if (this._monsters.Count == 0)
        {
            return;
        }

        var monsters = this._monsters.Keys.ToArray();
        var tasks = monsters.Select(async monster =>
        {
            this._monsters.TryRemove(monster, out _);
            monster.Died -= this.OnMonsterDied;

            if (!monster.IsDisposed)
            {
                await monster.CurrentMap.RemoveAsync(monster).ConfigureAwait(false);
                monster.Dispose();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private void OnMonsterDied(object? sender, DeathInformation e)
    {
        if (sender is Monster monster)
        {
            this._monsters.TryRemove(monster, out _);
            monster.Died -= this.OnMonsterDied;
        }
    }
}