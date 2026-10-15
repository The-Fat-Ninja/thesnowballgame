using System.Collections.Generic;
using NUnit.Framework;
using SnowballGame.Core;

namespace SnowballGame.Tests
{
    public class MatchTests
    {
        static MapDef OpenField()
        {
            var map = new MapDef("Field", 40, 30);
            map.Spawns[0].Add(new Vec2(1000, 1500));
            map.Spawns[0].Add(new Vec2(1000, 1000));
            map.Spawns[1].Add(new Vec2(2000, 1500));
            map.Spawns[1].Add(new Vec2(2000, 1000));
            return map;
        }

        static KidDef Plain(string name = "Kid") => new KidDef(name, "Thrower", 3, 5, 3, 3, 3, "#FFFFFF");

        static Match Duel(MapDef map, int seed = 1) =>
            new Match(map, new List<KidDef> { Plain("A") }, new List<KidDef> { Plain("B") }, seed);

        static KidOrder Throw(int kid, ThrowStyle style, Vec2 at) =>
            new KidOrder(kid) { HasThrow = true, Style = style, ThrowTarget = at };

        [Test]
        public void LineDriveHitsAStandingKid()
        {
            var m = Duel(OpenField());
            var b = m.KidById(1);
            int before = b.Warmth;
            var log = m.Resolve(new[] { Throw(0, ThrowStyle.Line, b.Pos) });
            Assert.Less(b.Warmth, before);
            Assert.AreEqual(1, log.Events.FindAll(e => e.Kind == EventKind.Thrown).Count);
        }

        [Test]
        public void ThrowingSpendsAmmoOffFreshSnow()
        {
            var map = OpenField();
            map.Fill(0, 0, 40, 30, Ground.Packed);
            var m = Duel(map);
            m.Resolve(new[] { Throw(0, ThrowStyle.Line, m.KidById(1).Pos) });
            Assert.AreEqual(1, m.KidById(0).Ammo);
        }

        [Test]
        public void LineDriveIsStoppedByCover()
        {
            var map = OpenField();
            map.AddCover(CoverKind.Car, 1500, 1500, 100);
            var m = Duel(map);
            var b = m.KidById(1);
            int before = b.Warmth;
            var log = m.Resolve(new[] { Throw(0, ThrowStyle.Line, b.Pos) });
            Assert.AreEqual(before, b.Warmth);
            Assert.IsTrue(log.Events.Exists(e => e.Kind == EventKind.CoverHit));
        }

        [Test]
        public void LobArcsOverCover()
        {
            var map = OpenField();
            map.AddCover(CoverKind.Car, 1500, 1500, 100);
            var m = Duel(map);
            var b = m.KidById(1);
            int before = b.Warmth;
            m.Resolve(new[] { Throw(0, ThrowStyle.Lob, b.Pos) });
            Assert.Less(b.Warmth, before);
        }

        [Test]
        public void LobCanBeDodgedByMoving()
        {
            var m = Duel(OpenField());
            var b = m.KidById(1);
            int before = b.Warmth;
            var dodge = new KidOrder(1);
            dodge.Path.Add(new Vec2(2000, 2400));
            m.Resolve(new[] { Throw(0, ThrowStyle.Lob, b.Pos), dodge });
            Assert.AreEqual(before, b.Warmth);
        }

        [Test]
        public void SnowbankBreaksAfterRepeatedHits()
        {
            var map = OpenField();
            map.AddCover(CoverKind.Snowbank, 1500, 1500, 90);
            var defs = new List<KidDef> { Plain("A") };
            defs[0].MaxAmmo = 3;
            var m = new Match(map, defs, new List<KidDef> { Plain("B") }, 3);
            var target = m.KidById(1).Pos;
            for (int i = 0; i < 3; i++)
                m.Resolve(new[] { Throw(0, ThrowStyle.Line, target) });
            Assert.IsFalse(m.Cover[0].Alive);
        }

        [Test]
        public void KidIsSoakedAtZeroWarmth()
        {
            var m = Duel(OpenField());
            var b = m.KidById(1);
            b.Warmth = 1;
            var log = m.Resolve(new[] { Throw(0, ThrowStyle.Line, b.Pos) });
            Assert.IsTrue(b.Soaked);
            Assert.IsTrue(log.Events.Exists(e => e.Kind == EventKind.Soaked));
            Assert.AreEqual(MatchOutcome.HomeWins, m.Outcome);
        }

        [Test]
        public void PackingOnlyWorksOnFreshSnow()
        {
            var map = OpenField();
            map.Fill(0, 0, 40, 30, Ground.Packed);
            var m = Duel(map);
            m.KidById(0).Ammo = 0;
            m.Resolve(new KidOrder[0]);
            Assert.AreEqual(0, m.KidById(0).Ammo);

            var m2 = Duel(OpenField());
            m2.KidById(0).Ammo = 0;
            m2.Resolve(new KidOrder[0]);
            Assert.Greater(m2.KidById(0).Ammo, 0);
        }

        [Test]
        public void WalkingStopsAtCover()
        {
            var map = OpenField();
            map.AddCover(CoverKind.Car, 1300, 1500, 100);
            var m = Duel(map);
            var walk = new KidOrder(0);
            walk.Path.Add(new Vec2(1600, 1500));
            m.Resolve(new[] { walk });
            var a = m.KidById(0);
            Assert.Less(a.Pos.X, 1300 - 100);
            Assert.IsTrue(m.IsWalkable(a.Pos));
        }

