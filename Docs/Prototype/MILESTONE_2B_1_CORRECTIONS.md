# Gate C Milestone 2B.1 — playtest corrections

2026-09-21. Unity remains 6000.6.2f1. No packages, scenes, fixtures, Movement legality or
Milestone 3 systems changed. Local contract §16 records the user-requested correction;
no Drive thematic owners were modified.

## Default path policy

BFS computes minimum Movement cost. Dynamic programming over shortest-path edges keeps the
best prefix per cell and incoming direction, ranked by:

1. Fewest direction changes (initial facing is not a turn).
2. Smallest sum of absolute integer cross products of path cells against start→destination.
   This is summed perpendicular distance with a query-constant normalization omitted.
3. Ordinal lexicographic direction sequence: N, NE, E, SE, S, SW, W, NW.

Minimum cost dominates all three criteria. No random smoothing, new legal paths or altered
corner/occupancy behavior. Presentation still previews the exact Core path before confirmation.
This supersedes the previous follow-up preference for deviation before turns.

Observed limitation during this pass: the user supplied a BaseMap screenshot for (2,5)→(8,5).
The requested turns-first ordering selects NE×3, SE×3 through (5,8), because it has one turn.
The compact six-step bypass through row 6 has two turns and therefore loses. This is a policy
tradeoff, not a cost/corner bug. Returning deviation before turns would select a compact bypass;
that preference was proposed to the user, but is not silently substituted for the explicit 2B.1
ordering. The existing open-space horizontal route has zero turns and remains straight.

## Cover implementation and boundaries

`Cover.cs` introduces None/Light/Strong, an ordered-size classifier, strongest-level aggregation,
and a pure battlefield query. `UnitProfile.CoverSize` is 1 for every current profile; larger or
smaller fixture units are not introduced. Synthetic size comparisons are tested through Classify.
The same integer supercover traversal feeds both solid LoS checks and Cover queries; occupied
cells no longer block ranged LoS. Dead/Escaped bodies and both endpoints are excluded.

**P — PROTOTYPE ASSUMPTION:** compare the screener center's projection along the shot:
`2 * dot(screen-source, target-source) >= |target-source|²`. Equality grants target Cover.
This chooses Euclidean/projection proximity rather than Chebyshev Range proximity. Corner-touch
cells use the same rule. Team is irrelevant. A qualifying equal/smaller body gives Light; a
larger body classifies Strong. Multiple bodies use the strongest level without stacking.

**T — PROTOTYPE TUNING:** Light modifies Accuracy by −15 percentage points before the normal
5–95 clamp. Strong has no invented protection: its numeric modifier query returns null. Current
profiles cannot produce Strong in a battle. Future larger-unit integration must supply an explicit
rule before combat execution; the resolver has a defensive failure rather than treating null as 0.
This does not add multi-cell footprints, projectile collisions, hit redirection or new Friendly Fire.

AttackPreview exposes base Accuracy, Aim, distance modifier, target Dodge, applied Frontal Evasion,
Cover level/modifier and final contact chance. BattlePresenter displays those values directly from
Core, including `Light Cover: -15 pp Accuracy`. Guard and damage remain separate and unchanged.

## Files

- Added Core/Cover.cs and Tests/CoverTests.cs, with Unity meta files.
- Updated Core/Pathfinder.cs, LineOfSight.cs, UnitProfile.cs, AttackPreview.cs, BattleResolver.cs.
- Updated Presentation/BattlePresenter.cs, Tests/PathfinderTests.cs, LineOfSightTests.cs and
  Tests/PlayMode/GrayboxPlayModeTests.cs.
- Updated the local prototype contract and added historical supersession notices to M2A/M2B reports.

## Validation

117 EditMode + 11 Presentation PlayMode = **128 passed, 0 failed, 0 skipped**.
All previous test cases retained; assertions explicitly contradicted by the new rules were updated
(unit blocking and old route priority). Thirteen new Cover cases exercise proximity, midpoint,
sizes, strongest classification, no stacking, exact tuning, reverse direction, inactive units,
corner supercover, off-line bodies and state/RNG-pure deterministic queries.
Existing tests retain solid walls, corner rules, range, Guard, movement and resolver determinism.
No C# compiler warnings/errors in the test runs; Core has no UnityEngine/UnityEditor dependency.
Tests used an isolated copy at /private/tmp/gate-c-m1-4dkkwcy6.

Mouse-driven Play Mode validation used macOS CGEvent clicks to select cells and press Confirm.
A temporary, uncommitted Editor probe prepared small fixtures through ConfigureBattle and read
state; it did not submit attacks or moves. This isolates each geometry without adding production
scenarios. Fixtures use seed 1, shooter (2,4), target (8,4), unless specified.

| Scenario | Observed result |
|---|---|
| Allied body (3,4), near shooter | Clear LoS, None Cover, contact 80%; confirmed attack, roll 95 misses. |
| Body (7,4), near target | Clear LoS, Light Cover −15 pp, contact 65%; confirmed attack, roll 95 misses; screener unchanged. |
| Clear shot | None Cover, contact 80%; confirmed attack. |
| BaseMap central wall | BlockedLineOfSight, no contact preview; Action remains available. |
| Reversed endpoints, body stays (7,4) | None Cover, contact 80%; confirmed attack. |
| Old open-space zigzag case (2,2)→(4,2) | Exact preview (3,2)→(4,2), cost 2, East facing; mouse confirmation produces both StepMoved events and final (4,2). |

Artifacts: /private/tmp/gate-c-light-cover.png and gate-c-{near-shooter,near-target,clear,wall,
reverse,path}-{preview,result}.txt (wall has preview only). These, the desktop helper and Editor
probe are not committed. Synthetic fixture labels inherit PrototypeFixture's fixed ID names;
profile/side state and token colors were used for checking, not these scenario-only labels.

The user screenshot revealed the turns-first wall-detour tradeoff above; this remains a known
readability limitation, not reported as resolved. Strong Cover gameplay and larger sizes remain
deferred. No ZoC/OA, Retreat, AI, new units, spells or content were implemented.
