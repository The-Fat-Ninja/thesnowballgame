# Art Direction

Status: **first draft**. The concept sheets referenced here are in
`../Concept/`.

## In one sentence

A warm, chunky, hand-made storybook winter: think stop-motion felt-and-clay
kids in a snow globe town, lit like a crisp morning.

## Touchstones

- Stop-motion and felt textures (Aardman-style warmth, but our own shapes).
- Toy-like stylised 3D with big readable shapes (the readability of Overcooked
  / Brawl Stars at a glance).
- Children's picture books about snow days — soft gradients, warm windows.

## Characters

- **Proportions:** about **3.5 heads tall**. Big heads, big hands (mittens are
  a key read), short legs, chunky boots.
- **Silhouette first:** each kid must be identifiable from silhouette alone
  (Bernard's clipboard and bobble, Pip's tiny round snowsuit, Tobias's
  bulk, Ada's goggles, Felix's giant pom-pom, Biscuit).
- **Materials (Substance Painter smart materials to build once and reuse):**
  - Knit (hats, scarves, mittens) — chunky cable knit with fuzz
  - Puffer nylon — soft sheen, quilted stitching
  - Wool/duffel — felted, toggles
  - Fleece — matte, soft
  - Rubber (snow boots, rain boots), denim, corduroy
  - **Snow-dusting overlay** — a mask that builds up as a kid gets chilled,
    and a darker "wet patch" when Soaked (driven by a shader parameter).
- **Team colour:** carried by the **scarf and hat pom-pom** (and the hat
  band). Driven by a material mask so any kid can be on either team.
  - Home team: **Ember Orange** `#F28C28`
  - Away team: **Plum Purple** `#7A4FD6`
  - Chosen because both pop against white/blue snow and stay distinct for the
    common colour-vision deficiencies. Always paired with shape (team flag
    icon over heads) — never colour alone.
- **Faces:** big, simple features. Expressions via a **texture flipbook**
  (eyes and mouth swap) plus a few blendshapes (cheeks, brows) — cheap on
  mobile, very expressive, fits the stop-motion feel.
- **Cheeks** go rosy as Warmth drops.

## Colour script

| Token | Hex | Use |
|---|---|---|
| Snow Light | `#F7F4EE` | Lit snow (warm cream) |
| Snow Shadow | `#AFC2E8` | Snow in shade |
| Deep Shadow | `#6E7FB8` | Footprints, trails, contact shadow |
| Sky Morning | `#CFE3F5` | Daytime sky |
| Window Glow | `#FFC766` | Lit windows, lamp posts |
| Brick | `#B5543C` | Houses |
| Pine | `#2F6B57` | Trees, hedges |
| Ember Orange | `#F28C28` | Home team |
| Plum Purple | `#7A4FD6` | Away team |
| Cocoa | `#6B3F2A` | UI accents, hot chocolate |
| Ink | `#2A2438` | Outlines, UI text |

Lighting: a low warm sun key with cool blue sky fill. Golden hour for finals,
blue hour as the "streetlights on" end-of-match moment.

## UI

- Landscape only on iPhone/iPad.

- Hand-drawn feel: rounded panels like mitten-knit, chunky type
  (candidates: *Baloo 2*, *Fredoka*, *Lilita One* — all free/OFL).
- Kid profile cards look like collectible trading cards / school photo cards.
- Touch targets ≥ 44 pt on iOS; the plan/execute UI must work one-handed.

## Animation list (per kid, shared rig)

Locomotion: idle (shiver variant), walk, run, crouch-walk, slip/slide, belly
slide, sled sit.
Snow: scoop, pack (loop), roll boulder, build wall (loop), repair.
Combat: throw lob, throw line drive, special throw per kid, dodge left/right,
duck behind cover, peek, hit reactions (front, back, big knockback), Soaked
(dramatic flop), walk to bench, sip cocoa, return.
Social: cheer, taunt, sulk, high-five (paired), celebrate win, lose.

All kids share one **base rig** with 3 body sizes (small / medium / large)
so animations retarget cleanly.

## Modular kit (custom avatars and the cosmetic shop)

Because players build their own kid and buy cosmetics, every kid — named or
custom — is assembled from the same modular kit:

- **Per body size:** base body, hands (mitten-ready), boots attach point.
- **Heads:** a few head shapes sharing one face UV layout so the expression
  flipbooks work on all of them; skin tone is a material parameter.
- **Hair:** short pieces designed to sit under any hat (with a "hat on" and
  "hat off" variant per hair style).
- **Outfit slots:** hat, coat, scarf, mittens, trousers, boots, back item
  (backpack / thermos / stick). Each piece is authored for all three body
  sizes, or uses a blendshape to fit them.
- **Team colour and wetness** stay as material masks on every piece, so any
  cosmetic still reads Home/Away and still gets snowy.
- Named kids are simply "preset outfits" built from this kit, plus their
  signature props.

## Pipeline (same toolchain as RAF)

1. **Concept** — sheets in `Docs/Concept/` (these SVGs first; later painted).
2. **Blender** (source of truth, `BlenderSource/`) — modular kid base mesh per
   body size, outfit pieces as separate meshes, shared rig. Meters, +Y up in
   Unity, +Z forward.
3. **Substance Painter** (`SubstanceSource/`) — the smart-material library
   above; team-colour and wetness as mask channels packed for URP.
4. **Unity 6 URP** — import to `Assets/Art/`, verify scale/pivots/materials,
   prefab per kid, outfit swapping by mesh toggles.
5. Polygon budget guide: kid ≈ 6–9k tris (LOD1 ≈ 3k), map ≈ 150–250k tris on
   PC, half on mobile.
