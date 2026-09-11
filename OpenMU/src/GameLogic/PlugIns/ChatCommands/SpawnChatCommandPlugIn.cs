// <copyright file="SpawnChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands.Arguments;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// GM chat command to spawn a monster/boss at the character feet or at map coordinates.
/// </summary>
[Guid("C8E91A42-6F3D-4B7A-9E15-2D84F0B6C1A9")]
[PlugIn]
[Display(Name = nameof(PlugInResources.SpawnChatCommandPlugIn_Name), Description = nameof(PlugInResources.SpawnChatCommandPlugIn_Description), ResourceType = typeof(PlugInResources))]
[ChatCommandHelp(Command, typeof(SpawnChatCommandArgs), CharacterStatus.GameMaster)]
internal class SpawnChatCommandPlugIn : ChatCommandPlugInBase<SpawnChatCommandArgs>
{
    private const string Command = "/spawn";

    private static readonly Dictionary<string, short> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["kundun"] = 275,
        ["goldenkundun"] = 604,
        ["erohim"] = 295,
        ["goldenerohim"] = 602,
        ["hellmaine"] = 309,
        ["goldenhellmaine"] = 603,
        ["selupan"] = 459,
        ["silvester"] = 583,
        ["ferea"] = 588,
        ["nix"] = 592,
        ["frozenking"] = 611,
        ["infernal"] = 612,
        ["pharaoh"] = 618,
        ["darkness"] = 619,
        ["dragonlord"] = 623,
        ["flamestone"] = 624,
        ["netherlord"] = 694,
        ["abbadon"] = 702,
        ["nefarius"] = 724,
        ["obsidar"] = 733,
        ["jack"] = 753,
        ["pumpkin"] = 753,
    };

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => CharacterStatus.GameMaster;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player gameMaster, SpawnChatCommandArgs arguments)
    {
        if (gameMaster.CurrentMap is null)
        {
            return;
        }

        if (!TryResolveMonsterNumber(arguments, out var monsterNumber, out var resolveError))
        {
            await gameMaster.ShowBlueMessageAsync(resolveError).ConfigureAwait(false);
            return;
        }

        var monsterDef = gameMaster.GameContext.Configuration.Monsters.FirstOrDefault(m => m.Number == monsterNumber);
        if (monsterDef is null)
        {
            await gameMaster.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MonsterNotFoundByNumber), monsterNumber).ConfigureAwait(false);
            return;
        }

        byte centerX;
        byte centerY;
        if (arguments.X >= 0 && arguments.Y >= 0)
        {
            centerX = (byte)Math.Clamp(arguments.X, 0, 255);
            centerY = (byte)Math.Clamp(arguments.Y, 0, 255);
        }
        else
        {
            centerX = gameMaster.Position.X;
            centerY = gameMaster.Position.Y;
        }

        var gameMap = gameMaster.CurrentMap;
        var area = new MonsterSpawnArea
        {
            GameMap = gameMap.Definition,
            MonsterDefinition = monsterDef,
            SpawnTrigger = SpawnTrigger.OnceAtEventStart,
            Quantity = 1,
            X1 = (byte)Math.Max(centerX - 1, byte.MinValue),
            X2 = (byte)Math.Min(centerX + 1, byte.MaxValue),
            Y1 = (byte)Math.Max(centerY - 1, byte.MinValue),
            Y2 = (byte)Math.Min(centerY + 1, byte.MaxValue),
        };

        INpcIntelligence intelligence = new BasicMonsterIntelligence();
        var monster = new Monster(area, monsterDef, gameMap, gameMaster.GameContext.DropGenerator, intelligence, gameMaster.GameContext.PlugInManager, gameMaster.GameContext.PathFinderPool);
        intelligence.Npc = monster;

        monster.Initialize();
        await gameMap.AddAsync(monster).ConfigureAwait(false);
        monster.OnSpawn();

        await gameMaster.ShowBlueMessageAsync(
            $"[/spawn] {monsterDef.Designation} (id={monsterNumber}) @ {centerX}/{centerY} object={monster.Id}").ConfigureAwait(false);
    }

    private static bool TryResolveMonsterNumber(SpawnChatCommandArgs arguments, out short monsterNumber, out string error)
    {
        monsterNumber = 0;
        error = string.Empty;

        if (!string.IsNullOrWhiteSpace(arguments.Alias))
        {
            if (Aliases.TryGetValue(arguments.Alias.Trim(), out monsterNumber))
            {
                return true;
            }

            error = $"[/spawn] unknown alias '{arguments.Alias}'. Try id=604 or alias=goldenkundun.";
            return false;
        }

        if (arguments.Id > 0)
        {
            monsterNumber = arguments.Id;
            return true;
        }

        error = "[/spawn] usage: /spawn id=604 | /spawn alias=goldenkundun | /spawn id=604 x=120 y=130";
        return false;
    }
}
