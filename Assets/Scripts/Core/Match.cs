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
        readonly List<Cloud> clouds = new List<Cloud>();
        int nextProjectileId = 1;
        int nextCoverId;
        int nextCloudId = 1;

        /// <summary>Powder clouds hanging in the air right now.</summary>
        public IReadOnlyList<Cloud> Clouds => clouds;

        sealed class Projectile
        {
            public int Id;
            public int OwnerId;
            public int Team;
            public ThrowStyle Style;
            public SnowballType Type;
            public Vec2 Pos;
            public Vec2 Start;
            public Vec2 Dir;
            public int RangeLeft;
            public Vec2 Landing;
            public int FlightTicks;
            public int Age;
            public int Apex;
            public int Speed;
            public int Radius;
            public bool Done;

            public SnowballSpec Spec => Rules.Spec(Type);
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
                var kid = new Kid
                {
                    Id = Kids.Count,
                    Team = team,
                    Def = def,
                    Pos = spawns[i % spawns.Count],
                    Facing = new Vec2(team == Home ? 100 : -100, 0),
                    Warmth = max,
                    MaxWarmth = max,
                    MakeType = def.Favourite,
                };
                // Kids turn up with one of their favourite and the rest packed
                // (boulders have to be rolled on the day).
                for (int n = 0; n < def.MaxAmmo; n++)
                    kid.Pouch.Add(n == 0 && def.Favourite != SnowballType.Boulder ? def.Favourite : SnowballType.Packed);
                Kids.Add(kid);
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

        /// <summary>
        /// True when a line drive from a to b would not hit cover or a house, and
        /// there's no powder cloud in the way to hide the target.
        /// </summary>
        public bool IsLineOfFireClear(Vec2 a, Vec2 b)
        {
            int len = Vec2.Dist(a, b);
            int steps = System.Math.Max(1, len / 25);
            for (int i = 1; i <= steps; i++)
            {
                Vec2 p = Vec2.Lerp(a, b, i * 1000 / steps);
                if (Map.TerrainAt(p) == Ground.Blocked) return false;
                if (CoverAt(p, Rules.ProjectileRadius) != null) return false;
                if (CloudAt(p) != null) return false;
            }
            return true;
        }

        /// <summary>True when any powder cloud sits on the line a→b.</summary>
        public bool IsSightBlocked(Vec2 a, Vec2 b)
        {
            if (clouds.Count == 0) return false;
            int len = Vec2.Dist(a, b);
            int steps = System.Math.Max(1, len / 40);
            for (int i = 0; i <= steps; i++)
                if (CloudAt(Vec2.Lerp(a, b, i * 1000 / steps)) != null) return true;
            return false;
        }

        Cloud CloudAt(Vec2 p)
        {
            foreach (var c in clouds)
                if (Vec2.DistSq(p, c.Pos) <= (long)c.Radius * c.Radius) return c;
            return null;
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
                // WorkTicks carries over: packing a boulder can take more than one turn.
                k.BuildDone = false;
                k.MovedThisTurn = false;
                k.Action = k.Soaked ? KidAction.Soaked : KidAction.Idle;
            }
            if (orders != null)
                foreach (var o in orders)
                    if (o != null && o.KidId >= 0 && o.KidId < Kids.Count && !Kids[o.KidId].Soaked)
                    {
                        Kids[o.KidId].Order = o;
                        if (o.Make.HasValue) Kids[o.KidId].MakeType = o.Make.Value;
                    }

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
            if (o.Make.HasValue) k.MakeType = o.Make.Value;
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
            if (k.SlowTicks > 0) k.SlowTicks--;

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
            // A boulder needs an empty pouch: the kid holds on to what they have until it's thrown.
            if (k.CanPack(k.MakeType) && Map.TerrainAt(k.Pos) == Ground.Fresh)
            {
                k.Action = KidAction.Pack;
                k.WorkTicks++;
                if (k.WorkTicks >= Rules.PackTicks(k.MakeType, k.Def.Craft))
                {
                    k.Pouch.Add(k.MakeType);
                    k.WorkTicks = 0;
                    log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.Packed, KidId = k.Id, Pos = k.Pos, Amount = (int)k.MakeType });
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
            if (k.SlowTicks > 0) step = step * Rules.SlowSpeedPermille / 1000;
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
            k.WorkTicks = 0;
            var ball = TakeBall(k, o.Ball);
            Launch(k, ball, ball == SnowballType.Boulder ? ThrowStyle.Roll : o.Style, o.ThrowTarget, tick, log);
        }

        /// <summary>Takes the asked-for snowball from the pouch, or the oldest one carried.</summary>
        static SnowballType TakeBall(Kid k, SnowballType? wanted)
        {
            int i = wanted.HasValue ? k.Pouch.IndexOf(wanted.Value) : -1;
            if (i < 0) i = 0;
            var ball = k.Pouch[i];
            k.Pouch.RemoveAt(i);
            return ball;
        }

        /// <summary>The snowball a throw order will actually use, for previews.</summary>
        public static SnowballType BallFor(Kid k, KidOrder o) =>
            o.Ball.HasValue && k.Carries(o.Ball.Value) ? o.Ball.Value : k.Pouch.Count > 0 ? k.Pouch[0] : SnowballType.Packed;

        void Launch(Kid k, SnowballType type, ThrowStyle style, Vec2 target, int tick, TurnLog log)
        {
            var spec = Rules.Spec(type);
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
            // Throwing blind into (or through) a powder cloud spoils the aim.
            int spreadPermille = spec.SpreadPermille;
            if (IsSightBlocked(k.Pos, k.Pos + to)) spreadPermille = spreadPermille * Rules.CloudBlindSpreadPermille / 1000;
            int miss = (6 - k.Def.Aim) * spreadPermille;
            var p = new Projectile { Id = nextProjectileId++, OwnerId = k.Id, Team = k.Team, Style = style, Type = type, Start = k.Pos };

            if (style == ThrowStyle.Roll)
            {
                p.Dir = to.WithLength(1000);
                p.Pos = k.Pos + p.Dir.WithLength(Rules.KidBodyRadius + Rules.BoulderStartRadius + 5);
                p.RangeLeft = range;
                p.Radius = Rules.BoulderStartRadius;
                p.Speed = Rules.BoulderSpeedPerTick;
            }
            else if (style == ThrowStyle.Line)
            {
                int spread = miss * dist / 40000;
                Vec2 side = to.Perp.WithLength(rng.Range(-spread, spread));
                Vec2 dir = (to + side).WithLength(1000);
                p.Dir = dir;
                p.Pos = k.Pos + dir.WithLength(Rules.KidBodyRadius + 15);
                p.RangeLeft = range;
                p.Speed = Rules.LineSpeedPerTick * spec.SpeedPermille / 1000;
            }
            else
            {
                int spread = (miss * 20 + dist * spreadPermille / 50) / 1000;
                Vec2 landing = k.Pos + to + new Vec2(rng.Range(-spread, spread), rng.Range(-spread, spread));
                p.Pos = k.Pos;
                p.Landing = landing;
                int speed = Rules.LobSpeedPerTick * spec.SpeedPermille / 1000;
                p.FlightTicks = System.Math.Max(Rules.LobMinFlightTicks * 1000 / spec.SpeedPermille, Vec2.Dist(k.Pos, landing) / speed);
                p.Apex = System.Math.Min(500, 150 + Vec2.Dist(k.Pos, landing) / 4);
            }
            projectiles.Add(p);
            log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.Thrown, KidId = k.Id, OtherId = p.Id, Pos = k.Pos, Amount = (int)type });
        }

        void StepProjectiles(int tick, TurnLog log)
        {
            foreach (var p in projectiles)
            {
                if (p.Style == ThrowStyle.Line) StepLine(p, tick, log);
                else if (p.Style == ThrowStyle.Roll) StepRoll(p, tick, log);
                else StepLob(p, tick, log);
            }
            projectiles.RemoveAll(p => p.Done);
            foreach (var c in clouds) c.TicksLeft--;
            clouds.RemoveAll(c => c.TicksLeft <= 0);
        }

        void StepLine(Projectile p, int tick, TurnLog log)
        {
            const int substeps = 4;
            int step = p.Speed / substeps;
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
                    if (p.Spec.MakesCloud) Splash(p, tick, log);
                    else
                    {
                        DamageCover(c, p, tick, log, Rules.CoverDamagePerHit);
                        p.Done = true;
                    }
                    return;
                }
                var victim = KidHitAt(p, p.Pos, 0);
                if (victim != null)
                {
                    if (p.Spec.MakesCloud) Splash(p, tick, log);
                    else
                    {
                        Chill(victim, p, p.Spec.LineChill, tick, log);
                        p.Done = true;
                    }
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

            var spec = p.Spec;
            if (spec.MakesCloud)
            {
                Splash(p, tick, log);
                return;
            }
            // Slush splatters everyone under it; other snowballs hit the kid nearest the landing spot.
            var victims = new List<Kid>();
            if (spec.HitsArea)
            {
                foreach (var k in Kids)
                {
                    if (k.Soaked || k.Team == p.Team) continue;
                    long r = k.Def.HitRadius + spec.Splash;
                    if (Vec2.DistSq(k.Pos, p.Landing) <= r * r) victims.Add(k);
                }
            }
            else
            {
                var v = KidHitAt(p, p.Landing, spec.Splash);
                if (v != null) victims.Add(v);
            }
            if (victims.Count == 0)
            {
                Splash(p, tick, log);
                return;
            }
            foreach (var victim in victims)
            {
                int chill = spec.LobChill;
                if (!victim.MovedThisTurn && IsNearCover(victim.Pos))
                    chill = chill * Rules.DuckedLobPermille / 1000;
                Chill(victim, p, chill, tick, log);
            }
            if (spec.HitsArea) log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.Splash, KidId = p.OwnerId, OtherId = p.Id, Pos = p.Landing, Amount = (int)p.Type });
            p.Done = true;
        }

        /// <summary>
        /// A boulder rolls along the ground, growing as it picks up snow. It flattens the
        /// first kid in its way (and shoves them back), thumps cover, and where it comes
        /// to rest it stays, as a new snowbank.
        /// </summary>
        void StepRoll(Projectile p, int tick, TurnLog log)
        {
            const int substeps = 2;
            int step = p.Speed / substeps;
            p.Radius = System.Math.Min(Rules.BoulderMaxRadius, p.Radius + Rules.BoulderGrowthPerTick);
            for (int s = 0; s < substeps && !p.Done; s++)
            {
                Vec2 next = p.Pos + p.Dir.WithLength(step);
                if (Map.TerrainAt(next) == Ground.Blocked)
                {
                    Settle(p, tick, log);
                    return;
                }
                var c = CoverAt(next, p.Radius);
                if (c != null)
                {
                    DamageCover(c, p, tick, log, Rules.BoulderCoverDamage);
                    Settle(p, tick, log);
                    return;
                }
                p.Pos = next;
                p.RangeLeft -= step;
                var victim = KidHitAt(p, p.Pos, p.Radius - Rules.ProjectileRadius);
                if (victim != null)
                {
                    Chill(victim, p, p.Spec.LineChill, tick, log);
                    Vec2 push = p.Dir.WithLength(10);
                    for (int i = 0; i < Rules.BoulderKnockback / 10; i++)
                    {
                        Vec2 to = victim.Pos + push;
                        if (!IsWalkable(to) || Vec2.DistSq(to, p.Pos) < Vec2.DistSq(victim.Pos, p.Pos)) break;
                        victim.Pos = to;
                    }
                    Settle(p, tick, log);
                    return;
                }
                if (p.RangeLeft <= 0)
                {
                    Settle(p, tick, log);
                    return;
                }
            }
        }

        /// <summary>A stopped boulder becomes a snowbank, unless there's no room for one.</summary>
        void Settle(Projectile p, int tick, TurnLog log)
        {
            p.Done = true;
            bool room = Map.TerrainAt(p.Pos) != Ground.Blocked && CoverAt(p.Pos, p.Radius - 10) == null;
            foreach (var kid in Kids)
            {
                long r = p.Radius + Rules.KidBodyRadius;
                if (!kid.Soaked && Vec2.DistSq(kid.Pos, p.Pos) < r * r) room = false;
            }
            if (!room)
            {
                log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.Splash, KidId = p.OwnerId, OtherId = p.Id, Pos = p.Pos, Amount = (int)p.Type });
                return;
            }
            var bank = new Cover
            {
                Id = nextCoverId++, Kind = CoverKind.Snowbank, Pos = p.Pos, Radius = p.Radius,
                Hp = Rules.BoulderCoverHp, MaxHp = Rules.BoulderCoverHp,
            };
            Cover.Add(bank);
            log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.CoverBuilt, KidId = p.OwnerId, OtherId = bank.Id, Pos = bank.Pos });
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
            Vec2 travel = p.Style == ThrowStyle.Lob ? p.Landing - p.Start : p.Dir;
            if (Vec2.Dot(travel, victim.Facing) > 0)
                amount = amount * Rules.BackHitBonusPermille / 1000;
            victim.Warmth -= amount;
            log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.HitKid, KidId = p.OwnerId, OtherId = victim.Id, Pos = victim.Pos, Amount = amount });
            int slow = p.Spec.SlowTicks;
            if (slow > 0 && victim.Warmth > 0)
            {
                victim.SlowTicks = System.Math.Max(victim.SlowTicks, slow);
                log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.Slowed, KidId = p.OwnerId, OtherId = victim.Id, Pos = victim.Pos, Amount = slow });
            }
            if (victim.Warmth <= 0)
            {
                victim.Warmth = 0;
                victim.Soaked = true;
                victim.Action = KidAction.Soaked;
                log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.Soaked, KidId = victim.Id, OtherId = p.OwnerId, Pos = victim.Pos });
            }
        }

        void DamageCover(Cover c, Projectile p, int tick, TurnLog log, int damage)
        {
            log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.CoverHit, KidId = p.OwnerId, OtherId = c.Id, Pos = p.Pos, Amount = (int)p.Type });
            if (!c.Destructible) return;
            c.Hp -= damage;
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
            Vec2 at = p.Style == ThrowStyle.Lob ? p.Landing : p.Pos;
            log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.Splash, KidId = p.OwnerId, OtherId = p.Id, Pos = at, Amount = (int)p.Type });
            var spec = p.Spec;
            if (!spec.MakesCloud) return;
            var cloud = new Cloud { Id = nextCloudId++, Pos = at, Radius = spec.CloudRadius, TicksLeft = spec.CloudTicks };
            clouds.Add(cloud);
            log.Events.Add(new MatchEvent { Tick = tick, Kind = EventKind.CloudCreated, KidId = p.OwnerId, OtherId = cloud.Id, Pos = at, Amount = cloud.Radius });
        }

        Frame Snapshot()
        {
            var f = new Frame { Kids = new KidFrame[Kids.Count] };
            for (int i = 0; i < Kids.Count; i++)
            {
                var k = Kids[i];
                f.Kids[i] = new KidFrame
                {
                    Id = k.Id, Pos = k.Pos, Facing = k.Facing, Warmth = k.Warmth, Ammo = k.Ammo,
                    Pouch = k.Pouch.ToArray(), Slowed = k.SlowTicks > 0,
                    Action = k.Soaked ? KidAction.Soaked : k.Action,
                };
            }
            foreach (var p in projectiles)
            {
                int h;
                if (p.Style == ThrowStyle.Line) h = Rules.LineHeight;
                else if (p.Style == ThrowStyle.Roll) h = p.Radius;
                else
                {
                    int t = System.Math.Min(p.Age, p.FlightTicks), T = p.FlightTicks;
                    h = 100 + 4 * p.Apex * t * (T - t) / (T * T) - 100 * t / T;
                }
                f.Projectiles.Add(new ProjectileFrame { Id = p.Id, Style = p.Style, Type = p.Type, Pos = p.Pos, Height = h, Radius = p.Radius });
            }
            foreach (var c in clouds)
                f.Clouds.Add(new CloudFrame { Id = c.Id, Pos = c.Pos, Radius = c.Radius, TicksLeft = c.TicksLeft });
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
