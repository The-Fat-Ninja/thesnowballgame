using System.Collections.Generic;

namespace SnowballGame.Core
{
    /// <summary>
    /// A snowball fight between two crews, resolved one WEGO turn at a time:
    /// both crews' orders go in, Rules.TurnTicks of simulation come out.
    /// Fully deterministic for a given map, crews, seed and order sequence.
    /// </summary>
    public sealed class Match
    {
        public const int Home = 0;
        public const int Away = 1;

        public readonly MapDef Map;
        public readonly List<Kid> Kids = new List<Kid>();
        public readonly List<Cover> Cover = new List<Cover>();
        public int Turn { get; private set; } = 1;
        public MatchOutcome Outcome { get; private set; } = MatchOutcome.InProgress;

        readonly Rng rng;
        readonly List<Projectile> projectiles = new List<Projectile>();
        int nextProjectileId = 1;
        int nextCoverId;

        sealed class Projectile
        {
            public int Id;
            public int OwnerId;
            public int Team;
            public ThrowStyle Style;
            public Vec2 Pos;
            public Vec2 Start;
            public Vec2 Dir;
            public int RangeLeft;
            public Vec2 Landing;
            public int FlightTicks;
            public int Age;
            public int Apex;
            public bool Done;
        }

        public Match(MapDef map, IList<KidDef> home, IList<KidDef> away, int seed)
        {
            Map = map;
            rng = new Rng(seed);
            foreach (var c in map.Cover)
                Cover.Add(new Cover { Id = nextCoverId++, Kind = c.Kind, Pos = c.Pos, Radius = c.Radius, Hp = c.Hp, MaxHp = c.Hp });
            AddCrew(home, Home);
            AddCrew(away, Away);
        }

        void AddCrew(IList<KidDef> crew, int team)
        {
            var spawns = Map.Spawns[team];
            for (int i = 0; i < crew.Count; i++)
            {
                var def = crew[i];
                int max = Rules.MaxWarmth(def.Grit);
                Kids.Add(new Kid
                {
                    Id = Kids.Count,
                    Team = team,
                    Def = def,
                    Pos = spawns[i % spawns.Count],
                    Facing = new Vec2(team == Home ? 100 : -100, 0),
                    Warmth = max,
                    MaxWarmth = max,
                    Ammo = def.MaxAmmo,
                });
            }
        }

        public Kid KidById(int id) => Kids[id];

        public IEnumerable<Kid> Crew(int team)
        {
            foreach (var k in Kids)
                if (k.Team == team) yield return k;
        }

        // ------------------------------------------------------------ queries

        /// <summary>True when a kid could stand at p (inside the map, not in a house or cover).</summary>
        public bool IsWalkable(Vec2 p)
        {
            if (Map.TerrainAt(p) == Ground.Blocked) return false;
            if (p.X < Rules.KidBodyRadius || p.Z < Rules.KidBodyRadius ||
                p.X > Map.Width - Rules.KidBodyRadius || p.Z > Map.Depth - Rules.KidBodyRadius) return false;
            foreach (var c in Cover)
            {
                if (!c.Alive) continue;
                long r = c.Radius + Rules.KidBodyRadius;
                if (Vec2.DistSq(p, c.Pos) < r * r) return false;
            }
            return true;
        }

        /// <summary>True when a kid can walk the straight line a→b.</summary>
        public bool IsPathClear(Vec2 a, Vec2 b)
        {
            int len = Vec2.Dist(a, b);
            int steps = System.Math.Max(1, len / 20);
            for (int i = 1; i <= steps; i++)
                if (!IsWalkable(Vec2.Lerp(a, b, i * 1000 / steps))) return false;
            return true;
        }

        /// <summary>True when a line drive from a to b would not hit cover or a house.</summary>
        public bool IsLineOfFireClear(Vec2 a, Vec2 b)
        {
            int len = Vec2.Dist(a, b);
            int steps = System.Math.Max(1, len / 25);
            for (int i = 1; i <= steps; i++)
            {
                Vec2 p = Vec2.Lerp(a, b, i * 1000 / steps);
                if (Map.TerrainAt(p) == Ground.Blocked) return false;
                if (CoverAt(p, Rules.ProjectileRadius) != null) return false;
            }
            return true;
        }

