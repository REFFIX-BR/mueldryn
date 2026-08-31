// -----------------------------------------------------------------------
// <copyright file="KalimaTicketConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns;
using MonsterSpawnArea = MUnique.OpenMU.Persistence.BasicModel.MonsterSpawnArea;

/// <summary>
/// Consume handler for the Kalima ticket of the item shop. It opens a Kalima gate next to the player,
/// like a lost map does, but without having to collect the symbols of kundun first. Which Kalima is
/// opened depends on the level of the character.
/// </summary>
[Guid("4A0E1D87-C055-4B9B-9D64-3E8F0B7C2F54")]
[PlugIn]
[Display(Name = "Kalima ticket", Description = "Opens the gate to the Kalima which fits the character level.")]
public class KalimaTicketConsumeHandlerPlugIn : BaseConsumeHandlerPlugIn
{
    private const byte GateNpcStartNumber = 152;

    private static readonly int[] KalimaMapNumbers = [24, 25, 26, 27, 28, 29, 36];

    /// <summary>Minimum character level of each Kalima, from the highest to the lowest one.</summary>
    private static readonly int[] KalimaMinimumLevels = [380, 340, 300, 260, 220, 180, 0];

    /// <inheritdoc />
    public override ItemIdentifier Key => new(48, 13);

    /// <inheritdoc />
    public override async ValueTask<bool> ConsumeItemAsync(Player player, Item item, Item? targetItem, FruitUsage fruitUsage)
    {
        if (!this.CheckPreconditions(player, item)
            || player.CurrentMap is not { } currentMap
            || player.Attributes is null)
        {
            return false;
        }

        if (player.CurrentMiniGame is not null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.NoKalimaGateOnEventMap)).ConfigureAwait(false);
            return false;
        }

        var gatePosition = player.Position;
        if (player.IsAtSafezone() || currentMap.Terrain.SafezoneMap[gatePosition.X, gatePosition.Y])
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.NoKalimaGateInSafezone)).ConfigureAwait(false);
            return false;
        }

        var kalimaLevel = ResolveKalimaLevel((int)player.Attributes[Stats.Level]);
        var gateNpcDefinition = player.GameContext.Configuration.Monsters
            .FirstOrDefault(def => def.Number == GateNpcStartNumber + kalimaLevel - 1);
        if (gateNpcDefinition is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.UndefinedGateNpc)).ConfigureAwait(false);
            return false;
        }

        var targetGate = player.GameContext.Configuration.Maps
            .FirstOrDefault(map => map.Number == KalimaMapNumbers[kalimaLevel - 1])?.ExitGates.FirstOrDefault();
        if (targetGate is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KalimaEntranceNotFound)).ConfigureAwait(false);
            return false;
        }

        var spawnArea = new MonsterSpawnArea
        {
            Direction = Direction.West,
            Quantity = 1,
            MonsterDefinition = gateNpcDefinition,
            SpawnTrigger = SpawnTrigger.ManuallyForEvent,
            X1 = gatePosition.X,
            X2 = gatePosition.X,
            Y1 = gatePosition.Y,
            Y2 = gatePosition.Y,
        };

        if (!await base.ConsumeItemAsync(player, item, targetItem, fruitUsage).ConfigureAwait(false))
        {
            return false;
        }

        var gate = new GateNpc(spawnArea, gateNpcDefinition, currentMap, player, targetGate, TimeSpan.FromMinutes(1));
        gate.Initialize();
        await currentMap.AddAsync(gate).ConfigureAwait(false);
        return true;
    }

    private static int ResolveKalimaLevel(int characterLevel)
    {
        for (var i = 0; i < KalimaMinimumLevels.Length; i++)
        {
            if (characterLevel >= KalimaMinimumLevels[i])
            {
                return KalimaMinimumLevels.Length - i;
            }
        }

        return 1;
    }
}
