# Open Questions for the Owner

Answers go into `OWNER_DECISIONS.md` as they are made.

## Game shape

1. **Play style:** happy to prototype "planned simultaneous turns" (WEGO) first,
   with real-time on the same rules core second? (`../Design/PlayStyle_Options.md`)
2. **Single-player vs multiplayer:** single-player campaign first, then online
   PvP? Async PvP on iOS? Local couch play on Steam / Steam Deck?
3. **Squad size:** 4 v 4 assumed. 3 v 3 is cheaper to animate and easier on a
   phone screen.
4. **Business model:** premium (one price, cosmetics earned) or free-to-play
   with cosmetics? This changes progression design a lot.

## Tone and setting

5. **Setting flavour:** a British village (terraces, "Year 6", gritting lorry,
   allotments) or a more neutral/North American suburb, or the deliberate mix
   in the draft?
6. **Age rating target:** PEGI 3 / ESRB E assumed.
7. **Named kids:** happy with the draft roster as a starting point? Any kids
   you already have in mind (perhaps based on family — with permission)?

## Art

8. **Art style:** stylised 3D in the felt / stop-motion direction (fits the
   Blender + Painter pipeline), or something else — cel-shaded, low-poly?
9. **Camera:** 3/4 top-down tactical camera assumed. Orientation on iPhone:
   landscape only?
10. **Avatars:** fully customisable avatar kids as well as the named roster,
    or named kids only (with cosmetic outfits)?

## Technical

11. **Unity version:** match RAF (Unity 6000.6.3f1, URP 17.6)?
12. **Repository:** Git LFS for `.blend`, `.spp`, `.fbx`, `.psd`, textures —
    same as RAF. CI on the self-hosted runner too, later?
13. **Store presence:** Steam page and App Store bundle ID — any names reserved
    yet? Is "The Snowball Game" the final title?
