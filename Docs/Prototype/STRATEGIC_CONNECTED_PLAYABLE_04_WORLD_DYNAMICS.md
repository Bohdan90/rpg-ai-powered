# Connected Playable 04 — World Dynamics (document 49)

2026-09-29. Starting `develop @ 4fe785183eaad4c3bc5652a9fd6f92737daac34f`.
Authority: Drive 49 (`10IYIP6y-q-vRtqpvJqraxASHjGW1QpScp_3Qx1QBAQg`) read fully,
48/latest44/46, owner33 §§0/8/10, owner17 participation/placement/Tempo/Withdrawal,
owner05 supply boundaries, owner26 recovery. Earlier 02+03 user **KEEP** remains closed.

**Status:** A–E implemented. Automated evidence below; F controlled manual gate is
**INCOMPLETE**. This is not full document-49 DoD acceptance or player acceptance.
No owner contradiction identified; no new balancing numbers or systems introduced.

## Launch

Open `Assets/_Project/Scenes/TacticalGraybox.unity` in Unity **6000.6.2f1**, enter Play.
In the right tactical panel scroll to **Crossroads Incident 04 · West first** or
**Crossroads Incident 04 · East first**. **Incident 04 · control (no incidents)**
uses the same extended map/target16 without incident activity. The earlier
**Start Connected Mission 01** and **Crossroads economy Hotseat** selectors remain.
Select a graph node, then Move or Attack the selected adjacent occupied node.
End both human activations; use **Continue World Phase** for each actor and the
final end transaction. A contacted human chooses Fight/Strategic Withdrawal;
NPC fights reuse tactical AI, PvP reuses tactical Hotseat. After combat use
**Return persistent result to World**, then continue the preserved world cursor.

Save/Load is stable-map only, including handoff and resolved world-battle boundaries.
**Load Incident 04** also works from the initial tactical selector. Incident saves
use `Application.persistentDataPath/CrossroadsIncident04/manual.json`; original
Crossroads and Mission01 slots are separate and unchanged. No mid-battle/modal save.

## Implementation / prototype numbers

- Same 13 nodes/16 edges, plus axis-symmetric 08–14 Lodge–15 Cordon–16 Anchor,
  three edges20 each. Original geometry/edge costs retained. Neutral Lodge is not
  capturable. Target16 in incident only; original Crossroads target8.
- Both six-character human forces retain existing IDs, Gold300, KeepFood36,
  Provisions30/30, body consumption1, WaystationFood24/supply<=6, Mine75,
  replacement100/one order per side per Refresh and native capacity6.
- One portal: R1 Precursor; R2/R3 Red; R4/R5 Whiteout2/1; R6 Closure;
  R7 Recovery; R8 Stable, never repeats. `IncidentsEnabled=false` leaves it Stable.
- Manifest A=Commander HW + HW + HA; B=Commander HW +2HA. Deterministic separate
  persistent IDs from initialization. Oldest pending entry only at Red begin,
  at most one/boundary, occupied Anchor defers. Whiteout makes unentered actors
  Unreleased, not dead. No off-map active consumption/recovery or respawn.
- Actors use physical least-cost paths/Tempo and visible required-path contact,
  at most one initiated encounter per slot. Raid arms on arrival and completes
  next own activation only if still present. Both humans get the intervening cycle.
  Ravage atomically disables supply, retaining inaccessible Food; ownership/Pressure
  persist. No Loot, currency gain, repair or automatic mission failure.
- Return is physical; Red/Whiteout permits exit preserving the off-region roster.
  Closure strands on-map remnants. They return/guard Anchor with area14–16;
  they survive Stable. Occupancy prevents allied stacking and can block a return.
- Shared Protect contract: same continuing physical owner at08 for two COMPLETE
  cycles, first eligible R2. Mid-cycle arrival gives no credit; leave/return,
  ownership loss, destruction/displacement reset. Defensive hold and surviving
  Commanderless holder remain eligible. +150Gold once, no extra HP/XP/Pressure.
  Ravage fails an open contract; R6 expires it after final R5 evaluation.
- Core owns all state/rules. New `IncidentState`, `IncidentBattleBridge`,
  `IncidentSaveData` extend the existing Crossroads adapter; original-mode hooks
  are no-ops. Presentation only projects state/dispatches commands. No UnityEngine
  dependency added to Core, combat tuning/profile/AI movement rules unchanged.

