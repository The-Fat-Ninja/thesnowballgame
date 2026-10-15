using System.Collections.Generic;
using SnowballGame.Core;
using UnityEngine;

namespace SnowballGame.Presentation
{
    /// <summary>
    /// Shows simulation frames and events: kids between frames, snowballs in flight,
    /// snow puffs, hit flashes and cover changes. Shared by the playable WEGO scene
    /// and the demo-tape recorder, so both look the same.
    /// </summary>
    public sealed class PlaybackView
    {
        readonly Greybox box;
        readonly WorldView world;
        readonly Transform root;
        readonly Match match;
        readonly IReadOnlyDictionary<int, KidView> kids;
        readonly Dictionary<int, GameObject> balls = new Dictionary<int, GameObject>();
        readonly Dictionary<int, GameObject> clouds = new Dictionary<int, GameObject>();
        readonly List<(Transform t, float age, float life, Vector3 from, Vector3 to)> fx = new List<(Transform, float, float, Vector3, Vector3)>();
        float spin;

        public PlaybackView(Greybox box, WorldView world, Transform root, Match match, IReadOnlyDictionary<int, KidView> kids)
        {
            this.box = box;
            this.world = world;
            this.root = root;
            this.match = match;
            this.kids = kids;
        }

        public void ShowFrames(Frame a, Frame b, float t)
        {
            for (int k = 0; k < a.Kids.Length; k++)
            {
                var kid = match.Kids[k];
                kids[k].Apply(a.Kids[k], b.Kids[k], t, kid.MaxWarmth, kid.MaxAmmo);
            }
            var seen = new HashSet<int>();
            foreach (var pa in a.Projectiles)
            {
                seen.Add(pa.Id);
                var pb = b.Projectiles.Find(x => x.Id == pa.Id);
                bool hasB = pb.Id == pa.Id;
                Vector3 wa = Greybox.World(pa.Pos, pa.Height / 100f);
                Vector3 wb = hasB ? Greybox.World(pb.Pos, pb.Height / 100f) : wa;
                if (!balls.TryGetValue(pa.Id, out var ball))
                {
                    ball = MakeBall(pa.Type, wa);
                    balls[pa.Id] = ball;
                }
                ball.SetActive(true);
                var bt = ball.transform;
                bt.localPosition = Vector3.Lerp(wa, wb, t);
                Vector3 vel = wb - wa;
                switch (pa.Type)
                {
                    case SnowballType.Boulder:
                        // Grows as it rolls, and visibly rolls.
                        float r = Mathf.Lerp(pa.Radius, hasB ? pb.Radius : pa.Radius, t) / 100f;
                        bt.localScale = Vector3.one * r * 2f;
                        if (vel.sqrMagnitude > 1e-6f)
                            bt.localRotation = Quaternion.LookRotation(vel.normalized) * Quaternion.Euler(spin * 220f, 0, 0);
                        break;
                    case SnowballType.Quick:
                        // Stretched along its flight: a streak.
                        if (vel.sqrMagnitude > 1e-6f) bt.localRotation = Quaternion.LookRotation(vel.normalized);
                        break;
                    case SnowballType.Powder:
                        bt.localRotation = Quaternion.Euler(0, spin * 300f, spin * 170f);
                        break;
                }
            }
            foreach (var kv in balls)
                if (!seen.Contains(kv.Key)) kv.Value.SetActive(false);
            ShowClouds(a, b, t);
        }

        GameObject MakeBall(SnowballType type, Vector3 at)
        {
            Color c = Palette.Ball(type);
            float d = Palette.BallSize(type);
            switch (type)
            {
                case SnowballType.Quick:
                    return box.Prim(PrimitiveType.Sphere, root, at, new Vector3(d, d, d * 2.6f), c, "QuickBall");
                case SnowballType.Slush:
                    var slush = box.Prim(PrimitiveType.Sphere, root, at, new Vector3(d, d * 0.85f, d), c, "Slushball");
                    box.Prim(PrimitiveType.Sphere, slush.transform, new Vector3(0, -0.45f, 0), Vector3.one * 0.35f, Palette.DeepShadow, "Drip");
                    return slush;
                case SnowballType.Powder:
                    var puff = box.Prim(PrimitiveType.Sphere, root, at, Vector3.one * d, c, "PowderPuff");
                    box.Prim(PrimitiveType.Sphere, puff.transform, new Vector3(0.35f, 0.2f, 0), Vector3.one * 0.7f, c);
                    box.Prim(PrimitiveType.Sphere, puff.transform, new Vector3(-0.3f, -0.15f, 0.2f), Vector3.one * 0.65f, c);
                    box.Prim(PrimitiveType.Sphere, puff.transform, new Vector3(0, 0.1f, -0.35f), Vector3.one * 0.6f, c);
                    return puff;
                case SnowballType.Boulder:
                    var boulder = box.Prim(PrimitiveType.Sphere, root, at, Vector3.one * 0.6f, c, "Boulder");
                    // A band of darker packed snow so the roll reads.
                    box.Prim(PrimitiveType.Cylinder, boulder.transform, Vector3.zero, new Vector3(1.02f, 0.06f, 1.02f), Palette.SnowShadow, "Band")
                        .transform.localRotation = Quaternion.Euler(0, 0, 90);
                    return boulder;
                default:
                    return box.Prim(PrimitiveType.Sphere, root, at, Vector3.one * d, c, "Snowball");
            }
        }

