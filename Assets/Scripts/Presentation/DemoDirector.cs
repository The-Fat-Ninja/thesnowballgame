using System.Collections.Generic;
using SnowballGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SnowballGame.Presentation
{
    /// <summary>
    /// Plays an AI-vs-AI match on Maple Court for a demo tape, in either play style,
    /// with captions. Driven with a fixed time step by the editor recorder
    /// (Assets/Editor/DemoTapeRecorder.cs) so footage is smooth and repeatable.
    ///
    /// Real time: the same rules core advanced tick by tick with no pauses. Maisie
    /// stands in for the player (ringed) and re-plans every 0.3 s, as if steered;
    /// every other kid re-plans about once a second.
    /// WEGO: each turn shows a planning beat with the home crew's plans drawn, then
    /// both crews' plans play out for three seconds.
    /// </summary>
    public sealed class DemoDirector : IDemoTape
    {
        public enum Style { Realtime, Wego }

        const float IntroSeconds = 3f;
        const float OutroSeconds = 3f;
        const float PlanSeconds = 2.6f;
        const int WegoTurns = 8;
        const int PlayerReplanTicks = 3;
        const int SquadReplanTicks = 10;

        readonly Style style;
        readonly Camera cam;
        readonly Transform root;
        readonly Match match;
        readonly SimpleAi homeAi, awayAi;
        readonly Dictionary<int, KidView> kids = new Dictionary<int, KidView>();
        readonly Dictionary<int, KidOrder> current = new Dictionary<int, KidOrder>();
        readonly PlaybackView playback;
        readonly PlanOverlay plans;
        readonly DemoCaptions captions;

        float time;
        float outro = -1f;
        // Real time.
        TurnLog lastTick;
        float tickPhase;
        int player = -1;
        // WEGO.
        bool planning;
        float phaseTime;
        TurnLog turnLog;
        int eventCursor;
        List<KidOrder> homePlan;
        int turnsPlayed;

        public bool Finished { get; private set; }
        public float Seconds => time;

        public DemoDirector(Style style, Material lit, Material unlit, Transform parent, Camera cam, int seed)
        {
            this.style = style;
            this.cam = cam;
            var box = new Greybox(lit, unlit);
            match = new Match(Maps.MapleCourt(), Roster.MapleCourtCrew(), Roster.HillRoadHooligans(), seed);
            bool realtime = style == Style.Realtime;
            homeAi = new SimpleAi(Match.Home, seed * 17 + 3) { Realtime = realtime };
            awayAi = new SimpleAi(Match.Away, seed * 31 + 7) { Realtime = realtime };

            root = new GameObject($"Demo_{style}").transform;
            root.SetParent(parent, false);
            var world = new WorldView(box, root);
            world.Build(match);
            foreach (var k in match.Kids) kids[k.Id] = new KidView(box, root, k);
            playback = new PlaybackView(box, world, root, match, kids);
            plans = new PlanOverlay(box, root, match);
            MatchController.FrameCamera(cam, match.Map);

            captions = new DemoCaptions(root, cam);
            ShowCard(realtime ? "REAL-TIME" : "WEGO",
                realtime
                    ? "Concept footage · same rules core, no pauses\nYou steer one kid (ringed); AI runs the rest of your crew"
                    : "Planned simultaneous turns · the prototype as built\nPlan every kid while time is frozen, then GO! plays 3 seconds");
            if (realtime) PickPlayer();
            playback.ShowFrames(Snapshot(), Snapshot(), 0f);
        }

        public void Dispose()
        {
            if (root != null) Object.DestroyImmediate(root.gameObject);
        }

        /// <summary>Advances the demo by dt seconds.</summary>
        public void Advance(float dt)
        {
            time += dt;
            foreach (var kv in kids) kv.Value.Tick(cam, dt);
            playback.Tick(dt);

            if (time < IntroSeconds)
            {
                captions.Card.SetActive(true);
            }
            else if (outro >= 0f)
            {
                outro += dt;
                if (outro > OutroSeconds) Finished = true;
            }
            else
            {
                captions.Card.SetActive(false);
                if (style == Style.Realtime) AdvanceRealtime(dt);
                else AdvanceWego(dt);
                if (match.Outcome != MatchOutcome.InProgress && (style == Style.Realtime || !planning && turnLog == null))
                    StartOutro();
            }
            UpdateCaptions();
        }

        /// <summary>Call just before rendering so the captions lay out in edit mode.</summary>
        public void PrepareRender() => DemoCaptions.PrepareRender();

        // --------------------------------------------------------------- real time

        void AdvanceRealtime(float dt)
        {
            tickPhase += dt * Rules.TicksPerSecond;
            while (tickPhase >= 1f && match.Outcome == MatchOutcome.InProgress)
            {
                tickPhase -= 1f;
                if (player < 0 || match.KidById(player).Soaked) PickPlayer();
                foreach (var k in match.Kids)
                {
                    if (k.Soaked) continue;
                    int every = k.Id == player ? PlayerReplanTicks : SquadReplanTicks;
                    if ((match.RealtimeTick + k.Id * 3) % every != 0) continue;
                    var o = (k.Team == Match.Home ? homeAi : awayAi).PlanOne(match, k);
                    if (o == null) continue;
                    match.SetRealtimeOrder(o);
                    current[k.Id] = o;
                }
                lastTick = match.StepRealtime(1);
                foreach (var e in lastTick.Events) playback.ShowEvent(e);
            }
            if (lastTick != null && lastTick.Frames.Count == 2)
                playback.ShowFrames(lastTick.Frames[0], lastTick.Frames[1], Mathf.Clamp01(tickPhase));

            // The steered kid's current intent, as a player would see their cursor.
            plans.Begin();
            if (player >= 0 && current.TryGetValue(player, out var po) && !match.KidById(player).Soaked)
                plans.Order(match.KidById(player), po, true, Palette.Team(Match.Home), false);
            plans.End();
        }

        void PickPlayer()
        {
            if (player >= 0 && kids.TryGetValue(player, out var old)) old.SetSelected(false);
            player = -1;
            foreach (var k in match.Crew(Match.Home))
                if (!k.Soaked) { player = k.Id; break; }
            if (player >= 0) kids[player].SetSelected(true);
        }

        // --------------------------------------------------------------- WEGO

        void AdvanceWego(float dt)
        {
            phaseTime += dt;
            if (turnLog == null && !planning)
            {
                if (match.Outcome != MatchOutcome.InProgress || turnsPlayed >= WegoTurns)
                {
                    StartOutro();
                    return;
                }
                planning = true;
                phaseTime = 0f;
                homePlan = homeAi.Plan(match);
            }

            if (planning)
            {
                // Plans appear one kid at a time, as a player would lay them down.
                int shown = Mathf.Min(homePlan.Count, 1 + (int)(phaseTime / (PlanSeconds / (homePlan.Count + 1))));
                plans.Begin();
                for (int i = 0; i < shown; i++)
                    plans.Order(match.KidById(homePlan[i].KidId), homePlan[i], i == shown - 1, Palette.Team(Match.Home));
                plans.End();
                for (int i = 0; i < homePlan.Count; i++)
                    kids[homePlan[i].KidId].SetSelected(i == shown - 1);

                if (phaseTime >= PlanSeconds)
                {
                    planning = false;
                    phaseTime = 0f;
                    foreach (var kv in kids) kv.Value.SetSelected(false);
                    plans.Clear();
                    var all = new List<KidOrder>(homePlan);
                    all.AddRange(awayAi.Plan(match));
                    turnLog = match.Resolve(all);
                    eventCursor = 0;
                }
                return;
            }

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
                turnLog = null;
                turnsPlayed++;
            }
        }

        // --------------------------------------------------------------- captions

        void StartOutro()
        {
            if (outro >= 0f) return;
            outro = 0f;
            plans.Clear();
            string result = match.Outcome switch
            {
                MatchOutcome.HomeWins => "Maple Court Crew wins!",
                MatchOutcome.AwayWins => "The Hill Road Hooligans win!",
                MatchOutcome.Draw => "It's a draw — streetlights on!",
                _ => $"…and so on, for up to {Rules.TurnLimit} turns",
            };
            ShowCard(style == Style.Realtime ? "REAL-TIME" : "WEGO", $"{result}\n{Clock()} of play shown");
        }

        void UpdateCaptions()
        {
            captions.Title.text = style == Style.Realtime ? "REAL-TIME (concept)" : "WEGO (prototype)";
            if (style == Style.Realtime)
                captions.Subtitle.text = "Continuous play · orders change any time · ringed kid = you";
            else if (planning)
                captions.Subtitle.text = $"Turn {match.Turn} · PLANNING — time is frozen while you plan";
            else if (turnLog != null)
                captions.Subtitle.text = $"Turn {turnLog.Turn} · GO! — both crews' plans play out";
            else
                captions.Subtitle.text = "";
            captions.Clock.text = Clock();
            int home = 0, away = 0;
            foreach (var k in match.Kids)
                if (!k.Soaked) { if (k.Team == Match.Home) home++; else away++; }
            captions.Score.text = $"<color=#{ColorUtility.ToHtmlStringRGB(Palette.Ember)}>Maple Court {home}</color>  v  <color=#{ColorUtility.ToHtmlStringRGB(Palette.Plum)}>{away} Hill Road</color>";
        }

        string Clock()
        {
            float t = Mathf.Max(0f, time - IntroSeconds - Mathf.Max(0f, outro));
            return $"{(int)t / 60}:{(int)t % 60:00}";
        }

        void ShowCard(string heading, string body) => captions.ShowCard(heading, body);

        Frame Snapshot()
        {
            var f = new Frame { Kids = new KidFrame[match.Kids.Count] };
            foreach (var k in match.Kids)
                f.Kids[k.Id] = new KidFrame { Id = k.Id, Pos = k.Pos, Facing = k.Facing, Warmth = k.Warmth, Ammo = k.Ammo, Pouch = k.Pouch.ToArray(), Action = KidAction.Idle };
            return f;
        }
    }
}
