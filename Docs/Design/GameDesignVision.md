# The Snowball Game — Design Vision

Status: **vision draft v0.2** (2026-10-14). Updated with the owner's rulings
in `../Development/OWNER_DECISIONS.md`.

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

**Hollybrook**, a North American suburb with a slightly timeless feel (porches,
mailboxes, station wagons next to minivans, no phones in anyone's hands),
during **Snow Week**: five days of snow closures. Cul-de-sacs and tree-lined
streets, the elementary school field, the park with the frozen pond, the
sledding hill, the community garden, the woods behind the subdivision with a
half-built tree fort. In-game text uses US English.

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

**Between matches:** earn **Snowflakes** → unlock kids, cosmetics (hats,
mittens, scarves, sleds), and ability variants → pick your next crew of four
→ choose the next street on the town map.

**Over the campaign:** five Snow Days. Each day opens a new district of
Hollybrook. Beat a street's crew and its captain *joins your roster* — the
campaign is how you recruit your team.

**Your Kid:** the player makes their own customisable kid, who is always the
crew captain (see *Your Kid* below). The named kids are the rest of the crew.

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
- **Pack** time depends on snowball type (and the kid's Craft).
- A kid carries a **mixed pouch**; you choose which snowball to throw and what
  to make next. Every kid has a **favourite** they make by default.

As built in the prototype (numbers in `Rules.cs`; chill is per hit, before the
back-hit bonus):

| Snowball | Make time | Line / lob chill | What makes it different |
|---|---|---|---|
| **Packed** | 1.0× | 25 / 20 | The all-rounder. |
| **Quick** | 0.45× | 15 / 12 | Flies 35% faster with 30% less spread; small landing spot. |
| **Slush** | 1.3× | 15 / 15 | Lobbed, it hits **everyone** within 1.3 m of where it lands, and **slows** whoever it hits to 55% pace for 3 s. Slower in flight. |
| **Powder puff** | 0.6× | 0 / 0 | Bursts into a **cloud** (1.7 m, 4 s). Line drives can't see through it (AI and plan lines treat it as blocked), and any throw into, out of or through it has **3× spread**. |
| **Boulder** | 40 − 4×Craft ticks; needs an empty pouch and fills it | 35 | **Rolled**, not thrown: grows from 0.3 m to 0.75 m as it rolls, flattens and **shoves back** the first enemy, thumps cover for 40, and **becomes a snowbank** where it stops. |

Favourites: Pip & Jun Quick · Bernard Slush · Rosa & Felix Powder · Tobias
Boulder · Maisie & Ada Packed. Design draft note: slush was meant to need slush
terrain; the prototype lets any kid make it on fresh snow, more slowly.

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
| Parked car / shed / trash can / mailbox | Indestructible hard cover |
| Hedge | Blocks sight, not throws (lobs go over) |
| Sled | Pick-up item; a fast dash on slopes, can be used as a shield |

### Terrain

- **Deep snow:** slow, but lots of ammo and hides your footprints poorly.
- **Packed path:** fast, no ammo.
- **Ice (pond, puddles):** you slide; momentum carries you. Some kids are great
  on it.
- **Slopes:** height = range bonus; sleds go downhill fast; boulders roll.
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
| **King of the Hill** | Hold the top of the sledding hill. |
| **Fort Siege** | One crew builds and defends a fort, the other attacks; swap sides. |
| **Snowman Showdown** | Build the biggest snowman while stopping the other crew building theirs. |
| **Big Kids** (co-op survival) | Waves of teenagers from the high school. |

## Play style

Real-time vs turn-based is still open. The options and a recommendation are in
`PlayStyle_Options.md`. Short version: **build one deterministic rules core
(as on RAF) and prototype "planned simultaneous turns" first**, with a
real-time view as the second prototype on the same core.

## Your Kid (customisable avatar)

- Built in a kid creator: body size (small / medium / large, matching the
  three rig sizes), skin tone, face shape, eyes, hair style and colour,
  freckles/glasses, then outfit pieces (hat, coat, scarf, mittens, trousers,
  boots) and a sled.
- **Role is chosen, not bought:** Your Kid picks one of the six roles and an
  ability loadout from that role's pool (unlocked through play). The named kids
  keep their fixed signature kits — they're the characters; Your Kid is you.
- Your Kid's look is your identity online (preset quick-chat and emotes only —
  no free text, in line with the age rating).
- Every cosmetic item fits every kid, named or custom, on the shared rig.

## Business model — free-to-play

Principles: **fair, generous, kind to kids and parents.**

- **Never pay-to-win.** Abilities, kids and loadouts are earned by playing.
  Money buys looks only.
- **Earned currency — Snowflakes:** from matches, daily snow-day challenges and
  the campaign. Buys cosmetics and unlocks kids faster.
- **Premium currency — Cocoa Cups:** cosmetics only.
- **Snow Pass (seasonal):** a free track and a paid track of cosmetics per
  season (Winter Break, Ice Festival, Spring Thaw...). No time-limited power.
- **Named kids:** all unlock free through the campaign; a cosmetic "first
  edition" outfit can be bought.
- **No loot boxes.** Every item is shown before you buy it.
- **Children's audience rules apply:** COPPA / GDPR-K, parental gate on
  purchases, no third-party ad SDKs; decide early whether to list in the App
  Store Kids category (stricter: no third-party analytics). See
  `../Development/OWNER_DECISIONS.md`.
- The campaign is fully playable for free; it's the shop window.

## Squad size

**4 v 4 is the design target, provisional.** Squad size is a number in the
rules core (2–4); the first prototype playtests 3 v 3 against 4 v 4 on a real
iPhone. In WEGO you plan each kid (with sensible standing orders, so you only
touch the ones you want to change); in real-time you control one kid and give
quick orders to the rest.

## Platforms

- **iOS** (iPhone + iPad): touch-first, **landscape only**, short sessions,
  Game Center, async PvP.
- **Steam** (Windows; macOS optional): mouse/keyboard and **full gamepad**,
  targeting **Steam Deck Verified**; **local couch play** (split control, one
  shared screen) as well as online PvP.
- One codebase, Unity **6000.6.3f1** / URP 17.6 (same as RAF), two quality
  tiers.

## Release order

1. Single-player campaign (WEGO) — vertical slice first.
2. Online PvP (iOS async + live), Steam couch play.
3. Real-time mode on the same rules core, if playtests support it.

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
(e.g. "Has been soaked 50 times", "Favourite sled").

## What makes it different

- The terrain is destructible and *rebuildable*; cover is a resource you spend
  and make.
- Abilities come from kids' real-feeling hobbies (a baseball pitcher, a hockey
  goalie, an inventor) rather than superpowers — grounded, then exaggerated.
- The campaign recruits your roster from the crews you beat.
- Warm, nostalgic tone in a genre usually done as military or sci-fi.
