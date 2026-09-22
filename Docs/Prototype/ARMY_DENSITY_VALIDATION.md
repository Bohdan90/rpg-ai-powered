# Gate C — single-army density fixtures

P — PROTOTYPE EXPERIMENT. User-selected provisional Gate C baselines: ordinary field
23×17, full siege 35×27. Neither is final universal canon. No further size iteration
without new playtest evidence. Source: Drive checkpoint 43 post-checkpoint update and
current task. Prior implementation baseline: `18088fe`.

## Scope and deployment

Added `Field_23x17_Full_9v9` and `Siege_35x27_Full_9v9`. Each side has one HW Commander,
three HW Infantry, three HA and two EW. Synthetic tactical density roster: no strategic
Capacity legality claim, roster builder, new profile, combat tuning or rule changes.

IDs 1/6 remain Commanders; original IDs/names 1–10 retained. Additional West IDs
11–14 and East IDs 15–18 identify Infantry 2/3, HA-Center and EW-Flanker 2.
Deployment is sorted by stable ID, matching BattleState ordering. Seed: 20260921.

| Role | West ID | East ID | Field West | Field East | Siege West | Siege East |
|---|---:|---:|---|---|---|---|
| HW Commander | 1 | 6 | (2,8) | (20,8) | (2,13) | (18,13) |
| HW Infantry | 2 | 7 | (3,6) | (19,6) | (3,11) | (17,11) |
| HA Left | 3 | 8 | (1,5) | (21,5) | (1,10) | (19,10) |
| HA Right | 4 | 9 | (1,11) | (21,11) | (1,16) | (19,16) |
| EW Flanker | 5 | 10 | (3,12) | (19,12) | (3,17) | (15,16) |
| HW Infantry 2 | 11 | 15 | (3,8) | (19,8) | (3,13) | (17,13) |
| HW Infantry 3 | 12 | 16 | (3,10) | (19,10) | (3,15) | (17,15) |
| HA Center | 13 | 17 | (1,8) | (21,8) | (1,13) | (19,13) |
| EW Flanker 2 | 14 | 18 | (3,4) | (19,4) | (3,9) | (15,10) |

West formation spans three columns and nine rows, ranged units behind the infantry,
EW on both wings. Field East mirrors it. Siege East uses several interior columns;
attackers retain their rear deployment and are not moved closer to the fortress.

## Geometry and implementation

There was no existing Field_23x17_Full fixture. The new field uses the existing
three-cell central-wall topology: (11,7), (11,8), (11,9); West retreat x=0, East x=22.

Siege reuses the exact `Siege_35x27_Large` Board construction. Center (17,13), fortress
11×9 envelope x=12..22/y=9..17, 24 solid cells. One-cell static solid/opaque moat proxy
extends to x=11..23/y=8..18, 32 additional solids. Four three-cell openings/crossings
remain aligned at centerline offsets −1..1. Defender East may retreat at every legal
outer-perimeter cell; West only x=0. Openings/crossings are routes, not escape endpoints.

Core changes are fixture data only in SizeExperimentFixture. Existing selector uses
the appended enum choices; Presentation adds names and a synthetic-roster explanation.
Existing view synchronization hides surplus tokens when switching back from 18 to 10.
No pathfinding, combat, camera or retreat implementation changes.

## Validation

Eight focused EditMode cases cover composition/uniqueness, valid and deterministic
spawns, geometry equivalence, executable deterministic initial movement for every
figure, both field rear exits and a real three-activation defender escape through the
north crossing with all 18 units present. New PlayMode case checks names, Commander
markers, reset and 18→10 view cleanup. Existing all-fixture loading test now includes
both new fixtures; original profiles remain checked by their unchanged IDs.

Final full suites on Unity 6000.6.2f1: **248 EditMode + 27 PlayMode = 275 passed,
0 failed, 0 skipped**. Focused density suite: 8/8. Initial full PlayMode run caught
fixture-array order differing from BattleState's ID order; sorted deployment by ID,
passed focused selector regression, then reran both full suites successfully.
Core noEngineReferences remains true; no UnityEngine/Presentation imports added.

Physical macOS mouse checks ran in an isolated Unity copy of the same sources.
Both actual initial 18-unit deployments were inspected and moved by mouse; focus/fit
controls worked. Prepared contact scenarios retained all 18 profiles/units and the
original board; only involved positions/active actor were set by a temporary external
probe, then previews and commands were selected/confirmed with real mouse events.
This is integration validation, not a complete organic battle or timing benchmark.

| Fixture | Mouse checks and observations |
|---|---|
| Field 23×17 | Initial deployment and flank move; prepared melee contact/exit showed one OA and resolved before surviving movement; ranged Light Cover preview/shot; actual initial HA-Left rear-edge escape left 17 visuals and battle ongoing; focus/fit usable. |
| Siege 35×27 | Nine defenders visible inside with several open interior lanes; initial attacker advance; prepared western crossing and exterior northward flank route; contact/OA and Cover shot; prepared defender at north edge escaped with 17 visuals and battle ongoing; focus/fit usable. |

The full interior→crossing→outer-perimeter retreat with the untouched 18-unit roster
was additionally executed by the focused Core test. Manual edge-escape check covered
Presentation removal/continued battle rather than repeating all intervening activations.

No obviously immobile initial unit or forced single-file start was found. The 18-line
queue remains readable but pushes preview/actions below the fold; HUD scrolling is
required. On 35×27, whole-board tokens are small; existing focus/zoom is useful.
Crossings remain local bottlenecks by existing geometry; no new congestion rule or
geometry adjustment was introduced. Rear-deployed field archers can retreat in one
step, consistent with the existing rear-edge deployment convention. Whole-battle
congestion, ranged dominance and activation duration remain for the user's playtest.

An isolated-editor Unity SearchDatabase indexing exception occurred during startup;
Play Mode and mouse checks continued. It was an editor indexing stack, not a Core or
Presentation exception. No project/package changes were made to address it.

Local test XML/logs: `/private/tmp/gate-c-density-{focused,edit,play}.*`.
Local screenshots: `/private/tmp/gate-c-density-7.png` and `gate-c-density-8.png`.
These generated artifacts and the temporary mouse/probe tools are not committed.

Launch: **Gate C → Play Tactical Graybox → Fixture → Field_23x17_Full_9v9** or
**Siege_35x27_Full_9v9**. Existing smaller/5v5 comparisons remain selectable.