        /// <summary>Powder clouds: a billowing clump that thins out as it blows away.</summary>
        void ShowClouds(Frame a, Frame b, float t)
        {
            var seen = new HashSet<int>();
            foreach (var ca in a.Clouds)
            {
                seen.Add(ca.Id);
                if (!clouds.TryGetValue(ca.Id, out var go))
                {
                    go = new GameObject($"PowderCloud_{ca.Id}");
                    go.transform.SetParent(root, false);
                    var rnd = new System.Random(ca.Id);
                    Color c = Palette.Ball(SnowballType.Powder);
                    for (int i = 0; i < 9; i++)
                    {
                        float ang = i * 40f * Mathf.Deg2Rad, rr = i == 0 ? 0f : 0.55f + (float)rnd.NextDouble() * 0.35f;
                        float s = i == 0 ? 1.1f : 0.6f + (float)rnd.NextDouble() * 0.35f;
                        box.Prim(PrimitiveType.Sphere, go.transform, new Vector3(Mathf.Cos(ang) * rr, 0.35f + (float)rnd.NextDouble() * 0.4f, Mathf.Sin(ang) * rr), Vector3.one * s, i % 2 == 0 ? c : Color.white, "Billow");
                    }
                    clouds[ca.Id] = go;
                }
                go.SetActive(true);
                var cb = b.Clouds.Find(x => x.Id == ca.Id);
                float left = Mathf.Lerp(ca.TicksLeft, cb.Id == ca.Id ? cb.TicksLeft : 0, t);
                float fade = Mathf.Clamp01(left / 10f);
                float size = ca.Radius / 100f * Mathf.Lerp(0.35f, 1f, fade);
                go.transform.localPosition = Greybox.World(ca.Pos);
                go.transform.localScale = new Vector3(size, size * 0.9f, size);
                go.transform.localRotation = Quaternion.Euler(0, spin * 20f + ca.Id * 37f, 0);
            }
            foreach (var kv in clouds)
                if (!seen.Contains(kv.Key)) kv.Value.SetActive(false);
        }

        public void ShowEvent(MatchEvent e)
        {
            switch (e.Kind)
            {
                case EventKind.HitKid:
                    if (kids.TryGetValue(e.OtherId, out var victim)) victim.Flash();
                    Puff(Greybox.World(e.Pos, 1.0f), 1f);
                    break;
                case EventKind.Slowed:
                    Splat(Greybox.World(e.Pos, 0.03f), 0.9f);
                    break;
                case EventKind.Splash:
                    var type = (SnowballType)e.Amount;
                    if (type == SnowballType.Slush) Splat(Greybox.World(e.Pos, 0.03f), Rules.Spec(type).Splash / 50f);
                    else if (type == SnowballType.Boulder) Puff(Greybox.World(e.Pos, 0.4f), 2.2f);
                    else if (type == SnowballType.Quick) Puff(Greybox.World(e.Pos, 0.1f), 0.4f);
                    else if (type != SnowballType.Powder) Puff(Greybox.World(e.Pos, 0.1f), 0.7f);
                    break;
                case EventKind.CloudCreated:
                    Puff(Greybox.World(e.Pos, 0.6f), 2.6f, Palette.Ball(SnowballType.Powder));
                    break;
                case EventKind.CoverHit:
                case EventKind.CoverDestroyed:
                    Puff(Greybox.World(e.Pos, 0.6f), 0.8f);
                    world.RefreshCover(match.Cover.Find(c => c.Id == e.OtherId));
                    break;
                case EventKind.CoverBuilt:
                    var built = match.Cover.Find(c => c.Id == e.OtherId);
                    if (built != null && !world.HasCover(built.Id)) world.AddCover(built);
                    Puff(Greybox.World(e.Pos, 0.4f), 1f);
                    break;
                case EventKind.Soaked:
                    Puff(Greybox.World(e.Pos, 0.8f), 1.6f);
                    break;
            }
        }

        public void RefreshAllCover()
        {
            foreach (var c in match.Cover) world.RefreshCover(c);
        }

        /// <summary>Ages puffs and splats; spins snowballs.</summary>
        public void Tick(float dt)
        {
            spin += dt;
            for (int i = fx.Count - 1; i >= 0; i--)
            {
                var (t, age, life, from, to) = fx[i];
                age += dt;
                if (age > life || t == null)
                {
                    if (t != null)
                    {
                        if (Application.isPlaying) Object.Destroy(t.gameObject);
                        else Object.DestroyImmediate(t.gameObject);
                    }
                    fx.RemoveAt(i);
                    continue;
                }
                t.localScale = Vector3.Lerp(from, to, age / life);
                fx[i] = (t, age, life, from, to);
            }
        }

        /// <summary>A burst of snow that swells and vanishes.</summary>
        void Puff(Vector3 at, float size, Color? colour = null)
        {
            var p = box.Prim(PrimitiveType.Sphere, root, at, Vector3.one * 0.3f * size, colour ?? Palette.SnowLight, "Puff");
            fx.Add((p.transform, 0f, 0.45f, Vector3.one * 0.3f * size, Vector3.one * (0.3f + 1f) * size));
        }

        /// <summary>A grey slush splat on the snow that soaks away.</summary>
        void Splat(Vector3 at, float diameter)
        {
            var p = box.Prim(PrimitiveType.Cylinder, root, at, new Vector3(diameter, 0.01f, diameter), Palette.Ball(SnowballType.Slush), "SlushSplat");
            fx.Add((p.transform, 0f, 1.6f, new Vector3(diameter, 0.01f, diameter), new Vector3(diameter * 0.2f, 0.01f, diameter * 0.2f)));
        }
    }
}
