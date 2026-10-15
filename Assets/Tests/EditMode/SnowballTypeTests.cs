using System.Collections.Generic;
using NUnit.Framework;
using SnowballGame.Core;

namespace SnowballGame.Tests
{
    /// <summary>Each snowball type does the thing that makes it worth carrying.</summary>
    public class SnowballTypeTests
    {
        static KidDef Plain(string name, int maxAmmo = 2) =>
            new KidDef(name, "Thrower", 3, 5, 3, 3, 3, "#FFFFFF") { MaxAmmo = maxAmmo };

        /// <summary>One home kid at (1000,1500) facing east; away kids where asked.</summary>
        static Match Field(int seed, params Vec2[] away)
        {
            var map = new MapDef("Field", 40, 30);
            map.Spawns[0].Add(new Vec2(1000, 1500));
            var awayDefs = new List<KidDef>();
            for (int i = 0; i < away.Length; i++)
            {
                map.Spawns[1].Add(away[i]);
                awayDefs.Add(Plain("B" + i));
            }
            return new Match(map, new List<KidDef> { Plain("A", 3) }, awayDefs, seed);
        }

        static void Give(Kid k, params SnowballType[] balls)
        {
            k.Pouch.Clear();
            k.Pouch.AddRange(balls);
        }

        static KidOrder Throw(SnowballType ball, ThrowStyle style, Vec2 at) =>
            new KidOrder(0) { HasThrow = true, Style = style, Ball = ball, ThrowTarget = at };

        static int FirstHitTick(TurnLog log)
        {
            var hit = log.Events.Find(e => e.Kind == EventKind.HitKid);
            return hit.Kind == EventKind.HitKid ? hit.Tick : int.MaxValue;
        }

        [Test]
        public void QuickBallPacksFasterFliesFasterAndStingsLess()
        {
            Assert.Less(Rules.PackTicks(SnowballType.Quick, 3), Rules.PackTicks(SnowballType.Packed, 3));

            var target = new Vec2(2200, 1500);
            var quick = Field(1, target);
            Give(quick.KidById(0), SnowballType.Quick);
            var quickLog = quick.Resolve(new[] { Throw(SnowballType.Quick, ThrowStyle.Line, target) });

            var packed = Field(1, target);
            Give(packed.KidById(0), SnowballType.Packed);
            var packedLog = packed.Resolve(new[] { Throw(SnowballType.Packed, ThrowStyle.Line, target) });

            Assert.Less(FirstHitTick(quickLog), FirstHitTick(packedLog), "quick ball should arrive first");
            int quickChill = quick.KidById(1).MaxWarmth - quick.KidById(1).Warmth;
            int packedChill = packed.KidById(1).MaxWarmth - packed.KidById(1).Warmth;
            Assert.AreEqual(Rules.Spec(SnowballType.Quick).LineChill, quickChill);
            Assert.AreEqual(Rules.Spec(SnowballType.Packed).LineChill, packedChill);
            Assert.Less(quickChill, packedChill);
        }

        [Test]
        public void SlushLobSplattersEveryoneUnderItAndSlowsThem()
        {
            var m = Field(2, new Vec2(2000, 1450), new Vec2(2000, 1560));
            Give(m.KidById(0), SnowballType.Slush);
            var log = m.Resolve(new[] { Throw(SnowballType.Slush, ThrowStyle.Lob, new Vec2(2000, 1505)) });

            foreach (int id in new[] { 1, 2 })
            {
                var k = m.KidById(id);
                Assert.Less(k.Warmth, k.MaxWarmth, $"{k.Def.Name} should be splattered");
                Assert.Greater(k.SlowTicks, 0, $"{k.Def.Name} should be slowed");
            }
            Assert.AreEqual(2, log.Events.FindAll(e => e.Kind == EventKind.Slowed).Count);

            // A packed lob on the same spot only hits one of them.
            var p = Field(2, new Vec2(2000, 1450), new Vec2(2000, 1560));
            Give(p.KidById(0), SnowballType.Packed);
            var plog = p.Resolve(new[] { Throw(SnowballType.Packed, ThrowStyle.Lob, new Vec2(2000, 1505)) });
            Assert.LessOrEqual(plog.Events.FindAll(e => e.Kind == EventKind.HitKid).Count, 1);
        }

