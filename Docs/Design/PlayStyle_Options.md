# Play Style Options — Real-time vs Turn-based

Status: **open decision**. This document lays out the options so we can
prototype rather than argue.

## The constraint that matters most

The same game has to feel good with **one thumb on an iPhone** and with a
**mouse or a Steam Deck**. Controlling four kids at once in real time on a
phone is the hard case.

## Option A — Real-time squad action

You directly control one kid (twin-stick / click-to-move-and-aim). The other
three follow AI orders ("Hold", "Follow", "Build here", "Push") and you can
swap kids at any time.

- **For:** immediate, physical, great for Steam and gamepad; dodging a
  snowball feels brilliant; easy to make online PvP exciting.
- **Against:** hard on touch; squad AI has to be good; netcode for real-time
  physics is the most expensive multiplayer option.
- **Reference feel:** Overcooked-style readability, Brawl Stars controls.

## Option B — Classic turn-based tactics

Grid or free movement with action points. Move, throw, build, overwatch.
Teams alternate.

- **For:** perfect on touch, easy async multiplayer on iOS, deep, cheap netcode.
- **Against:** loses the "dodge the snowball" physicality; slower; a crowded
  genre.
- **Reference feel:** XCOM, Into the Breach, Mario + Rabbids.

## Option C — Planned simultaneous turns ("WEGO") — **recommended first prototype**

Both crews plan at the same time (about 10–15 seconds, or untimed in
single-player): drag a path for each kid, set a throw target or ability. Then
**both plans play out together for ~3 seconds** in real time, with physics:
snowballs in flight, kids dodging, cover crumbling. Then plan again.

- **For:**
  - Touch-friendly *and* mouse/gamepad-friendly.
  - Keeps the physical comedy and the "did they dodge?" moment — every
    resolution is a little replay worth watching.
  - Mind-games: you're predicting where they'll *be*, which is exactly what a
    real snowball fight is.
  - Async multiplayer is natural on iOS (send your plan, get theirs).
  - Live PvP needs only plan exchange plus deterministic simulation — far
    cheaper than real-time netcode.
- **Against:** less familiar to casual players (needs a good tutorial); the
  resolution has to be deterministic.
- **Reference feel:** Frozen Synapse, Laser League's readability, Worms'
  slapstick.

## Recommendation

1. Build a **deterministic, engine-independent rules core** (pure C#, fixed
   tick, seeded RNG — the same split as RAF's rules core / Unity interface).
   Kids, snow, cover, warmth, throws and abilities live there.
2. Abilities and timings are defined in **ticks** (e.g. 10 ticks per second),
   so the same data drives either mode:
   - in WEGO, a plan is a list of orders executed over N ticks;
   - in real-time, input becomes orders every tick.
3. **Prototype C first** in a greybox map (one cul-de-sac, 2v2 with
   placeholder capsules). Then put a **real-time view (A)** on the same core
   and play both for a week.
4. Decide with the playtests. Keeping both is possible (e.g. WEGO campaign on
   iOS, real-time "Recess" mode on Steam), but costs balancing time.

## Ability design rules that work for both modes

- Every ability has a **telegraph** (what it will do) and a **duration in
  ticks**.
- No ability requires frame-perfect timing.
- Abilities use **cooldowns in ticks** plus an **ultimate charge** ("Snow Day
  Special") built by hitting, building and reviving.
- Area effects are circles/cones/lines — easy to preview on touch.
