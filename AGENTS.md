# The Snowball Game

Conventions follow the RAF The Battle of Britain 1940 project unless noted.

## Layout (planned)

- `Docs/` - design, concept art, working records (see `Docs/README.md`).
- `Tools/` - scripts (concept-sheet generator, later build and CI tools).
- `BlenderSource/` - editable `.blend` files (source of truth for models).
- `SubstanceSource/` - Substance Painter projects.
- Unity project at the repository root once created (`Assets/`, `Packages/`,
  `ProjectSettings/`).

## 3D asset workflow

Blender is the source of truth. Export game-ready assets into
`Assets/Art/Models/`. Use metres; in Unity +Y is up and +Z is forward.
Inspect every asset in both Blender and Unity (scale, orientation, hierarchy,
materials, normals, pivots) before calling it done. Fix problems in the
source, not in the imported mesh.

## Rules core

Game rules live in an engine-independent, deterministic C# assembly (fixed
tick, seeded RNG) so the game can run as planned-simultaneous-turns, real-time
or turn-based over the same rules. Unity presentation code reads it; it never
reads Unity.

## Version control

- Git with the GitHub remote `The-Fat-Ninja/thesnowballgame`.
- Git LFS for binary sources (see `.gitattributes`).
- Do not commit generated Unity folders (`Library/`, `Temp/`, `Logs/`,
  `UserSettings/`, `Build/`).
- Work on a branch for substantial changes; do not merge to `main` without the
  owner's approval.
