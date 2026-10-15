using System.Collections.Generic;
using SnowballGame.Core;
using UnityEngine;

namespace SnowballGame.Presentation
{
    /// <summary>
    /// Draws orders on the snow: walk paths, throw arcs, landing rings and range
    /// circles. Lines are pooled; call Begin, draw, then End each frame.
    /// </summary>
    public sealed class PlanOverlay
    {
        readonly Greybox box;
        readonly Transform root;
        readonly Match match;
        readonly List<LineRenderer> lines = new List<LineRenderer>();
        int cursor;

        public PlanOverlay(Greybox box, Transform parent, Match match)
        {
            this.box = box;
            this.match = match;
            root = new GameObject("Plans").transform;
            root.SetParent(parent, false);
        }

        public void Begin() => cursor = 0;

        public void End()
        {
            for (int i = cursor; i < lines.Count; i++) lines[i].positionCount = 0;
        }

        public void Clear()
        {
            Begin();
            End();
        }

        /// <summary>
        /// Draws one kid's order. showReach marks where a WEGO walk runs out of turn time.
        /// </summary>
        public void Order(Kid kid, KidOrder o, bool selected, Color teamColour, bool showReach = true)
        {
            float w = selected ? 0.14f : 0.09f;
            if (o.Path.Count > 0)
            {
                var pts = new List<Vector3> { Greybox.World(kid.Pos, 0.05f) };
                foreach (var p in o.Path) pts.Add(Greybox.World(p, 0.05f));
                var lr = Next(teamColour, w);
                lr.positionCount = pts.Count;
                lr.SetPositions(pts.ToArray());

                if (showReach)
                {
                    int reach = Match.MoveReach(kid), walked = 0;
                    Vec2 prev = kid.Pos;
                    foreach (var p in o.Path)
                    {
                        int seg = Vec2.Dist(prev, p);
                        if (walked + seg > reach)
                        {
                            Vec2 stop = Vec2.Lerp(prev, p, (reach - walked) * 1000 / Mathf.Max(1, seg));
                            Circle(stop, 30, Palette.Brick, 0.08f);
                            break;
                        }
                        walked += seg;
                        prev = p;
                    }
                }
                Circle(Match.PathEnd(kid, o.Path), 40, teamColour, 0.07f);
            }
            if (o.HasThrow)
            {
                Vec2 origin = o.ThrowFirst ? kid.Pos : Match.PathEnd(kid, o.Path);
                Vec2 target = o.ThrowTarget;
                int range = Match.MaxRange(kid, o.Style);
                if (Vec2.Dist(origin, target) > range) target = origin + (target - origin).WithLength(range);
                bool clear = o.Style == ThrowStyle.Lob || match.IsLineOfFireClear(origin, target);
                var lr = Next(clear ? Color.white : Palette.Brick, w * 0.8f);
                var pts = new List<Vector3>();
                Vector3 a = Greybox.World(origin, 1.1f), b = Greybox.World(target, o.Style == ThrowStyle.Lob ? 0.1f : 1.1f);
                float apex = o.Style == ThrowStyle.Lob ? 1.5f + Vector3.Distance(a, b) / 4f : 0f;
                for (int i = 0; i <= 20; i++)
                {
                    float t = i / 20f;
                    pts.Add(Vector3.Lerp(a, b, t) + Vector3.up * (4f * apex * t * (1 - t)));
                }
                lr.positionCount = pts.Count;
                lr.SetPositions(pts.ToArray());
                Circle(target, o.Style == ThrowStyle.Lob ? Rules.LobSplashRadius + 30 : 35, clear ? Color.white : Palette.Brick, 0.06f);
            }
            if (o.HasBuild) Circle(o.BuildAt, Rules.BuiltWallRadius, Palette.SnowShadow, 0.1f);
        }

        public void Circle(Vec2 centre, int radiusCm, Color c, float width)
        {
            var lr = Next(c, width);
            const int n = 48;
            lr.positionCount = n + 1;
            Vector3 o = Greybox.World(centre, 0.04f);
            float r = radiusCm / 100f;
            for (int i = 0; i <= n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                lr.SetPosition(i, o + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
            }
        }

        LineRenderer Next(Color c, float width)
        {
            if (cursor >= lines.Count) lines.Add(box.Line(root, c, width, "PlanLine"));
            var lr = lines[cursor++];
            lr.sharedMaterial = box.Unlit(c);
            lr.widthMultiplier = width;
            return lr;
        }
    }
}
