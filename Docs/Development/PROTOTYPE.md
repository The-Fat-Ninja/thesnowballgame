# WEGO Prototype — Maple Court

Status: **first playable greybox** (2026-10-14), branch `feature/wego-prototype`.

## Play it

1. Open the project in Unity **6000.6.3f1**.
2. Open `Assets/Scenes/MapleCourt.unity` and press **Play**.
3. You are the **Maple Court Crew** (orange hats) against the **Hill Road
   Hooligans** (purple hats, AI). Squad size, seed and a watch mode (AI plays
   both sides) are on the `Match` object in the Inspector.

### Each turn

1. **Pick a kid** (tap them, keys `1`–`4`, `Tab` or *Next kid*).
2. **Walk:** tap the snow to add waypoints. The grey ring is how far they get
   in one turn; a red dot marks where they'll run out of time.
3. **Throw:** *Line drive* (`L`, or right-click) is fast and flat, and cover
   stops it. *Lob* (`O`, or shift + right-click) arcs over cover onto a spot,
   but a kid who moves can dodge it. A red throw line means cover is in the way.
   *Throw first* (`F`) throws before walking.
4. **Ball** (`T`) picks which carried snowball to throw; **Make** (`P`)
   changes what the kid packs from now on. Both buttons take the snowball's
   colour. A kid holding a boulder shows **Roll** instead of Line drive / Lob.
5. **Build wall** (`B`): tap within 1.5 m of where the kid ends up (fresh snow
   only). Bernard is fastest.
6. Kids without orders hold still and **pack snowballs** if they're on fresh
   snow. The street and turning circle are packed: no ammo there.
7. Press **GO!** (`Space`). Both crews' plans play out together for 3 seconds.

Camera: `WASD`/arrows to pan, mouse wheel to zoom.

### Rules in the prototype

- **Warmth** instead of health; at zero a kid is **Soaked** and out.
- Hits from behind (snowball travelling the way the kid faces) do +50%.
- A kid who holds still next to cover **ducks**: lobs do half.
- Line drives chip snowbanks, snowmen and built walls down and then break them.
  Cars, walls, hedges, trees and houses are permanent.
- Snowballs still in the air at the end of a turn land during the next one.
- Win by soaking the other crew; after 15 turns the streetlights come on and
  the crew with more total warmth wins.
- Stats matter: Arm → range, Aim → spread, Hustle → speed, Grit → warmth,
  Craft → packing/building speed. Pip is small (harder to hit); Ada carries
  3 snowballs.
- **Five snowball types** — Packed, Quick, Slush, Powder puff and the rolled
  Boulder — each with its own job; the table is in
  `Docs/Design/GameDesignVision.md` ("Snow as ammunition"). Packing progress
  carries over between turns (a boulder can take more than one).

### Telling the snowballs apart

| | In flight | In the pouch (pips) | On impact |
|---|---|---|---|
| Packed | white ball | white | white puff |
| Quick | small icy-blue streak | small blue | small puff |
| Slush | heavy grey-blue drip | grey-blue | grey splat on the snow; slush puddle at slowed kids' feet |
| Powder | lilac puff | lilac | billowing cloud that thins as it blows away |
| Boulder | banded ball that grows and rolls | one big pip | big puff; leaves a snowbank |

Plan lines take the snowball's colour too; the landing ring shows a slush
splash or powder cloud at full size.

Not in yet: kid abilities and Specials, Your Kid, weather, sleds, the snowplow,
sound, real art, online play, touch gestures for the camera.

## How it's built

| Layer | Where | Notes |
|---|---|---|
| Rules core | `Assets/Scripts/Core` (`SnowballGame.Core`, no engine references) | Integer maths in centimetres, 10 ticks/s, seeded RNG: deterministic, so WEGO plan exchange and replays will work for online play. `Match.Resolve(orders)` returns a `TurnLog` (31 frames + events). |
| AI | `Core/SimpleAi.cs` | Reloads when empty, otherwise picks a covered spot at good range and throws at the nearest kid. Plans from the same information a player has. |
| Presentation | `Assets/Scripts/Presentation` (`SnowballGame.Presentation`) | Builds the map, kids and HUD from the core at runtime from primitives; plays turn logs back by interpolating frames. |
| Editor tools | `Assets/Editor/ProjectSetup.cs` | **Snowball Game ▸ Setup Project** recreates the URP asset, materials, player settings and the scene. **Snowball Game ▸ Capture Maple Court** renders PNGs to `Logs/Captures/`. |

## Real-time comparison footage

The core also runs in real time: `Match.SetRealtimeOrder` replaces a kid's
order at any moment and `Match.StepRealtime(ticks)` advances the same rules
without the turn pause (ducking uses a rolling one-second window instead of
"held still this turn"). `SimpleAi.Realtime` makes the AI keep to its chosen
spot, throw from where it stands and lead running targets.

**Snowball Game ▸ Record Demo Tapes** (or batch mode,
`-executeMethod SnowballGame.EditorTools.DemoTapeRecorder.Run`) renders two
1080p/30 fps MP4s of AI-vs-AI matches on Maple Court with captions:

- `Recordings/RealtimeDemo.mp4` — no pauses. Maisie stands in for the player
  (ringed, re-plans every 0.3 s as if steered, her intent drawn on the snow);
  every other kid re-plans about once a second.
- `Recordings/WegoDemo.mp4` — eight turns: a planning beat with the home
  crew's plans laid down kid by kid, then the 3-second GO!.
- `Recordings/SnowballTypes.mp4` — a one-minute captioned tour of the five
  snowball types, one staged scene each (also **Snowball Game ▸ Record
  Snowball Showcase**). Scenes run on the real rules core with kids' Aim raised
  to 5; each uses the first seed that shows its effect and logs it (all five
  currently show it on seed 1).

Stills go to `Recordings/Stills/` (every 10 s; every second for the showcase). `Recordings/` is not
committed. The real-time tape is concept footage: there is no playable
real-time scene with direct controls yet.

## Checks

```
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/test-core.ps1
```

runs the rules tests with `dotnet test` (seconds, no Unity). In Unity, the
same tests run in the Test Runner (EditMode), and `MapleCourtSmokeTests`
(PlayMode) loads the scene and plays three turns through the controller.

Batch mode, from the project folder (Unity closed):

```
Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode-results.xml
Unity.exe -batchmode -quit -projectPath . -executeMethod SnowballGame.EditorTools.SceneCapture.Run
```

## Playtest questions

- Is 4 v 4 readable and fun to plan on a phone-sized screen, or is 3 v 3 better?
- Do lobs feel dodgeable but still worth throwing?
- Is a 3-second turn the right length?
- Does the AI feel fair? Too passive or too sharp?
