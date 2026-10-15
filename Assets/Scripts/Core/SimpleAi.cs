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
        readonly Dictionary<int, Vec2> lastDestination = new Dictionary<int, Vec2>();

        /// <summary>
        /// Real-time play: re-planned every second or so, the AI sticks to the spot it
        /// was heading for, throws from where it stands and leads moving targets.
        /// </summary>
        public bool Realtime;

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

        /// <summary>Plans a single kid (real-time controllers re-plan kids one at a time).</summary>
        public KidOrder PlanOne(Match match, Kid k)
        {
            if (k.Soaked || k.Team != team) return null;
            var reserved = new List<Vec2>();
            foreach (var kv in lastDestination)
                if (kv.Key != k.Id && !match.KidById(kv.Key).Soaked) reserved.Add(kv.Value);
            return PlanKid(match, k, reserved);
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
                    if (Realtime && lastDestination.TryGetValue(k.Id, out var prev) && Vec2.DistSq(prev, c) < 150L * 150)
                        score += 160;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = c;
                    }
                }

            if (best != k.Pos) order.Path.Add(best);
            reserved.Add(best);
            lastDestination[k.Id] = best;

            if (!needsAmmo)
            {
                // WEGO: throw from where this kid will stand. Real time: throw now, from here.
                Vec2 from = Realtime ? k.Pos : best;
                Kid t = Nearest(match, from) ?? target;
                int dist = Vec2.Dist(from, t.Pos);
                Vec2 aim = t.Pos;
                if (Realtime && t.Action == KidAction.Walk)
                {
                    // Lead a running kid by roughly the snowball's flight time.
                    int flight = System.Math.Max(Rules.LobMinFlightTicks, dist / Rules.LobSpeedPerTick);
                    aim = t.Pos + t.Facing.WithLength(t.SpeedPerTick * flight * 7 / 10);
                }
                if (match.IsLineOfFireClear(from, aim) && dist <= lineRange)
                {
                    order.HasThrow = true;
                    order.Style = ThrowStyle.Line;
                    order.ThrowTarget = Realtime ? t.Pos : aim;
                }
                else if (dist <= lobRange)
                {
                    order.HasThrow = true;
                    order.Style = ThrowStyle.Lob;
                    order.ThrowTarget = aim;
                }
                order.ThrowFirst = Realtime;
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