        [Test]
        public void BuilderRaisesASnowWall()
        {
            var m = Duel(OpenField());
            int before = m.Cover.Count;
            m.Resolve(new[] { new KidOrder(0) { HasBuild = true, BuildAt = new Vec2(1120, 1500) } });
            Assert.AreEqual(before + 1, m.Cover.Count);
            Assert.AreEqual(CoverKind.SnowWall, m.Cover[m.Cover.Count - 1].Kind);
        }

        [Test]
        public void SameSeedSameMatch()
        {
            string Run(int seed)
            {
                var m = new Match(Maps.MapleCourt(), Roster.MapleCourtCrew(), Roster.HillRoadHooligans(), seed);
                var home = new SimpleAi(Match.Home, seed + 1);
                var away = new SimpleAi(Match.Away, seed + 2);
                var sb = new System.Text.StringBuilder();
                while (m.Outcome == MatchOutcome.InProgress)
                {
                    var orders = home.Plan(m);
                    orders.AddRange(away.Plan(m));
                    m.Resolve(orders);
                    foreach (var k in m.Kids) sb.Append(k.Pos).Append(k.Warmth).Append(';');
                }
                return sb.Append(m.Outcome).ToString();
            }
            Assert.AreEqual(Run(42), Run(42));
            Assert.AreNotEqual(Run(42), Run(43));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void AiMatchesFinishOnMapleCourt(int squad)
        {
            int decisive = 0;
            for (int seed = 1; seed <= 10; seed++)
            {
                var m = new Match(Maps.MapleCourt(), Roster.Take(Roster.MapleCourtCrew(), squad),
                    Roster.Take(Roster.HillRoadHooligans(), squad), seed);
                var home = new SimpleAi(Match.Home, seed * 7);
                var away = new SimpleAi(Match.Away, seed * 13);
                int guard = 0;
                while (m.Outcome == MatchOutcome.InProgress && guard++ < 50)
                {
                    var orders = home.Plan(m);
                    orders.AddRange(away.Plan(m));
                    var log = m.Resolve(orders);
                    Assert.AreEqual(Rules.TurnTicks + 1, log.Frames.Count);
                    foreach (var k in m.Kids)
                        Assert.IsTrue(m.IsWalkable(k.Pos) || k.Soaked, $"{k.Def.Name} stuck at {k.Pos}");
                }
                Assert.AreNotEqual(MatchOutcome.InProgress, m.Outcome);
                if (m.Outcome != MatchOutcome.Draw) decisive++;
                bool anySoaked = m.Kids.Exists(k => k.Soaked);
                bool anyChilled = m.Kids.Exists(k => k.Warmth < k.MaxWarmth);
                Assert.IsTrue(anySoaked || anyChilled, "AI never landed a hit");
            }
            Assert.Greater(decisive, 0);
        }

        static string RunRealtime(int seed, out Match m)
        {
            m = new Match(Maps.MapleCourt(), Roster.MapleCourtCrew(), Roster.HillRoadHooligans(), seed);
            var home = new SimpleAi(Match.Home, seed + 1) { Realtime = true };
            var away = new SimpleAi(Match.Away, seed + 2) { Realtime = true };
            var sb = new System.Text.StringBuilder();
            int guard = 0;
            while (m.Outcome == MatchOutcome.InProgress && guard++ < 1000)
            {
                // Each kid re-plans about once a second, staggered so they don't all switch at once.
                foreach (var k in m.Kids)
                    if (!k.Soaked && (m.RealtimeTick + k.Id * 3) % 10 == 0)
                        m.SetRealtimeOrder((k.Team == Match.Home ? home : away).PlanOne(m, k));
                var log = m.StepRealtime(1);
                Assert.AreEqual(2, log.Frames.Count);
                foreach (var k in m.Kids) sb.Append(k.Pos).Append(k.Warmth).Append(';');
            }
            return sb.Append(m.Outcome).ToString();
        }

        [Test]
        public void RealtimeMatchFinishesAndIsDeterministic()
        {
            string a = RunRealtime(5, out var m);
            Assert.AreNotEqual(MatchOutcome.InProgress, m.Outcome);
            Assert.LessOrEqual(m.RealtimeTick, Rules.TurnLimit * Rules.TurnTicks);
            Assert.IsTrue(m.Kids.Exists(k => k.Warmth < k.MaxWarmth), "nobody got hit");
            Assert.AreEqual(a, RunRealtime(5, out _));
        }

        [Test]
        public void RealtimeOrderCanBeReplacedMidWalk()
        {
            var m = Duel(OpenField());
            var a = m.KidById(0);
            var east = new KidOrder(0);
            east.Path.Add(new Vec2(1600, 1500));
            m.SetRealtimeOrder(east);
            m.StepRealtime(5);
            Assert.Greater(a.Pos.X, 1000);
            var north = new KidOrder(0);
            north.Path.Add(new Vec2(a.Pos.X, 2200));
            m.SetRealtimeOrder(north);
            m.StepRealtime(30);
            Assert.AreEqual(2200, a.Pos.Z);
            Assert.Less(a.Pos.X, 1600);
        }

        [Test]
        public void MapleCourtSpawnsAreWalkable()
        {
            var m = new Match(Maps.MapleCourt(), Roster.MapleCourtCrew(), Roster.HillRoadHooligans(), 1);
            foreach (var k in m.Kids)
                Assert.IsTrue(m.IsWalkable(k.Pos), k.Def.Name);
        }
    }
}
