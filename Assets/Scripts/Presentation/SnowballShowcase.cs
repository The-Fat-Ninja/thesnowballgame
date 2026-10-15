using System;
using System.Collections.Generic;
using SnowballGame.Core;
using UnityEngine;

namespace SnowballGame.Presentation
{
    /// <summary>
    /// A captioned tour of the five snowball types on Maple Court, one short staged
    /// scene each, for the demo-tape recorder. Every scene runs through the real
    /// rules core; for each one the first seed (1-100) whose rolls show the effect
    /// clearly is used, so the tape is representative rather than lucky.
    /// Kids' Aim is raised to 5 so throws land where they're aimed: the tour is
    /// about the snowballs, not the throwing.
    /// </summary>
    public sealed class SnowballShowcase : IDemoTape
    {
        enum Phase { Intro, Card, Plan, Play, Hold, Outro }

        sealed class Scene
        {
            public string Title;
            public string Body;
            public string Legend;
            public SnowballType Type;
            public Action<Match> Setup;
            public List<Func<Match, List<KidOrder>>> Turns = new List<Func<Match, List<KidOrder>>>();
            public Func<Match, List<TurnLog>, bool> Shows;
            public string After;
        }

        const float IntroSeconds = 3f, CardSeconds = 2.4f, PlanSeconds = 1.2f, HoldSeconds = 2.2f, OutroSeconds = 4f;
        // Kid ids: home Maisie 0, Bernard 1, Pip 2, Rosa 3; away Tobias 4, Ada 5, Felix 6, Jun 7.
        const int Maisie = 0, Bernard = 1, Pip = 2, Rosa = 3, Tobias = 4, Ada = 5, Felix = 6, Jun = 7;
        const int LaneA = 1650, LaneB = 2450;

        readonly Material lit, unlit;
        readonly Transform parent;
        readonly Camera cam;
        readonly DemoCaptions captions;
        readonly Transform captionRoot;
        readonly List<Scene> scenes = new List<Scene>();

        Phase phase = Phase.Intro;
        float phaseTime, time;
        int sceneIndex = -1, turnIndex;
        Transform root;
        Match match;
        PlaybackView playback;
        PlanOverlay plans;
        readonly Dictionary<int, KidView> kids = new Dictionary<int, KidView>();
        List<KidOrder> pending;
        TurnLog turnLog;
        int eventCursor;

        public bool Finished { get; private set; }
        public float Seconds => time;

        public SnowballShowcase(Material lit, Material unlit, Transform parent, Camera cam)
        {
            this.lit = lit;
            this.unlit = unlit;
            this.parent = parent;
            this.cam = cam;
            captionRoot = new GameObject("ShowcaseCaptions").transform;
            captionRoot.SetParent(parent, false);
            captions = new DemoCaptions(captionRoot, cam);
            BuildScenes();
            NextScene();
            captions.ShowCard("SNOWBALLS", "Five kinds of snowball, and what makes each one different\nEvery scene runs on the real rules core");
        }

