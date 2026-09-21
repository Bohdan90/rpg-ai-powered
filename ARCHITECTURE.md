# Gate C architecture baseline

The Unity project root is this directory (`My project/`). Milestones 1 and 2A implement the pure deterministic combat core and battlefield legality; no presentation gameplay is implemented.

| Folder | Purpose / dependency boundary |
| --- | --- |
| `Assets/_Project/Core` | `RPG.Core`: plain C#; no UnityEngine references or project dependencies. |
| `Assets/_Project/Presentation` | `RPG.Presentation`: Unity-facing C#; explicitly references Core. |
| `Assets/_Project/Tests` | `RPG.Tests`: Editor-only NUnit tests; explicitly references Core. |
| `Assets/_Project/Data` | Reserved for project data assets. |
| `Assets/_Project/Scenes` | Reserved for project scenes. |
| `Assets/_Project/UI` | Reserved for UI assets; put Unity-facing C# in Presentation. |

Core uses `noEngineReferences: true` and disables automatic precompiled references.
Presentation explicitly references Core. All three assemblies disable automatic
references from predefined assemblies; future assembly dependencies must be explicit.
Core contains the combat model/resolver plus pure grid, movement, pathfinding and LoS. Presentation remains a placeholder.
Existing template scenes and settings are unchanged.

`CoreArchitectureTests` loads the compiled Core assembly and rejects dependencies
whose names start with `Unity`, or equal `RPG.Presentation`. Run it in the Unity
Test Runner's EditMode tab. Tests are excluded from player builds.

## Verification (2026-09-21)

- Required Unity directories, asset metadata, and package manifest/lockfile are present.
- **Pinned Gate C editor: 6000.6.2f1**, explicitly approved for this prototype.
  Do not downgrade to 6.3 LTS or upgrade without explicit approval.
  `ProjectSettings/ProjectVersion.txt` records this exact version; package versions
  remain recorded in `Packages/manifest.json` and `Packages/packages-lock.json`.
- URP 17.6.0 is installed, with valid pipeline assets assigned to the PC and Mobile
  quality levels. The empty Graphics default is overridden by those quality settings.
- Rider integration 3.0.38 and Unity Test Framework 1.8.0 are installed and locked.
- Milestone 1 baseline: **57 passed**. After Milestone 2A: **97 passed, 0 failed,
  0 skipped**, including the compiled Core dependency test. See
  `Docs/Prototype/MILESTONE_2A_IMPLEMENTATION.md` for current geometry and API details.
- Rider's `Packages.Rider.Editor.RiderScriptEditor.SyncSolution` completed successfully
  and generated a solution and all three RPG C# projects. The generated Core project
  has no UnityEngine references; Presentation and Tests reference Core.
- The installed editor adds an iOS/Xcode tooling reference to compiler/IDE inputs,
  even with engine references disabled. The compiled Core assembly has no Unity
  dependency, as verified by the architecture test.
- The Git root is this Unity project directory. Assets (including `.meta` files),
  Packages, ProjectSettings, and Docs are tracked together. Generated Unity directories
  and IDE state are ignored.
- The original Core-only commits are preserved as ancestors. Repository metadata was
  moved to the project root; the baseline commit records the corrected file paths.
  Branches and the configured GitHub remote were preserved; no history rewrite or push.

Verification used a temporary copy because the original project was open in Unity.
After Milestone 2A, all 97 tests ran successfully. Current test results
and logs are at `/private/tmp/gate-c-m1-4dkkwcy6/`: `TestResults.xml` and `tests.log`.
The earlier Rider generation log is at `/private/tmp/gate-c-verify-puaz7w_z/rider-generation.log`.
To generate the local solution for Rider, select Rider in Unity's External Tools
preferences and use Regenerate project files. Generated IDE files stay ignored.

No ECS, dependency injection, service locator, ability, save, campaign, networking,
or other gameplay framework is included.
