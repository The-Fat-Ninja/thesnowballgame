using System.Collections.Generic;
using SnowballGame.Core;
using UnityEngine;

namespace SnowballGame.Presentation
{
    /// <summary>Builds the greybox map (ground, houses, cover) from the rules-core map.</summary>
    public sealed class WorldView
    {
        readonly Greybox box;
        readonly Transform root;
        readonly Dictionary<int, GameObject> cover = new Dictionary<int, GameObject>();
        readonly Dictionary<int, Vector3> coverScale = new Dictionary<int, Vector3>();

        public WorldView(Greybox box, Transform root)
        {
            this.box = box;
            this.root = root;
        }

        public void Build(Match match)
        {
            BuildGround(match.Map);
            BuildHouses(match.Map);
            var merged = new HashSet<int>();
            foreach (var kind in new[] { CoverKind.Car, CoverKind.GardenWall, CoverKind.Hedge })
                MergeRuns(match, kind, merged);
            foreach (var c in match.Cover)
                if (!merged.Contains(c.Id)) AddCover(c);
        }

        /// <summary>
        /// Indestructible cover authored as a run of circles (a car, a wall, a hedge)
        /// is drawn as one object spanning the run.
        /// </summary>
        void MergeRuns(Match match, CoverKind kind, HashSet<int> merged)
        {
            var pieces = match.Cover.FindAll(c => c.Kind == kind && !c.Destructible);
            var done = new HashSet<int>();
            foreach (var start in pieces)
            {
                if (done.Contains(start.Id)) continue;
                var run = new List<Cover> { start };
                done.Add(start.Id);
                for (int i = 0; i < run.Count; i++)
                    foreach (var other in pieces)
                    {
                        if (done.Contains(other.Id)) continue;
                        long reach = run[i].Radius + other.Radius + 10;
                        if (Vec2.DistSq(run[i].Pos, other.Pos) <= reach * reach)
                        {
                            run.Add(other);
                            done.Add(other.Id);
                        }
                    }
                int minX = int.MaxValue, maxX = int.MinValue, minZ = int.MaxValue, maxZ = int.MinValue, r = 0;
                foreach (var c in run)
                {
                    minX = Mathf.Min(minX, c.Pos.X); maxX = Mathf.Max(maxX, c.Pos.X);
                    minZ = Mathf.Min(minZ, c.Pos.Z); maxZ = Mathf.Max(maxZ, c.Pos.Z);
                    r = Mathf.Max(r, c.Radius);
                }
                float lx = (maxX - minX + 2 * r) / 100f, lz = (maxZ - minZ + 2 * r) / 100f;
                var go = new GameObject($"{kind}_run");
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3((minX + maxX) / 200f, 0, (minZ + maxZ) / 200f);
                var t = go.transform;
                switch (kind)
                {
                    case CoverKind.Car:
                        var paint = run[0].Id % 3 == 0 ? Palette.Hex("#C8382E") : run[0].Id % 3 == 1 ? Palette.Hex("#5C6B8A") : Palette.Hex("#E0A93A");
                        box.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.5f, 0), new Vector3(lx, 0.75f, lz * 0.9f), paint, "Body");
                        box.Prim(PrimitiveType.Cube, t, new Vector3(-lx * 0.05f, 1.1f, 0), new Vector3(lx * 0.55f, 0.5f, lz * 0.8f), Palette.Hex("#9FB3D0"), "Cabin");
                        box.Prim(PrimitiveType.Cube, t, new Vector3(-lx * 0.05f, 1.38f, 0), new Vector3(lx * 0.57f, 0.08f, lz * 0.82f), Palette.SnowLight, "RoofSnow");
                        box.Prim(PrimitiveType.Cube, t, new Vector3(lx * 0.3f, 0.9f, 0), new Vector3(lx * 0.35f, 0.06f, lz * 0.88f), Palette.SnowLight, "HoodSnow");
                        foreach (float wx in new[] { -0.32f, 0.32f })
                            foreach (float wz in new[] { -0.45f, 0.45f })
                            {
                                var wheel = box.Prim(PrimitiveType.Cylinder, t, new Vector3(lx * wx, 0.3f, lz * wz), new Vector3(0.55f, 0.1f, 0.55f), Palette.Ink, "Wheel");
                                wheel.transform.localRotation = Quaternion.Euler(90, 0, 0);
                            }
                        break;
                    case CoverKind.GardenWall:
                        box.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.3f, 0), new Vector3(lx, 0.6f, lz), Palette.Brick);
                        box.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.63f, 0), new Vector3(lx + 0.05f, 0.07f, lz + 0.08f), Palette.SnowLight);
                        break;
                    case CoverKind.Hedge:
                        box.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.6f, 0), new Vector3(lx, 1.2f, lz * 0.9f), Palette.Pine);
                        box.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.22f, 0), new Vector3(lx * 0.95f, 0.1f, lz * 0.8f), Palette.SnowLight);
                        break;
                }
                foreach (var c in run)
                {
                    merged.Add(c.Id);
                    cover[c.Id] = go;
                    coverScale[c.Id] = go.transform.localScale;
                }
            }
        }

        void BuildGround(MapDef map)
        {
            const int px = 8;
            int w = map.CellsX, d = map.CellsZ;
            var tex = new Texture2D(w * px, d * px, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "TerrainGrid",
            };
            var pixels = new Color[w * px * d * px];
            for (int cx = 0; cx < w; cx++)
                for (int cz = 0; cz < d; cz++)
                {
                    Color c = map.Cells[cx, cz] switch
                    {
                        Ground.Packed => Palette.Packed,
                        Ground.Ice => Palette.Ice,
                        Ground.Blocked => Palette.SnowShadow,
                        _ => Palette.SnowGround,
                    };
                    for (int x = 0; x < px; x++)
                        for (int z = 0; z < px; z++)
                        {
                            bool edge = x == 0 || z == 0;
                            Color pc = edge ? Color.Lerp(c, Palette.SnowShadow, 0.35f) : c;
                            pixels[(cz * px + z) * w * px + cx * px + x] = pc;
                        }
                }
            tex.SetPixels(pixels);
            tex.Apply();

            // Snow beyond the playable area.
            box.Prim(PrimitiveType.Plane, root, new Vector3(map.Width / 200f, -0.02f, map.Depth / 200f),
                new Vector3(map.Width / 100f * 0.25f, 1f, map.Depth / 100f * 0.25f), Palette.SnowShadow, "Surroundings");

            var ground = box.Prim(PrimitiveType.Plane, root, new Vector3(map.Width / 200f, 0f, map.Depth / 200f),
                new Vector3(map.Width / 1000f, 1f, map.Depth / 1000f), Color.white, "Ground");
            var mat = new Material(box.Lit(Color.white)) { mainTexture = tex };
            ground.GetComponent<Renderer>().sharedMaterial = mat;
            // A Unity plane's UV runs opposite to world X and Z; flip it so cells line up.
            ground.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        void BuildHouses(MapDef map)
        {
            int w = map.CellsX, d = map.CellsZ;
            var used = new bool[w, d];
            for (int z = 0; z < d; z++)
                for (int x = 0; x < w; x++)
                {
                    if (used[x, z] || map.Cells[x, z] != Ground.Blocked) continue;
                    int x1 = x;
                    while (x1 + 1 < w && !used[x1 + 1, z] && map.Cells[x1 + 1, z] == Ground.Blocked) x1++;
                    int z1 = z;
                    while (z1 + 1 < d && RowBlocked(map, used, x, x1, z1 + 1)) z1++;
                    for (int xx = x; xx <= x1; xx++)
                        for (int zz = z; zz <= z1; zz++) used[xx, zz] = true;
                    House(map, x, z, x1 + 1, z1 + 1);
                }
        }

        static bool RowBlocked(MapDef map, bool[,] used, int x0, int x1, int z)
        {
            for (int x = x0; x <= x1; x++)
                if (used[x, z] || map.Cells[x, z] != Ground.Blocked) return false;
            return true;
        }

        void House(MapDef map, int x0, int z0, int x1, int z1)
        {
            var go = new GameObject("House");
            go.transform.SetParent(root, false);
            float sx = x1 - x0, sz = z1 - z0;
            go.transform.localPosition = new Vector3(x0 + sx / 2f, 0f, z0 + sz / 2f);
            var t = go.transform;
            const float h = 3.2f;
            box.Prim(PrimitiveType.Cube, t, new Vector3(0, h / 2f, 0), new Vector3(sx, h, sz), Palette.Brick, "Walls");
            // Gable roof running along the long axis, capped with snow.
            bool alongZ = sz >= sx;
            float span = alongZ ? sx : sz, len = alongZ ? sz : sx;
            float side = span / Mathf.Sqrt(2f);
            var roof = box.Prim(PrimitiveType.Cube, t, new Vector3(0, h, 0), alongZ ? new Vector3(side, side, len + 0.4f) : new Vector3(len + 0.4f, side, side), Palette.Roof, "Roof");
            roof.transform.localRotation = alongZ ? Quaternion.Euler(0, 0, 45) : Quaternion.Euler(45, 0, 0);
            var snow = box.Prim(PrimitiveType.Cube, t, new Vector3(0, h + 0.08f, 0), alongZ ? new Vector3(side * 0.96f, side * 0.96f, len + 0.5f) : new Vector3(len + 0.5f, side * 0.96f, side * 0.96f), Palette.SnowLight, "RoofSnow");
            snow.transform.localRotation = roof.transform.localRotation;

            // Lit windows and a door on the side facing the middle of the map.
            float faceX = (x0 + x1) / 2f < map.CellsX / 2f ? sx / 2f + 0.01f : -sx / 2f - 0.01f;
            for (int i = 0; i < 2; i++)
            {
                float wz = (i == 0 ? -0.28f : 0.28f) * sz;
                box.Prim(PrimitiveType.Cube, t, new Vector3(faceX, 1.9f, wz), new Vector3(0.05f, 0.8f, 0.9f), Palette.Glow, "Window");
            }
            box.Prim(PrimitiveType.Cube, t, new Vector3(faceX, 0.9f, 0), new Vector3(0.05f, 1.8f, 0.9f), Palette.Pine, "Door");
        }

        public void AddCover(Cover c)
        {
            var go = new GameObject($"{c.Kind}_{c.Id}");
            go.transform.SetParent(root, false);
            go.transform.localPosition = Greybox.World(c.Pos);
            var t = go.transform;
            float d = c.Radius * 2f / 100f;
            switch (c.Kind)
            {
                case CoverKind.Snowbank:
                    box.Prim(PrimitiveType.Sphere, t, new Vector3(0, 0.1f, 0), new Vector3(d * 1.15f, 1.0f, d * 1.15f), Palette.SnowLight);
                    break;
                case CoverKind.SnowWall:
                    box.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.4f, 0), new Vector3(d, 0.8f, d), Palette.SnowLight);
                    box.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.82f, 0), new Vector3(d * 0.9f, 0.06f, d * 0.9f), Palette.SnowShadow);
                    break;
                case CoverKind.Snowman:
                    box.Prim(PrimitiveType.Sphere, t, new Vector3(0, 0.45f, 0), Vector3.one * 0.95f, Palette.SnowLight);
                    box.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.1f, 0), Vector3.one * 0.62f, Palette.SnowLight);
                    box.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.55f, 0), Vector3.one * 0.42f, Palette.SnowLight);
                    var nose = box.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 1.55f, -0.25f), new Vector3(0.06f, 0.12f, 0.06f), Palette.Ember);
                    nose.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case CoverKind.Car:
                    box.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.55f, 0), new Vector3(d * 0.8f, 0.9f, d), Palette.Hex("#5C6B8A"));
                    box.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.2f, 0), new Vector3(d * 0.8f, 0.45f, d * 0.8f), Palette.Hex("#8796B5"));
                    box.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.45f, 0), new Vector3(d * 0.82f, 0.08f, d * 0.82f), Palette.SnowLight);
                    break;
                case CoverKind.GardenWall:
                    box.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.3f, 0), new Vector3(d * 1.6f, 0.6f, d), Palette.Brick);
                    box.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.62f, 0), new Vector3(d * 1.6f, 0.06f, d * 1.05f), Palette.SnowLight);
                    break;
                case CoverKind.Hedge:
                    box.Prim(PrimitiveType.Sphere, t, new Vector3(0, 0.6f, 0), new Vector3(d * 1.2f, 1.2f, d), Palette.Pine);
                    box.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.0f, 0), new Vector3(d * 0.9f, 0.4f, d * 0.7f), Palette.SnowLight);
                    break;
                case CoverKind.Tree:
                    box.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.8f, 0), new Vector3(0.35f, 0.8f, 0.35f), Palette.Cocoa);
                    box.Prim(PrimitiveType.Sphere, t, new Vector3(0, 2.2f, 0), new Vector3(2.2f, 1.8f, 2.2f), Palette.Pine);
                    box.Prim(PrimitiveType.Sphere, t, new Vector3(0, 3.1f, 0), new Vector3(1.5f, 1.3f, 1.5f), Palette.Pine);
                    box.Prim(PrimitiveType.Sphere, t, new Vector3(0, 3.55f, 0), new Vector3(1.0f, 0.5f, 1.0f), Palette.SnowLight);
                    break;
                case CoverKind.TrashCan:
                    box.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.45f, 0), new Vector3(d, 0.45f, d), Palette.Hex("#3C8D4A"));
                    box.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.92f, 0), new Vector3(d * 1.05f, 0.03f, d * 1.05f), Palette.SnowLight);
                    break;
                case CoverKind.Streetlight:
                    box.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 2f, 0), new Vector3(0.15f, 2f, 0.15f), Palette.Hex("#3A3550"));
                    box.Prim(PrimitiveType.Sphere, t, new Vector3(0, 4.05f, 0), Vector3.one * 0.4f, Palette.Glow);
                    break;
            }
            cover[c.Id] = go;
            coverScale[c.Id] = go.transform.localScale;
        }

        /// <summary>Shrinks damaged snow cover and removes destroyed pieces.</summary>
        public void RefreshCover(Cover c)
        {
            if (!cover.TryGetValue(c.Id, out var go)) return;
            if (!c.Alive)
            {
                go.SetActive(false);
                return;
            }
            if (c.Destructible)
            {
                float f = Mathf.Lerp(0.45f, 1f, c.Hp / (float)c.MaxHp);
                var s = coverScale[c.Id];
                go.transform.localScale = new Vector3(s.x, s.y * f, s.z);
            }
        }

        public bool HasCover(int id) => cover.ContainsKey(id);
    }
}
