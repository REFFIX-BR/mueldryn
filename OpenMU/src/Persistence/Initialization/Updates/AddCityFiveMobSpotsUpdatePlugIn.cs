// <copyright file="AddCityFiveMobSpotsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Replaces dense area monster packs with 5-mob farm spots on main cities/maps (MuAyra-style).
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("A1B2C3D4-E5F6-4789-9ABC-DEF012345678")]
public class AddCityFiveMobSpotsUpdatePlugIn : UpdatePlugInBase
{
    internal const string PlugInName = "City five-mob farm spots";
    internal const string PlugInDescription = "Clears dense automatic area packs and adds 5-mob spots on Lorencia, Dungeon, Devias, Noria, Lost Tower, Atlans, Tarkan and Elbeland.";

    /// <summary>
    /// Spot entries: map number, monster number, x, y. Quantity is always 5 (bumped by later farm-density update).
    /// </summary>
    internal static readonly (short Map, short Monster, byte X, byte Y)[] Spots =
    {
            (0, 7, 13, 16),
            (0, 7, 41, 16),
            (0, 7, 13, 44),
            (0, 7, 41, 44),
            (0, 7, 13, 72),
            (0, 2, 140, 25),
            (0, 2, 168, 25),
            (0, 2, 196, 25),
            (0, 2, 224, 25),
            (0, 0, 140, 53),
            (0, 0, 168, 53),
            (0, 0, 196, 53),
            (0, 0, 224, 53),
            (0, 0, 140, 81),
            (0, 0, 168, 81),
            (0, 0, 196, 81),
            (0, 0, 224, 81),
            (0, 1, 75, 115),
            (0, 3, 188, 120),
            (0, 1, 69, 16),
            (0, 1, 97, 16),
            (0, 1, 69, 44),
            (0, 1, 97, 44),
            (0, 1, 41, 72),
            (0, 1, 69, 72),
            (0, 1, 97, 72),
            (0, 1, 13, 100),
            (0, 1, 41, 100),
            (0, 4, 97, 100),
            (0, 4, 13, 128),
            (0, 4, 41, 128),
            (0, 4, 97, 128),
            (0, 4, 13, 156),
            (0, 4, 41, 156),
            (0, 4, 69, 156),
            (0, 4, 97, 156),
            (0, 4, 13, 184),
            (0, 3, 213, 123),
            (0, 3, 185, 151),
            (0, 3, 213, 151),
            (0, 3, 185, 179),
            (0, 3, 213, 179),
            (0, 3, 185, 207),
            (0, 3, 213, 207),
            (0, 3, 185, 235),
            (0, 3, 213, 235),
            (0, 14, 132, 175),
            (0, 6, 151, 187),
            (0, 6, 100, 201),
            (0, 6, 128, 201),
            (0, 6, 100, 229),
            (0, 6, 128, 229),
            (0, 14, 156, 229),
            (1, 9, 10, 82),
            (1, 11, 229, 106),
            (1, 8, 11, 114),
            (1, 16, 86, 115),
            (1, 13, 178, 122),
            (1, 13, 242, 171),
            (1, 17, 92, 190),
            (1, 12, 82, 208),
            (1, 14, 109, 238),
            (1, 8, 195, 23),
            (1, 8, 158, 17),
            (1, 8, 119, 47),
            (1, 8, 21, 55),
            (1, 8, 43, 122),
            (1, 16, 104, 8),
            (1, 16, 48, 11),
            (1, 16, 232, 17),
            (1, 16, 128, 27),
            (1, 16, 235, 64),
            (1, 16, 212, 86),
            (1, 14, 62, 181),
            (1, 14, 39, 205),
            (1, 14, 4, 229),
            (1, 14, 67, 242),
            (1, 14, 16, 246),
            (1, 14, 154, 247),
            (1, 12, 79, 154),
            (1, 12, 194, 222),
            (1, 15, 98, 54),
            (1, 15, 128, 110),
            (1, 15, 68, 125),
            (1, 15, 196, 148),
            (1, 15, 217, 229),
            (1, 11, 51, 157),
            (1, 11, 122, 155),
            (1, 11, 148, 149),
            (1, 11, 109, 176),
            (1, 11, 215, 183),
            (1, 11, 155, 188),
            (1, 11, 245, 201),
            (1, 11, 137, 214),
            (1, 11, 166, 226),
            (1, 11, 230, 248),
            (1, 17, 174, 161),
            (1, 17, 51, 223),
            (1, 5, 74, 62),
            (1, 5, 120, 75),
            (1, 5, 138, 86),
            (1, 5, 69, 88),
            (1, 13, 247, 87),
            (1, 13, 158, 95),
            (1, 13, 196, 100),
            (1, 10, 39, 93),
            (2, 21, 33, 12),
            (2, 21, 64, 13),
            (2, 22, 34, 40),
            (2, 23, 58, 45),
            (2, 21, 163, 45),
            (2, 23, 241, 84),
            (2, 20, 10, 55),
            (2, 20, 10, 83),
            (2, 20, 38, 83),
            (2, 20, 10, 111),
            (2, 23, 224, 123),
            (2, 20, 111, 11),
            (2, 20, 139, 11),
            (2, 20, 167, 11),
            (2, 20, 83, 39),
            (2, 19, 111, 39),
            (2, 19, 139, 39),
            (2, 19, 83, 67),
            (2, 19, 111, 67),
            (2, 19, 139, 67),
            (2, 19, 55, 95),
            (2, 19, 83, 95),
            (2, 19, 111, 95),
            (2, 21, 139, 95),
            (2, 21, 167, 95),
            (2, 21, 55, 123),
            (2, 21, 83, 123),
            (2, 21, 111, 123),
            (2, 21, 139, 123),
            (2, 23, 167, 123),
            (2, 23, 55, 151),
            (2, 23, 83, 151),
            (2, 23, 111, 151),
            (2, 23, 139, 151),
            (2, 23, 167, 151),
            (2, 22, 55, 179),
            (2, 22, 83, 179),
            (2, 22, 111, 179),
            (2, 22, 139, 179),
            (2, 22, 167, 179),
            (2, 22, 55, 207),
            (2, 22, 83, 207),
            (2, 22, 111, 207),
            (2, 24, 203, 85),
            (2, 24, 203, 113),
            (2, 24, 203, 141),
            (2, 24, 203, 169),
            (2, 24, 231, 169),
            (2, 24, 203, 197),
            (2, 23, 231, 197),
            (2, 20, 11, 189),
            (2, 20, 11, 217),
            (2, 20, 216, 216),
            (2, 20, 201, 240),
            (3, 29, 111, 8),
            (3, 29, 139, 8),
            (3, 29, 167, 8),
            (3, 29, 195, 8),
            (3, 28, 223, 8),
            (3, 28, 251, 8),
            (3, 28, 111, 36),
            (3, 28, 139, 36),
            (3, 33, 167, 36),
            (3, 33, 195, 36),
            (3, 33, 223, 36),
            (3, 33, 251, 36),
            (3, 33, 64, 11),
            (3, 33, 64, 39),
            (3, 33, 64, 67),
            (3, 33, 92, 67),
            (3, 28, 120, 67),
            (3, 28, 64, 95),
            (3, 28, 92, 95),
            (3, 28, 120, 95),
            (3, 27, 140, 75),
            (3, 27, 168, 75),
            (3, 27, 196, 75),
            (3, 27, 224, 75),
            (3, 26, 252, 75),
            (3, 30, 12, 9),
            (3, 30, 12, 37),
            (3, 31, 12, 65),
            (3, 31, 12, 93),
            (3, 31, 12, 121),
            (3, 31, 40, 121),
            (3, 27, 142, 107),
            (3, 27, 114, 135),
            (3, 27, 142, 135),
            (3, 27, 114, 163),
            (3, 26, 142, 163),
            (3, 3, 172, 124),
            (3, 3, 200, 124),
            (3, 3, 228, 124),
            (3, 3, 172, 152),
            (3, 3, 200, 152),
            (3, 3, 228, 152),
            (3, 30, 74, 140),
            (3, 30, 46, 168),
            (3, 31, 74, 168),
            (3, 27, 169, 189),
            (3, 27, 197, 189),
            (3, 27, 225, 189),
            (3, 32, 9, 173),
            (3, 32, 9, 201),
            (3, 32, 37, 201),
            (3, 32, 65, 201),
            (3, 32, 93, 201),
            (3, 32, 9, 229),
            (3, 32, 37, 229),
            (3, 32, 65, 229),
            (3, 30, 110, 189),
            (3, 30, 138, 189),
            (3, 31, 110, 217),
            (3, 31, 138, 217),
            (3, 31, 166, 217),
            (3, 31, 194, 217),
            (4, 37, 127, 28),
            (4, 37, 112, 42),
            (4, 40, 54, 49),
            (4, 40, 53, 89),
            (4, 36, 210, 91),
            (4, 37, 83, 95),
            (4, 40, 6, 100),
            (4, 35, 32, 102),
            (4, 41, 105, 112),
            (4, 36, 190, 114),
            (4, 35, 54, 117),
            (4, 41, 98, 167),
            (4, 35, 41, 173),
            (4, 41, 85, 184),
            (4, 39, 234, 235),
            (4, 41, 116, 181),
            (4, 41, 98, 230),
            (4, 36, 227, 119),
            (4, 35, 49, 196),
            (4, 37, 101, 8),
            (4, 35, 63, 155),
            (4, 40, 18, 56),
            (4, 37, 102, 73),
            (4, 37, 33, 130),
            (4, 35, 73, 129),
            (4, 37, 155, 28),
            (4, 35, 8, 167),
            (4, 36, 169, 42),
            (4, 40, 32, 31),
            (4, 41, 124, 143),
            (4, 36, 237, 77),
            (4, 39, 219, 213),
            (4, 40, 54, 20),
            (4, 41, 141, 203),
            (4, 37, 124, 92),
            (4, 41, 148, 160),
            (7, 45, 21, 37),
            (7, 51, 227, 57),
            (7, 48, 65, 152),
            (7, 51, 37, 217),
            (7, 48, 231, 116),
            (7, 48, 223, 145),
            (7, 48, 172, 158),
            (7, 48, 228, 168),
            (7, 48, 34, 188),
            (7, 48, 170, 186),
            (7, 48, 221, 189),
            (7, 48, 152, 200),
            (7, 48, 125, 207),
            (7, 48, 232, 206),
            (7, 48, 95, 212),
            (7, 51, 218, 12),
            (7, 51, 200, 53),
            (7, 51, 168, 58),
            (7, 51, 131, 68),
            (7, 51, 221, 89),
            (7, 51, 115, 91),
            (7, 51, 186, 94),
            (7, 51, 90, 113),
            (7, 51, 142, 122),
            (7, 51, 74, 127),
            (7, 51, 23, 160),
            (7, 51, 118, 170),
            (7, 51, 83, 176),
            (7, 51, 52, 178),
            (7, 45, 47, 17),
            (7, 45, 69, 25),
            (7, 45, 58, 54),
            (7, 45, 34, 62),
            (7, 45, 24, 86),
            (7, 45, 19, 125),
            (7, 46, 128, 13),
            (7, 46, 115, 32),
            (7, 46, 72, 82),
            (7, 46, 50, 93),
            (7, 52, 177, 19),
            (7, 52, 162, 111),
            (7, 52, 117, 134),
            (7, 52, 98, 145),
            (7, 47, 152, 22),
            (7, 47, 86, 65),
            (7, 47, 36, 112),
            (8, 57, 144, 47),
            (8, 60, 199, 103),
            (8, 58, 76, 126),
            (8, 58, 100, 131),
            (8, 61, 73, 174),
            (8, 61, 128, 179),
            (8, 61, 153, 190),
            (8, 61, 86, 202),
            (8, 61, 120, 208),
            (8, 61, 176, 204),
            (8, 61, 15, 210),
            (8, 61, 157, 216),
            (8, 61, 39, 216),
            (8, 61, 106, 225),
            (8, 61, 8, 237),
            (8, 61, 40, 243),
            (8, 58, 34, 92),
            (8, 58, 29, 123),
            (8, 58, 50, 138),
            (8, 58, 115, 144),
            (8, 58, 32, 157),
            (8, 62, 137, 20),
            (8, 62, 137, 69),
            (8, 62, 150, 89),
            (8, 62, 158, 124),
            (8, 62, 151, 155),
            (8, 60, 99, 34),
            (8, 60, 110, 78),
            (8, 57, 82, 60),
            (8, 57, 46, 73),
            (8, 57, 74, 91),
            (8, 61, 198, 157),
            (8, 61, 124, 239),
            (8, 58, 8, 176),
            (8, 62, 123, 97),
            (8, 61, 56, 228),
            (51, 422, 95, 48),
            (51, 424, 179, 82),
            (51, 424, 138, 92),
            (51, 419, 123, 120),
            (51, 419, 151, 120),
            (51, 421, 92, 95),
            (51, 421, 92, 123),
            (51, 419, 51, 144),
            (51, 3, 18, 130),
            (51, 3, 18, 158),
            (51, 421, 123, 163),
            (51, 421, 151, 163),
            (51, 423, 192, 152),
            (51, 418, 59, 164),
            (51, 418, 87, 164),
            (51, 418, 89, 211),
            (51, 418, 117, 211),
            (51, 3, 135, 199),
            (51, 3, 135, 227),
            (51, 3, 57, 90),
            (51, 418, 24, 204),
            (51, 3, 184, 179),
            (51, 3, 173, 223),
            (51, 423, 185, 131),
            (51, 418, 109, 247),
            (51, 424, 182, 53),
            (51, 419, 72, 190),
            (51, 3, 8, 177),
    };

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddCityFiveMobSpots;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 08, 11, 16, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var targetMaps = new HashSet<short> { 0, 1, 2, 3, 4, 7, 8, 51 };
        var monstersByNumber = gameConfiguration.Monsters.ToDictionary(m => m.Number);

