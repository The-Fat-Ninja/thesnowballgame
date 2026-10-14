# Owner Decisions

The owner's rulings. Newest first within each date. Answers to
`OPEN_QUESTIONS.md` are recorded here.

## 2026-10-14

| # | Topic | Ruling |
|---|---|---|
| 1 | Play style | Prototype **planned simultaneous turns (WEGO) first**, real-time second on the same rules core. Owner likes WEGO; suspects real-time is more saleable — playtests decide, both stay possible. |
| 2 | Modes of play | **Single-player campaign first**, then online PvP, **async PvP on iOS**, and **local couch play on Steam / Steam Deck**. |
| 3 | Squad size | **4 v 4 stays the design target, provisionally.** Prototype with squad size as a setting (2–4) and confirm with playtests. See note below. |
| 4 | Business model | **Free-to-play** with cosmetics. No pay-to-win. |
| 5 | Setting | **North American suburb.** |
| 6 | Age rating | **PEGI 3 / ESRB E.** |
| 7 | Roster | Draft roster accepted as a starting point. |
| 8 | Art style | Stylised 3D, felt / stop-motion direction. |
| 9 | Camera | 3/4 top-down tactical camera; **landscape only** on iPhone/iPad. |
| 10 | Avatars | **Customisable player avatar kid** as well as the named roster. |
| 11 | Engine | Unity **6000.6.3f1**, URP 17.6 (matches RAF). |
| 12 | Repository | Git LFS as RAF; self-hosted CI later. |
| 13 | Title / stores | Nothing reserved yet; "The Snowball Game" is a working title. |

### Note on 3 — is controlling four kids difficult?

It depends on the mode:

- **WEGO (planned turns):** four is comfortable. You plan one kid at a time
  with no pressure in single-player, and in timed PvP every kid keeps its last
  order and has a sensible default ("hold cover, throw at the nearest kid in
  range, re-pack when empty"). You only touch the kids you want to change.
  Four is what gives the game its tactical depth.
- **Real-time:** nobody controls four kids directly, especially on touch. You
  control one and swap freely; the other three follow quick orders (Hold /
  Follow / Push / Build here) and squad AI. Quality of that AI is the real
  cost.
- **Screen space:** four per side is eight kids on a landscape phone screen —
  fine at our map size (40 × 30 m) and zoom, but it is the thing to check
  first on a real iPhone.

So: keep 4 v 4, build the rules core so squad size is just a number, and
playtest 3 v 3 against 4 v 4 in the first prototype. Smaller maps (Back
Gardens) can run 2 v 2.

### Consequences to plan for

- **F2P + ESRB E + iOS** means a children's audience: COPPA (US) and GDPR-K
  (EU/UK) apply, and if we list in the App Store **Kids category** there are
  no third-party ads or analytics and purchases must be behind a parental
  gate. Decide early whether to target the Kids category or a general
  "everyone" listing; it shapes analytics, chat and the shop. Online play
  should use preset quick-chat only (no free text).
- **Customisable avatars** push the art pipeline towards a modular kit (heads,
  hair, outfits on the shared rig) from day one — which also feeds the F2P
  cosmetic shop.
- **Store names:** reserve the Steam app and an App Store bundle ID once the
  title is settled.
