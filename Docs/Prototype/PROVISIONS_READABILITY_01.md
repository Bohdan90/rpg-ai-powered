# WP-03D — Provisions + essential readability

Starting `develop` / `2e8c454d99310030c6d4431029728fbf69e74652`.
User integrated playtest: combat acceptable for graybox; Central's relative ease is
not a blocker. Requested supply pressure and essential readability only.

## Scope

- New Mission01 starts at **30 / 30 Provisions**, provisional prototype tuning.
  Living figure cost1, Keep/Waystation transfer6, local Food12, end-of-Refresh
  ordering, Hungry at0/movement×1.25 unchanged. No new penalties/resources.
- Separate green HP and blue Armor bars, numeric current/max values, role labels
  and DEAD state projected directly from Core after each result. Existing weapon
  icons remain. No animation/prediction or independent damage state.
- Mission01 authored scout/civilian intelligence presents actual living roster
  count/composition at known actor markers and in a compact known-force list.
  Hard Guard 2HW+1EW; Patrol 1HW+2EW; Area Guard 2HW+1EW+1HA;
  Incursion A 1HW+2EW. Incursion B 1HW+1EW+1HA is hidden until its existing
  activation/report at Refresh4. Defeated/exited actors leave the current markers.
  Withdrawn living troops remain actual roster members. No fog/scouting system or
  general omniscience contract. Objective text describes existing known role only.
- Strategic status shows Provisions current/max, consumption/Refresh, Tempo,
  Refresh, primary objective, Keep supply6/HP40% function and Waystation Food/state.
- Existing version1 saves with max36 are safely rejected with an explicit
  different-tuning/new-mission message; no clamping/migration, overwrite or live
  mutation. Start a new mission to test30. Same-tuning round trips unchanged.
- Combat profiles, graph/edges, actors/compositions/timing/AI, recovery, XP and
  save transport/schema remain unchanged.

## Validation

Pinned Unity6000.6.2f1 in the existing isolated test copy. Real Core ordered tactical
commands and replay verification, not injected victories, drive automated routes.
Mouse-driven Unity routes and screenshots are recorded separately below.

```sh
bash /private/tmp/wp01-20260928/run-tests.sh wp03d-focused EditMode -testFilter 'ProvisionsTuningTests|StrategicRouteTests|StrategicScenarioTests|StrategicSaveTests|StrategicRecoveryTests'
bash /private/tmp/wp01-20260928/run-tests.sh wp03d-focused-ui PlayMode -testFilter 'EssentialReadabilityTests|StrategicRecoveryPresentationTests|StrategicSavePresentationTests|GrayboxPlayModeTests'
```

Focused Core: **65 passed, 0 failed/skipped**. Focused UI initially20/21: the new
test incorrectly assumed defeated Hard Guard had no escaped survivors. Corrected
the assertion to require its remaining1HW/withdrawal marker until actual exit;
no gameplay change. Rerun **21/21 PASS**, and marker/label-padding checks2/2 PASS.
Final layout/suite results are recorded below. Includes updated existing supply,
Keep/Waystation, recovery, save/determinism, new casualty/no-double-recount and
old-tuning rejection cases. UI tests cover real seeded contact failure/Guard,
Armor-only, HP-only, spill, death/reset, roster/intel removal/dormancy and load.

Automated evidence (seed20260929; Refreshes = completed supply checkpoints;
mission begins at R1; supply happens after battle casualties):

| Route | Refreshes / final R | Start | Replenished | Consumed | End | Hungry | Battles |
|---|---:|---:|---:|---:|---:|---|---:|
| Central | 4 / R5 | 30 | 0 | 22 | 8 | No | 3 |
| North | 4 / R5 | 30 | 0 | 20 | 10 | No | 3 |
| South | 5 / R6 | 30 | 12 | 30 | 12 | No | 3 |
| Central +1 field delay | 5 / R6 | 30 | 0 | 26 | 4 | No | 3 |

Normal Central consumes73% of starting stock, North67%; no regular Hungry.
One extra late Central Refresh halves the remaining8 to4, one current-roster
consumption away from zero. South spends30 but receives12 from the protected
Waystation; absent that supply its ledger would reachzero. This is a counterfactual
supply ledger, not a claim of an independently played alternate route. No attempt
to equalize route leftovers. Casualties naturally reduce later consumption.

