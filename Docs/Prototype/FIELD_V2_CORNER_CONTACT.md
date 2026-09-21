# Gate C — Field 19×13 V2 and exposed melee corner correction

2026-09-21. Baseline `8af4f87`; Unity `6000.6.2f1`. Explicit user playtest correction. No push, combat retuning, new siege mechanics or final battlefield-size decision.

## Root cause and reproduction

`LineOfSight.IsMeleeCornerClear` required both orthogonal side cells of a diagonal contact to be non-solid (`&&`). Basic attack validation/preview called it directly; ZoC called it and OA derived both preview and execution adjacency from ZoC. This was an overly strict melee contract, not a Presentation-only defect and not an accidental call to ranged supercover.

Before changing Core, the new regression suite ran against the old resolver: **17 tests, 8 passed, 9 failed**. All eight one-wall cases (four diagonal orientations × either side blocked) failed at attack preview. The ninth failure demonstrated the intended difference between melee contact and movement/ranged geometry. Open and double-wall cases already passed.

## Corrected contract

For diagonal-adjacent source/target cells, melee contact is allowed if **at least one** of the two orthogonal side cells is not solid (`||`). Two solid cells seal the corner and prohibit contact. Existing bounds/adjacency checks remain.

The shared helper serves:
- Basic melee validation and attack preview;
- Presentation's existing melee-corner status, through the same Core API;
- ZoC cell threat/source queries;
- OA path preview, exit detection, availability/legality rechecks during resolution.

Guard, facing, damage, contact/Guard RNG and OA ordering are unchanged. Occupying units in side cells retain their existing melee behavior: they are not solid walls and do not add melee screening.

**Intentionally different geometry:** movement still requires both side cells free of solid obstacles and other occupying units; pathfinding cannot cut a one-wall corner. Ranged center-to-center supercover still blocks a shot touching either solid side cell. Walls were not weakened globally. The new contact contract applies to all fixtures consistently.

The local contract appendix records the user's explicit rule update. No claim is made that this task modified the authoritative Drive pack.

## Field_19x13_ExpandedV2

Width 19 (`x=0..18`), height 13 (`y=0..12`). Central solid wall: **(9,5), (9,6), (9,7)**. Everything else is ordinary walkable ground.

| Unit | West position | East position |
|---|---|---|
| HW-Commander | (2,6) | (16,6) |
| HW-Infantry | (2,5) | (16,5) |
| HA-Left | (1,4) | (17,4) |
| HA-Right | (1,8) | (17,8) |
| EW-Flanker | (2,7) | (16,7) |

West starts facing East; East faces West. Same IDs, five profiles per side and seed **20260921**. Retreat: West `x=0`, East `x=18`, full height. No deployment-distance compensation. Movement, range, distance penalty, Cover, initiative, action economy and compact deterministic path policy are unchanged.

The original four enum IDs and fixtures remain unchanged. V2 is appended as the fifth selector entry. An explicit `IsSiege` predicate replaces enum ordering comparisons, so appending a field fixture cannot acquire fortress geometry or siege retreat semantics.

Playtest findings retained: 13×9 too small; 17×11 improved but still somewhat small; 23×17 too small; 27×21 likely a lower bound once future moat/bridges/exterior terrain occupy space. 31×25 / 35×27 remain a separate future experiment. None is declared final.

## Changed files

- `Assets/_Project/Core/LineOfSight.cs`: shared melee helper and explicit distinction from movement/ranged LoS.
- `Assets/_Project/Core/SizeExperimentFixture.cs`: fifth fixture, dimensions, explicit siege classification.
- `Assets/_Project/Tests/MeleeCornerContactTests.cs` and `.meta`: 16 directional/wall combinations plus movement/ranged separation regression.
- `Assets/_Project/Tests/LineOfSightTests.cs`: explicitly update obsolete single-wall melee expectation; retain double-wall rejection and ranged blocking assertions.
- `Assets/_Project/Tests/OpportunityAttackTests.cs`: explicitly update obsolete single-wall ZoC expectation; retain solid cell/sealed corner rejection.
- `Assets/_Project/Tests/SizeExperimentTests.cs`: V2 dimension/deployment/path case and exact geometry/retreat case.
- `Assets/_Project/Tests/PlayMode/SizeExperimentPresentationTests.cs`: selector/reset checks now include all five fixtures; new corner attack/ZoC/OA UI integration test.
- `Docs/Prototype/GATE_C_TACTICAL_PROTOTYPE_CONTRACT_v0_1.md`: local explicit user-approved correction appendix.
- `Docs/Prototype/FIELD_V2_CORNER_CONTACT.md`: this report.

No Presentation runtime change was necessary: the selector enumerates fixtures and the HUD/view/controller already use Core geometry and instance dimensions. No package, scene, Unity version or unit-profile changes.

## Validation

Automated: **188 EditMode + 22 PlayMode = 210 passed, 0 failed, 0 skipped** in an isolated copy of the project with Unity 6000.6.2f1. Additions: 17 focused corner cases, two V2 fixture cases and one PlayMode integration test. Two previous tests explicitly changed because they asserted the superseded one-wall melee rule; their sealed-wall/solid-cell negatives remain and ranged assertions were added. All existing tests were retained.

Core's `noEngineReferences` boundary remains intact; no UnityEngine/UnityEditor/Presentation imports in Core. Complete diff and generated-file exclusion checked. No package/editor changes.

Real mouse validation in the isolated Unity Editor (fixture setup only was scripted; target/path selection and confirmation used desktop mouse events):

- User wall shape reproduced at translated coordinates: mover `(4,4)`, enemy `(5,5)`, vertical wall `(5,4),(5,3),(5,2)`. Melee preview legal, contact 80%, Basic confirmed; enemy Armor `16 → 5`.
- Add solid `(4,5)` to seal the corner: preview `BlockedCorner`, no attack offered.
- Open corner, exit `(4,4) → (3,4)`: preview names one HW responder; OA resolves before StepMoved, mover Armor `6 → 0`, HP `32 → 26`, then reaches `(3,4)`.
- Same exit with sealed corner: zero OA risks, no OA event, HP/Armor unchanged; movement still executes normally.
- Fifth dropdown entry selected with the mouse: correct 19×13 board and ten initial units visible, camera/labels/retreat stripes fit the board.
- V2 path `(2,7) → (8,8)` previews the exact compact six-step route and executes successfully.
- V2 own-edge path `(2,7) → (0,7)` previews Escape/Safe, removes the view and preserves HP 32 / Armor 6; the battle continues.
- Prepared ranged situation on V2: Light Cover `−15 pp`, contact 75%, preview and confirmed attack work as before.

No new runtime problem found. The temporary Editor repeats the already recorded Unity Search indexing/account-service messages; the project suites pass. Existing PlayMode/domain-reload limitations remain unchanged. Larger siege fixtures/mechanics and final map-size selection remain deferred.

## Launch

Stop any current Play Mode after script refresh, then **Gate C → Play Tactical Graybox → Fixture (resets battle) → Field_19x13_ExpandedV2** (fifth entry). Fit whole board, wheel zoom and right-click centering remain available. Existing comparison fixtures are retained.
