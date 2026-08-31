// <copyright file="DungeonContext.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Dungeons;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence;
using Microsoft.Extensions.Logging;
using Pathfinding = MUnique.OpenMU.Pathfinding;

/// <summary>
/// The context for a Fortress of Imperial Dungeon instance.
/// Manages the lifecycle of a three-room progressive dungeon with scaling, HUD updates,
/// respawn at checkpoints, and individual reward delivery.
/// </summary>
/// <remarks>
/// Implements Requirements 1 (Instancing), 3 (Room Progression), 4 (Scaling), 7 (Respawn), and 8 (Rewards).
/// Key features as per task 8.1:
/// - Fields: _currentRoom, _rewardDelivered HashSet, _remainingTime, _hudCts
/// - OnGameStartAsync: calc HP/dmg multipliers, log error if N∉[1,5], start HUD loop
/// - RunHudUpdateLoopAsync: every 1s send packet 0x14 to all players
/// - OnMonsterDied: count kills, advance room when cleared
/// - AdvanceToNextRoomAsync: wait 3s, advance room, spawn wave
/// - OnPlayerDied: wait 3s, restore HP, teleport to checkpoint
/// - GameEndedAsync: deliver rewards if boss killed, cancel HUD
/// - DeliverRewardChestsAsync: one chest per player, check _rewardDelivered, drop if inventory full
/// </remarks>
public sealed class DungeonContext : MiniGameContext
{
    private static readonly HashSet<short> GateNpcNumbers = [524, 525, 526, 527, 528];

    private static readonly (byte X, byte Y)[] AllGateMarkers =
    [
        (194, 25),
        (234, 28),
        (216, 80),
        (180, 79),
        (154, 53),
        (166, 26),
        (233, 55),
        (217, 72),
        (218, 85),
        (152, 30),
        (157, 30),
        (174, 99),
        (186, 100),
    ];

    private readonly IGameContext _gameContext;
    private readonly object _waveSync = new();
    private readonly HashSet<string> _rewardDelivered = new();
    private readonly HashSet<ushort> _currentWaveMonsterIds = new();
    private TimeSpan _remainingTime;
    private TimeSpan _intermissionRemaining;
    private CancellationTokenSource? _hudCts;
    private double _hpMultiplier = 1;
    private double _damageMultiplier = 1;
    private double _defenseMultiplier = 1;
    private float _attackRateBonus;
    private bool _bossKilled;
    private bool _isAdvancingWave;
    private int _currentWave = 1;
    private int _waveRemaining;
    private int _participantCount = 1;
    private TimeSpan _lootRemaining = TimeSpan.Zero;
    private bool _completing;

    /// <summary>
    /// Initializes a new instance of the <see cref="DungeonContext"/> class.
    /// </summary>
    public DungeonContext(
        MiniGameMapKey key,
        MiniGameDefinition definition,
        IGameContext gameContext,
        IMapInitializer mapInitializer)
        : base(key, definition, gameContext, mapInitializer)
    {
        this._gameContext = gameContext;
    }

    /// <inheritdoc />
    public override ExitGate? GetPlayerRespawnGate()
    {
        return new ExitGate
        {
            Map = this.Map.Definition,
            X1 = DungeonWaveCatalog.ArenaX,
            Y1 = DungeonWaveCatalog.ArenaY,
            X2 = DungeonWaveCatalog.ArenaX,
            Y2 = DungeonWaveCatalog.ArenaY,
            Direction = Direction.South,
        };
    }

    /// <inheritdoc />
    protected override bool ShouldDetachPlayerOnMapRemove(Player player)
        => player.PlayerState.CurrentState.IsDisconnectedOrFinished() || player.IsAlive;