        public void Dispose()
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root.gameObject);
            if (captionRoot != null) UnityEngine.Object.DestroyImmediate(captionRoot.gameObject);
        }

        public void PrepareRender() => DemoCaptions.PrepareRender();

        public void Advance(float dt)
        {
            time += dt;
            phaseTime += dt;
            foreach (var kv in kids) kv.Value.Tick(cam, dt);
            playback?.Tick(dt);
            var scene = scenes[Mathf.Clamp(sceneIndex, 0, scenes.Count - 1)];

            switch (phase)
            {
                case Phase.Intro:
                    if (phaseTime >= IntroSeconds) Enter(Phase.Card);
                    break;
                case Phase.Card:
                    captions.ShowCard(scene.Title, scene.Body, Palette.BallLine(scene.Type) == Color.white ? Palette.Ember : Palette.BallLine(scene.Type));
                    if (phaseTime >= CardSeconds)
                    {
                        captions.Card.SetActive(false);
                        StartTurn(scene);
                    }
                    break;
                case Phase.Plan:
                    plans.Begin();
                    foreach (var o in pending)
                    {
                        var k = match.KidById(o.KidId);
                        plans.Order(k, o, true, Palette.Team(k.Team), false);
                    }
                    plans.End();
                    if (phaseTime >= PlanSeconds)
                    {
                        plans.Clear();
                        turnLog = match.Resolve(pending);
                        eventCursor = 0;
                        Enter(Phase.Play);
                    }
                    break;
                case Phase.Play:
                    float seconds = Rules.TurnTicks / (float)Rules.TicksPerSecond;
                    float tickF = Mathf.Clamp(phaseTime / seconds * Rules.TurnTicks, 0f, Rules.TurnTicks);
                    int f = Mathf.Min(Mathf.FloorToInt(tickF), Rules.TurnTicks - 1);
                    playback.ShowFrames(turnLog.Frames[f], turnLog.Frames[f + 1], tickF - f);
                    while (eventCursor < turnLog.Events.Count && turnLog.Events[eventCursor].Tick <= tickF)
                        playback.ShowEvent(turnLog.Events[eventCursor++]);
                    if (phaseTime >= seconds)
                    {
                        while (eventCursor < turnLog.Events.Count) playback.ShowEvent(turnLog.Events[eventCursor++]);
                        playback.ShowFrames(turnLog.Frames[Rules.TurnTicks], turnLog.Frames[Rules.TurnTicks], 0f);
                        playback.RefreshAllCover();
                        turnIndex++;
                        if (turnIndex < scene.Turns.Count) StartTurn(scene);
                        else
                        {
                            captions.Subtitle.text = scene.After;
                            Enter(Phase.Hold);
                        }
                    }
                    break;
                case Phase.Hold:
                    if (phaseTime >= HoldSeconds)
                    {
                        if (sceneIndex + 1 < scenes.Count)
                        {
                            NextScene();
                            Enter(Phase.Card);
                        }
                        else
                        {
                            captions.ShowCard("EVERY KID HAS A FAVOURITE",
                                "Pip & Jun: Quick · Bernard: Slush · Rosa & Felix: Powder · Tobias: Boulder\nPick what to throw (Ball, T) and what to make (Make, P) when planning");
                            Enter(Phase.Outro);
                        }
                    }
                    break;
                case Phase.Outro:
                    if (phaseTime >= OutroSeconds) Finished = true;
                    break;
            }
        }

        void Enter(Phase p)
        {
            phase = p;
            phaseTime = 0f;
        }

        void StartTurn(Scene scene)
        {
            pending = scene.Turns[turnIndex](match);
            Enter(Phase.Plan);
        }

        void NextScene()
        {
            sceneIndex++;
            turnIndex = 0;
            var scene = scenes[sceneIndex];
            int seed = PickSeed(scene);
            if (root != null) UnityEngine.Object.DestroyImmediate(root.gameObject);
            kids.Clear();

            match = NewMatch(seed);
            scene.Setup(match);
            var box = new Greybox(lit, unlit);
            root = new GameObject($"Showcase_{scene.Type}").transform;
            root.SetParent(parent, false);
            var world = new WorldView(box, root);
            world.Build(match);
            foreach (var k in match.Kids) kids[k.Id] = new KidView(box, root, k);
            playback = new PlaybackView(box, world, root, match, kids);
            plans = new PlanOverlay(box, root, match);
            MatchController.FrameCamera(cam, match.Map);

            captions.Title.text = scene.Title;
            captions.Title.color = Palette.BallLine(scene.Type) == Color.white ? Palette.Ink : Palette.BallLine(scene.Type);
            captions.Subtitle.text = scene.Body;
            captions.Clock.text = $"{sceneIndex + 1} / {scenes.Count}";
            captions.Score.text = scene.Legend;
        }

        /// <summary>The first seed whose rolls show the scene's effect; seed 1 if none do.</summary>
        static int PickSeed(Scene scene)
        {
            for (int seed = 1; seed <= 100; seed++)
            {
                var m = NewMatch(seed);
                scene.Setup(m);
                var logs = new List<TurnLog>();
                foreach (var turn in scene.Turns) logs.Add(m.Resolve(turn(m)));
                if (scene.Shows(m, logs))
                {
                    Debug.Log($"[SnowballShowcase] {scene.Title}: seed {seed} shows the effect.");
                    return seed;
                }
            }
            Debug.LogWarning($"[SnowballShowcase] {scene.Title}: no seed in 1-100 shows the effect; using seed 1.");
            return 1;
        }

        static Match NewMatch(int seed)
        {
            var home = Roster.MapleCourtCrew();
            var away = Roster.HillRoadHooligans();
            foreach (var d in home) d.Aim = 5;
            foreach (var d in away) d.Aim = 5;
            return new Match(Maps.MapleCourt(), home, away, seed);
        }

        // ------------------------------------------------------------ staging helpers

        static void Place(Match m, int id, int x, int z, params SnowballType[] pouch)
        {
            var k = m.KidById(id);
            k.Pos = new Vec2(x, z);
            k.Facing = new Vec2(k.Team == Match.Home ? 100 : -100, 0);
            k.Pouch.Clear();
            k.Pouch.AddRange(pouch);
        }

        /// <summary>Keeps the kids who aren't in a scene out of the way, empty-handed.</summary>
        static void Bench(Match m, params int[] cast)
        {
            var spots = new[] { new Vec2(250, 1200), new Vec2(250, 1800), new Vec2(3750, 1200), new Vec2(3750, 1800), new Vec2(250, 1400), new Vec2(3750, 1400), new Vec2(250, 1600), new Vec2(3750, 1600) };
            int s = 0;
            foreach (var k in m.Kids)
            {
                if (Array.IndexOf(cast, k.Id) >= 0) continue;
                while (Array.Exists(cast, c => m.KidById(c).Pos == spots[s % spots.Length])) s++;
                k.Pos = spots[s++ % spots.Length];
                k.Pouch.Clear();
                k.MakeType = SnowballType.Packed;
            }
        }

        static KidOrder Throw(int id, ThrowStyle style, SnowballType ball, Vec2 at) =>
            new KidOrder(id) { HasThrow = true, Style = style, Ball = ball, ThrowTarget = at };

        static KidOrder Walk(int id, Vec2 to)
        {
            var o = new KidOrder(id);
            o.Path.Add(to);
            return o;
        }

        static bool Hit(List<TurnLog> logs, int turn, int thrower, int victim) =>
            logs[turn].Events.Exists(e => e.Kind == EventKind.HitKid && e.KidId == thrower && e.OtherId == victim);

        static int HitTick(List<TurnLog> logs, int turn, int thrower)
        {
            var e = logs[turn].Events.Find(x => x.Kind == EventKind.HitKid && x.KidId == thrower);
            return e.Kind == EventKind.HitKid ? e.Tick : int.MaxValue;
        }

        // ------------------------------------------------------------ the scenes

        void BuildScenes()
        {
            var packed = new Scene
            {
                Type = SnowballType.Packed,
                Title = "PACKED",
                Body = "The all-rounder\n25 warmth on a line drive, 20 on a lob",
                Legend = "Maisie throws Packed",
                Setup = m =>
                {
                    Place(m, Maisie, 1150, LaneA, SnowballType.Packed);
                    Place(m, Jun, 2250, LaneA);
                    Bench(m, Maisie, Jun);
                },
                Shows = (m, logs) => Hit(logs, 0, Maisie, Jun),
                After = "A solid hit. Packed is the snowball every kid can always make.",
            };
            packed.Turns.Add(m => new List<KidOrder> { Throw(Maisie, ThrowStyle.Line, SnowballType.Packed, m.KidById(Jun).Pos) });

            var quick = new Scene
            {
                Type = SnowballType.Quick,
                Title = "QUICK",
                Body = "Made in half the time, flies a third faster, stings less\nTop lane: Quick · bottom lane: Packed, thrown at the same moment",
                Legend = "Pip: Quick  v  Maisie: Packed",
                Setup = m =>
                {
                    Place(m, Pip, 1150, LaneB, SnowballType.Quick);
                    Place(m, Maisie, 1150, LaneA, SnowballType.Packed);
                    Place(m, Felix, 2350, LaneB);
                    Place(m, Jun, 2350, LaneA);
                    Bench(m, Pip, Maisie, Felix, Jun);
                },
                Shows = (m, logs) => Hit(logs, 0, Pip, Felix) && Hit(logs, 0, Maisie, Jun) && HitTick(logs, 0, Pip) < HitTick(logs, 0, Maisie),
                After = "The quick ball lands first, but for 15 warmth instead of 25.",
            };
            quick.Turns.Add(m => new List<KidOrder>
            {
                Throw(Pip, ThrowStyle.Line, SnowballType.Quick, m.KidById(Felix).Pos),
                Throw(Maisie, ThrowStyle.Line, SnowballType.Packed, m.KidById(Jun).Pos),
            });

            var slush = new Scene
            {
                Type = SnowballType.Slush,
                Title = "SLUSH",
                Body = "Wet and heavy: lobbed, it splatters everyone under it\nand leaves them slowed for 3 seconds",
                Legend = "Bernard lobs Slush on Ada & Jun",
                Setup = m =>
                {
                    Place(m, Bernard, 1150, 1700, SnowballType.Slush);
                    Place(m, Ada, 2300, 1640);
                    Place(m, Jun, 2300, 1760);
                    Place(m, Tobias, 2300, 1400);
                    Bench(m, Bernard, Ada, Jun, Tobias);
                },
                // Tobias is exactly as quick as Ada (Hustle 2) and runs on the same packed street, so any gap is the slush.
                Shows = (m, logs) =>
                    m.KidById(Ada).Warmth < m.KidById(Ada).MaxWarmth && m.KidById(Jun).Warmth < m.KidById(Jun).MaxWarmth
                    && m.KidById(Tobias).Pos.X > m.KidById(Ada).Pos.X + 100,
                After = "Slushed kids (puddle at their feet) run at half pace. Dry Tobias, just as quick as Ada, pulls ahead.",
            };
            slush.Turns.Add(m => new List<KidOrder> { Throw(Bernard, ThrowStyle.Lob, SnowballType.Slush, new Vec2(2300, 1700)) });
            slush.Turns.Add(m => new List<KidOrder>
            {
                Walk(Ada, new Vec2(3300, 1640)),
                Walk(Jun, new Vec2(3300, 1760)),
                Walk(Tobias, new Vec2(3300, 1400)),
            });

            var powder = new Scene
            {
                Type = SnowballType.Powder,
                Title = "POWDER PUFF",
                Body = "No sting at all: it bursts into a cloud\nLine drives can't see through it, and throws from inside go wild",
                Legend = "Rosa puffs Jun · Felix aims clear",
                Setup = m =>
                {
                    Place(m, Rosa, 1150, LaneA, SnowballType.Powder);
                    Place(m, Maisie, 1150, LaneB);
                    Place(m, Jun, 2350, LaneA, SnowballType.Packed);
                    Place(m, Felix, 2350, LaneB, SnowballType.Packed);
                    Bench(m, Rosa, Maisie, Jun, Felix);
                },
                Shows = (m, logs) => logs[0].Events.Exists(e => e.Kind == EventKind.CloudCreated)
                    && !Hit(logs, 1, Jun, Rosa) && Hit(logs, 1, Felix, Maisie),
                After = "Jun throws blind out of the cloud and misses; Felix, in the clear, hits.",
            };
            powder.Turns.Add(m => new List<KidOrder> { Throw(Rosa, ThrowStyle.Lob, SnowballType.Powder, m.KidById(Jun).Pos) });
            powder.Turns.Add(m => new List<KidOrder>
            {
                Throw(Jun, ThrowStyle.Line, SnowballType.Packed, m.KidById(Rosa).Pos),
                Throw(Felix, ThrowStyle.Line, SnowballType.Packed, m.KidById(Maisie).Pos),
            });

            var boulder = new Scene
            {
                Type = SnowballType.Boulder,
                Title = "BOULDER",
                Body = "Rolled, not thrown: it grows as it goes, flattens and shoves the first kid\nand stays where it stops, as a new snowbank",
                Legend = "Tobias rolls a Boulder",
                Setup = m =>
                {
                    Place(m, Tobias, 2950, LaneA, SnowballType.Boulder);
                    Place(m, Pip, 1750, LaneA);
                    Bench(m, Tobias, Pip);
                },
                Shows = (m, logs) => Hit(logs, 0, Tobias, Pip) && logs[0].Events.Exists(e => e.Kind == EventKind.CoverBuilt),
                After = "35 warmth, a shove backwards, and a fresh snowbank in the middle of the street.",
            };
            boulder.Turns.Add(m => new List<KidOrder> { Throw(Tobias, ThrowStyle.Roll, SnowballType.Boulder, m.KidById(Pip).Pos) });

            scenes.Add(packed);
            scenes.Add(quick);
            scenes.Add(slush);
            scenes.Add(powder);
            scenes.Add(boulder);
        }
    }
}
