using System.Collections.Generic;
using System.Text;
using SnowballGame.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SnowballGame.Presentation
{
    /// <summary>
    /// The WEGO prototype loop on Maple Court: plan each kid's walk and throw,
    /// press GO!, watch both crews' plans play out for three seconds, repeat.
    /// Everything visible is generated at runtime from the rules core.
    /// </summary>
    public sealed class MatchController : MonoBehaviour
    {
        enum Phase { Planning, Playback, Over }
        enum Mode { Move, Line, Lob, Build }

        [Header("Materials (URP)")]
        [SerializeField] Material litMaterial;
        [SerializeField] Material unlitMaterial;

        [Header("Match")]
        [Range(2, 4)] [SerializeField] int squadSize = 4;
        [SerializeField] int seed = 2026;
        [Tooltip("Let the AI plan for your crew too (watch mode).")]
        [SerializeField] bool aiPlaysHome;

        Match match;
        SimpleAi awayAi, homeAi;
        Greybox box;
        WorldView world;
        Hud hud;
        Camera cam;
        Transform worldRoot, planRoot;
        readonly Dictionary<int, KidView> kids = new Dictionary<int, KidView>();
        readonly Dictionary<int, KidOrder> orders = new Dictionary<int, KidOrder>();
        readonly Dictionary<int, GameObject> balls = new Dictionary<int, GameObject>();
        readonly List<(Transform t, float age)> puffs = new List<(Transform, float)>();
        readonly List<LineRenderer> lines = new List<LineRenderer>();
        int lineCursor;

        Phase phase;
        Mode mode = Mode.Move;
        int selected = -1;
        TurnLog playing;
        float playTime;
        int eventCursor;
        string flashHint;
        float flashHintTime;

        const float PlaybackSeconds = Rules.TurnTicks / (float)Rules.TicksPerSecond;

        void Start() => NewMatch();

        public Match Match => match;
        public bool IsPlanning => phase == Phase.Planning;
        public bool IsOver => phase == Phase.Over;

        /// <summary>Queues an order for one of the player's kids (used by tests and future input schemes).</summary>
        public void SetOrder(KidOrder order)
        {
            if (phase == Phase.Planning && match.KidById(order.KidId).Team == Match.Home) orders[order.KidId] = order;
        }

        // ------------------------------------------------------------ setup

        public void NewMatch()
        {
            if (worldRoot != null) Destroy(worldRoot.gameObject);
            kids.Clear();
            orders.Clear();
            balls.Clear();
            puffs.Clear();
            lines.Clear();
            BuildWorld(seed);
            if (hud == null) BuildHud();
            hud.ResultPanel.SetActive(false);
            seed++;
            phase = Phase.Planning;
            SelectFirst();
            RefreshHud();
        }

        /// <summary>Builds the match and its view. Works in edit mode for captures.</summary>
        public void BuildWorld(int matchSeed)
        {
            cam = Camera.main;
            box = new Greybox(litMaterial, unlitMaterial);
            match = new Match(Maps.MapleCourt(), Roster.Take(Roster.MapleCourtCrew(), squadSize),
                Roster.Take(Roster.HillRoadHooligans(), squadSize), matchSeed);
            awayAi = new SimpleAi(Match.Away, matchSeed * 31 + 7);
            homeAi = new SimpleAi(Match.Home, matchSeed * 17 + 3);

            worldRoot = new GameObject("World").transform;
            worldRoot.SetParent(transform, false);
            planRoot = new GameObject("Plans").transform;
            planRoot.SetParent(worldRoot, false);
            world = new WorldView(box, worldRoot);
            world.Build(match);
            foreach (var k in match.Kids) kids[k.Id] = new KidView(box, worldRoot, k);
            FrameCamera();
        }

        void FrameCamera()
        {
            if (cam == null) return;
            var centre = new Vector3(match.Map.Width / 200f, 0, match.Map.Depth / 200f);
            cam.transform.position = centre + new Vector3(0, 28f, -24f);
            cam.transform.LookAt(centre + new Vector3(0, 0, -1.2f));
            cam.fieldOfView = 42f;
        }

        void BuildHud()
        {
            hud = new Hud();
            hud.Build(transform);
            var bottom = new Vector2(0.5f, 0f);
            var size = new Vector2(190, 90);
            Color idle = new Color(1, 1, 1, 0.92f);
            hud.AddButton("move", "Walk", bottom, new Vector2(-560, 60), size, idle, () => SetMode(Mode.Move));
            hud.AddButton("line", "Line drive", bottom, new Vector2(-360, 60), size, idle, () => SetMode(Mode.Line));
            hud.AddButton("lob", "Lob", bottom, new Vector2(-160, 60), size, idle, () => SetMode(Mode.Lob));
            hud.AddButton("build", "Build wall", bottom, new Vector2(40, 60), size, idle, () => SetMode(Mode.Build));
            hud.AddButton("first", "Throw first", bottom, new Vector2(240, 60), size, idle, ToggleThrowFirst);
            hud.AddButton("clear", "Clear", bottom, new Vector2(440, 60), size, idle, ClearSelected);
            hud.AddButton("go", "GO!", new Vector2(1f, 0f), new Vector2(-130, 70), new Vector2(220, 110), Palette.Ember, Go);
            hud.AddButton("next", "Next kid", new Vector2(1f, 0f), new Vector2(-130, 200), new Vector2(220, 80), idle, SelectNext);
            hud.AddResultButton("result_again", "Play again", NewMatch);
        }

        // ------------------------------------------------------------ frame loop

        void Update()
        {
            float dt = Time.deltaTime;
            if (match == null) return;
            HandleCamera(dt);
            if (phase == Phase.Planning) HandlePlanningInput();
            else if (phase == Phase.Playback) StepPlayback(dt);

            foreach (var kv in kids) kv.Value.Tick(cam, dt);
            for (int i = puffs.Count - 1; i >= 0; i--)
            {
                var (t, age) = puffs[i];
                age += dt;
                if (age > 0.45f || t == null)
                {
                    if (t != null) Destroy(t.gameObject);
                    puffs.RemoveAt(i);
                    continue;
                }
                t.localScale = Vector3.one * (0.3f + age * 2.2f);
                puffs[i] = (t, age);
            }
            if (flashHintTime > 0) flashHintTime -= dt;
            if (phase == Phase.Planning) DrawPlans();
            RefreshHint();
        }

        void HandleCamera(float dt)
        {
            if (cam == null) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            Vector3 move = Vector3.zero;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.z += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.z -= 1;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1;
            }
            cam.transform.position += move * (14f * dt);
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f && !PointerOverUi())
                {
                    Vector3 next = cam.transform.position + cam.transform.forward * Mathf.Sign(scroll) * 2f;
                    if (next.y > 8f && next.y < 45f) cam.transform.position = next;
                }
            }
        }

        // ------------------------------------------------------------ planning

        void HandlePlanningInput()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) Go();
                if (kb.tabKey.wasPressedThisFrame) SelectNext();
                if (kb.mKey.wasPressedThisFrame) SetMode(Mode.Move);
                if (kb.lKey.wasPressedThisFrame) SetMode(Mode.Line);
                if (kb.oKey.wasPressedThisFrame) SetMode(Mode.Lob);
                if (kb.bKey.wasPressedThisFrame) SetMode(Mode.Build);
                if (kb.fKey.wasPressedThisFrame) ToggleThrowFirst();
                if (kb.backspaceKey.wasPressedThisFrame || kb.deleteKey.wasPressedThisFrame) ClearSelected();
                var homeKids = HomeKids();
                for (int i = 0; i < homeKids.Count && i < 4; i++)
                    if (kb[UnityEngine.InputSystem.Key.Digit1 + i].wasPressedThisFrame) Select(homeKids[i].Id);
            }

            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame || PointerOverUi()) return;
            if (!GroundPoint(pointer.position.ReadValue(), out Vector3 hit)) return;
            Vec2 p = Greybox.Core(hit);

            bool right = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            var clickedKid = KidNear(p, Match.Home);
            if (clickedKid != null && mode == Mode.Move && !right)
            {
                Select(clickedKid.Id);
                return;
            }
            if (selected < 0) return;

            Mode act = right ? (Keyboard.current != null && Keyboard.current.shiftKey.isPressed ? Mode.Lob : Mode.Line) : mode;
            var kid = match.KidById(selected);
            var order = OrderFor(selected);
            switch (act)
            {
                case Mode.Move:
                    Vec2 from = order.Path.Count > 0 ? order.Path[order.Path.Count - 1] : kid.Pos;
                    if (!match.IsWalkable(p) || !match.IsPathClear(from, p))
                    {
                        Flash("Can't walk there — something's in the way.");
                        return;
                    }
                    order.Path.Add(p);
                    break;
                case Mode.Line:
                case Mode.Lob:
                    if (kid.Ammo <= 0)
                    {
                        Flash($"{kid.Def.Name} has no snowballs — stand on fresh snow to pack some.");
                        return;
                    }
                    var enemy = KidNear(p, Match.Away, 110);
                    order.HasThrow = true;
                    order.Style = act == Mode.Lob ? ThrowStyle.Lob : ThrowStyle.Line;
                    order.ThrowTarget = enemy != null ? enemy.Pos : p;
                    mode = Mode.Move;
                    break;
                case Mode.Build:
                    Vec2 stand = Match.PathEnd(kid, order.Path);
                    if (Vec2.Dist(stand, p) > Rules.BuildReach)
                    {
                        Flash("Build next to where the kid ends up (within 1.5 m).");
                        return;
                    }
                    order.HasBuild = true;
                    order.BuildAt = p;
                    mode = Mode.Move;
                    break;
            }
            RefreshHud();
        }

        KidOrder OrderFor(int kidId)
        {
            if (!orders.TryGetValue(kidId, out var o))
            {
                o = new KidOrder(kidId);
                orders[kidId] = o;
            }
            return o;
        }

        List<Kid> HomeKids()
        {
            var list = new List<Kid>();
            foreach (var k in match.Crew(Match.Home))
                if (!k.Soaked) list.Add(k);
            return list;
        }

        Kid KidNear(Vec2 p, int team, int radius = 80)
        {
            Kid best = null;
            long bestD = (long)radius * radius;
            foreach (var k in match.Kids)
            {
                if (k.Team != team || k.Soaked) continue;
                long d = Vec2.DistSq(k.Pos, p);
                if (d <= bestD)
                {
                    best = k;
                    bestD = d;
                }
            }
            return best;
        }

        void Select(int id)
        {
            if (selected >= 0 && kids.TryGetValue(selected, out var old)) old.SetSelected(false);
            selected = id;
            mode = Mode.Move;
            if (selected >= 0 && kids.TryGetValue(selected, out var v)) v.SetSelected(true);
            RefreshHud();
        }

        void SelectFirst()
        {
            var list = HomeKids();
            Select(list.Count > 0 ? list[0].Id : -1);
        }

        void SelectNext()
        {
            if (phase != Phase.Planning) return;
            var list = HomeKids();
            if (list.Count == 0) return;
            int i = list.FindIndex(k => k.Id == selected);
            Select(list[(i + 1) % list.Count].Id);
        }

        void SetMode(Mode m)
        {
            if (phase != Phase.Planning) return;
            mode = m;
            RefreshHud();
        }

        void ToggleThrowFirst()
        {
            if (selected < 0 || phase != Phase.Planning) return;
            var o = OrderFor(selected);
            o.ThrowFirst = !o.ThrowFirst;
            RefreshHud();
        }

        void ClearSelected()
        {
            if (selected < 0 || phase != Phase.Planning) return;
            orders.Remove(selected);
            mode = Mode.Move;
            RefreshHud();
        }

        void Flash(string text)
        {
            flashHint = text;
            flashHintTime = 2.2f;
        }

        // ------------------------------------------------------------ resolve and playback

        public void Go()
        {
            if (phase != Phase.Planning) return;
            var all = new List<KidOrder>();
            if (aiPlaysHome) all.AddRange(homeAi.Plan(match));
            else all.AddRange(orders.Values);
            all.AddRange(awayAi.Plan(match));
            playing = match.Resolve(all);
            orders.Clear();
            playTime = 0f;
            eventCursor = 0;
            phase = Phase.Playback;
            if (selected >= 0 && kids.TryGetValue(selected, out var v)) v.SetSelected(false);
            ClearLines();
            RefreshHud();
        }

        void StepPlayback(float dt)
        {
            playTime += dt;
            float tickF = Mathf.Clamp(playTime / PlaybackSeconds * Rules.TurnTicks, 0f, Rules.TurnTicks);
            int i = Mathf.Min(Mathf.FloorToInt(tickF), Rules.TurnTicks - 1);
            float t = tickF - i;
            ShowFrames(playing.Frames[i], playing.Frames[i + 1], t);

            while (eventCursor < playing.Events.Count && playing.Events[eventCursor].Tick <= tickF)
                ShowEvent(playing.Events[eventCursor++]);

            if (playTime >= PlaybackSeconds)
            {
                while (eventCursor < playing.Events.Count) ShowEvent(playing.Events[eventCursor++]);
                ShowFrames(playing.Frames[Rules.TurnTicks], playing.Frames[Rules.TurnTicks], 0f);
                foreach (var c in match.Cover) world.RefreshCover(c);
                if (match.Outcome != MatchOutcome.InProgress)
                {
                    phase = Phase.Over;
                    hud.ResultPanel.SetActive(true);
                    hud.ResultText.text = match.Outcome switch
                    {
                        MatchOutcome.HomeWins => "Maple Court Crew wins!",
                        MatchOutcome.AwayWins => "The Hill Road Hooligans win!",
                        _ => "It's a draw — streetlights on!",
                    };
                }
                else
                {
                    phase = Phase.Planning;
                    if (selected < 0 || match.KidById(selected).Soaked) SelectFirst();
                    else Select(selected);
                }
                RefreshHud();
            }
        }

        void ShowFrames(Frame a, Frame b, float t)
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
                    ball = box.Prim(PrimitiveType.Sphere, worldRoot, wa, Vector3.one * 0.22f, Color.white, "Snowball");
                    balls[pa.Id] = ball;
                }
                ball.SetActive(true);
                ball.transform.localPosition = Vector3.Lerp(wa, wb, t);
            }
            foreach (var kv in balls)
                if (!seen.Contains(kv.Key)) kv.Value.SetActive(false);
        }

        void ShowEvent(MatchEvent e)
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

        void Puff(Vector3 at, float size)
        {
            var p = box.Prim(PrimitiveType.Sphere, worldRoot, at, Vector3.one * 0.3f * size, Palette.SnowLight, "Puff");
            puffs.Add((p.transform, 0f));
        }

        // ------------------------------------------------------------ plan drawing

        void DrawPlans()
        {
            lineCursor = 0;
            foreach (var kv in orders)
            {
                var kid = match.KidById(kv.Key);
                if (kid.Soaked) continue;
                DrawOrder(kid, kv.Value, kv.Key == selected);
            }
            if (selected >= 0 && (mode == Mode.Line || mode == Mode.Lob))
            {
                var kid = match.KidById(selected);
                orders.TryGetValue(selected, out var o);
                Vec2 origin = o != null && !o.ThrowFirst ? Match.PathEnd(kid, o.Path) : kid.Pos;
                Circle(origin, Match.MaxRange(kid, mode == Mode.Lob ? ThrowStyle.Lob : ThrowStyle.Line), Palette.Team(Match.Home), 0.06f);
            }
            if (selected >= 0 && mode == Mode.Move)
            {
                var kid = match.KidById(selected);
                orders.TryGetValue(selected, out var o);
                Circle(kid.Pos, Match.MoveReach(kid), Palette.SnowShadow, 0.05f);
            }
            for (int i = lineCursor; i < lines.Count; i++) lines[i].positionCount = 0;
        }

        void DrawOrder(Kid kid, KidOrder o, bool isSelected)
        {
            float w = isSelected ? 0.14f : 0.09f;
            if (o.Path.Count > 0)
            {
                var pts = new List<Vector3> { Greybox.World(kid.Pos, 0.05f) };
                foreach (var p in o.Path) pts.Add(Greybox.World(p, 0.05f));
                var lr = NextLine(Palette.Team(Match.Home), w);
                lr.positionCount = pts.Count;
                lr.SetPositions(pts.ToArray());

                // Mark where the kid runs out of time if the walk is too long.
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
                Circle(Match.PathEnd(kid, o.Path), 40, Palette.Team(Match.Home), 0.07f);
            }
            if (o.HasThrow)
            {
                Vec2 origin = o.ThrowFirst ? kid.Pos : Match.PathEnd(kid, o.Path);
                Vec2 target = o.ThrowTarget;
                int range = Match.MaxRange(kid, o.Style);
                if (Vec2.Dist(origin, target) > range) target = origin + (target - origin).WithLength(range);
                bool clear = o.Style == ThrowStyle.Lob || match.IsLineOfFireClear(origin, target);
                var lr = NextLine(clear ? Color.white : Palette.Brick, w * 0.8f);
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

        void Circle(Vec2 centre, int radiusCm, Color c, float width)
        {
            var lr = NextLine(c, width);
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

        LineRenderer NextLine(Color c, float width)
        {
            if (lineCursor >= lines.Count) lines.Add(box.Line(planRoot, c, width, "PlanLine"));
            var lr = lines[lineCursor++];
            lr.sharedMaterial = box.Unlit(c);
            lr.widthMultiplier = width;
            return lr;
        }

        void ClearLines()
        {
            foreach (var l in lines) l.positionCount = 0;
        }

        // ------------------------------------------------------------ HUD text

        void RefreshHud()
        {
            if (hud == null || match == null) return;
            int turn = Mathf.Min(match.Turn, Rules.TurnLimit);
            hud.Banner.text = phase switch
            {
                Phase.Playback => $"Turn {playing?.Turn ?? turn} — here it comes!",
                Phase.Over => "Game over",
                _ => $"Maple Court · Turn {turn}/{Rules.TurnLimit} · plan your moves",
            };
            hud.RosterHome.text = RosterText("Maple Court Crew", Match.Home);
            hud.RosterAway.text = RosterText("Hill Road Hooligans", Match.Away);

            if (selected >= 0)
            {
                var k = match.KidById(selected);
                orders.TryGetValue(selected, out var o);
                string plan = o == null ? "standing order: hold and pack snow"
                    : $"{(o.Path.Count > 0 ? $"walk ({o.Path.Count} pts)" : "hold")}"
                      + (o.HasThrow ? $", {(o.Style == ThrowStyle.Lob ? "lob" : "line drive")}{(o.ThrowFirst ? " first" : "")}" : "")
                      + (o.HasBuild ? ", build wall" : "");
                hud.KidCard.text =
                    $"<b>{k.Def.Name}</b> · {k.Def.Role}\n" +
                    $"Warmth {k.Warmth}/{k.MaxWarmth} · Snowballs {k.Ammo}/{k.MaxAmmo}\n" +
                    $"Arm {k.Def.Arm} Aim {k.Def.Aim} Hustle {k.Def.Hustle} Grit {k.Def.Grit} Craft {k.Def.Craft}\n" +
                    $"Plan: {plan}";
            }
            else hud.KidCard.text = "";

            Color idle = new Color(1, 1, 1, 0.92f), on = Palette.Glow;
            hud.SetButtonColour("move", mode == Mode.Move ? on : idle);
            hud.SetButtonColour("line", mode == Mode.Line ? on : idle);
            hud.SetButtonColour("lob", mode == Mode.Lob ? on : idle);
            hud.SetButtonColour("build", mode == Mode.Build ? on : idle);
            bool first = selected >= 0 && orders.TryGetValue(selected, out var so) && so.ThrowFirst;
            hud.SetButtonColour("first", first ? on : idle);
            hud.SetInteractable(phase == Phase.Planning);
        }

        string RosterText(string title, int team)
        {
            var sb = new StringBuilder();
            string hex = ColorUtility.ToHtmlStringRGB(Palette.Team(team));
            sb.Append($"<color=#{hex}><b>{title}</b></color>\n");
            foreach (var k in match.Crew(team))
            {
                if (k.Soaked) sb.Append($"<color=#888888>{k.Def.Name} — soaked!</color>\n");
                else
                {
                    int pct = k.Warmth * 100 / k.MaxWarmth;
                    bool planned = team == Match.Home && orders.ContainsKey(k.Id);
                    sb.Append($"{k.Def.Name}  warmth {pct}%  balls {k.Ammo}{(planned ? "  <b>[plan]</b>" : "")}\n");
                }
            }
            return sb.ToString();
        }

        void RefreshHint()
        {
            if (hud == null) return;
            if (flashHintTime > 0)
            {
                hud.Hint.text = $"<color=#B5543C>{flashHint}</color>";
                return;
            }
            hud.Hint.text = phase != Phase.Planning ? "" : mode switch
            {
                Mode.Line => "Tap a target for a line drive (fast and flat; cover stops it)",
                Mode.Lob => "Tap a target spot for a lob (arcs over cover; they can dodge)",
                Mode.Build => "Tap next to the kid's end point to build a snow wall",
                _ => selected >= 0 ? "Tap the snow to add waypoints · right-click to throw · GO! when ready" : "Tap one of your kids",
            };
        }

        // ------------------------------------------------------------ helpers

        bool GroundPoint(Vector2 screen, out Vector3 hit)
        {
            hit = default;
            if (cam == null) return false;
            var ray = cam.ScreenPointToRay(screen);
            if (Mathf.Abs(ray.direction.y) < 0.0001f) return false;
            float d = -ray.origin.y / ray.direction.y;
            if (d < 0) return false;
            hit = ray.origin + ray.direction * d;
            return true;
        }

        static bool PointerOverUi() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // ------------------------------------------------------------ editor capture support

        /// <summary>Plays AI-vs-AI turns without playback and shows the result (edit-mode captures).</summary>
        public void SimulateForCapture(int turns)
        {
            for (int i = 0; i < turns && match.Outcome == MatchOutcome.InProgress; i++)
            {
                var all = homeAi.Plan(match);
                all.AddRange(awayAi.Plan(match));
                var log = match.Resolve(all);
                foreach (var e in log.Events)
                    if (e.Kind == EventKind.CoverBuilt)
                    {
                        var c = match.Cover.Find(x => x.Id == e.OtherId);
                        if (c != null && !world.HasCover(c.Id)) world.AddCover(c);
                    }
                ShowFrames(log.Frames[Rules.TurnTicks - 6], log.Frames[Rules.TurnTicks - 6], 0f);
            }
            foreach (var c in match.Cover) world.RefreshCover(c);
            // Draw the plans the home AI would make next, so captures show the planning overlay.
            var preview = homeAi.Plan(match);
            lineCursor = 0;
            foreach (var o in preview) DrawOrder(match.KidById(o.KidId), o, false);
            foreach (var kv in kids) kv.Value.Tick(cam, 0f);
        }

        public void ClearForCapture()
        {
            if (worldRoot != null) DestroyImmediate(worldRoot.gameObject);
            kids.Clear();
            lines.Clear();
            balls.Clear();
        }
    }
}