        Cover CoverAt(Vec2 p, int extra)
        {
            foreach (var c in Cover)
            {
                if (!c.Alive) continue;
                long r = c.Radius + extra;
                if (Vec2.DistSq(p, c.Pos) <= r * r) return c;
            }
            return null;
        }

        /// <summary>A kid near cover who holds still ducks: lobs only do half.</summary>
        public bool IsNearCover(Vec2 p)
        {
            foreach (var c in Cover)
            {
                if (!c.Alive) continue;
                long r = c.Radius + Rules.KidBodyRadius + Rules.DuckDistance;
                if (Vec2.DistSq(p, c.Pos) <= r * r) return true;
            }
            return false;
        }

        public static int MaxRange(Kid k, ThrowStyle style) => Rules.MaxRange(style, k.Def.Arm);

        /// <summary>Where the kid will stand after walking the given path (ignores blocking).</summary>
        public static Vec2 PathEnd(Kid k, IList<Vec2> path) => path.Count == 0 ? k.Pos : path[path.Count - 1];

        /// <summary>Roughly how far the kid can walk in one turn on fresh snow.</summary>
        public static int MoveReach(Kid k) => k.SpeedPerTick * Rules.FreshSnowSpeedPermille / 1000 * Rules.TurnTicks;

        // --------------------------------------------------------- resolution

        /// <summary>
        /// Resolves one turn. Orders for soaked or unknown kids are ignored; kids
        /// without orders stand still and pack snow if they can.
        /// </summary>
        public TurnLog Resolve(IEnumerable<KidOrder> orders)
        {
            var log = new TurnLog { Turn = Turn };
            if (Outcome != MatchOutcome.InProgress) return log;

            foreach (var k in Kids)
            {
                k.Order = null;
                k.PathIndex = 0;
                k.Thrown = false;
                k.Windup = 0;
                k.WorkTicks = 0;
                k.BuildDone = false;
                k.MovedThisTurn = false;
                k.Action = k.Soaked ? KidAction.Soaked : KidAction.Idle;
            }
            if (orders != null)
                foreach (var o in orders)
                    if (o != null && o.KidId >= 0 && o.KidId < Kids.Count && !Kids[o.KidId].Soaked)
                        Kids[o.KidId].Order = o;

            log.Frames.Add(Snapshot());
            for (int tick = 1; tick <= Rules.TurnTicks; tick++)
            {
                foreach (var k in Kids)
                    if (!k.Soaked) StepKid(k, tick, log);
                StepProjectiles(tick, log);
                log.Frames.Add(Snapshot());
            }

            // Snowballs still in the air carry on into next turn's playback.
            Turn++;
            UpdateOutcome();
            return log;
        }

        // ---------------------------------------------------------- real time

        /// <summary>Ticks played so far in real-time mode.</summary>
        public int RealtimeTick { get; private set; }

        /// <summary>Ticks over which "held still" counts for ducking in real-time mode (1 s).</summary>
        public const int RealtimeDuckWindow = 10;

        /// <summary>
        /// Real-time mode: replaces a kid's order straight away (the player or AI
        /// changed their mind). The same rules as WEGO, just without the turn pause.
        /// </summary>
        public void SetRealtimeOrder(KidOrder o)
        {
            if (o == null || o.KidId < 0 || o.KidId >= Kids.Count) return;
            var k = Kids[o.KidId];
            if (k.Soaked) return;
            k.Order = o;
            k.PathIndex = 0;
            k.Thrown = false;
            k.Windup = 0;
            k.BuildDone = false;
        }

        /// <summary>
        /// Real-time mode: advances the match by a few ticks with the current orders.
        /// The returned log has ticks + 1 frames for playback. The match ends at the
        /// same streetlights limit as WEGO (Rules.TurnLimit x Rules.TurnTicks ticks).
        /// </summary>
        public TurnLog StepRealtime(int ticks)
        {
            var log = new TurnLog { Turn = Turn };
            if (Outcome != MatchOutcome.InProgress) return log;
            log.Frames.Add(Snapshot());
            for (int i = 1; i <= ticks; i++)
            {
                RealtimeTick++;
                if (RealtimeTick % RealtimeDuckWindow == 0)
                    foreach (var k in Kids) k.MovedThisTurn = false;
                foreach (var k in Kids)
                    if (!k.Soaked) StepKid(k, i, log);
                StepProjectiles(i, log);
                log.Frames.Add(Snapshot());
            }
            Turn = 1 + RealtimeTick / Rules.TurnTicks;
            UpdateOutcome();
            return log;
        }

