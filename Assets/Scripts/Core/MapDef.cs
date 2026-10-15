using System.Collections.Generic;

namespace SnowballGame.Core
{
    public struct CoverDef
    {
        public CoverKind Kind;
        public Vec2 Pos;
        public int Radius;
        public int Hp;
    }

    /// <summary>A map: a terrain grid of 1 m cells, cover pieces and spawn spots.</summary>
    public sealed class MapDef
    {
        public string Name;
        public int Width;
        public int Depth;
        public Ground[,] Cells;
        public List<CoverDef> Cover = new List<CoverDef>();
        /// <summary>Spawn spots per team, used in order.</summary>
        public List<Vec2>[] Spawns = { new List<Vec2>(), new List<Vec2>() };

        public MapDef(string name, int widthM, int depthM)
        {
            Name = name;
            Width = widthM * Rules.CellSize;
            Depth = depthM * Rules.CellSize;
            Cells = new Ground[widthM, depthM];
        }

        public int CellsX => Cells.GetLength(0);
        public int CellsZ => Cells.GetLength(1);

        public Ground TerrainAt(Vec2 p)
        {
            int cx = p.X / Rules.CellSize;
            int cz = p.Z / Rules.CellSize;
            if (p.X < 0 || p.Z < 0 || cx >= CellsX || cz >= CellsZ) return Ground.Blocked;
            return Cells[cx, cz];
        }

        public void Fill(int x0, int z0, int x1, int z1, Ground t)
        {
            for (int x = x0; x < x1; x++)
                for (int z = z0; z < z1; z++)
                    if (x >= 0 && z >= 0 && x < CellsX && z < CellsZ) Cells[x, z] = t;
        }

        public void AddCover(CoverKind kind, int xCm, int zCm, int radius)
        {
            int hp = kind == CoverKind.Snowbank ? Rules.SnowbankHp
                : kind == CoverKind.Snowman ? Rules.SnowmanHp
                : kind == CoverKind.SnowWall ? Rules.BuiltWallHp
                : -1;
            Cover.Add(new CoverDef { Kind = kind, Pos = new Vec2(xCm, zCm), Radius = radius, Hp = hp });
        }

        /// <summary>A straight run of cover circles from one point to another.</summary>
        public void AddCoverLine(CoverKind kind, int x0, int z0, int x1, int z1, int radius)
        {
            var a = new Vec2(x0, z0);
            var b = new Vec2(x1, z1);
            int len = Vec2.Dist(a, b);
            int steps = System.Math.Max(1, len / (radius * 2 - radius / 2));
            for (int i = 0; i <= steps; i++)
            {
                Vec2 p = Vec2.Lerp(a, b, i * 1000 / steps);
                AddCover(kind, p.X, p.Z, radius);
            }
        }
    }
}
