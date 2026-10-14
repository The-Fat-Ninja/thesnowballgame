# The Snowball Game — Design Vision

Status: **first vision draft** (2026-10-14). Nothing here is locked; the open
questions are collected in `../Development/OPEN_QUESTIONS.md`.

## One-line pitch

School's closed, the snow is perfect, and the whole street is at war. Pick a
crew of four kids, each with a talent of their own, and out-throw, out-build
and out-think the gang from the next street over.

## Design pillars

1. **The best snow day ever.** Every map should feel like the morning you
   looked out of the window and the world had gone white. Warm, funny, safe,
   nostalgic. Nobody gets hurt; they get soaked, go home and get a hot chocolate.
2. **Every kid matters.** A squad of four, each with one clear role and a
   personality you can read from across the map. Losing your Builder should
   change how you play.
3. **The snow is the game.** Snow is ammunition, cover, terrain and weather at
   once. You scoop it, pack it, stack it, roll it, slide on it and hide in it.
   Cover gets knocked down and rebuilt during the match.
4. **Readable in a glance.** Works on a phone held in one hand and on a 27"
   monitor. Chunky silhouettes, strong team colours, clear telegraphs for every
   throw.
5. **Short, replayable matches.** 3–6 minutes. One more match.

## Setting

**Hollybrook**, a small, slightly timeless town (somewhere between a British
village and a North American suburb, so it travels well), during **Snow Week**:
five days of snow closures. Terraced streets and cul-de-sacs, the primary
school field, the park with the frozen pond, the sledging hill, the allotments,
the woods behind the estate with a half-built tree fort.

Every street has its own crew of kids, and every crew has a name, a flag (a
scarf on a broom handle) and a grudge.

Adults are never seen directly — only heard ("Dinner's in TEN MINUTES!"),
glimpsed as a curtain twitching, or present as rules of the world (the
**Street Rules**, below).

### The Street Rules (fair-play rules that are also mechanics)

- **No iceballs.** Packing ice into a snowball is the one real crime. The rival
  boss crew, *the Icicles*, cheat with them — that is their story hook.
