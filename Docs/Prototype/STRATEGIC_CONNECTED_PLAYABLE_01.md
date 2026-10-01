# WP-02 — Strategic Connected Playable 01

Started 2026-09-29 from `develop`, `79ac6257ceb76478d057ed8e95a5761cf5a32b38`.
WP-00 DONE / WP-01 TECHNICAL PASS accepted by coordinator. WP-01 player balance
acceptance remains pending. No combat profile retuning is part of this package.

## Authority and preflight

Read current Drive 44, all of 45, current WP-02 queue/closure delta in 46;
repo AGENTS, efficiency protocol, checkpoint and tactical boundaries. Task-local
owners: 05 supply; 17 Tempo, Attack/Withdrawal, target-centered participation;
19 retreat integration; 26 field recovery. Document 45 is the scenario spec.
Starting tracked tree clean; the same six pre-existing persistence `.meta` files
remain untracked and are preserved by SHA-256 comparison against WP-01 evidence.
Pinned Unity stays 6000.6.2f1. No broad WP-00/01 repetition.

## Implemented scope

- `StrategicGraph.cs`: authored 18 nodes/20 edges, deterministic paths, costs
  and hop distances. No 14–09 cross-link; no player occupation of Portal 11.
- `StrategicScenario.cs`: Core-owned player activation, Tempo and attack cost,
  Refresh order, supply, Hungry mobility, site states, guard/Patrol/incursion
  intent and deterministic mission result. Invalid commands/preview are pure.
- `StrategicBattleBridge.cs`: persistent roster encounter snapshots, target-centered
  one-hop participants, actual tactical outcome, graph withdrawal/40-Tempo debt,
  actor removal/exit and continuation from the suspended world-phase cursor.
- `StrategicHud.cs` plus bounded presenter/HUD integration: selectable graph,
  path/cost/block reason, actor reports, objectives, supply, persistent roster,
  battle entry and explicit Return persistent result to World. Existing tactical
  field is reused; no scene load resets the Core session.
- `StrategicScenarioTests.cs`, `StrategicRouteTests.cs`, and
  `PlayMode/StrategicPresentationTests.cs` cover Core/route/UI integration.
- Existing `UnitState.cs` / `Battlefield.cs` / `BattleGridView.cs` support an
  unavailable per-army retreat edge; `PersistentFormation.cs` fixes XP snapshots;
  `BattlePresenter.cs` / `BattleHud.cs` integrate persistent battle UI. Seven new
  scripts have their own `.meta`; existing unrelated `.meta` files are untouched.

Every character retains its persistent ID, profile, Commander flag, HP/Armor,
Alive/Dead/Escaped-Safe status, XP/levels/rank and fractional recovery carry.
Dead slots remain empty. No replacement, Armor repair or tactical-exit healing.
End-of-Refresh applies one existing field +15% Max-HP recovery with fractional
carry; it is separate from battle resolution. Commanderless troops retain their
locked roster and can fight/return to Keep.

Enemy rosters also persist. Escaped incursion survivors withdraw toward exit 17;
their raid objective does not silently resume. Routed guards cease protecting
their target, keep their survivor records and physically depart. Normal actor
order is Area Guard → Patrol → A → B; routed Hard Guard departure is non-hostile
cleanup afterward, before supply. It cannot create another attack phase.

Owner-17 integration beyond a naive one-v-one bridge: adjacent field actors join
by a single Battle-Target snapshot; attacking participants pay their own cost.
Negative-Tempo defenders still fight but receive `RetreatEdge.Unavailable` until
Refresh. This is an additive enum value in the existing replay snapshot field;
no replay file format change. Ordinary Gate C retreat edges are unchanged.

Persistence correctness found while integrating: enemy XP weight previously read
live Personal Level after the first side had received XP. It now uses battle-start
Base Power, so a level gained during resolution cannot change the other side's
award. A regression test covers an escaped participant crossing that threshold.

## Provisional scenario values