        void StepKid(Kid k, int tick, TurnLog log)
        {
            var o = k.Order;
            k.Action = KidAction.Idle;

            if (o != null && o.HasThrow && o.ThrowFirst && !k.Thrown && k.Ammo > 0)
            {
                DoWindupOrThrow(k, tick, log);
                return;
            }

            if (o != null && k.PathIndex < o.Path.Count)
            {
                Walk(k, o);
                return;
            }

            if (o != null && o.HasThrow && !k.Thrown && k.Ammo > 0)
            {
                DoWindupOrThrow(k, tick, log);
                return;
            }

            if (o != null && o.HasBuild && !k.BuildDone)
            {
                if (Vec2.Dist(k.Pos, o.BuildAt) <= Rules.BuildReach && Map.TerrainAt(k.Pos) == Ground.Fresh && CanBuildAt(o.BuildAt))
                {
                    k.Action = KidAction.Build;
                    k.WorkTicks++;
                    if (k.WorkTicks >= Rules.BuildTicks(k.Def.Craft))
                    {
                        var wall = new Cover
                        {
                            Id = nextCoverId++, Kind = CoverKind.SnowWall, Pos = o.BuildAt,
                            Radius = Rules.BuiltWallRadius, Hp = Rules.BuiltWallHp, MaxHp = Rules.BuiltWallHp,
                        };
                        Cover.Add(wall);
                        log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.CoverBuilt, KidId = k.Id, OtherId = wall.Id, Pos = wall.Pos });
                        k.BuildDone = true;
                        k.WorkTicks = 0;
                    }
                    return;
                }
                k.BuildDone = true; // can't build there; fall through to packing
            }

