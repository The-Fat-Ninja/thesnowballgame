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
        readonly List<(Transform t, float age)> puffs = new List<(Transform, float)>();

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
                    ball = box.Prim(PrimitiveType.Sphere, root, wa, Vector3.one * 0.22f, Color.white, "Snowball");
                    balls[pa.Id] = ball;
                }
                ball.SetActive(true);
                ball.transform.localPosition = Vector3.Lerp(wa, wb, t);
            }
            foreach (var kv in balls)
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
                case EventKind.Splash:
                    Puff(Greybox.World(e.Pos, 0.1f), 0.7f);
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

        /// <summary>Ages the snow puffs.</summary>
        public void Tick(float dt)
        {
            for (int i = puffs.Count - 1; i >= 0; i--)
            {
                var (t, age) = puffs[i];
                age += dt;
                if (age > 0.45f || t == null)
                {
                    if (t != null)
                    {
                        if (Application.isPlaying) Object.Destroy(t.gameObject);
                        else Object.DestroyImmediate(t.gameObject);
                    }
                    puffs.RemoveAt(i);
                    continue;
                }
                t.localScale = Vector3.one * (0.3f + age * 2.2f);
                puffs[i] = (t, age);
            }
        }

        void Puff(Vector3 at, float size)
        {
            var p = box.Prim(PrimitiveType.Sphere, root, at, Vector3.one * 0.3f * size, Palette.SnowLight, "Puff");
            puffs.Add((p.transform, 0f));
        }
    }
}