    /// <summary>
    /// Removes the player from the dungeon instance and warps them to Lorencia.
    /// </summary>
    public async ValueTask TryLeaveAsync(Player player)
    {
        if (!ReferenceEquals(player.CurrentMiniGame, this))
        {
            return;
        }

        this.Logger.LogInformation(
            "Dungeon {Key}: Player {Player} left the dungeon.",
            this.Key,
            player.Name);
        await this.DetachAndWarpOutAsync(player).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async ValueTask WarpPlayerOutAsync(Player player)
    {
        var lorencia = await this._gameContext.GetMapAsync(0).ConfigureAwait(false);
        var gate = lorencia?.SafeZoneSpawnGate
            ?? this._gameContext.Configuration.Maps.FirstOrDefault(m => m.Number == 0)?.ExitGates.FirstOrDefault(g => g.IsSpawnGate)
            ?? this._gameContext.Configuration.Maps.FirstOrDefault(m => m.Number == 0)?.ExitGates.FirstOrDefault();
        if (gate is not null)
        {
            await player.WarpToAsync(gate).ConfigureAwait(false);
            return;
        }

        await player.WarpToSafezoneAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override TimeSpan CountdownDuration => TimeSpan.FromSeconds(3);

    /// <inheritdoc />
    protected override TimeSpan GameDuration => TimeSpan.FromMinutes(30);

    /// <inheritdoc />
    protected override bool ShouldInitializeEventStartNpcs => false;

    /// <inheritdoc />
    protected override bool ShouldRunDefinitionSpawnWaves => false;

    /// <summary>
    /// Called when the game starts. Computes scaling multipliers, applies them to spawned monsters,
    /// and starts the HUD update loop.
    /// </summary>
    /// <param name="players">The players starting the game.</param>
    /// <remarks>
    /// Implements Requirements 4.1, 4.2 (Scaling), 4.4 (N validation), 3.6 (HUD updates).
    /// Task 8.1: calc HP/dmg multipliers, log error if N∉[1,5], start HUD loop.
    /// </remarks>
    protected override async ValueTask OnGameStartAsync(ICollection<Player> players)
    {
        await base.OnGameStartAsync(players).ConfigureAwait(false);

        var participantCount = players.Count;
        
        // Validate N ∈ [1, 5] and log error if out of range (Requirement 4.4, Task 8.1)
        if (participantCount < 1 || participantCount > 5)
        {
            this.Logger.LogError(
                "Dungeon {Key}: Invalid participant count {Count}. Must be between 1 and 5. Using N=1 as fallback.",
                this.Key,
                participantCount);
            participantCount = 1;
        }

        // Calculate HP/damage multipliers with party scaling (Task 8.1)
        var (hpMultiplier, damageMultiplier, defenseMultiplier, attackRateBonus) = this.ComputeScalingMultipliers(participantCount);
        this._hpMultiplier = hpMultiplier;
        this._damageMultiplier = damageMultiplier;
        this._defenseMultiplier = defenseMultiplier;
        this._attackRateBonus = attackRateBonus;
        this._participantCount = participantCount;

        this.Logger.LogInformation(
            "Dungeon {Key}: Starting with {Count} participants. HP x{HpMult:F2}, Damage x{DmgMult:F2}, Defense x{DefMult:F2}, AttackRate +{AtkRate}",
            this.Key,
            participantCount,
            hpMultiplier,
            damageMultiplier,
            defenseMultiplier,
            attackRateBonus);

        this._remainingTime = this.GameDuration;
        this._hudCts = new CancellationTokenSource();
        _ = Task.Run(() => this.RunHudUpdateLoopAsync(this._hudCts.Token), this._hudCts.Token);
        try
        {
            await this.Map.ClearEventSpawnedNpcsAsync().ConfigureAwait(false);
            await this.OpenAllDungeonGatesAsync(removeNpcs: false).ConfigureAwait(false);
            await this.OpenArenaWalkAreaAsync().ConfigureAwait(false);
            await this.WarpPlayersToArenaAsync().ConfigureAwait(false);
            await this.SpawnWaveAsync(1).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Dungeon {Key}: Failed to start wave 1.", this.Key);
        }
    }

    /// <summary>
    /// Runs the HUD update loop, sending packet 0x14 updates to all players every second.
    /// </summary>
    /// <param name="ct">Cancellation token to stop the loop.</param>
    /// <remarks>
    /// Implements Requirement 3.6 (HUD updates every second).
    /// Task 8.1: every 1s send packet 0x14 to all players.
    /// </remarks>
    private async Task RunHudUpdateLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && this.State == MiniGameState.Playing)
            {
                var hudUpdate = this.BuildHudUpdate();
                await this.ForEachPlayerAsync(async player =>
                {
                    await this.SendDungeonHudUpdateAsync(player, hudUpdate).ConfigureAwait(false);
                }).ConfigureAwait(false);

                await Task.Delay(TimeSpan.FromSeconds(1), ct).ConfigureAwait(false);

                if (this._remainingTime > TimeSpan.Zero)
                {
                    this._remainingTime = this._remainingTime.Subtract(TimeSpan.FromSeconds(1));
                }

                if (this._intermissionRemaining > TimeSpan.Zero)
                {
                    this._intermissionRemaining = this._intermissionRemaining.Subtract(TimeSpan.FromSeconds(1));
                }

                if (this._lootRemaining > TimeSpan.Zero)
                {
                    this._lootRemaining = this._lootRemaining.Subtract(TimeSpan.FromSeconds(1));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the dungeon ends
            this.Logger.LogDebug("Dungeon {Key}: HUD update loop cancelled.", this.Key);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Dungeon {Key}: Error in HUD update loop: {Message}", this.Key, ex.Message);
        }
    }

    /// <summary>
    /// Sends a DungeonHudUpdate to a specific player.
    /// </summary>
    /// <param name="player">The player to send the update to.</param>
    /// <param name="update">The HUD update data.</param>
    /// <remarks>
    /// This method will be connected to the packet-sending infrastructure via view plugin system.
    /// The actual packet sending (0x14 via EventSchedulePackets.SendDungeonHudUpdateAsync)
    /// will be implemented through a view plugin in the GameServer layer.
    /// </remarks>
    private async ValueTask SendDungeonHudUpdateAsync(Player player, DungeonHudUpdate update)
    {
        await player.InvokeViewPlugInAsync<IShowDungeonHudPlugIn>(p => p.ShowDungeonHudUpdateAsync(update)).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the objective text for the specified room.
    /// </summary>
    /// <param name="room">The room phase.</param>
    /// <returns>The objective description text.</returns>
    private DungeonHudUpdate BuildHudUpdate()
    {
        var lootPhase = this._bossKilled || this._lootRemaining > TimeSpan.Zero;
        string objective;
        if (lootPhase)
        {
            objective = "Pegue as recompensas";
        }
        else if (this._intermissionRemaining > TimeSpan.Zero)
        {
            objective = $"Prox. wave em {(int)Math.Ceiling(this._intermissionRemaining.TotalSeconds)}s";
        }
        else if (this._currentWave == DungeonWaveCatalog.WaveCount)
        {
            objective = "Derrote Gaia";
        }
        else if (this._currentWave == 5)
        {
            objective = "Derrote Jerry";
        }
        else
        {
            objective = "Elimine a wave";
        }

        var timerSeconds = this._lootRemaining > TimeSpan.Zero
            ? this._lootRemaining.TotalSeconds
            : this._remainingTime.TotalSeconds;
        var remaining = lootPhase ? 0 : Math.Max(0, this._waveRemaining);
        return new DungeonHudUpdate(
            (byte)this._currentWave,
            (ushort)remaining,
            (uint)Math.Max(0, timerSeconds),
            objective);
    }

    private async ValueTask AnnounceAsync(string message)
    {
        await this.ForEachPlayerAsync(player =>
            player.InvokeViewPlugInAsync<IShowMessagePlugIn>(p => p.ShowMessageAsync(message, MessageType.GoldenCenter)).AsTask())
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Called when a monster dies. Counts kills per room and advances to next room when cleared.
    /// </summary>
    /// <param name="sender">The monster that died.</param>
    /// <param name="e">Death information.</param>
    /// <remarks>
    /// Implements Requirement 3.2 (Advance to next room after all kills).
    /// Task 8.1: count kills, advance room when cleared.
    /// </remarks>
    protected override void OnMonsterDied(object? sender, DeathInformation e)
    {
        if (sender is not AttackableNpcBase npc)
        {
            return;
        }

        if (npc.Definition.Number is { } gateNumber && GateNpcNumbers.Contains(gateNumber))
        {
            var gatePosition = ((byte)npc.Position.X, (byte)npc.Position.Y);
            _ = Task.Run(async () =>
            {
                try
                {
                    await this.OpenGatesAndUnblockAsync([gatePosition], removeNpcs: true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "Dungeon {Key}: Failed to unblock gate {Number}.", this.Key, gateNumber);
                }
            });
            return;
        }

        var shouldAdvance = false;
        var gaiaDefeated = false;
        lock (this._waveSync)
        {
            if (!this._currentWaveMonsterIds.Remove(npc.Id))
            {
                return;
            }

            this._waveRemaining = Math.Max(0, this._waveRemaining - 1);
            gaiaDefeated = this._currentWave == DungeonWaveCatalog.WaveCount
                && npc.Definition.Number == DungeonWaveCatalog.GaiaMonsterNumber;
            if (this._waveRemaining <= 0 && !this._isAdvancingWave && this._currentWave < DungeonWaveCatalog.WaveCount)
            {
                this._isAdvancingWave = true;
                shouldAdvance = true;
            }
        }

        this.Logger.LogInformation(
            "Dungeon {Key}: Wave {Wave} monster killed. Remaining: {Remaining}",
            this.Key,
            this._currentWave,
            this._waveRemaining);

        if (gaiaDefeated)
        {
            this._bossKilled = true;
            this._waveRemaining = 0;
            if (this._lootRemaining <= TimeSpan.Zero)
            {
                this._lootRemaining = TimeSpan.FromSeconds(DungeonWaveCatalog.LootWindowSeconds);
            }

            if (!this._completing)
            {
                this._completing = true;
                this.Logger.LogInformation("Dungeon {Key}: Gaia defeated. Completing dungeon.", this.Key);
                _ = Task.Run(() => this.CompleteDungeonAsync().AsTask());
            }

            return;
        }

        if (shouldAdvance)
        {
            _ = Task.Run(() => this.AdvanceToNextWaveAsync().AsTask());
        }
    }

    private async ValueTask AdvanceToNextWaveAsync()
    {
        var nextWave = this._currentWave + 1;
        this._currentWave = nextWave;
        this._waveRemaining = 0;
        this._intermissionRemaining = TimeSpan.FromSeconds(DungeonWaveCatalog.IntermissionSeconds);
        this.Logger.LogInformation(
            "Dungeon {Key}: Wave cleared. Next wave {Wave} in {Seconds}s.",
            this.Key,
            nextWave,
            DungeonWaveCatalog.IntermissionSeconds);

        await this.AnnounceAsync($"A próxima wave começará em {DungeonWaveCatalog.IntermissionSeconds} segundos").ConfigureAwait(false);
        var intermissionHud = this.BuildHudUpdate();
        await this.ForEachPlayerAsync(async player =>
        {
            await this.SendDungeonHudUpdateAsync(player, intermissionHud).ConfigureAwait(false);
        }).ConfigureAwait(false);
        await Task.Delay(TimeSpan.FromSeconds(DungeonWaveCatalog.IntermissionSeconds)).ConfigureAwait(false);
        this._intermissionRemaining = TimeSpan.Zero;

        if (this.State != MiniGameState.Playing)
        {
            this._isAdvancingWave = false;
            return;
        }

        await this.SpawnWaveAsync(nextWave).ConfigureAwait(false);
        this._isAdvancingWave = false;
    }

    private async ValueTask SpawnWaveAsync(int waveNumber)
    {
        var layout = DungeonWaveCatalog.Waves.FirstOrDefault(w => w.Number == waveNumber);
        if (layout.Number == 0)
        {
            this.Logger.LogWarning("Dungeon {Key}: Missing wave layout {Wave}.", this.Key, waveNumber);
            return;
        }

        this._currentWave = waveNumber;
        this._currentWaveMonsterIds.Clear();
        this._waveRemaining = 0;

        var announcement = waveNumber switch
        {
            5 => "Wave 5: Jerry apareceu!",
            DungeonWaveCatalog.WaveCount => "Wave 10: Gaia surgiu!",
            _ => $"Wave {waveNumber} começou!",
        };
        await this.AnnounceAsync(announcement).ConfigureAwait(false);

        var spawnCount = DungeonScalingCalculator.ComputeMonsterCount(layout.Count, this._participantCount, layout.IsBoss);
        for (var i = 0; i < spawnCount; i++)
        {
            var monsterNumber = layout.MonsterNumbers[i % layout.MonsterNumbers.Length];
            var spawned = await this.SpawnMonsterAsync(monsterNumber, layout.ExtraHpMultiplier, layout.ExtraDamageMultiplier, layout.IsBoss).ConfigureAwait(false);
            if (spawned is not null)
            {
                this._currentWaveMonsterIds.Add(spawned.Id);
                this._waveRemaining++;
            }
        }

        this.Logger.LogInformation(
            "Dungeon {Key}: Spawned wave {Wave} with {Count} monsters.",
            this.Key,
            waveNumber,
            this._waveRemaining);

        var hudUpdate = this.BuildHudUpdate();
        await this.ForEachPlayerAsync(async player =>
        {
            await this.SendDungeonHudUpdateAsync(player, hudUpdate).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private async ValueTask<Monster?> SpawnMonsterAsync(short monsterNumber, float extraHpMultiplier, float extraDamageMultiplier, bool isBoss)
    {
        var definition = this._gameContext.Configuration.Monsters.FirstOrDefault(m => m.Number == monsterNumber);
        if (definition is null)
        {
            this.Logger.LogWarning("Dungeon {Key}: Monster {Number} was not found.", this.Key, monsterNumber);
            return null;
        }

        var point = isBoss
            ? new Pathfinding.Point(DungeonWaveCatalog.ArenaX, DungeonWaveCatalog.ArenaY)
            : this.FindArenaSpawnPoint();
        var area = new MonsterSpawnArea
        {
            GameMap = this.Map.Definition,
            MonsterDefinition = definition,
            SpawnTrigger = SpawnTrigger.OnceAtEventStart,
            Quantity = 1,
            X1 = point.X,
            X2 = point.X,
            Y1 = point.Y,
            Y2 = point.Y,
        };

        var monster = new Monster(
            area,
            definition,
            this.Map,
            NullDropGenerator.Instance,
            new BasicMonsterIntelligence(),
            this._gameContext.PlugInManager,
            this._gameContext.PathFinderPool,
            this);
        monster.Initialize();
        await this.Map.AddAsync(monster).ConfigureAwait(false);
        monster.OnSpawn();
        this.ApplyCombatScaling(monster, extraHpMultiplier, extraDamageMultiplier);
        return monster;
    }

    private Pathfinding.Point FindArenaSpawnPoint()
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            var x = (byte)Rand.NextInt(DungeonWaveCatalog.SpawnMinX, DungeonWaveCatalog.SpawnMaxX + 1);
            var y = (byte)Rand.NextInt(DungeonWaveCatalog.SpawnMinY, DungeonWaveCatalog.SpawnMaxY + 1);
            if (this.Map.Terrain.WalkMap[x, y])
            {
                return new Pathfinding.Point(x, y);
            }
        }

        return new Pathfinding.Point(DungeonWaveCatalog.ArenaX, DungeonWaveCatalog.ArenaY);
    }

    private async ValueTask WarpPlayersToArenaAsync()
    {
        var gate = new ExitGate
        {
            Map = this.Map.Definition,
            X1 = DungeonWaveCatalog.ArenaX,
            Y1 = DungeonWaveCatalog.ArenaY,
            X2 = DungeonWaveCatalog.ArenaX,
            Y2 = DungeonWaveCatalog.ArenaY,
            Direction = Direction.South,
        };

        await this.ForEachPlayerAsync(player => player.WarpToAsync(gate).AsTask()).ConfigureAwait(false);
    }

    private async ValueTask OpenArenaWalkAreaAsync()
    {
        var area = (
            StartX: DungeonWaveCatalog.WalkMinX,
            StartY: DungeonWaveCatalog.WalkMinY,
            EndX: DungeonWaveCatalog.WalkMaxX,
            EndY: DungeonWaveCatalog.WalkMaxY);
        for (var x = area.StartX; x <= area.EndX; x++)
        {
            for (var y = area.StartY; y <= area.EndY; y++)
            {
                this.Map.Terrain.WalkMap[x, y] = true;
                this.Map.Terrain.UpdateAiGridValue(x, y);
            }
        }

        await this.SendTerrainUnblockAsync([area]).ConfigureAwait(false);
    }

    private async ValueTask OpenAllDungeonGatesAsync(bool removeNpcs)
    {
        var markers = this.Map.GetNpcsInRange(new Pathfinding.Point(128, 128), 255)
            .Where(npc => npc.Definition?.Number is { } number && GateNpcNumbers.Contains(number))
            .Select(npc => ((byte)npc.Position.X, (byte)npc.Position.Y))
            .Concat(AllGateMarkers)
            .Distinct()
            .ToList();

        this.Logger.LogInformation(
            "Dungeon {Key}: Opening {Count} gate paths so players can walk through without destroying them.",
            this.Key,
            markers.Count);

        await this.OpenGatesAndUnblockAsync(markers, removeNpcs).ConfigureAwait(false);
    }

    private async ValueTask OpenGatesAndUnblockAsync(IReadOnlyList<(byte X, byte Y)> markers, bool removeNpcs)
    {
        if (markers.Count == 0)
        {
            return;
        }

        const int radius = 6;
        var uniquePoints = markers
            .Distinct()
            .ToList();

        if (removeNpcs)
        {
            foreach (var npc in this.Map.GetNpcsInRange(new Pathfinding.Point(128, 128), 255).ToList())
            {
                if (npc.Definition?.Number is not { } number || !GateNpcNumbers.Contains(number))
                {
                    continue;
                }

                var nearMarker = uniquePoints.Any(marker =>
                    Math.Abs(npc.Position.X - marker.X) <= radius
                    && Math.Abs(npc.Position.Y - marker.Y) <= radius);
                if (!nearMarker)
                {
                    continue;
                }

                await this.Map.RemoveAsync(npc).ConfigureAwait(false);
            }
        }

        var areas = uniquePoints
            .Select(marker => AreaAround(marker.X, marker.Y, radius))
            .Distinct()
            .ToList();

        foreach (var area in areas)
        {
            for (var x = area.StartX; x <= area.EndX; x++)
            {
                for (var y = area.StartY; y <= area.EndY; y++)
                {
                    this.Map.Terrain.WalkMap[x, y] = true;
                    this.Map.Terrain.UpdateAiGridValue(x, y);
                }
            }
        }

        await this.SendTerrainUnblockAsync(areas).ConfigureAwait(false);
    }

    /// <summary>
    /// C1 0x46 is limited to 255 bytes (~62 areas). Sending more disconnects the client.
    /// </summary>
    private async ValueTask SendTerrainUnblockAsync(IReadOnlyList<(byte StartX, byte StartY, byte EndX, byte EndY)> areas)
    {
        const int maxAreasPerPacket = 40;
        for (var offset = 0; offset < areas.Count; offset += maxAreasPerPacket)
        {
            var chunk = areas.Skip(offset).Take(maxAreasPerPacket).ToList();
            await this.ForEachPlayerAsync(player =>
                player.InvokeViewPlugInAsync<IChangeTerrainAttributesViewPlugin>(
                    view => view.ChangeAttributesAsync(TerrainAttributeType.Blocked, false, chunk)).AsTask()).ConfigureAwait(false);
        }
    }

    private static (byte StartX, byte StartY, byte EndX, byte EndY) AreaAround(byte x, byte y, int radius)
    {
        var startX = (byte)Math.Clamp(x - radius, 0, 255);
        var startY = (byte)Math.Clamp(y - radius, 0, 255);
        var endX = (byte)Math.Clamp(x + radius, 0, 255);
        var endY = (byte)Math.Clamp(y + radius, 0, 255);
        return (startX, startY, endX, endY);
    }

    private static (byte StartX, byte StartY, byte EndX, byte EndY) HorizontalGateArea(byte x, byte y)
    {
        const int gateWidth = 6;
        const int gateHeight = 2;
        var startX = (byte)Math.Clamp(x - (gateWidth / 2), 0, 255 - gateWidth + 1);
        var startY = (byte)Math.Clamp((int)y, 0, 255 - gateHeight + 1);
        return (startX, startY, (byte)(startX + gateWidth - 1), (byte)(startY + gateHeight - 1));
    }

    private static (byte StartX, byte StartY, byte EndX, byte EndY) VerticalGateArea(byte x, byte y)
    {
        const int gateWidth = 2;
        const int gateHeight = 6;
        var startX = (byte)Math.Clamp((int)x, 0, 255 - gateWidth + 1);
        var startY = (byte)Math.Clamp(y - (gateHeight / 2), 0, 255 - gateHeight + 1);
        return (startX, startY, (byte)(startX + gateWidth - 1), (byte)(startY + gateHeight - 1));
    }

    /// <summary>
    /// Called when a player dies. Waits 3 seconds, restores HP, and teleports to checkpoint.
    /// </summary>
    /// <param name="sender">The player that died.</param>
    /// <param name="e">Death information.</param>
    /// <remarks>
    /// Implements Requirements 7.1, 7.2, 7.3, 7.4 (Respawn at checkpoint with full HP, time continues).
    /// Task 8.1: wait 3s, restore HP, teleport to checkpoint.
    /// </remarks>
    protected override void OnPlayerDied(object? sender, DeathInformation e)
    {
        if (sender is not Player player)
        {
            return;
        }

        this.Logger.LogInformation(
            "Dungeon {Key}: Player {Player} died on wave {Wave}. Respawn at the arena.",
            this.Key,
            player.Name,
            this._currentWave);
    }

    /// <summary>
    /// Called when the game ends. Delivers rewards if boss killed, cancels HUD updates.
    /// </summary>
    /// <param name="finishers">The players who finished the game.</param>
    /// <remarks>
    /// Implements Requirements 3.4, 3.5, 8 (Reward delivery on success, no reward on timeout).
    /// Task 8.1: deliver rewards if boss killed, cancel HUD.
    /// </remarks>
    protected override async ValueTask GameEndedAsync(ICollection<Player> finishers)
    {
        await base.GameEndedAsync(finishers).ConfigureAwait(false);

        // Check if boss was killed (success) or timeout
        var bossKilled = this._bossKilled;
        var timedOut = this._remainingTime <= TimeSpan.Zero;

        // Deliver rewards if boss killed and not timed out (Task 8.1)
        if (bossKilled && !timedOut)
        {
            this.Logger.LogInformation(
                "Dungeon {Key}: Completed successfully. Delivering rewards to {Count} eligible players.",
                this.Key,
                finishers.Count);
            await this.DeliverRewardChestsAsync(finishers).ConfigureAwait(false);
        }
        else if (timedOut)
        {
            this.Logger.LogInformation("Dungeon {Key}: Timed out. No rewards will be delivered.", this.Key);
            await this.ShowGoldenMessageAsync("Time's up! Dungeon failed.").ConfigureAwait(false);
        }
        else
        {
            this.Logger.LogInformation("Dungeon {Key}: Ended without completing boss. No rewards.", this.Key);
        }

        // Cancel HUD updates (Task 8.1)
        await this.StopHudAsync().ConfigureAwait(false);
    }

    private async ValueTask CompleteDungeonAsync()
    {
        try
        {
            await this.AnnounceAsync("Gaia foi derrotado! Dungeon concluida.").ConfigureAwait(false);

            if (this._lootRemaining <= TimeSpan.Zero)
            {
                this._lootRemaining = TimeSpan.FromSeconds(DungeonWaveCatalog.LootWindowSeconds);
            }

            var lootHud = this.BuildHudUpdate();
            await this.ForEachPlayerAsync(async player =>
            {
                await this.SendDungeonHudUpdateAsync(player, lootHud).ConfigureAwait(false);
            }).ConfigureAwait(false);

            var players = new List<Player>();
            await this.ForEachPlayerAsync(player =>
            {
                players.Add(player);
                return Task.CompletedTask;
            }).ConfigureAwait(false);

            this.Logger.LogInformation(
                "Dungeon {Key}: Completed successfully. Delivering rewards to {Count} eligible players.",
                this.Key,
                players.Count);
            await this.DeliverRewardChestsAsync(players).ConfigureAwait(false);

            await this.AnnounceAsync($"Voce tem {DungeonWaveCatalog.LootWindowSeconds} segundos para recolher as recompensas.").ConfigureAwait(false);

            var lootDeadline = DateTime.UtcNow.Add(this._lootRemaining > TimeSpan.Zero
                ? this._lootRemaining
                : TimeSpan.FromSeconds(DungeonWaveCatalog.LootWindowSeconds));
            while (DateTime.UtcNow < lootDeadline && this.State == MiniGameState.Playing)
            {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Dungeon {Key}: Failed during loot window after Gaia.", this.Key);
        }
        finally
        {
            await this.StopHudAsync().ConfigureAwait(false);
            this.FinishEvent();
        }
    }

    private async ValueTask StopHudAsync()
    {
        if (this._hudCts is null)
        {
            return;
        }

        await this._hudCts.CancelAsync().ConfigureAwait(false);
        this._hudCts.Dispose();
        this._hudCts = null;
    }

    /// <summary>
    /// Delivers reward chests to eligible players.
    /// </summary>
    /// <param name="eligible">The eligible players.</param>
    /// <remarks>
    /// Implements Requirements 8.1, 8.2, 8.3, 8.4, 8.5 (Individual reward, eligibility, drop if full, idempotency).
    /// </remarks>
    private async ValueTask DeliverRewardChestsAsync(IEnumerable<Player> eligible)
    {
        foreach (var player in eligible)
        {
            // Check if player is on the map (Requirement 8.2)
            if (player.CurrentMap != this.Map)
            {
                this.Logger.LogDebug("Dungeon {Key}: Player {Player} not on map. Skipping reward.", this.Key, player.Name);
                continue;
            }

            // Check if reward already delivered (Requirement 8.4, 8.5)
            var playerKey = player.SelectedCharacter?.Id.ToString() ?? player.Name;
            if (this._rewardDelivered.Contains(playerKey))
            {
                this.Logger.LogDebug("Dungeon {Key}: Reward already delivered to {Player}. Skipping duplicate.", this.Key, player.Name);
                continue;
            }

            var rewards = this.Definition.Rewards.Where(reward => reward.ItemReward is not null).ToList();
            if (rewards.Count == 0)
            {
                this.Logger.LogWarning("Dungeon {Key}: No item rewards configured for {Player}.", this.Key, player.Name);
                continue;
            }

            var toInventory = 0;
            var toGround = 0;
            var failed = false;
            foreach (var reward in rewards)
            {
                try
                {
                    var (inv, ground) = await this.GiveConfiguredRewardAsync(player, reward).ConfigureAwait(false);
                    toInventory += inv;
                    toGround += ground;
                }
                catch (Exception ex)
                {
                    failed = true;
                    this.Logger.LogError(
                        ex,
                        "Dungeon {Key}: Failed delivering reward '{Reward}' to {Player}.",
                        this.Key,
                        reward.ItemReward?.Description,
                        player.Name);
                }
            }

            var delivered = toInventory + toGround;

            // Mark only after a successful pass so GameEndedAsync can retry on hard failures.
            if (!failed)
            {
                this._rewardDelivered.Add(playerKey);
            }

            this.Logger.LogInformation(
                "Dungeon {Key}: Delivered {Count} reward item(s) to {Player} ({Inventory} inventory, {Ground} ground).",
                this.Key,
                delivered,
                player.Name,
                toInventory,
                toGround);

            if (toInventory > 0)
            {
                await player.InvokeViewPlugInAsync<IShowMessagePlugIn>(
                    p => p.ShowMessageAsync($"Recompensa da dungeon: {toInventory} item(ns) no inventario.", MessageType.BlueNormal)).ConfigureAwait(false);
            }

            if (toGround > 0)
            {
                await player.InvokeViewPlugInAsync<IShowMessagePlugIn>(
                    p => p.ShowMessageAsync($"Inventario cheio! {toGround} item(ns) cairam no chao.", MessageType.BlueNormal)).ConfigureAwait(false);
            }
            else if (delivered == 0 && !failed)
            {
                await player.InvokeViewPlugInAsync<IShowMessagePlugIn>(
                    p => p.ShowMessageAsync("Nenhuma recompensa extra desta vez.", MessageType.BlueNormal)).ConfigureAwait(false);
            }
        }
    }

    private async ValueTask<(int ToInventory, int ToGround)> GiveConfiguredRewardAsync(Player player, MiniGameReward reward)
    {
        if (reward.ItemReward is null)
        {
            return (0, 0);
        }

        var toInventory = 0;
        var toGround = 0;
        var amount = Math.Max(1, reward.RewardAmount);
        for (var i = 0; i < amount; i++)
        {
            var chance = reward.ItemReward.Chance;
            if (chance > 0 && chance < 1 && !Rand.NextRandomBool(chance))
            {
                this.Logger.LogDebug(
                    "Dungeon {Key}: {Player} missed chance {Chance} for {Reward}.",
                    this.Key,
                    player.Name,
                    chance,
                    reward.ItemReward.Description);
                continue;
            }

            var item = this.CreateRewardItem(player, reward.ItemReward);
            if (item is null)
            {
                this.Logger.LogWarning(
                    "Dungeon {Key}: Failed to create reward {Reward} for {Player}.",
                    this.Key,
                    reward.ItemReward.Description,
                    player.Name);
                continue;
            }

            switch (await this.GiveOrDropItemAsync(player, item).ConfigureAwait(false))
            {
                case RewardDelivery.Inventory:
                    toInventory++;
                    break;
                case RewardDelivery.Ground:
                    toGround++;
                    break;
            }
        }

        return (toInventory, toGround);
    }

    private Item? CreateRewardItem(Player player, DropItemGroup group)
    {
        if (group.ItemType == SpecialItemType.Ancient)
        {
            return this.CreateTier1AncientItem(player, group);
        }

        var generated = this.DropGenerator.GenerateItemDrop(group);
        if (generated is null)
        {
            return null;
        }

        // DropGenerator returns TemporaryItem; inventory EF collections require a persistent Item.
        if (generated is TemporaryItem temporaryItem)
        {
            return temporaryItem.MakePersistent(player.PersistenceContext);
        }

        return generated;
    }

    private Item? CreateTier1AncientItem(Player player, DropItemGroup group)
    {
        var pieces = new List<ItemOfItemSet>();
        foreach (var definition in group.PossibleItems)
        {
            foreach (var set in definition.PossibleItemSetGroups)
            {
                if (!DungeonRewards.IsTier1AncientSet(set.Name))
                {
                    continue;
                }

                pieces.AddRange(set.Items.Where(itemOfSet => object.Equals(itemOfSet.ItemDefinition, definition)));
            }
        }

        if (pieces.Count == 0)
        {
            return null;
        }

        var piece = pieces[Rand.NextInt(0, pieces.Count)];
        var item = player.PersistenceContext.CreateNew<Item>();
        item.Definition = piece.ItemDefinition;
        item.Durability = item.GetMaximumDurabilityOfOnePiece();
        item.HasSkill = item.CanHaveSkill();
        item.ItemSetGroups.Add(piece);
        if (piece.BonusOption is { } bonusOption)
        {
            var bonusOptionLink = player.PersistenceContext.CreateNew<ItemOptionLink>();
            bonusOptionLink.ItemOption = bonusOption;
            var levels = bonusOption.LevelDependentOptions.Select(option => option.Level).Distinct().ToList();
            bonusOptionLink.Level = levels.Count > 0 ? levels[Rand.NextInt(0, levels.Count)] : 1;
            item.ItemOptions.Add(bonusOptionLink);
        }

        return item;
    }

    private async ValueTask<RewardDelivery> GiveOrDropItemAsync(Player player, Item item)
    {
        if (player.Inventory is not null && await player.Inventory.AddItemAsync(item).ConfigureAwait(false))
        {
            // AddItemAsync updates server inventory only; the client needs ItemAppear or it stays invisible.
            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
            this.Logger.LogInformation(
                "Dungeon {Key}: Added {Item} (slot {Slot}) to {Player} inventory.",
                this.Key,
                item,
                item.ItemSlot,
                player.Name);
            return RewardDelivery.Inventory;
        }

        if (player.CurrentMap is null)
        {
            this.Logger.LogWarning(
                "Dungeon {Key}: Could not deliver {Item} to {Player}: inventory full and no map for drop.",
                this.Key,
                item,
                player.Name);
            player.PersistenceContext.Detach(item);
            return RewardDelivery.Failed;
        }

        this.Logger.LogInformation(
            "Dungeon {Key}: Player {Player} inventory full. Dropping {Item} at {Position}.",
            this.Key,
            player.Name,
            item,
            player.Position);

        var droppedItem = new DroppedItem(item, player.Position, this.Map, player, player.GetAsEnumerable());
        await this.Map.AddAsync(droppedItem).ConfigureAwait(false);
        return RewardDelivery.Ground;
    }

    private enum RewardDelivery
    {
        Failed,
        Inventory,
        Ground,
    }

    /// <summary>
    /// Computes scaling multipliers based on participant count and difficulty.
    /// </summary>
    /// <param name="n">Number of participants.</param>
    /// <returns>HP, damage, defense multipliers and a raw attack-rate bonus so hits land.</returns>
    private (double HpMultiplier, double DamageMultiplier, double DefenseMultiplier, float AttackRateBonus) ComputeScalingMultipliers(int n)
    {
        var difficultyMultiplier = this.GetDifficultyMultiplier();
        var hpMultiplier = DungeonScalingCalculator.ComputeHpMultiplier(n, difficultyMultiplier.Hp);
        var damageMultiplier = DungeonScalingCalculator.ComputeDamageMultiplier(n, difficultyMultiplier.Damage);
        var defenseMultiplier = DungeonScalingCalculator.ComputeHpMultiplier(n, difficultyMultiplier.Defense);
        var attackRateBonus = DungeonScalingCalculator.ComputeAttackRateBonus(n, difficultyMultiplier.AttackRateBonus);
        return (hpMultiplier, damageMultiplier, defenseMultiplier, attackRateBonus);
    }

    /// <summary>
    /// Gets the difficulty multipliers from the definition.
    /// </summary>
    /// <returns>HP, damage, defense multipliers and attack-rate bonus.</returns>
    private (double Hp, double Damage, double Defense, float AttackRateBonus) GetDifficultyMultiplier()
    {
        return this.Definition.GameLevel switch
        {
            (byte)DungeonDifficulty.Hard => (3.0, 8.0, 2.5, 2_000_000f),
            (byte)DungeonDifficulty.Hell => (6.0, 15.0, 4.5, 4_000_000f),
            _ => (0.18, 0.12, 0.10, 0f),
        };
    }

    private void ApplyCombatScaling(Monster monster, float extraHpMultiplier, float extraDamageMultiplier)
    {
        if (monster.Attributes is null)
        {
            return;
        }

        var extraHp = extraHpMultiplier;
        var extraDamage = extraDamageMultiplier;
        if (this.Definition.GameLevel == (byte)DungeonDifficulty.Normal)
        {
            extraHp = Math.Min(extraHpMultiplier, 1.1f);
            extraDamage = Math.Min(extraDamageMultiplier, 1.05f);
        }

        var hp = new SimpleElement((float)(this._hpMultiplier * extraHp), AggregateType.Multiplicate);
        var damage = new SimpleElement((float)(this._damageMultiplier * extraDamage), AggregateType.Multiplicate);
        var defense = new SimpleElement((float)this._defenseMultiplier, AggregateType.Multiplicate);
        var attackRate = new SimpleElement(this._attackRateBonus, AggregateType.AddRaw);

        monster.Attributes.AddElement(hp, Stats.MaximumHealth);
        monster.Health = (int)monster.Attributes[Stats.MaximumHealth];
        monster.Attributes.AddElement(damage, Stats.MinimumPhysBaseDmg);
        monster.Attributes.AddElement(damage, Stats.MaximumPhysBaseDmg);
        monster.Attributes.AddElement(defense, Stats.DefenseBase);
        monster.Attributes.AddElement(defense, Stats.DefenseRatePvm);
        monster.Attributes.AddElement(attackRate, Stats.AttackRatePvm);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        if (this._hudCts is not null)
        {
            await this._hudCts.CancelAsync().ConfigureAwait(false);
            this._hudCts.Dispose();
            this._hudCts = null;
        }

        await base.DisposeAsyncCore().ConfigureAwait(false);
    }
}