- **No faces.** Hits register on body/back/legs. Keeps the tone gentle and
  gives a clean hit-zone design (back hits = bonus, it's a sneak).
- **Soaked means you're out.** You go and sit on the Hot Chocolate Bench.
- **When the streetlights come on, game's over.** The match timer, diegetic:
  the sky dims and lights flick on in the last 30 seconds.

## Core loop

**In a match:** scoop snow → pack it (choose a snowball type) → move between
cover → throw / use an ability → rebuild or move cover → soak the other side
out, or win the objective.

**Between matches:** earn **Snow Day stars** → unlock kids, cosmetics (hats,
mittens, scarves, sledges), and ability variants → pick your next crew of four
→ choose the next street on the town map.

**Over the campaign:** five Snow Days. Each day opens a new district of
Hollybrook. Beat a street's crew and its captain *joins your roster* — the
campaign is how you recruit your team.

## Key mechanics

### Warmth, not health

Each kid has a **Warmth** meter (their "Grit" stat sets its size).

| State | Effect |
|---|---|
| Toasty (full) | Normal |
| Chilly | Packing speed reduced |
| Shivering | Movement and throw range reduced |
| **Soaked** | Out — walks to the Hot Chocolate Bench |

Warmth slowly returns behind cover and out of the wind, quickly near a support
kid's thermos, and never in the open during a blizzard.

### Snow as ammunition

- Each kid carries 1–3 snowballs (depends on the kid).
- **Scoop** anywhere there is loose snow. Packed paths, ice and indoors have
  none — map design uses this.
- **Pack** time depends on snowball type. Faster in fresh powder, slower in
  slush.

| Snowball | Make time | Effect |
|---|---|---|
| Quick ball | very short | Light chill, fast flat throw |
| Packed ball | medium | Standard damage, good arc |
| Slushball | medium (needs slush) | Splash, Slows target |
| Powder puff | short | No damage, creates a cloud that blocks line of sight |
| Rolled boulder | long (rolled across snow; grows as it rolls) | Big knockback, becomes cover where it stops |

### Throwing

- Every throw shows a **telegraphed arc** before release and a landing marker
  while in flight (readability pillar, and it makes dodging possible).
- **Lob** (arc over cover, slower, can be dodged) vs **Line drive** (flat,
  fast, blocked by cover).
- **Back hits** from flanking do bonus chill.
- **Wind** bends arcs on some maps; it's shown as drifting flakes.

### Cover that changes

| Cover | Notes |
|---|---|
| Snowbank / drift | Low cover, chips down in 3 stages, can be re-packed |
| Snow wall (built) | Built by any kid slowly, by the Builder quickly |
| Snow fort | Multi-wall, 3 build stages, the objective in Siege mode |
| Snowman | Full cover, can be "borrowed" (the hat!) for disguise |
| Tree | Full cover, can be shaken to dump snow on anyone underneath |
| Parked car / shed / wheelie bin | Indestructible hard cover |
| Hedge | Blocks sight, not throws (lobs go over) |
| Sledge | Pick-up item; a fast dash on slopes, can be used as a shield |

### Terrain

- **Deep snow:** slow, but lots of ammo and hides your footprints poorly.
- **Packed path:** fast, no ammo.
- **Ice (pond, puddles):** you slide; momentum carries you. Some kids are great
  on it.
- **Slopes:** height = range bonus; sledges go downhill fast; boulders roll.
- **Footprints:** kids leave tracks in fresh snow. Tracks reveal recent
  movement — a light stealth layer for scouts.

### Weather (per match modifiers)

- **Clear & crisp:** baseline.
- **Snowfall:** ammo everywhere, cover slowly re-fills.
- **Blizzard:** short sight range, warmth drains in the open.
- **Thaw:** cover melts over time, slush everywhere, slushballs strong.
- **Golden hour:** long shadows, a cosmetic "hero" lighting for finals.

## Game modes (initial set)

| Mode | Summary |
|---|---|
| **Last Kid Standing** | Elimination. Soak the whole other crew. |
| **Capture the Scarf** | Classic CTF with each crew's scarf-on-a-broom flag. |
| **King of the Hill** | Hold the top of the sledging hill. |
| **Fort Siege** | One crew builds and defends a fort, the other attacks; swap sides. |
| **Snowman Showdown** | Build the biggest snowman while stopping the other crew building theirs. |
| **Big Kids** (co-op survival) | Waves of teenagers from the secondary school. |

## Play style

Real-time vs turn-based is still open. The options and a recommendation are in
`PlayStyle_Options.md`. Short version: **build one deterministic rules core
(as on RAF) and prototype "planned simultaneous turns" first**, with a
real-time view as the second prototype on the same core.

## Platforms

- **iOS** (iPhone + iPad): touch-first, portrait-or-landscape decision open,
  short sessions, Game Center.
- **Steam** (Windows; macOS optional): mouse/keyboard and **full gamepad**,
  targeting **Steam Deck Verified** — the Deck is a natural fit for this game.
- One codebase, Unity 6 / URP, two quality tiers.

## Audience and tone

Family-friendly (target PEGI 3/7, ESRB E). Cartoon, physical comedy, no
violence beyond a snowball to the back. Appeals to kids 8–14 and to adults who
remember snow days. Humour comes from the kids' personalities and barks
("That was NOT a fair one!"), from slapstick (slipping on ice, a tree dumping
snow), and from the unseen grown-ups.

## Kid profiles

Every kid has a collectible **profile card**: portrait, name, age, street,
role, five stats (Arm, Aim, Hustle, Grit, Craft), likes/dislikes, a signature
bark and a short bio. See `Characters.md`. Profiles also unlock as you play
(e.g. "Has been soaked 50 times", "Favourite sledge").

## What makes it different

- The terrain is destructible and *rebuildable*; cover is a resource you spend
  and make.
- Abilities come from kids' real-feeling hobbies (a cricket bowler, a hockey
  goalie, an inventor) rather than superpowers — grounded, then exaggerated.
- The campaign recruits your roster from the crews you beat.
- Warm, nostalgic tone in a genre usually done as military or sci-fi.