        foreach (var map in gameConfiguration.Maps.Where(m => targetMaps.Contains(m.Number)))
        {
            // Remove dense area packs so the map is farmed via 5-mob spots instead of overlapping blobs.
            foreach (var spawn in map.MonsterSpawns.Where(s => s.SpawnTrigger == SpawnTrigger.Automatic && s.Quantity > 1))
            {
                spawn.Quantity = 0;
            }
        }

        // High range avoids collision with existing dungeon/map spawn numbers (Dungeon already uses ~500+).
        short nextId = 1000;
        short lastMap = -1;
        foreach (var (mapNumber, monsterNumber, x, y) in Spots)
        {
            if (mapNumber != lastMap)
            {
                nextId = 1000;
                lastMap = mapNumber;
            }

            var map = gameConfiguration.Maps.FirstOrDefault(m => m.Number == mapNumber);
            if (map is null || !monstersByNumber.TryGetValue(monsterNumber, out var monster))
            {
                nextId++;
                continue;
            }

            // Idempotent: skip if this spot guid already exists.
            var already = map.MonsterSpawns.Any(s =>
                s.MonsterDefinition?.Number == monsterNumber
                && s.X1 == x && s.Y1 == y && s.X2 == x && s.Y2 == y
                && s.Quantity == 5
                && s.SpawnTrigger == SpawnTrigger.Automatic);
            if (already)
            {
                nextId++;
                continue;
            }

            var spawn = context.CreateNew<MonsterSpawnArea>();
            spawn.SetGuid(mapNumber, nextId++);
            spawn.GameMap = map;
            spawn.MonsterDefinition = monster;
            spawn.SpawnTrigger = SpawnTrigger.Automatic;
            spawn.Direction = Direction.Undefined;
            spawn.Quantity = 5;
            spawn.X1 = x;
            spawn.X2 = x;
            spawn.Y1 = y;
            spawn.Y2 = y;
            map.MonsterSpawns.Add(spawn);
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
