# Gate C — Siege scale V2 (Part B)

Baseline: **5aff414**, accepted Part A Archer engagement fallback. Part B changes only
fixture geometry and debug description; no combat retuning or rule changes.
Read Drive 18 deployment/core siege structure/outer-route progression/retreat sections
and 19 siege deployment/physical retreat/full defender perimeter. No canon contradiction:
this is a neutral static spatial proxy, not a racial fortress or fortification-level system.

## Exact geometry — P/T, not final size or canon

| Fixture | Dimensions | Center | Fortress envelope | Outer moat envelope |
|---|---|---|---|---|
| Siege_31x25_Medium | 31×25 | (15,12) | x10..20, y8..16 | x9..21, y7..17 |
| Siege_35x27_Large | 35×27 | (17,13) | x12..22, y9..17 | x11..23, y8..18 |

Using center-relative (dx,dy), unchanged fortress solids satisfy:
`abs(dx)==5 && abs(dy)>1`, or `abs(dy)==4 && abs(dx)>1`, inside [-5..5]×[-4..4].
This is the same **11×9 envelope / 24 solid cells** as both earlier siege fixtures.
New moat ring solids satisfy:
`abs(dx)==6 && abs(dy)>1`, or `abs(dy)==5 && abs(dx)>1`, inside [-6..6]×[-5..5].
This adds **32 solid cells**, so total solids = **56** in each new fixture.
Ring thickness is one cell; outer envelope 13×11. No extra terrain mechanics.

Fixed crossings are three cells wide on all four approaches, aligned with existing
fortress openings: west/east x=cx±6, y=cy−1..cy+1; south/north y=cy±5,
x=cx−1..cx+1. The next inner row/column is the existing matching fortress opening.
Crossing cells remain ordinary passable cells. Neither crossing nor fortress opening
is an escape endpoint.

**P — geometry representation limitation:** the proxy reuses ordinary solid/opaque
cells and existing gray blocks; it blocks both movement and LoS. No real moat/water
visibility rule is canonized. No damage, status, special cost, bridge state, destructibility,
structure HP, gates, ladders, equipment or racial layout is implemented.
The four approaches and their width are this experiment's topology, not a new universal
bridge-count rule; 18's actual level/racial progression remains separate.

## Deterministic deployment

Same seed 20260921, profiles and 5v5 order in both; West attacks from west, East inside.

| Unit | Medium West | Medium East | Large West | Large East |
|---|---|---|---|---|
| HW-Commander | (2,12) | (16,12) | (2,13) | (18,13) |
| HW-Infantry | (2,11) | (16,11) | (2,12) | (18,12) |
| HA-Left | (1,10) | (17,10) | (1,11) | (19,11) |
| HA-Right | (1,14) | (17,14) | (1,15) | (19,15) |
| EW-Flanker | (2,13) | (16,13) | (2,14) | (18,14) |

West faces East; East faces West. Larger map does not move West closer to fortress.
The outer ring leaves 9 vs 11 columns on each lateral exterior and 7 vs 8 rows above/below.
West retreat remains x=0; East may physically escape through every legal outer edge.
Strategic sectors do not disable defender edges. Previous five fixtures remain unchanged.

## Files / validation

- Core/SizeExperimentFixture.cs: two appended enum entries, dimensions and fixed ring.
- Presentation/BattleHud.cs: existing selector picks up enum entries; clarify proxy text.
- Tests/SizeExperimentTests.cs: two new dimensions/spawn cases and two full-edge retreat cases.
- Tests/SiegeScaleV2Tests.cs + .meta: nine focused geometry/formation/path/escape/LoS/OA cases.
- Tests/PlayMode/SizeExperimentPresentationTests.cs: one new selector/framing/crossing test.
- Local contract, working checkpoint and this report.

26 fixture focused tests passed. Tests execute ordinary paths across multiple activations
without boosting Movement: initial attacker approach into fortress, interior defender exit
to outer perimeter, north/south exterior circuits, and attacker non-escape at defender edge.
Query paths repeat deterministically without changing state/RNG. All ordinary combat rules,
including Part A, are shared by both fixtures. Full suite: **240 EditMode + 26 PlayMode = 266 passed, 0 failed/skipped**.
The existing selector test now expects seven choices and exercises all seven. Its first
run exposed the stale five-choice expectation, which was corrected before focused
UI tests and final full suites. Results: /private/tmp/gate-c-siege-edit.xml and
gate-c-siege-play.xml. Manual results follow below.

No final size preference is inferred; the user will personally compare both fixtures.

## Mouse Play Mode results — both fixtures passed

Real OS mouse selection/confirmation in an isolated Unity copy; temporary scenario setup
and mouse scripts are outside Git. Initial approaches use the full unchanged 5v5 fixture;
localized geometry/combat checks use small deterministic arrangements on the same boards.

For BOTH 31×25 and 35×27:
- selected new fixture from dropdown; ten units and correct board framing;
- moved initial West EW toward fortress; moved through west moat crossing and wall opening;
- traversed west/east exterior and north/south flanks with explicit Core paths;
- fired Bow through crossing, previewed Light Cover through crossing, rejected solid-ring shot;
- exit from engagement at crossing warned of one OA; OA hit resolved before surviving mover stepped;
- defender walked from interior via north crossing across three ordinary activations to outer edge;
  escaped at (15,24) / (17,26), preserving HP 32 and Armor 6, producing Withdrawal in the single-defender check;
- attacker entering the same north edge remained Active;
- Fit whole board and Focus active unit controls operated on both sizes.

Screenshots inspected: /private/tmp/gate-c-siege-5.png, gate-c-siege-6.png and gate-c-siege-focus.png.
The first mouse attempt stopped because the temporary window was on a different monitor;
the harness was corrected to position/raise only that window and refuse clicks outside it.
Both full matrices then completed successfully. No repository code changes were needed.

## Limitations / observations

No impassable-access or spawn issue was exposed. Exterior routes exist on both flanks;
that establishes playability, not superior balance or final size. A real moat's visual and
LoS behavior remains outside this proxy: the ring currently looks like ordinary gray solids.
At whole-board scale small unit labels are reduced; focus/zoom provides inspection.
Existing hovered-cell text can persist across fixture resets until the next cell hover;
selected command previews and Core state refresh correctly. Existing Unity AI account
NoSubscription messages are unrelated to gameplay/tests. No new runtime/compiler failures.
