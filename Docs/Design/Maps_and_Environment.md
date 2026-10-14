# Maps and Environment

Status: **first draft**.

## Map rules of thumb

- **Small:** about 40 × 30 m playable space. A full match fits on a phone
  screen at the default zoom, with 2–3 zoom steps.
- **Mirrored or "fair-but-different":** competitive maps are mirrored;
  campaign maps can favour the defending crew (they're on home turf).
- **Three lanes, one high point:** a left flank, a right flank, a contested
  middle, and something high (a hill, tree fort, climbing frame).
- **Ammo economy:** fresh snow at the edges, packed paths and ice in the middle
  — you have to leave cover to reload.
- **Every map has one "toy":** a feature only that map has (the pond, the
  slide, the bonfire, the greenhouse).
- **Grid-compatible:** layouts work on a 1 m grid underneath, in case we go
  turn-based, while still looking organic.

## Launch maps (one per campaign district)

### 1. Maple Court (cul-de-sac) — tutorial / home

Semicircle of houses with porches and driveways round a turning circle. Parked
cars and an SUV (hard cover), low front-yard walls, trash cans and mailboxes, a
streetlight in the middle, a half-built snowman. **Toy:** the town snowplow
drives through on a timer and clears a strip of snow (no ammo in that strip
for a while) — and throws up a new snowbank along the curb.

### 2. Hill Road — the sledding hill

A long slope with a flat top. Defenders on top get range; attackers come up
three gullies. **Toy:** sleds at the bottom; boulders rolled from the top
grow and roll down the lanes.

### 3. The Community Garden

Raised beds, tool sheds, cold frames, a scarecrow, a compost heap.
Lots of hedges (block sight, not lobs). **Toy:** the greenhouse — inside, no
snow (no ammo), but you're warm.

### 4. Hollybrook Park & the Frozen Pond

The pond is the middle: ice, no ammo, sliding. A gazebo, benches, a
duck house, trees. **Toy:** the ice cracks if three or more kids stand on one
spot — they drop into ankle-deep water (instant Shivering; no one drowns —
it's a shallow ornamental pond, clearly signposted).

### 5. The Woods & the Tree Fort

Dense trees, fallen logs, a stream with stepping stones, the tree fort on a
rise. **Toy:** the rope swing — a fast traverse across the stream.

### 6. The Icicles' Subdivision (finale)

Clean, new, too tidy: show-home lawns, a playpark, a roundabout. **Toy:**
garden sprinklers left on — they freeze the ground into ice in a pattern on a
timer.

### Extra / later

- **Hollybrook Elementary — the school field:** jungle gym, the big slide,
  soccer goals, portable classroom, flagpole. Map for Capture the Scarf.
- **Main Street Holiday Fair:** stalls, lights, a carousel. Seasonal event map.
- **Backyards:** picket fences you can climb, trampolines, a swing set. Tight 2v2.

## Interactive props (shared library)

| Prop | Behaviour |
|---|---|
| Snowbank | 3 damage stages, re-packable |
| Snow wall (built) | 3 build stages, 3 damage stages |
| Snowman | Full cover; hat/carrot/scarf cosmetics; Felix can swap with it |
| Tree (pine / bare) | Full cover; shake to dump snow; climbable (Jun) |
| Parked car | Hard cover; car alarm goes off when hit (reveals nearby kids) |
| Trash can | Hard cover; can be pushed (lid comes off — comedy) |
| Mailbox | Small hard cover |
| Hedge | Sight blocker |
| Garden wall | Low hard cover |
| Sledge | Pick-up; downhill dash; shield |
| Bucket | Pick-up; instant wall segment for anyone |
| Lamp post | Lights come on = last 30 s |
| Thermos station (bench) | Hot Chocolate Bench — where soaked kids sit |

## Environment art language

- **Snow is not white.** It is warm cream in sunlight, lavender-blue in
  shadow, with a sparkle pass. Footprints and trails read as blue-violet.
- **Warm against cold.** Windows glow amber; doors are painted strong
  colours; Christmas lights are optional per map.
- **Chunky, soft shapes.** Rounded roof edges with thick snow caps, bevelled
  bricks, oversized icicles. Nothing sharp except the Icicles' subdivision (which
  is deliberately angular and grey — the art tells the story).
- **Low-poly-friendly.** Silhouettes and colour do the work so that iPhone
  builds can drop detail without losing the look.

## Snow tech notes (Unity / URP)

- **Snow deformation:** a top-down render texture of "trail stamps" read by
  the snow surface shader (vertex displacement on PC, normal/colour-only on
  mobile). Footprints, sled tracks and boulder trails come for free.
- **Destructible cover:** authored as stage meshes in Blender (Stage 0–3)
  swapped by the rules core, plus a particle puff. Cheap, deterministic,
  mobile-safe — no runtime mesh destruction.
- **Built cover:** a modular wall kit (straight, corner, end cap) in the same
  stages.
- **Weather:** a shared VFX graph per weather type; blizzard reduces fog
  distance and the rules core's sight range together.
