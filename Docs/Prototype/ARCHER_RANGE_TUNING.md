# Gate C — Human Archer bow envelope tuning (2026-09-21)

Baseline: local commit 87b51d5. Unity 6000.6.2f1. No package/editor changes.

## Prototype values

**T — PROTOTYPE TUNING**, not final global canon.
Human Archer TI: HP 28, Armor 4, Movement 4, Initiative 12, Accuracy 80%,
Dodge 5 pp, Guard 0%, Physical Basic Damage 10, Bow Range **10** (previously 6).
Steady Aim remains +15 pp Accuracy before any Movement is spent and still consumes
remaining Movement on the shot. It no longer adds +1 Range. Moving does not reduce
the maximum Bow Range: both stationary and moved shots have a cap of 10.

Distance penalty remains `5 × max(0, distance − 4)` pp.

| Distance | Penalty | Accuracy before Dodge/Cover/etc. | With Steady Aim |
|---:|---:|---:|---:|
| 1–4 | 0 | 80 | 95 |
| 5 | −5 | 75 | 90 |
| 6 | −10 | 70 | 85 |
| 7 | −15 | 65 | 80 |
| 8 | −20 | 60 | 75 |
| 9 | −25 | 55 | 70 |
| 10 | −30 | 50 | 65 |

The working distance ~7 / 1.75× baseline Movement and extreme distance 10 / 2.5×
are experimental design reference points, not new global rules.
Contact still includes target Dodge/Frontal Evasion/Cover and existing clamp.
No minimum range, adjacent-shot penalty, sidearm or disengage mechanic was added.

## Exact changed files

- `Assets/_Project/Core/UnitProfile.cs`: Archer Range 10.
- `Assets/_Project/Core/BattleResolver.cs`: remove Steady Aim range bonus from validation and preview.
- `Assets/_Project/Presentation/BattlePresenter.cs`: explicit Steady Aim Accuracy-only text.
- `Assets/_Project/Tests/BattleResolverTests.cs`: update superseded profile/range expectations.
- `Assets/_Project/Tests/LineOfSightTests.cs`: preserve boundary checks at new range.
- `Assets/_Project/Tests/ArcherRangeTuningTests.cs` and `.meta`: nine new EditMode cases.
- `Assets/_Project/Tests/PlayMode/SizeExperimentPresentationTests.cs`: one new HUD integration test.
- `Docs/Prototype/GATE_C_TACTICAL_PROTOTYPE_CONTRACT_v0_1.md`: current tuning and local change record.
- `Docs/Prototype/MILESTONE_1_IMPLEMENTATION.md`: historical-value supersession notice.
- `Docs/Prototype/MILESTONE_2A_IMPLEMENTATION.md`: historical-value supersession notice.
- `Docs/Prototype/ARCHER_RANGE_TUNING.md`: this report.

No changes to army composition, maps/deployment, movement, pathfinding, Initiative,
damage, pools, Dodge/Guard, Cover, facing, Frontal Evasion, ZoC/OA, Retreat or RNG.
Core remains pure C# with `noEngineReferences: true`; Presentation consumes Core preview.

## Automated validation

**214 EditMode + 24 PlayMode = 238 passed; 0 failed; 0 skipped.**
Existing tests preserved; expectations specifically superseded by this tuning updated.
New tests cover 4/5/7/8/10 distance penalties before/after real movement, cap 10,
invalid range 11 state/RNG purity, preview/contact-roll agreement, Steady Aim movement
commit, additive Cover, and EW inability to reach melee from distance 10 in six steps.
HUD test covers 10 vs 11 and stationary vs moved preview/command submission.
An initial new test inadvertently moved along its own Retreat edge; corrected its
fixture to start away from that edge, then reran the full suite successfully.

Results: `/private/tmp/gate-c-bow-edit.xml`, `/private/tmp/gate-c-bow-play.xml`.

## Playtest launch

Gate C → Play Tactical Graybox → Fixture → Field_19x13_ExpandedV2.
Restart Play Mode after compilation if an existing session is still running.
The board remains 19×13, with central solids (9,5), (9,6), (9,7), unchanged deployment.
No final balance or final battlefield-size conclusion is made.

## Mouse-driven Play Mode validation

Completed in an isolated Unity copy using physical OS mouse clicks on the real HUD,
with temporary fixture setup outside Git. All shots/moves submitted through normal
selection and confirmation; scenario setup only supplied deterministic positions.

- Field_19x13_ExpandedV2 selected from dropdown; original ten-unit formation and framing intact.
- Distances 7/8/10: legal, penalties −15/−20/−30; shots confirmed and applied.
- Distance 11: OutOfRange, no shot offered.
- Distance 10 stationary vs after an actual orthogonal move: Aim +15 vs +0, Range stays 10,
  contact against HW 60% vs 45%; both submitted successfully.
- Distance 10 with target-side ordinary body: Light Cover −15, distance −30, Aim +15,
  Dodge −5, final contact 45%; shot remains legal and was submitted.
- Existing (12,5)→(9,8) exposed-wall touch remains clear; interior wall and sealed double-wall vertex remain blocked.
- EW moved six cells from distance 10 to distance 4; attempted melee target selection returns OutOfRange.
- Screenshots inspected: `/private/tmp/gate-c-bow-cover.png`, `/private/tmp/gate-c-bow-v2.png`.

No unexpected combat interactions were found. Existing minor debug-label limitation:
for a nonadjacent melee target the HUD also says “LoS / corner: blocked” because its
melee-contact query includes adjacency; the authoritative rejection is correctly
OutOfRange. This pre-existing wording was not expanded into unrelated UI work.
At full-board framing the long fixture name is clipped in the selector and the
preview uses scrolling, as before; zoom/focus controls remain available.
The temporary Editor also reports an unrelated Unity AI account NoSubscription
message; runtime/test validation does not depend on that service.