Topology/costs and initial six-player roster match document 45. Tempo 100; Attack
cost up to50 at nonnegative Tempo; Withdrawal40; Portal interaction5 and ends
activation. Provisions36/36; consumption1 per living member; Keep transfers up to6;
Waystation stock12 transfers up to6 while intact; depletion causes next-activation
movement ×1.25 rounded upward, without starvation HP or Morale penalties.
B emergence is Refresh4, independent of Portal investigation; arrival arms a raid,
holding through the next activation Ravages. Village Ravage is immediate defeat.

**One evidence-backed composition delta:** Incursion B is now **1 HW + 1 EW + 1 HA**,
instead of initial 1 HW + 2 EW + 1 HA. All other proxy fixtures and all WP-01
profiles/AI weights remain unchanged. The initial South mouse run (not just the
AI simulation) won A/Area Guard, then lost four characters to B; two HWs escaped
to node15 with Tempo −10, and Village subsequently Ravaged. Evidence is preserved
under `results/initial-south/`, including `south-B-world.json` and
`south-original-failure.png`. This justified removing one EW from the late
scenario encounter, within document45's explicit composition-tuning allowance.
No probability, damage, recovery, timing, topology or mission objective was softened.

## Validation evidence

Evidence root `/private/tmp/wp02-20260929/`; early automated XML/log files also
under `/private/tmp/wp01-20260928/results/wp02-*.{xml,log}` because the isolated
Unity import cache was reused. Repository Assets were copied before each run;
the prior WP-01 manual helper was moved out. New manual helper is outside repo.

Initial real-combat route simulations used the unchanged minimal Tactical AI for
both sides and verified each battle journal. Central completed at Refresh5 with
one death. North exposed and helped fix an incorrect omniscient reroute by B:
its authored path must meet the blocking player, not avoid it. South reached B
but lost after Armor attrition; this is evidence, not an automatic combat retune.
An escaped bridge guard also exposed a path blocked by its allied camp; friendly
occupied intermediate nodes are now excluded from its physical exit path.

After this bounded correction, `wp02-focused-tuned.xml` reports **37/0/0**
passed/failed/skipped across strategic Core/routes and existing persistence tests.
Actual Core-combat route results for seed20260929:

| Route | Finish | Survivors | Provisions | Notable consequence |
|---|---:|---:|---:|---|
| Central | Refresh5 | 4/6 | 14 | Commander dead; Commanderless remnant completes request |
| North | Refresh5 | 4/6 | 16 | Early death absent in subsequent encounters; bridge cleared on return |
| South | Refresh6 | 6/6 | 18 | Waystation preserved; stock used twice; Armor attrition persists |

These are automated simulations, not mouse results. Provisions margins exceed
the approximate paper estimates: consumption occurs at completed Refresh
boundaries, mission success can occur before another boundary, casualties reduce
consumption, and the South route revisits the preserved Waystation. No extra
provision charge was invented to force the approximate paper arithmetic.

Mouse automation uses actual OS clicks on nodes, Move, tactical cells, Confirm,
Defend/End and Return to World. It uses Core AI suggestions as a repeatable tactical
driver; no HP/result injection or auto-victory. This is controlled UI validation,
not a claim of a human player's subjective balance acceptance.

### Controlled mouse-driven Unity validation

Actual OS clicks in isolated Unity 6000.6.2f1, seed20260929; Core AI suggestions
selected tactical commands, executed via cells/Confirm/Defend/End. This is a
repeatable UI smoke, not a human player balance verdict. Three complete final
route runs agree with the automated route table above:

| Case | Result and evidence under `/private/tmp/wp02-20260929/results/` |
|---|---|
| Central | PASS. Portal R2; cleared bridge used for Village response; Waystation lost nonfatally; B stopped; Keep R5. `Central-final.{png,json}`, `central-*-world.json` |
| North | PASS. Patrol06→07 bypass at R2; Portal R3; still-guarded bridge fought on return; B stopped; Keep R5. `North-final.{png,json}`, `north-bypass.{png,json}` |
| South | PASS after documented B composition tune. A contact from15; Waystation preserved/refilled twice, Food12→0; Portal R3; B defeated via South Fork; Keep R6. `South-final.{png,json}`, `south-*-world.json` |
| Delayed North | PASS. One extra activation puts Patrol back on06; Move blocked with reason and Attack available. `delayed-north-R2`, `delayed-north-R3` screenshots/JSON |
| Ignore A | PASS. Waystation Ravaged, Food12 remains inaccessible; visit at R3/end gives no refill, Provisions30 at R4; mission still ongoing. `ignore-A-no-supply.{png,json}` |
| Ignore B | PASS. Arrival at Village arms raid; next B activation causes explicit Village Ravaged defeat and disables commands. `ignore-B-failure.{png,json}` |
| Permanent guard defeat | PASS. Central Hard Guard remains removed through later Area/B battles; same world and cleared corridor retained. Central world captures |
| Early death/Commanderless | PASS. North baron-5 dies in Area battle and stays absent in bridge/B battles. Dead Commander stays dead, roster locked, surviving formation can finish. North world captures |
| Final physical Withdrawal | PASS. All six units escape through actual tactical movement; same Safe IDs/HP/Armor return, node03→01, Tempo0→−40, move blocked; next Refresh Tempo60. `final-withdrawal-{start,end,world,debt,refresh}.{png,json}` |

Final withdrawal smoke also exercised the final source including the fixture-label
correction. The coalition deployment extension does not change the single-army
layouts used in those three routes; focused tests cover multiple approaches.

### Hypotheses / playtest questions

- **H1 demonstrated:** Central forces the bridge fight; prompt North bypasses
  Patrol but pays bridge cost on return; delayed North meets Patrol; South fights A.
- **H2 demonstrated:** North loses an archer before later encounters; Central
  finishes Commanderless; Armor remains stripped. In the original South run,
  tactical attrition forced two survivors out to node15 with negative Tempo,
  invalidating the planned Keep return and allowing Village failure. Final
  withdrawal independently proves the same displacement/debt mechanism. This
  is evidence of changed available decisions; human preference still needs playtest.
- **H3 demonstrated:** A/B advance, arm, ravage and exit without player action;
  Patrol changes the route window. The map cannot remain a static fight menu.
- **H4 demonstrated:** North avoids a fight by timing; South buys preserved
  supply at the price of an early fight. Ignoring A remains a valid mission path.
- B warning, direction and one-activation raid grace are visible. Initial B4 was
  too punishing for this controlled South driver; B3 makes all routes viable.
  No final WP-01 profile, probability or AI-weight changes were made.
- No universal dominant route is established by one deterministic final run per
  route. South retained all six and more supply, but took longer; Central/North
  lost two. Broader route balance/fairness and subjective competence of the Human
  army remain **PENDING player feedback**, not an automated claim.
- Portal remains active on success; requesting Council assistance demonstrates
  local containment rather than permanently solving recurring emergence.

### Bounded integration decisions and limitations

Coalition deployment uses a battle-local frame: the primary encounter stays
West/East, other pre-battle approach vectors rotate into that frame and map to
nearest cardinal sectors (deterministic tie-break). Matching per-army Retreat
edges persist in replay; negative Tempo disables that army's edge. A withdrawn
Lead cannot be advanced into the target when its allies win. No reserve waves.

UI is intentionally low-fi and scrollable. Units/commanders now use persistent
identities rather than old fixture-index names; tactical selector reflects23×17.
World reports show current HP/Armor, Safe/dead, XP/levels, Command rank, sites and
supply. No campaign save/load or fog-of-war is added; scout reports are authored
scenario visibility. No blocked contract decision identified.

Isolated editor emitted Unity account/AI subscription/network warnings and an
editor `UnityEditor.Search.SearchDatabase` indexing exception during startup.
These are recorded in `manual*.log`; no corresponding game-state exception was
observed. Test-run helper is outside repo and removed from the test copy before
regression. One initial batch invocation was rejected while that editor was still
shutting down; it was rerun after exit, not counted as a passing suite.

## Test commands and delivery

