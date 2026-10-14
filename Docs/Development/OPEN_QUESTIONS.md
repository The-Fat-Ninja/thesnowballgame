# Open Questions for the Owner

Answered 2026-10-14; rulings recorded in `OWNER_DECISIONS.md`. New questions
are at the end.

## Game shape

1. **Play style:** happy to prototype "planned simultaneous turns" (WEGO) first,
   with real-time on the same rules core second? (`../Design/PlayStyle_Options.md`)
   Yes, I suspect real-time will be more saleable but I like wego.
2. **Single-player vs multiplayer:** single-player campaign first, then online
   PvP? Async PvP on iOS? Local couch play on Steam / Steam Deck?
   Yes.
3. **Squad size:** 4 v 4 assumed. 3 v 3 is cheaper to animate and easier on a
   phone screen.
   Would 4 teammates be difficult for the player to control?
4. **Business model:** premium (one price, cosmetics earned) or free-to-play
   with cosmetics? This changes progression design a lot.
   Free-to-play

## Tone and setting

5. **Setting flavour:** a British village (terraces, "Year 6", gritting lorry,
   allotments) or a more neutral/North American suburb, or the deliberate mix
   in the draft?
   North American suburb
6. **Age rating target:** PEGI 3 / ESRB E assumed.
   yes
7. **Named kids:** happy with the draft roster as a starting point? Any kids
   you already have in mind (perhaps based on family — with permission)?
   That's fine for now

## Art

8. **Art style:** stylised 3D in the felt / stop-motion direction (fits the
   Blender + Painter pipeline), or something else — cel-shaded, low-poly?
   Agreed
9. **Camera:** 3/4 top-down tactical camera assumed. Orientation on iPhone:
   landscape only?
   yes
10. **Avatars:** fully customisable avatar kids as well as the named roster,
    or named kids only (with cosmetic outfits)?
   Customizable
## Technical

11. **Unity version:** match RAF (Unity 6000.6.3f1, URP 17.6)?
   Yes
12. **Repository:** Git LFS for `.blend`, `.spp`, `.fbx`, `.psd`, textures —
    same as RAF. CI on the self-hosted runner too, later?
    Yes
13. **Store presence:** Steam page and App Store bundle ID — any names reserved
    yet? Is "The Snowball Game" the final title?
    Not yet

## New questions (2026-10-14)

14. **App Store listing:** target the **Kids category** (stricter: no
    third-party analytics, parental gates everywhere) or a general "everyone"
    listing with age-appropriate design?
15. **Your Kid vs named kids:** is the custom kid always the captain, or
    optional (a player could field four named kids)?
16. **Title shortlist:** want me to draft alternative titles and check for
    clashes on Steam and the App Store before you reserve names?