## Exact scheduler and bridge

Begin: portal transitions/entry, surviving raider Tempo100+debt, Hold eligibility.
Two human activations each claim their current objective. World phase captures a
stable ordinal-ID actor list. The cursor advances BEFORE a slot starts a contact;
that battle can pause the resolver. Contact choices/battles block human map commands
and saving. Resolved battle returns to a stable saveable cursor, never a new activation.
Removed/exited actors are skipped. Supporting another actor does not reset Tempo.

After the last slot: evaluate Hold/payment, score objectives, then existing human
consumption -> finite supply -> HP recovery -> next Tempo/debt; Mine income;
paid recruit completion (new recruits first consume next cycle); active raider
consumption/field HP; advance Refresh/start boundary only while the match continues (terminal04 keeps the
completed Refresh number; original03 remains unchanged). Terminal elimination completes
the non-interactive transaction once without releasing next-cycle actors or supplying
a destroyed force. HP15%
field /40% own functioning Keep, clamp with existing fractional remainder; no Armor
repair, resurrection or battle-exit recovery. No double tick between world battles.

Target-centered participant snapshot: adjacent allied nonnegative attackers each pay
Attack Cost; target and direct allied defenders join regardless of Tempo; no recursive
joins or human coalition. All bodies initially deployed in approach-derived cardinal
field sectors, per-formation retreat edges/debt. Tactical coalition slots are separate
from persistent ownership, including strategic East attacking and inactive human
NPC defense. Blue/Orange owners and actual rear directions are explicitly labelled.

`PersistentBattle` now accepts formation arrays while preserving the one-formation
API. Characters remain the SAME records; personal XP denominator is the original
participant count; each Commander separately gets pool/(7 × participating armies).
Each result is applied once via battle number and consumed encounter. Retreat reserves
free destinations per original formation in ordinal-ID order, maximizing minimum
separation from participating enemies within two graph hops. No destination means
stay/pay40, not teleport. Only a continuing, non-withdrawn lead advances into a vacated
target; other participants stay unless their own result moves/removes them.

## Save and determinism

Incident-only version3 / scenario `Crossroads-Incident-04`; original Crossroads
version2 and Mission01 formats remain supported. No unrelated slot overwrite/migration.
Canonical binary SHA-256 covers phase/countdown, manifest/rosters/resources, armed raid,
actor iteration/cursor, paid result number, Hold identity/continuity/payment/terminal
state, ravage/unavailable Food, both human economies/queues and existing seed/battle
counter. Candidate restore validates before replacing live state. Preview/invalid
commands have checksum no-mutation coverage; no actor/preview combat RNG consumption.

Tests compare round-trip hashes at lifecycle boundaries and deterministic continuation
from a resolved world battle cursor; repeated result application is rejected. Real
Core-command incident battles are replayed and resolve persistent attrition through a
complete automated match. These automated drivers are NOT counted as manual play.

## Automated evidence

Runner (isolated copy, same Unity version):
`bash /private/tmp/wp01-20260928/run-tests.sh <name> EditMode|PlayMode [-testFilter ...]`
Source sync: `rsync -a Assets/_Project/ /private/tmp/wp01-20260928/project/Assets/_Project/`.
External manual helper is removed from the test project's Assets before batch tests.

- Initial existing Crossroads focused:38 Edit PASS.
- Final focused: **70 EditMode +9 PlayMode PASS**, 0 failed/skipped.
- First full run:438 Edit +60 Play PASS, before terminal/UI follow-up; not the final diff.
- Final full regression: **441 EditMode +61 PlayMode =502 PASS**, 0 failed/skipped.
- Fresh XML and matching `.log` files in `/private/tmp/wp01-20260928/results/`:
  `cp04-verified-focused.xml` (70 Edit, filter `IncidentTests|Crossroads`),
  `cp04-commit-ui.xml` (9 Play, filter `IncidentPresentationTests|CrossroadsPresentationTests`),
  `cp04-commit-edit.xml` (441 Edit, unfiltered),
  `cp04-commit-play.xml` (61 Play, unfiltered).
  All final runs completed 2026-09-29 America/New_York; full runs cover final code.
  Replay, deterministic continuation and preview/invalid mutation checks PASS.
  No baseline failures; an overlapping runner launch was rejected as project-locked
  before tests and rerun sequentially.