Runner: `/private/tmp/wp01-20260928/run-tests.sh` invokes pinned Unity with
`-batchmode -projectPath /private/tmp/wp01-20260928/project -runTests
-testPlatform <platform> -testResults <results>/<name>.xml -logFile <results>/<name>.log`;
EditMode also uses `-nographics`. Source synchronized with
`rsync -a Assets/_Project/ /private/tmp/wp01-20260928/project/Assets/_Project/`.
No automated test runs against the user's open editor project.

```sh
bash /private/tmp/wp01-20260928/run-tests.sh wp02-focused-final EditMode -testFilter 'StrategicScenarioTests|StrategicRouteTests|PersistentFormationTests'
bash /private/tmp/wp01-20260928/run-tests.sh wp02-focused-ui-complete PlayMode -testFilter 'StrategicPresentationTests'
bash /private/tmp/wp01-20260928/run-tests.sh wp02-final-edit EditMode
bash /private/tmp/wp01-20260928/run-tests.sh wp02-final-play PlayMode
```

Fresh final focused EditMode: **35/0/0** (28 strategic cases +3 real-combat
routes +4 existing persistent-formation tests). Focused PlayMode: **2/0/0**,
including the complete Central UI bridge and explicit failure presentation.
Fresh full EditMode: **329/0/0**, completed 2026-09-29 14:06:36 UTC.
Fresh full PlayMode: **39/0/0**, completed 2026-09-29 14:07:51 UTC.
**Total368 passed, zero failed/skipped**; +31 EditMode and +2 PlayMode versus
accepted WP-01. XML/log pairs for all four final commands are copied to
`/private/tmp/wp02-20260929/results/` with the command names above; original
artifacts remain in `/private/tmp/wp01-20260928/results/`. `final-source.sha256`
records the tested C# source, and checksum-based rsync confirmed identical source
in repo and isolated copy after testing. Full regression includes unchanged
WP-01 probabilities/readability, Hotseat/AI, OA/Escape replay, previews/invalid
commands, field23×17 and siege41×39/ranged overlay. New route journals and
negative-Tempo snapshot replay PASS; strategic preview/invalid/determinism PASS. No accepted-baseline
failure was observed; development failures (test-fixture assumptions, UI test
synthetic navigation event, route/path correctness and original B composition)
were investigated rather than excluded. Real mouse events validate the buttons.

Delivery uses one coherent local commit, `Strategy: connect Mission 01 world and
persistent battles`, built on accepted WP-01 `79ac625`. The new graph/state,
bridge, UI and integration tests depend on one another. Resolve exact delivered
HEAD with `git rev-parse HEAD` (a commit cannot contain its own hash); the final
coordinator response records it. Six unrelated starting `.meta` files stay
untracked with identical hashes; no other pre-existing work was changed. Expected
remaining status after committing this package (all pre-existing):

```text
?? Assets/_Project/Core/PersistenceSliceScenario.cs.meta
?? Assets/_Project/Core/PersistentFormation.cs.meta
?? Assets/_Project/Tests/PersistenceProgressionTests.cs.meta
?? Assets/_Project/Tests/PersistenceSliceScenarioTests.cs.meta
?? Assets/_Project/Tests/PersistentFormationTests.cs.meta
?? Assets/_Project/Tests/PlayMode/PersistenceSlicePresentationTests.cs.meta
```

NO PUSH. Implementation/automated validation/controlled mouse validation are
reported separately from **PENDING player balance acceptance**. Recommendation:
coordinator review this bounded package; no further system started automatically.

## Launch

Open `Assets/_Project/Scenes/TacticalGraybox.unity` (or **Gate C → Play Tactical
Graybox**), enter Play Mode, click **Start Connected Mission 01**. Select a node,
review its path/cost, then Move. Attack an adjacent actor or traverse the protected
bridge to enter battle. Resolve it with existing tactical controls, then click
**Return persistent result to World**. End strategic activation advances actors
and Refresh. The right panel retains roster, supply and world-event evidence.

No cities, research, recruitment, economy expansion, production Portal state
machine, new classes/abilities, strategic Hotseat, real siege, save/load or
production assets. WP-03 is not started here. No push.
