using System;
using System.Collections.Generic;

namespace SnowballGame.Core
{
    /// <summary>The kids, from Docs/Design/Characters.md.</summary>
    public static class Roster
    {
        public static List<KidDef> MapleCourtCrew() => new List<KidDef>
        {
            new KidDef("Maisie", "Thrower", 5, 4, 3, 2, 2, "#2BA6A0"),
            new KidDef("Bernard", "Builder", 2, 3, 2, 4, 5, "#6D7A8C") { Favourite = SnowballType.Slush },
            new KidDef("Pip", "Scout", 2, 2, 5, 2, 3, "#FFD23F") { HitRadius = Rules.SmallHitRadius, Favourite = SnowballType.Quick },
            new KidDef("Rosa", "Support", 2, 3, 3, 4, 3, "#C8382E") { Favourite = SnowballType.Powder },
        };

        public static List<KidDef> HillRoadHooligans() => new List<KidDef>
        {
            new KidDef("Tobias", "Heavy", 3, 2, 2, 5, 3, "#2E7D4F") { Favourite = SnowballType.Boulder },
            new KidDef("Ada", "Thrower", 4, 3, 2, 2, 4, "#E0A93A") { MaxAmmo = 3 },
            new KidDef("Felix", "Trickster", 3, 3, 4, 2, 3, "#3B6FB6") { Favourite = SnowballType.Powder },
            new KidDef("Jun", "Scout", 4, 5, 3, 2, 1, "#4E8F6A") { Favourite = SnowballType.Quick },
        };

        public static List<KidDef> Take(List<KidDef> crew, int count) =>
            crew.GetRange(0, Math.Max(1, Math.Min(count, crew.Count)));
    }

    public static class Maps
    {
        /// <summary>
        /// Maple Court cul-de-sac, 40 x 30 m, mirrored left/right.
        /// Layout from Docs/Concept/Map_MapleCourt_v01.svg.
        /// </summary>
        public static MapDef MapleCourt()
        {
            var map = new MapDef("Maple Court", 40, 30);
            const int W = 4000;

            // The street across the middle and the turning circle are packed (no ammo).
            map.Fill(0, 13, 40, 17, Ground.Packed);
            var centre = new Vec2(2000, 1500);
            for (int x = 0; x < map.CellsX; x++)
                for (int z = 0; z < map.CellsZ; z++)
                {
                    var c = new Vec2(x * 100 + 50, z * 100 + 50);
                    long d = Vec2.DistSq(c, centre);
                    if (d <= 650L * 650 && d > 230L * 230) map.Cells[x, z] = Ground.Packed;
                }
            // A frozen puddle north of the circle.
            map.Fill(19, 8, 22, 10, Ground.Ice);

            // Houses at both ends (the spawns sit on the street between them).
            map.Fill(0, 1, 4, 10, Ground.Blocked);
            map.Fill(0, 20, 4, 29, Ground.Blocked);
            map.Fill(36, 1, 40, 10, Ground.Blocked);
            map.Fill(36, 20, 40, 29, Ground.Blocked);

            void Mirrored(Action<Func<int, int>> place)
            {
                place(x => x);
                place(x => W - x);
            }

            Mirrored(f =>
            {
                // Parked cars.
                map.AddCoverLine(CoverKind.Car, f(880), 1100, f(1120), 1100, 100);
                map.AddCoverLine(CoverKind.Car, f(1280), 1900, f(1520), 1900, 100);
                // Low front-yard walls.
                map.AddCoverLine(CoverKind.GardenWall, f(520), 430, f(1080), 430, 30);
                map.AddCoverLine(CoverKind.GardenWall, f(520), 2570, f(1080), 2570, 30);
                // Snowbanks.
                map.AddCoverLine(CoverKind.Snowbank, f(760), 650, f(1040), 650, 95);
                map.AddCoverLine(CoverKind.Snowbank, f(720), 2200, f(880), 2200, 95);
                map.AddCoverLine(CoverKind.Snowbank, f(1430), 800, f(1570), 800, 85);
                map.AddCoverLine(CoverKind.Snowbank, f(1380), 2250, f(1520), 2250, 85);
                map.AddCover(CoverKind.Snowbank, f(1750), 400, 80);
                // Hedges.
                map.AddCoverLine(CoverKind.Hedge, f(520), 120, f(760), 120, 55);
                map.AddCoverLine(CoverKind.Hedge, f(520), 2880, f(760), 2880, 55);
                // Trash cans and trees.
                map.AddCover(CoverKind.TrashCan, f(1050), 300, 28);
                map.AddCover(CoverKind.TrashCan, f(1050), 2650, 28);
                map.AddCover(CoverKind.Tree, f(600), 960, 70);
                map.AddCover(CoverKind.Tree, f(1300), 250, 70);
                map.AddCover(CoverKind.Tree, f(1300), 2750, 70);

                int side = f(0) == 0 ? 0 : 1;
                int sx = side == 0 ? 250 : W - 250;
                map.Spawns[side].Add(new Vec2(sx, 1200));
                map.Spawns[side].Add(new Vec2(sx, 1800));
                map.Spawns[side].Add(new Vec2(sx, 1400));
                map.Spawns[side].Add(new Vec2(sx, 1600));
            });

            // Centre: the half-built snowman on the island and the streetlight.
            map.AddCover(CoverKind.Snowman, 2000, 1500, 55);
            map.AddCover(CoverKind.Streetlight, 2000, 600, 15);
            return map;
        }
    }
}