        [Test]
        public void SlowedKidsWalkShorter()
        {
            Vec2 Walked(int slow)
            {
                var m = Field(3, new Vec2(3500, 500));
                var a = m.KidById(0);
                a.SlowTicks = slow;
                var walk = new KidOrder(0);
                walk.Path.Add(new Vec2(1000, 2900));
                m.Resolve(new[] { walk });
                return a.Pos;
            }
            Assert.Less(Walked(Rules.TurnTicks).Z, Walked(0).Z);
        }

        [Test]
        public void PowderPuffDoesNoHarmButHidesTheTarget()
        {
            var b = new Vec2(2000, 1500);
            var m = Field(4, b);
            Give(m.KidById(0), SnowballType.Powder);
            var log = m.Resolve(new[] { Throw(SnowballType.Powder, ThrowStyle.Lob, b) });

            Assert.AreEqual(m.KidById(1).MaxWarmth, m.KidById(1).Warmth);
            Assert.IsTrue(log.Events.Exists(e => e.Kind == EventKind.CloudCreated));
            Assert.AreEqual(1, m.Clouds.Count);
            Assert.IsFalse(m.IsLineOfFireClear(m.KidById(0).Pos, b), "a cloud blocks the line of fire");
            Assert.IsTrue(log.Frames[Rules.TurnTicks].Clouds.Count == 1, "clouds are in the playback frames");
        }

        [Test]
        public void PowderCloudsBlowAway()
        {
            var m = Field(5, new Vec2(2000, 1500));
            Give(m.KidById(0), SnowballType.Powder);
            m.Resolve(new[] { Throw(SnowballType.Powder, ThrowStyle.Lob, new Vec2(2000, 1500)) });
            m.Resolve(new KidOrder[0]);
            m.Resolve(new KidOrder[0]);
            Assert.AreEqual(0, m.Clouds.Count);
        }

        [Test]
        public void ThrowingIntoACloudSpoilsTheAim()
        {
            int Hits(bool cloud)
            {
                int hits = 0;
                for (int seed = 1; seed <= 30; seed++)
                {
                    var b = new Vec2(2200, 1500);
                    var m = Field(seed, b);
                    var a = m.KidById(0);
                    if (cloud)
                    {
                        Give(a, SnowballType.Powder);
                        m.Resolve(new[] { Throw(SnowballType.Powder, ThrowStyle.Lob, b) });
                    }
                    Give(a, SnowballType.Packed);
                    var log = m.Resolve(new[] { Throw(SnowballType.Packed, ThrowStyle.Line, b) });
                    if (log.Events.Exists(e => e.Kind == EventKind.HitKid)) hits++;
                }
                return hits;
            }
            int clear = Hits(false), blind = Hits(true);
            Assert.Greater(clear, blind, $"clear {clear} vs through a cloud {blind}");
        }

        [Test]
        public void BoulderNeedsAnEmptyPouch()
        {
            var m = Field(6, new Vec2(3500, 500));
            var a = m.KidById(0);
            a.MakeType = SnowballType.Boulder;
            Give(a, SnowballType.Packed);
            m.Resolve(new KidOrder[0]);
            m.Resolve(new KidOrder[0]);
            CollectionAssert.AreEqual(new[] { SnowballType.Packed }, a.Pouch);

            Give(a);
            m.Resolve(new KidOrder[0]);
            m.Resolve(new KidOrder[0]);
            Assert.IsTrue(a.CarryingBoulder);
            Assert.IsFalse(a.CanPack(SnowballType.Packed), "a boulder fills the pouch");
        }