Controlled Unity Central and +1-delay both PASS with identical ledger to the table.
The delay uses a temporary test save at Village after B, before returning to Keep;
load and one End activation leave4, then return succeeds. This does not overwrite
the user manual slot. Real OS clicks + existing Core AI suggestions drive combat;
no victory/HP/Provisions injection. Central was captured before the final purely
visual cell-width adjustment; North/South validate that final display.

North mouse route PASS: R5,10 Provisions; Patrol at07 after the timed bypass,
known1HW+2EW. First battle leaves5 participants; later consumption falls5 then4.
Final North battle visibly distinguishes HW12/40 HP/Armor0 from HW40/40/Armor16,
and marks Dead at0 HP. Final cell-width layout avoids adjacent text overflow;
small fit-board numbers still benefit from the existing zoom/focus control.
South zoom screenshot shows healthy HA28/28/Armor4, wounded EW11/32/Armor0 and
DEAD EW0/32 separately, with bow/arrow and sword/shield icons intact.

South mouse route PASS: R6,12 Provisions, all6 survivors, Waystation Intact and
Food0. Its two completed transfers of6 provide a real12-Provisions benefit.
**All four controlled Unity ledgers match the table above exactly**, no Hungry;
all conclude CouncilAssistanceRequested. No alternative route optimization or
combat tuning was used. No new game runtime error observed; existing Unity Editor
Search indexing exception and account/subscription warnings remain unrelated.

Screenshots/JSON: `/private/tmp/wp03d-20260929/results/`:
`initial-final`, `north-bypass`, `{Central,North,South,Delay}-final`,
`delay-critical`, `north-B-end`, `south-A-zoom`, plus each battle's start/mid/end/
world captures. External driver/helper is outside repo and removed before suites.
The user's existing manual slot is untouched; the delay branch uses a temp-only
version1 snapshot. Route transcripts/commands: `/private/tmp/wp03d-20260929/play.py`.

Final tested source: **65 focused EditMode +21 focused PlayMode PASS**;
**363 full EditMode +48 full PlayMode =411 PASS**, zero failed/skipped.
EditMode ended2026-09-29 17:58:41 UTC; PlayMode ended18:00:15 UTC.
No baseline game failures were found; the corrected new test expectation above
was not an implementation regression. Tactical replay, OA/Escape, preview/invalid
state and RNG, same-tuning save/load continuation, recovery/world/supply and field/
siege regressions all remain green.

```sh
bash /private/tmp/wp01-20260928/run-tests.sh wp03d-focused-ui-verified PlayMode -testFilter 'EssentialReadabilityTests|StrategicRecoveryPresentationTests|StrategicSavePresentationTests|GrayboxPlayModeTests'
bash /private/tmp/wp01-20260928/run-tests.sh wp03d-full-edit EditMode
bash /private/tmp/wp01-20260928/run-tests.sh wp03d-full-play PlayMode
```

Fresh XML/logs copied to `/private/tmp/wp03d-20260929/results/`:
`wp03d-focused`, `wp03d-focused-ui-verified`, `wp03d-full-edit`, `wp03d-full-play`
(each `.xml` + `.log`), with `tested-source.sha256`. Same Assets source in isolated
runner verified against repository. Full suites ran after the final code changes;
only report/checkpoint edits followed.

## Handoff

Own coherent commit title: `Prototype: tune Provisions and add essential readability`.
Resolve delivered hash from Git/final coordinator report; no hash-only follow-up
commit. No gameplay deviation from requested30/30 candidate. Save compatibility
limit is explicit above; old36 saves require a new mission, not silent conversion.
Pre-existing scene and two ProjectSettings modifications plus six persistence
`.meta` files are excluded, SHA-256 unchanged. Exact remaining status:

```text
 M Assets/_Project/Scenes/TacticalGraybox.unity
 M ProjectSettings/EditorBuildSettings.asset
 M ProjectSettings/ProjectSettings.asset
?? Assets/_Project/Core/PersistenceSliceScenario.cs.meta
?? Assets/_Project/Core/PersistentFormation.cs.meta
?? Assets/_Project/Tests/PersistenceProgressionTests.cs.meta
?? Assets/_Project/Tests/PersistenceSliceScenarioTests.cs.meta
?? Assets/_Project/Tests/PersistentFormationTests.cs.meta
?? Assets/_Project/Tests/PlayMode/PersistenceSlicePresentationTests.cs.meta
```

NO PUSH.
No additional Hotseat, Economy, City or Research work. Updated package still needs
actual player feedback; controlled validation is not player balance acceptance.
