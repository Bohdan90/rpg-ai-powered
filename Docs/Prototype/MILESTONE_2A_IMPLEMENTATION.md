# Gate C — Milestone 2A: Core Grid, Movement, Pathfinding and LoS

> Historical milestone report. Milestone 2B.1 supersedes unit-blocked ranged LoS and the older
> path tie-break. See [MILESTONE_2B_1_CORRECTIONS.md](MILESTONE_2B_1_CORRECTIONS.md) for current behavior.

Date: 2026-09-21. Baseline: `cd15539`. Pinned Unity: **6000.6.2f1**.
Authority: [contract v0.1](GATE_C_TACTICAL_PROTOTYPE_CONTRACT_v0_1.md), sections 5–7.
The contract, package files, editor version, combat tuning and RNG algorithm are unchanged.

## Scope and files

Pure `RPG.Core` C# only. No presentation or Milestone 3 systems.

Added:

- `Assets/_Project/Core/Battlefield.cs`: immutable 13x9 solid/walkable geometry;
  `ControlMap` is empty, `BaseMap` has solids (6,3), (6,4), (6,5).
- `Assets/_Project/Core/MovementRules.cs`: shared single-step and submitted-path checks.
- `Assets/_Project/Core/MoveCommand.cs`: copied, read-only explicit step list.
- `Assets/_Project/Core/Pathfinder.cs`: bounded deterministic BFS and `PathResult`.
- `Assets/_Project/Core/LineOfSight.cs`: integer supercover and adjacent melee corner checks.
- `Assets/_Project/Tests/MovementTests.cs`, `PathfinderTests.cs`, `LineOfSightTests.cs`.
- Matching `.meta` files for all eight new C# files, plus this report.

Changed:

- `BattleState.cs`: immutable battlefield reference, active-unit occupancy query,
  validation of initial active placements against bounds and solids.
- `UnitState.cs`: position has an internal setter for resolver execution.
- `BattleResolver.cs`: optional battlefield at start, movement validation/execution,
  ranged LoS and melee solid-corner validation. Existing combat math is unchanged.
- `BattleResult.cs`: explicit movement and LoS error reasons.
- `BattleEvent.cs`: appended `MovementStarted` and `StepMoved`, with optional From/To.
  Existing enum values remain stable; existing MovementConsumed/FacingChanged events reused.
- `BattleTestFixtures.cs`: snapshot checks now include battlefield solids.
- `README.md`, `ARCHITECTURE.md`: current milestone and validation references.

All original 57 test cases remain. No original test expectations were weakened or removed.

## Public flow

```csharp
var state = BattleResolver.StartBattle(units, seed, Battlefield.BaseMap).State;
var actor = state.CurrentUnitId.Value;
var path = Pathfinder.FindPath(state, actor, destination);
if (path.Found && path.Cost > 0)
{
    var result = BattleResolver.Apply(state, new MoveCommand(actor, path.Steps));
    state = result.State;
}
// PreviewAttack / BasicAttackCommand / EndActivationCommand use the updated positions.
```

The resolver never trusts the pathfinder or caller. It validates every consecutive step
against current state before cloning or executing anything. Failure returns the same state,
no events, no spent resources and unchanged RNG. Success returns a new snapshot.

Each step costs 1, moves the actor, increments historical movement spend, consumes one
Movement and faces along that step. A diagonal requires both side cells to be free of solid
obstacles and active units. Allied and enemy occupancy behave identically. Dead/Escaped
records do not occupy cells; occupancy is derived from current unit state, not cached.

Commands are atomic in M2A. A path with an invalid late step does not execute its valid
prefix. Movement may be split before Action. Basic Attack/Defend spent Action prevents
further movement; any movement history prevents Defend even after returning to origin.
End discards unused resources. M1's optional final-facing choice is already executable
through EndActivationCommand and remains unchanged.

## Deterministic pathfinding

FIFO breadth-first search is sufficient because all edges cost 1. It explores only the
117 battlefield cells and stops at remaining Movement. It returns a shortest path within
that budget or `Found = false`. No heuristics, external navigation library or RNG.

Equal-cost tie-break: enqueue neighbours clockwise **N, NE, E, SE, S, SW, W, NW**;
mark visited when enqueued and preserve the first predecessor. Arrays and FIFO iteration
make this independent of dictionary/hash iteration, seed and unit collection order.
`MovementRules.ValidateStep` is shared with resolver validation, including occupied corners.

## Deterministic LoS

Center-to-center supercover traverses vertical/horizontal grid boundaries using integer
cross-products of odd crossing numerators and absolute coordinate deltas. No floating-point
angles, physics or raycasts. At an equal crossing time it checks both orthogonal side cells
and the diagonal cell: even touching a blocked corner blocks the shot.

Intermediate solids and active units block ranged LoS. Source and target cells are excluded.
Out-of-bounds endpoints return false. Adjacent melee checks solid diagonal corners only;
it does not apply ranged unit-screening. This follows the milestone's explicit distinction.

