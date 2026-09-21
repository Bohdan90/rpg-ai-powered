# rpg-ai-powered

Gate C Tactical Prototype. This directory is the Unity project and Git repository root.

- **Pinned editor: Unity 6000.6.2f1** (upgrade only with explicit approval).
- Dependencies: `Packages/manifest.json` and `Packages/packages-lock.json`.
- Accepted Milestone 1: pure deterministic C# combat core and 57 passing EditMode tests.
- `RPG.Core` has no UnityEngine dependency; Presentation remains a placeholder.

Open this directory with the pinned editor. Run Test Runner -> EditMode -> RPG.Tests.
Do not commit Library, Temp, Obj, Logs, UserSettings, build outputs or local IDE state.
Commit Assets and their `.meta` files, Packages, ProjectSettings and Docs together.

See [architecture](ARCHITECTURE.md), the
[prototype contract](Docs/Prototype/GATE_C_TACTICAL_PROTOTYPE_CONTRACT_v0_1.md), and
[Milestone 1 report](Docs/Prototype/MILESTONE_1_IMPLEMENTATION.md).

Milestone 2 gameplay has not started.
