using System.Collections.Generic;

namespace SnowballGame.Core
{
    /// <summary>
    /// A plain opponent for the prototype: reload when empty, otherwise move to a
    /// covered spot at a comfortable range and throw at the nearest kid.
    /// Plans from the current state only, exactly like a human in WEGO.
    /// </summary>
    public sealed class SimpleAi
    {
        readonly int team;
        readonly Rng rng;

        public SimpleAi(int team, int seed)
        {
            this.team = team;
            rng = new Rng(seed);
        }

        public List<KidOrder> Plan(Match match)
        {
            var orders = new List<KidOrder>();
            var reserved = new List<Vec2>();
            foreach (var k in match.Crew(team))
            {
                if (k.Soaked) continue;
                var o = PlanKid(match, k, reserved);
                if (o != null) orders.Add(o);
            }
            return orders;
        }

        KidOrder PlanKid(Match match, Kid k, List<Vec2> reserved)
        {
            Kid target = Nearest(match, k.Pos);
            if (target == null) return null;
            var order = new KidOrder(k.Id);

            int reach = Match.MoveReach(k) * 8 / 10;
            int lobRange = Match.MaxRange(k, ThrowStyle.Lob);
            int lineRange = Match.MaxRange(k, ThrowStyle.Line);
            int ideal = lineRange * 7 / 10;
            bool needsAmmo = k.Ammo == 0;

            Vec2 best = k.Pos;
            int bestScore = int.MinValue;
            const int step = 100;
            for (int dx = -reach; dx <= reach; dx += step)
                for (int dz = -reach; dz <= reach; dz += step)
                {
                    var c = new Vec2(k.Pos.X + dx, k.Pos.Z + dz);
                    if ((long)dx * dx + (long)dz * dz > (long)reach * reach) continue;
                    if (!match.IsWalkable(c) || !match.IsPathClear(k.Pos, c)) continue;
                    bool taken = false;
                    foreach (var r in reserved)
                        if (Vec2.DistSq(r, c) < 120L * 120) taken = true;
                    if (taken) continue;

                    int score = 0;
                    bool fresh = match.Map.TerrainAt(c) == Ground.Fresh;
                    if (fresh) score += needsAmmo ? 400 : 40;
                    if (match.IsNearCover(c)) score += 120;
                    if (!match.IsLineOfFireClear(target.Pos, c)) score += 150;
                    int d = Vec2.Dist(c, target.Pos);
                    if (!needsAmmo) score -= System.Math.Abs(d - ideal) / 5;
                    else score -= Vec2.Dist(c, k.Pos) / 10;
                    if (d < 400) score -= 300;
                    score += rng.Range(0, 60);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = c;
                    }
                }

            if (best != k.Pos) order.Path.Add(best);
            reserved.Add(best);

            if (!needsAmmo)
            {
                // Throw at whoever is nearest to where this kid will stand.
                Kid t = Nearest(match, best) ?? target;
                int dist = Vec2.Dist(best, t.Pos);
                if (match.IsLineOfFireClear(best, t.Pos) && dist <= lineRange)
                {
                    order.HasThrow = true;
                    order.Style = ThrowStyle.Line;
                    order.ThrowTarget = t.Pos;
                }
                else if (dist <= lobRange)
                {
                    order.HasThrow = true;
                    order.Style = ThrowStyle.Lob;
                    order.ThrowTarget = t.Pos;
                }
            }
            return order;
        }

        Kid Nearest(Match match, Vec2 from)
        {
            Kid best = null;
            long bestD = long.MaxValue;
            foreach (var e in match.Kids)
            {
                if (e.Team == team || e.Soaked) continue;
                long d = Vec2.DistSq(from, e.Pos);
                if (d < bestD)
                {
                    best = e;
                    bestD = d;
                }
            }
            return best;
        }
    }
}