- Development correction: one test expected the wrong exception type; actual safe-load
  failure is `InvalidDataException`. Corrected assertion; no failed behavior weakened.
- New coverage includes physical Escape/replay, no inter-battle healing, both human/NPC
  mappings, allied offense costs/negative defense, no recursive/third-party joins,
  reserved/no-destination retreat, actual AI command combat, continuous Hold/Commanderless,
  entry delay/no burst, Whiteout/stranding, lasting supply loss, paid reward once,
  save cursor/economy continuity and terminal transaction/no next release.

## Controlled manual evidence — partial, mandatory gate still open

External local evidence: `/private/tmp/cp04-20260929/results/`.
Harness uses actual OS mouse events and existing UI buttons; read-only path/AI command
suggestions do not execute Core commands. Rare fixtures were prepared but NOT executed.

| Check | Actual result |
|---|---|
| M1 full incident match / legal victory | **PARTIAL, NOT COMPLETED**. Mouse launch; West→08, East→06, both activations; R2 Red/A entry; actual NPC approach16→15→14, defending West chooses Fight. Nine persistent bodies deployed, human West maps tactical Orange/East, NPC maps Blue/West. No completed battle/victory evidence. |
| M2 successful Hold + recovery/recruit/redeploy | **NOT RUN** to completion. Arrival R1 produced0 full Hold cycles; R2 eligibility visible. No manual reward or later recruit battle claimed. |
| M3 ignore/ravage/interruption/stranding | **NOT RUN** manually; automated fixtures pass separately. |
| M4 save/recreate/load | **PARTIAL PASS** mid-R2 human handoff. Exact hash `TCQwIceKXxvw0Pcoe6wVFcbtjOqs5gfl1I7TTn28cj8=` before/after. West08/East06, Provisions30/24, active manifest restored. World-battle continuation and immediate post-reward/raid manual checks **NOT RUN**. |
| M5 mirror/control comparison | **NOT RUN** manually; automated mirrored/control rules tested. |

Screenshots/JSON: `m1-r2-begin`, `m4-mid-refresh-saved`,
`m4-mid-refresh-loaded`, `m1-r2-contact`, `m1-r2-battle1-start`.
No full manual balance findings are inferred from those partial steps.

Manual limitation: first tactical click exposed an external helper's XZ projection
error (fixed outside repo). During subsequent editor reload/restart, OS mouse harness
stopped delivering runtime pointer events; foreground activation also failed until
AX focus was applied. Accessibility reported trusted; clicks still produced no runtime
pointer callback. The cause of that remaining GUI-input failure is not established as
an implementation defect. Do not mark M1–M5 PASS from automated tests/screenshots.
Initial standalone load-error visibility and misleading fixed West/East Retreat
labels were improved as bounded Presentation correctness fixes; final manual recheck
of those changes remains pending. No production art or UI redesign.

## Deviations, observations, limitations and handoff

- No numerical/scope/canon deviation. One coherent local commit is appropriate because
  incident scheduler, bridge, save DTO and UI are interdependent; no empty A–F milestones.
- F is not complete until the remaining controlled M1–M5 are run. Coordinator acceptance
  and integrated **04** player KEEP remain **PENDING**. Earlier02+03 KEEP is unaffected.
- First mover, intervention/Anchor camping necessity, reward dominance, turtling,
  target16 match pacing and repetitive waiting are **not manually assessed**. No retuning.
- Explicit Continue World Phase exposes stable continuation boundaries. This is graybox
  UX, not automatic world AI/framework. No online/fog, cities/research, full portal travel,
  Loot/repair, new classes, production save profiles or unrelated systems.
- Nine foreign files preserved byte-for-byte against preflight SHA-256 manifest
  `/private/tmp/cp04-20260929/preexisting.json`: scene, two settings, six old .meta.
  Their existing scene diff includes trailing whitespace; it is not part of this package.
- Local implementation commit title: `World: add Crossroads Incident 04 lifecycle and persistent aftermath`.
  Read actual final hash with `git rev-parse HEAD`; report does not invent its own hash.
- **NO PUSH. STOP**; finish manual gate/coordinator review before any new systems.

Exact preserved working-tree status after committing own files:

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