            // Standing order: pack snow when there's fresh snow underfoot.
            if (k.Ammo < k.MaxAmmo && Map.TerrainAt(k.Pos) == Ground.Fresh)
            {
                k.Action = KidAction.Pack;
                k.WorkTicks++;
                if (k.WorkTicks >= Rules.PackTicks(k.Def.Craft))
                {
                    k.Ammo++;
                    k.WorkTicks = 0;
                    log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.Packed, KidId = k.Id, Pos = k.Pos, Amount = k.Ammo });
                }
            }
        }

        bool CanBuildAt(Vec2 p)
        {
            if (Map.TerrainAt(p) == Ground.Blocked) return false;
            foreach (var kid in Kids)
            {
                if (kid.Soaked) continue;
                long r = Rules.BuiltWallRadius + Rules.KidBodyRadius;
                if (Vec2.DistSq(kid.Pos, p) < r * r) return false;
            }
            return CoverAt(p, Rules.BuiltWallRadius - 10) == null;
        }

        void Walk(Kid k, KidOrder o)
        {
            Vec2 target = o.Path[k.PathIndex];
            int permille = Map.TerrainAt(k.Pos) switch
            {
                Ground.Packed => Rules.PackedSpeedPermille,
                Ground.Ice => Rules.IceSpeedPermille,
                _ => Rules.FreshSnowSpeedPermille,
            };
            int step = k.SpeedPerTick * permille / 1000;
            Vec2 next = Vec2.MoveTowards(k.Pos, target, step);
            if (next == k.Pos || !IsPathClear(k.Pos, next))
            {
                // Blocked: abandon the rest of the walk and get on with the job.
                k.PathIndex = o.Path.Count;
                return;
            }
            Vec2 delta = next - k.Pos;
            if (delta.LengthSq > 0) k.Facing = delta.WithLength(100);
            k.Pos = next;
            k.MovedThisTurn = true;
            k.Action = KidAction.Walk;
            k.WorkTicks = 0;
            if (k.Pos == target) k.PathIndex++;
        }

        void DoWindupOrThrow(Kid k, int tick, TurnLog log)
        {
            var o = k.Order;
            Vec2 aim = o.ThrowTarget - k.Pos;
            if (aim.LengthSq > 0) k.Facing = aim.WithLength(100);
            if (k.Windup < Rules.WindupTicks)
            {
                k.Windup++;
                k.Action = KidAction.Windup;
                return;
            }
            k.Action = KidAction.Throw;
            k.Thrown = true;
            k.Ammo--;
            k.WorkTicks = 0;
            Launch(k, o.Style, o.ThrowTarget, tick, log);
        }

        void Launch(Kid k, ThrowStyle style, Vec2 target, int tick, TurnLog log)
        {
            int range = Rules.MaxRange(style, k.Def.Arm);
            Vec2 to = target - k.Pos;
            int dist = to.Length;
            if (dist == 0)
            {
                to = k.Facing;
                dist = 1;
            }
            if (dist > range)
            {
                to = to.WithLength(range);
                dist = range;
            }
            int miss = 6 - k.Def.Aim;
            var p = new Projectile { Id = nextProjectileId++, OwnerId = k.Id, Team = k.Team, Style = style, Start = k.Pos };

            if (style == ThrowStyle.Line)
            {
                int spread = miss * dist / 40;
                Vec2 side = to.Perp.WithLength(rng.Range(-spread, spread));
                Vec2 dir = (to + side).WithLength(1000);
                p.Dir = dir;
                p.Pos = k.Pos + dir.WithLength(Rules.KidBodyRadius + 15);
                p.RangeLeft = range;
            }
            else
            {
                int spread = miss * 20 + dist / 50;
                Vec2 landing = k.Pos + to + new Vec2(rng.Range(-spread, spread), rng.Range(-spread, spread));
                p.Pos = k.Pos;
                p.Landing = landing;
                p.FlightTicks = System.Math.Max(Rules.LobMinFlightTicks, Vec2.Dist(k.Pos, landing) / Rules.LobSpeedPerTick);
                p.Apex = System.Math.Min(500, 150 + Vec2.Dist(k.Pos, landing) / 4);
            }
            projectiles.Add(p);
            log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.Thrown, KidId = k.Id, OtherId = p.Id, Pos = k.Pos, Amount = (int)style });
        }

        void StepProjectiles(int tick, TurnLog log)
        {
            foreach (var p in projectiles)
            {
                if (p.Style == ThrowStyle.Line) StepLine(p, tick, log);
                else StepLob(p, tick, log);
            }
            projectiles.RemoveAll(p => p.Done);
        }

        void StepLine(Projectile p, int tick, TurnLog log)
        {
            const int substeps = 4;
            int step = Rules.LineSpeedPerTick / substeps;
            for (int s = 0; s < substeps && !p.Done; s++)
            {
                p.Pos += p.Dir.WithLength(step);
                p.RangeLeft -= step;

                if (Map.TerrainAt(p.Pos) == Ground.Blocked)
                {
                    Splash(p, tick, log);
                    return;
                }
                var c = CoverAt(p.Pos, Rules.ProjectileRadius);
                if (c != null)
                {
                    DamageCover(c, p, tick, log);
                    p.Done = true;
                    return;
                }
                var victim = KidHitAt(p, p.Pos, 0);
                if (victim != null)
                {
                    Chill(victim, p, Rules.LineChill, tick, log);
                    p.Done = true;
                    return;
                }
                if (p.RangeLeft <= 0)
                {
                    Splash(p, tick, log);
                    return;
                }
            }
        }

        void StepLob(Projectile p, int tick, TurnLog log)
        {
            p.Age++;
            p.Pos = Vec2.Lerp(p.Start, p.Landing, System.Math.Min(1000, p.Age * 1000 / p.FlightTicks));
            if (p.Age < p.FlightTicks) return;

            var victim = KidHitAt(p, p.Landing, Rules.LobSplashRadius);
            if (victim != null)
            {
                int chill = Rules.LobChill;
                if (!victim.MovedThisTurn && IsNearCover(victim.Pos))
                    chill = chill * Rules.DuckedLobPermille / 1000;
                Chill(victim, p, chill, tick, log);
                p.Done = true;
                return;
            }
            Splash(p, tick, log);
        }

        Kid KidHitAt(Projectile p, Vec2 at, int extra)
        {
            Kid best = null;
            long bestD = long.MaxValue;
            foreach (var k in Kids)
            {
                if (k.Soaked || k.Team == p.Team) continue;
                long r = k.Def.HitRadius + Rules.ProjectileRadius + extra;
                long d = Vec2.DistSq(k.Pos, at);
                if (d <= r * r && d < bestD)
                {
                    best = k;
                    bestD = d;
                }
            }
            return best;
        }

        void Chill(Kid victim, Projectile p, int amount, int tick, TurnLog log)
        {
            // A hit from behind (snowball travelling the way the kid faces) stings more.
            Vec2 travel = p.Style == ThrowStyle.Line ? p.Dir : p.Landing - p.Start;
            if (Vec2.Dot(travel, victim.Facing) > 0)
                amount = amount * Rules.BackHitBonusPermille / 1000;
            victim.Warmth -= amount;
            log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.HitKid, KidId = p.OwnerId, OtherId = victim.Id, Pos = victim.Pos, Amount = amount });
            if (victim.Warmth <= 0)
            {
                victim.Warmth = 0;
                victim.Soaked = true;
                victim.Action = KidAction.Soaked;
                log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.Soaked, KidId = victim.Id, OtherId = p.OwnerId, Pos = victim.Pos });
            }
        }

        void DamageCover(Cover c, Projectile p, int tick, TurnLog log)
        {
            log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.CoverHit, KidId = p.OwnerId, OtherId = c.Id, Pos = p.Pos });
            if (!c.Destructible) return;
            c.Hp -= Rules.CoverDamagePerHit;
            if (c.Hp <= 0)
            {
                c.Hp = 0;
                c.Alive = false;
                log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.CoverDestroyed, KidId = p.OwnerId, OtherId = c.Id, Pos = c.Pos });
            }
        }

        void Splash(Projectile p, int tick, TurnLog log)
        {
            p.Done = true;
            log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.Splash, KidId = p.OwnerId, OtherId = p.Id, Pos = p.Style == ThrowStyle.Lob ? p.Landing : p.Pos });
        }

        Frame Snapshot()
        {
            var f = new Frame { Kids = new KidFrame[Kids.Count] };
            for (int i = 0; i < Kids.Count; i++)
            {
                var k = Kids[i];
                f.Kids[i] = new KidFrame { Id = k.Id, Pos = k.Pos, Facing = k.Facing, Warmth = k.Warmth, Ammo = k.Ammo, Action = k.Soaked ? KidAction.Soaked : k.Action };
            }
            foreach (var p in projectiles)
            {
                int h;
                if (p.Style == ThrowStyle.Line) h = Rules.LineHeight;
                else
                {
                    int t = System.Math.Min(p.Age, p.FlightTicks), T = p.FlightTicks;
                    h = 100 + 4 * p.Apex * t * (T - t) / (T * T) - 100 * t / T;
                }
                f.Projectiles.Add(new ProjectileFrame { Id = p.Id, Style = p.Style, Pos = p.Pos, Height = h });
            }
            return f;
        }

        void UpdateOutcome()
        {
            bool homeUp = false, awayUp = false;
            int homeWarmth = 0, awayWarmth = 0;
            foreach (var k in Kids)
            {
                if (k.Soaked) continue;
                int pct = k.Warmth * 100 / k.MaxWarmth;
                if (k.Team == Home) { homeUp = true; homeWarmth += pct; }
                else { awayUp = true; awayWarmth += pct; }
            }
            if (!homeUp && !awayUp) Outcome = MatchOutcome.Draw;
            else if (!awayUp) Outcome = MatchOutcome.HomeWins;
            else if (!homeUp) Outcome = MatchOutcome.AwayWins;
            else if (Turn > Rules.TurnLimit)
                Outcome = homeWarmth > awayWarmth ? MatchOutcome.HomeWins
                    : awayWarmth > homeWarmth ? MatchOutcome.AwayWins : MatchOutcome.Draw;
        }
    }
}