        [Test]
        public void BoulderRollsFlattensKnocksBackAndBecomesASnowbank()
        {
            var b = new Vec2(2000, 1500);
            var m = Field(7, b);
            Give(m.KidById(0), SnowballType.Boulder);
            int banks = m.Cover.Count;
            var log = m.Resolve(new[] { Throw(SnowballType.Boulder, ThrowStyle.Line, b) });

            var victim = m.KidById(1);
            Assert.AreEqual(victim.MaxWarmth - Rules.BoulderChill, victim.Warmth);
            Assert.Greater(victim.Pos.X, b.X, "knocked back");
            Assert.AreEqual(banks + 1, m.Cover.Count);
            var bank = m.Cover[m.Cover.Count - 1];
            Assert.AreEqual(CoverKind.Snowbank, bank.Kind);
            Assert.Greater(bank.Radius, Rules.BoulderStartRadius, "it grew as it rolled");
            Assert.IsTrue(log.Frames.Exists(f => f.Projectiles.Exists(p => p.Style == ThrowStyle.Roll)));
        }

        [Test]
        public void BoulderThumpsCoverHarderThanASnowball()
        {
            var map = new MapDef("Field", 40, 30);
            map.Spawns[0].Add(new Vec2(1000, 1500));
            map.Spawns[1].Add(new Vec2(3000, 1500));
            map.AddCover(CoverKind.Snowbank, 1600, 1500, 90);
            var m = new Match(map, new List<KidDef> { Plain("A") }, new List<KidDef> { Plain("B") }, 8);
            Give(m.KidById(0), SnowballType.Boulder);
            m.Resolve(new[] { Throw(SnowballType.Boulder, ThrowStyle.Line, new Vec2(3000, 1500)) });
            Assert.AreEqual(Rules.SnowbankHp - Rules.BoulderCoverDamage, m.Cover[0].Hp);
            Assert.Greater(Rules.BoulderCoverDamage, Rules.CoverDamagePerHit);
        }

        [Test]
        public void ChosenBallIsThrownAndOthersStayInThePouch()
        {
            var m = Field(9, new Vec2(2000, 1500));
            var a = m.KidById(0);
            Give(a, SnowballType.Packed, SnowballType.Slush, SnowballType.Quick);
            var log = m.Resolve(new[] { Throw(SnowballType.Slush, ThrowStyle.Line, new Vec2(2000, 1500)) });
            var thrown = log.Events.Find(e => e.Kind == EventKind.Thrown);
            Assert.AreEqual((int)SnowballType.Slush, thrown.Amount);
            Assert.IsFalse(a.Carries(SnowballType.Slush));
            Assert.IsTrue(a.Carries(SnowballType.Quick));
        }

        [Test]
        public void MakeOrderChangesWhatAKidPacks()
        {
            var m = Field(10, new Vec2(3500, 500));
            var a = m.KidById(0);
            Give(a);
            m.Resolve(new[] { new KidOrder(0) { Make = SnowballType.Quick } });
            Assert.Greater(a.Ammo, 0);
            foreach (var b in a.Pouch) Assert.AreEqual(SnowballType.Quick, b);
            Assert.AreEqual(SnowballType.Quick, a.MakeType, "the choice sticks for later turns");
        }

        [Test]
        public void EveryTypeTurnsUpInAnAiMatch()
        {
            var seen = new HashSet<SnowballType>();
            for (int seed = 1; seed <= 6; seed++)
            {
                var m = new Match(Maps.MapleCourt(), Roster.MapleCourtCrew(), Roster.HillRoadHooligans(), seed);
                var home = new SimpleAi(Match.Home, seed * 3);
                var away = new SimpleAi(Match.Away, seed * 5);
                while (m.Outcome == MatchOutcome.InProgress)
                {
                    var orders = home.Plan(m);
                    orders.AddRange(away.Plan(m));
                    foreach (var e in m.Resolve(orders).Events)
                        if (e.Kind == EventKind.Thrown) seen.Add((SnowballType)e.Amount);
                }
            }
            foreach (SnowballType t in System.Enum.GetValues(typeof(SnowballType)))
                Assert.IsTrue(seen.Contains(t), $"the AI never threw a {t}");
        }
    }
}
