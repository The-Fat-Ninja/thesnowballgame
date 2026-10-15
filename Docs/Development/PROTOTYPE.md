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
4. **Build wall** (`B`): tap within 1.5 m of where the kid ends up (fresh snow
   only). Bernard is fastest.
5. Kids without orders hold still and **pack snowballs** if they're on fresh
   snow. The street and turning circle are packed: no ammo there.
6. Press **GO!** (`Space`). Both crews' plans play out together for 3 seconds.

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

Not in yet: kid abilities and Specials, Your Kid, weather, sleds, the snowplow,
sound, real art, online play, touch gestures for the camera.

## How it's built

| Layer | Where | Notes |
|---|---|---|
| Rules core | `Assets/Scripts/Core` (`SnowballGame.Core`, no engine references) | Integer maths in centimetres, 10 ticks/s, seeded RNG: deterministic, so WEGO plan exchange and replays will work for online play. `Match.Resolve(orders)` returns a `TurnLog` (31 frames + events). |
| AI | `Core/SimpleAi.cs` | Reloads when empty, otherwise picks a covered spot at good range and throws at the nearest kid. Plans from the same information a player has. |
| Presentation | `Assets/Scripts/Presentation` (`SnowballGame.Presentation`) | Builds the map, kids and HUD from the core at runtime from primitives; plays turn logs back by interpolating frames. |
| Editor tools | `Assets/Editor/ProjectSetup.cs` | **Snowball Game ▸ Setup Project** recreates the URP asset, materials, player settings and the scene. **Snowball Game ▸ Capture Maple Court** renders PNGs to `Logs/Captures/`. |

A real-time mode can reuse the core: feed orders every tick instead of every
30 ticks.

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
