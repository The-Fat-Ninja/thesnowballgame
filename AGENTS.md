# The Snowball Game

Conventions follow the RAF The Battle of Britain 1940 project unless noted.

## Layout

- Unity 6000.6.3f1 project at the repository root (`Assets/`, `Packages/`,
  `ProjectSettings/`).
- `Assets/Scripts/Core` - rules core (`SnowballGame.Core`, no engine references).
- `Assets/Scripts/Presentation` - Unity views, input and HUD.
- `Assets/Editor` - project setup and capture tools.
- `Assets/Tests/EditMode`, `Assets/Tests/PlayMode` - Unity tests;
  `Tests/SnowballGame.Core.Tests` compiles the core and edit-mode tests for
  `dotnet test` (`Tools/test-core.ps1`).
- `Docs/` - design, concept art, working records (see `Docs/README.md`).
- `Tools/` - scripts.
- `BlenderSource/`, `SubstanceSource/` - art sources (planned).

## Checks before committing

- `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/test-core.ps1`
- Unity batch compile / tests as in `Docs/Development/PROTOTYPE.md`.
- The core targets C# 9 (Unity's compiler); the dotnet test project enforces
  `LangVersion 9.0`.

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