Range remains Chebyshev: melee 1, Archer base 6, Steady Aim up to 7 when no Movement was
spent. Archer distance penalties and all contact/Guard/damage calculations remain M1 code.
Attack turns the actor toward the target, without rotating the target. Frontal Evasion and
Guard use current positions and facing.

## P — PROTOTYPE ASSUMPTION: executable API choices

- `StartBattle` without an explicit battlefield uses the contract's empty control map,
  preserving M1 fixture calls. A caller must select BaseMap for the obstacle scenario.
  All active starting units must be inside bounds, off solids, and in unique cells.
- Explicit paths **exclude the origin** and include every destination step, not sparse
  waypoints. Null/empty command paths and zero-length/jumping steps are invalid.
  Legal revisits are allowed and each traversal costs Movement; returning to origin
  does not erase movement history. No automatic rerouting occurs during execution.
- Path queries return executable movement for the current active unit, requiring an
  available Action and respecting its remaining Movement. They do not expose a future-turn
  or hypothetical-budget planner. A query to the origin returns Found=true with zero
  steps; callers should not submit that as a MoveCommand. Found=false uses an empty list.
- Fixed clockwise FIFO search order is the implementation tie-break for equally short paths.

These are local API/tie-break choices, not new gameplay canon. There is no change to the
specified movement, combat, range or LoS rules. The former M1 limitation of unbounded,
unscreened attacks is replaced by board/LoS legality. This report supersedes that part of
the historical M1 report; M1 acceptance remains 57 passing tests.

## Validation

Complete Unity EditMode suite on pinned **6000.6.2f1**:
**97 passed, 0 failed, 0 skipped** (57 existing + 40 new cases).

Coverage includes: orthogonal/diagonal costs; both solid/occupied diagonal side cells;
allied/enemy occupancy; inactive records; all-or-nothing invalid paths; Movement budgets;
split Move -> Move -> Attack -> End; Move -> End; Action -> Move rejection;
return-to-origin -> Defend rejection; position/facing updates; copied command data;
state/RNG query isolation; deterministic path and command replay; shortest base-map detour;
clear/blocked/reversed/shallow/steep/corner LoS; excluded endpoints; inactive screening;
melee versus ranged corner distinction; range 6/7 limits; actual-movement Steady Aim loss;
position-based Elf evasion; and compiled Core dependency isolation.

Tests ran on a temporary project copy to avoid a second process opening the user's editor
project. Results: `/private/tmp/gate-c-m1-4dkkwcy6/TestResults.xml`; editor log: `tests.log`
in the same directory. The directory name is retained from the earlier test harness.

No assembly definitions, packages, Unity version, contract, scenes, presentation code or
combat profile/RNG files changed. All additions and edits were reviewed before local commit.
No push requested or performed.

## Limits and recommended Milestone 2B

No deployment interaction, board rendering, unit GameObjects, camera, input, UI/highlights,
AI, animation, telemetry UI, ZoC/OA, movement interrupts or retreat/field-control execution.
The outer columns are ordinary cells for now; reaching them does not mark a unit Escaped.

Milestone 2B should be a thin Unity graybox presentation in RPG.Presentation: display the
13x9 board, obstacles and units, select the active unit and inspect an explicit path/attack
preview, submit commands to this resolver, and show structured results/resources/facing.
Include Move, Basic Attack, Defend and End controls and hotseat verification. Presentation
must never modify Core state or implement its own legality/combat logic. Keep OA, retreat
and AI deferred to their later milestones.

## Follow-up: direct-route preference (2026-09-21, after Milestone 2B)

The original FIFO tie-break above is superseded by this user-approved path selection rule:
minimize **step count**, then **summed perpendicular deviation from the start-to-destination line**,
then **direction changes**, then lexicographic clockwise direction order N, NE, E, SE, S, SW, W, NW.
Deviation is the sum of absolute integer cross products at path cells; the omitted line-length
normalizer is constant for a query. The initial facing does not count as a path turn.

BFS first computes minimum distances within the Movement budget. A second pass over shortest-path
edges retains the best route per cell and incoming direction, so turn minimization remains valid.
All comparisons are integer/ordinal; queries remain state/RNG independent. For example, (2,4) to
(4,4) now goes through (3,4), not (3,5). Legal shortest detours still work around blocked corners.

This is a local **P — PROTOTYPE ASSUMPTION** about choosing between equal-cost legal paths, not a
change to Movement cost, occupancy, range, LoS or combat rules. The chosen path and thus final movement
facing can differ from previous builds; explicit submitted paths still execute unchanged.
Presentation automatically uses the new Core path. No presentation-side routing was added.

Validation: 104 EditMode tests and 11 Presentation PlayMode tests passed (115 total, zero failed
or skipped), including seven added path-selection cases. No compiler warnings/errors were reported
by these runs. Core remains independent of UnityEngine; the contract and packages are unchanged.
