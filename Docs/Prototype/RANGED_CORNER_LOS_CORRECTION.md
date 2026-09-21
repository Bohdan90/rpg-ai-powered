# Gate C — ranged exposed-corner / sealed-vertex correction

2026-09-21. Implemented on current HEAD `06e5fa1`, preserving its Field 19×13 and melee correction on top of `8af4f87`. Unity 6000.6.2f1; local commit only, no push.

## Final user-approved geometry

| Case | Melee contact | Ranged solid LoS | Diagonal movement |
|---|---|---|---|
| Open diagonal | Allowed | Clear | Allowed if unoccupied |
| One solid orthogonal side, corner grazed | Allowed | Clear | Blocked |
| Two solid orthogonal sides / shared vertex | Blocked | Blocked: sealed zero-width gap | Blocked |
| Ray enters a solid cell interior | n/a | Blocked | Cannot enter solid cells |
| Segment only follows one wall boundary | n/a | Clear, unless it also encounters a sealed vertex | Unchanged |

The user's latest clarification **retracts** the intermediate answer permitting pure touches at two-wall junctions: a ray through the shared vertex of diagonally opposed solids is blocked. A single exposed corner remains legal. No broader weakening of walls is intended.

## Root causes and changes

Melee (already fixed in `06e5fa1`): `IsMeleeCornerClear` formerly required both side cells to be non-solid. It now requires at least one, shared by Basic validation/preview, ZoC and OA preview/execution. No further melee mechanics change here.

Ranged: `IsClear` formerly treated every solid cell enumerated by inclusive supercover as an obstruction, including cells touched only at one vertex. The exact user shot `HA (12,5) → EW (9,8)` with wall `(9,7)` reproduced the bug before this change: the three-case regression run had **2 passed / 1 failed**, failing the corner-only shot; true interior and clear cases passed.

The existing supercover remains a conservative candidate enumeration. For each solid candidate, Core now:
1. Clips the finite segment against the **open square interior** of the cell. Coordinates are doubled integers (centers even, boundaries odd), with rational parameter interval comparisons. A positive-length interior intersection blocks; an isolated touch or parallel boundary overlap does not. No epsilon, random sampling or physics.
2. Checks for a diagonally opposite solid sharing a vertex exactly on the finite segment. Integer cross product tests collinearity, and projection confines the vertex to the segment. Such a shared vertex blocks even without interior intersection.

Shooter and target cells retain their endpoint exclusion. Basic ranged validation, attack preview and the HUD LoS status already call `LineOfSight.IsClear`; no Presentation runtime patch is needed.

**Unit Cover remains unchanged:** inclusive supercover, directional target-proximity/size classification, strongest non-stacking level, Light `−15 pp`, no invented wall-cover number. Unit corner touches may still provide Cover. No projectile collision or hit transfer on a miss.

**Movement/pathfinding remain unchanged.** Their stricter corner/occupancy checks are intentional. Facing/Guard, Accuracy/Dodge, damage, RNG, OA resources, Retreat and unit profiles are unchanged.

Boundary test nuance: current gameplay shoots between integer cell centers, so a nonzero horizontal/vertical center-to-center segment cannot lie along a half-integer cell boundary. The internal clipping helper used by actual LoS is tested with doubled-coordinate boundary endpoints; this does not introduce free aiming or a new public ballistic API.

## Changed files in this follow-up

- `Assets/_Project/Core/LineOfSight.cs`: solid interior clipping and sealed-vertex check; inclusive supercover retained for Cover.
- `Assets/_Project/Tests/RangedCornerTests.cs` and `.meta`: exact user case, finite/open-cell clipping, boundary travel, reversed rays, sealed vertices in all diagonal orientations, true-interior negatives and retained unit corner Cover.
- `Assets/_Project/Tests/LineOfSightTests.cs`: explicitly revise obsolete corner-only solid blocking expectations; retain horizontal/vertical interior blocking and other invariants.
- `Assets/_Project/Tests/MeleeCornerContactTests.cs`: separation test now expects ranged single-wall grazing to be clear, with movement still blocked; all melee/ZoC/OA matrix cases retained.
- `Assets/_Project/Tests/PlayMode/SizeExperimentPresentationTests.cs`: ranged UI preview/confirmation regression for exposed corner, true interior and sealed vertex.
- `Docs/Prototype/GATE_C_TACTICAL_PROTOTYPE_CONTRACT_v0_1.md`: current geometry paragraphs and explicit latest local clarification appendix.
- `Docs/Prototype/RANGED_CORNER_LOS_CORRECTION.md`: this report.

Prior fixture/melee files and full initial change list remain in `FIELD_V2_CORNER_CONTACT.md`. Local documentation records explicit user instructions; no Drive pack mutation is claimed.

## Field_19x13_ExpandedV2 (retained)

19×13, `x=0..18`, `y=0..12`; solids **(9,5), (9,6), (9,7)**. West retreat `x=0`, East `x=18`.

| Unit | West | East |
|---|---|---|
| HW-Commander | (2,6) | (16,6) |
| HW-Infantry | (2,5) | (16,5) |
| HA-Left | (1,4) | (17,4) |
| HA-Right | (1,8) | (17,8) |
| EW-Flanker | (2,7) | (16,7) |

West faces East; East faces West. Same 5v5, IDs/profiles and seed 20260921. All four previous comparison fixtures remain. No movement/range compensation, final size selection or new siege systems. The playtest findings that 13×9/23×17 are too small and 17×11/27×21 likely still small are preserved; larger siege experiments remain deferred.

## Validation

- **205 EditMode + 23 PlayMode = 228 passed; 0 failed; 0 skipped**, pinned Unity in an isolated project copy. Relative to `06e5fa1`: 17 new focused EditMode cases and one new PlayMode test. Previous tests retained; obsolete single-wall-touch assertions explicitly updated to the latest contract.
- Existing five-fixture load/reset/deployment/movement tests and melee/ZoC/OA regression matrix pass. Core remains `noEngineReferences: true`; no Unity dependency or package/editor change.
- Real desktop mouse validation used prepared initial states, then actual board selection and confirmation through the HUD. Exact user shot with only `(9,7)` solid: preview clear, contact 85%, confirmed hit removes Armor `6 → 0` and HP `32 → 28`.
- Same ray with `(10,7)` solid: `BlockedLineOfSight`, confirmation disabled.
- Same ray with `(9,7)` and `(10,8)` solid: `BlockedLineOfSight`, confirmation disabled (pure shared-vertex gap sealed).
- Melee one-wall corner: preview legal and attack confirmed. Double-wall corner: `BlockedCorner`. Leaving one-wall diagonal contact: one OA warning and actual OA; sealed corner: no OA warning/event.
- V2 selected through dropdown; all ten units/camera framing readable. Six-step compact path to `(8,8)` executed; retreat to `(0,7)` produced Safe with HP 32 / Armor 6 and battle continued. Prepared V2 unit-screening case still showed Light Cover `−15 pp`.
- Complete diff inspected; no generated Unity folders staged. Existing temporary Editor search-index/account-service messages remain unrelated to Core/runtime behavior. No new runtime issue found; no balance or final map-size conclusion drawn.

## Launch

Stop existing Play Mode after script refresh → **Gate C → Play Tactical Graybox → Fixture → Field_19x13_ExpandedV2** (fifth entry). Existing fit/focus/zoom/right-click centering remain unchanged.
