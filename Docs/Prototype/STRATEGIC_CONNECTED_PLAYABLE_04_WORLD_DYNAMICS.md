# Connected Playable 04 — World Dynamics (document 49)

2026-09-29. Starting `develop @ 4fe785183eaad4c3bc5652a9fd6f92737daac34f`.
Authority: Drive 49 (`10IYIP6y-q-vRtqpvJqraxASHjGW1QpScp_3Qx1QBAQg`) read fully,
48/latest44/46, owner33 §§0/8/10, owner17 participation/placement/Tempo/Withdrawal,
owner05 supply boundaries, owner26 recovery. Earlier 02+03 user **KEEP** remains closed.

**Status:** A–E implementation `20473ec` preserved; stage F controlled manual
validation is now **COMPLETE** (resumed 2026-09-29 from that same live HEAD).
Coordinator acceptance and integrated 04 **PLAYER ACCEPTED remain PENDING**.
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

## Stage F resumption — controlled mouse evidence completed

Live starting branch/HEAD: `develop @ 20473ece24aab885cd3bd3d8cb0fe48a47e3811a`.
Read current48/latest44/46 and49 §§17–20; no A–E reimplementation. No runtime or
automated-test code changed. The prior 70+9 focused /441+61 full results above belong
to implementation20473ec; their XML was rechecked, **not rerun or claimed as fresh**.
The current user instruction permits reuse when executable/test code is unchanged.
This follow-up changes only report/checkpoint and labelled manual starting-state JSON.

The original attempt had M1/M4 partial and M2/M3/M5 unfinished. Its valid launch,
R1→R2 and mid-Refresh save/hash evidence was reused. All remaining cases below were
subsequently exercised using OS mouse events on the ordinary UI. Human tactical
commands used read-only legal AI/path suggestions, followed by actual board clicks
and Confirm/End/Escape; no direct resolver calls or fabricated outcomes. This is a
controlled developer run, not evidence of an unaided user's strategy or balance verdict.

Input diagnosis: isolated Unity6000.6.2f1, same implementation, seed20260929.
Target window PID86050, bounds(0,42,1728,1005), maximized Game view; panel-to-screen
origin(224,228), scale0.84375. A visible selector click produced a runtime PointerDown
on `incident-start-west`. Full M1/M2 then ran successfully. After external fixture-helper
assembly reload, pointer delivery stopped despite correct visible cursor coordinates.
The user clarified concurrent manual takeover; two-monitor incompatibility was NOT
established. One Play Mode exit/re-entry restored delivery with the existing HID mouse
helper; M3/M5/supplements then completed. No new permissions mechanism, security bypass,
general mouse framework, editor restart loop or product input patch. The temporary
MANUAL BLOCKED diagnosis is superseded; exact reload/takeover cause remains unproven.
The isolated editor is closed; the user's main editor was not closed or modified.

| Case | Actual result / evidence |
|---|---|
| **M1 PASS** | Continued real R2 save. Two PvE defenses, then PvP Hotseat with East choosing physical tactical Escape; same World throughout. Raider A defeated after84 commands; B after145. West lost `duel-West-3/5/6`, retained3 living bodies. PvP8-command evacuation placed East at04 with Tempo60; West advanced into06. Legal victory **West17:East3 at completedR10**. No forced battle result, healing, casualty or victory. `m1-r2-battle1-*`, `m1-r3-battle1-*`, `m1-pvp-*`, `m1-victory`. |
| **M2 PASS** | R1 arrival gave0 credit; R2 defense→1 cycle; R3 defense→2. At R4 Gold300→450, reward paid once. Branch from the **actual post-Hold save**, not a damage fixture: damaged West08→ownKeep01; HP of `duel-West-2` stayed35 on arrival and became40 only on completedRefresh, Armor0→0. Paid100 Gold; save/recreate/load pending order; R5 Gold350, KeepFood36→33; recruit `duel-West-recruit-1` joined, old3 Dead records stayedDead. Moved01→07 and attacked06; recruit deployed and returned through an8-command ordinary Escape battle. `m2-reward`, `m2-damaged-expedition-natural-save`, `m2-arrived-keep`, `m2-paid-recruit-*`, `m2-recovered-recruit`, `m2-recruit-deployed`, `m2-redeployed-return`. |
| **M3 PASS** | Ordinary ignore run: West07/East06; A armed08 atR2, ravaged onR3 slot; R4 UI RAVAGED, inaccessibleFood24, HoldFailed, no winner. R5 still24/inaccessible, match continues. Labelled Hold fixture: lawful Withdrawal08→03, Tempo100→60, Hold1→0, Gold300/no reward. Labelled blocked-return fixture: R5 A15/Tempo−40, East16; R6 Closure leaves same3-member A at15/StrandedReturn/Tempo60. Contact→human Withdrawal; East later leaves area to08. At R8 Stable A physically16/GuardingClosedAnchor, not erased/exited; B remainsUnreleased. `m3-ravaged*`, `m3-lawful-withdrawal`, `m3-reset-after`, `m3-closure-survivor`, `m3-blocked-return-contact`, `m3-stable-remnant`. |
| **M4 PASS** | Prior mid-R2 save/recreate/load retained. New save/recreate/load after first world battle at its stable cursor: same hash, R2/Pressure1:1/Food18, A defeated, B staged. Continue finished that phase once: R3/Pressure2:2/Food12/Hold1 and B entered. Immediate post-reward save/load kept Gold450/HoldCompleted; later totals matched only ordinary income or recruitment, no repeated150. Post-raid save/load kept inaccessibleFood24/Failed; next cycle did not ravage/pay twice. Pending recruit save/load completed exactly once. Actual HUD/screenshots plus hashes below, not hashes alone. |
| **M5 PASS (short comparison)** | East-first: East reached/owned08, West06; R2 first/activeEast and A entry16, so Waystation obligation swapped side. No claim of a full mirrored combat match. No-incidents control used same map/target16 throughR4: portalStable, HoldDisabled, no active raiders,6 intact West figures, Food6, ProvisionsW30/E12. `m5-east-first`, `m5-no-incident`. |
| **Grouped raiders PASS** | Labelled R3 fixture: West08 attacksA14 with alliedB15 adjacent. **All12 initial bodies**, header `West human / Raider A AI + Raider B AI`; one shared encounter, no serial replacement battles. Actual North-edge Escape resolved21 commands; West→03/Tempo10. Both distinct raider Commanders earned0.131142857… CommandXP. `grouped-12-deployed`, `grouped-raiders-*`. |
| **Two battles / no Refresh PASS** | Labelled R3 fixture West07, A06, B08 (not adjacent to each other). First real90-command fight defeatedA. Return atR3/Tempo50; Commander `duel-West-1` **HP32/Armor0**. Move06→07, attackB08; second deployment retained all6 IDs and exact pools, including32/0. Ordinary15-command Escape completed; return stillR3/Tempo−40, Provisions30. No End Refresh/recovery or state injection between fights. `two-battles-before`, `two-battles-between`, `two-battles-second-start`, `two-battles-second-world`. |

Save/recreate/load hashes matched exactly:

| Boundary | SHA-256 representation used by prototype |
|---|---|
| Prior mid-R2 | `TCQwIceKXxvw0Pcoe6wVFcbtjOqs5gfl1I7TTn28cj8=` |
| Resolved world battle cursor | `Sa/IAlkBf2kcyN0JNYdBH05a6X93drk+EB3+zCcB7q8=` |
| Immediately after reward | `QKzUoQwcX/vSOMnDxZGEJlycbJOBZwCaMPjfwSsfigI=` |
| Immediately after raid | `O/RIcouPDTLeIV0o/GJBU75WLNEf+huYAWncUaeksTg=` |
| Paid pending recruit | `Fcpa+29MV9mPqYx10R+gjHYoYO8PTDl/4+V41N7ipl4=` |

Seven exported tactical replays matched all attempts: **84,145,8,8,21,90,15** commands,
respectively M1 PvE-A/PvE-B/PvP, M2 recruit battle, grouped, two-battles first/second.
This is fresh replay verification of the controlled runs, separate from the historical
502-test regression. Original preview/invalid/RNG automated evidence remains unchanged.

Artifacts: `/private/tmp/cp04-20260929/results/` contains each named `.png`/`.json`,
`*-replay` exports and `*-verification.txt`; `/private/tmp/cp04-20260929/stage-f/`
contains input diagnosis and the isolated editor log. Only the primary Unity screenshots
are evidence; incidental desktop/secondary-screen captures are not gameplay proof.
Reproducible rare-case files and exact human actions: [manual fixtures](Fixtures/Incident04/README.md).
Only their starting states were injected; their observed movement, Withdrawal, combat,
XP, lifecycle and outcomes followed mouse-driven commands. No manual cases remain NOT RUN.

### Observations, not automatic balance changes

- In the main run the Waystation defender West bore the incident cost:3 real deaths;
  East held Mine and reached525 Gold byR4. The150 reward did not pay for replacing all3
  lost bodies. This run does not establish optimal defense or overall reward dominance.
- Mirroring changed who arrived first/held08; it does not prove first-mover fairness.
  No Anchor camping was used in the main match. Ignore/control runs demonstrate it is
  possible to continue without intervention; strategic desirability is not proven.
- Target16 allowed a full return/recruit/redeploy branch atR4–R5 before the mainR10
  ending. Attrition changed the roster from6 to3 and left Armor4/0/11, making return
  and paid replacement materially different from a free reset.
- After incidents ended, the deliberately passive opponent allowed repeated objective
  scoring; late waiting involved repetitive End/Continue clicks. No active stalemate
  was observed, but this controlled ending is not a competitive camping/turtling test.
- Save/load caused no observed outcome change or duplicate reward/raid/economy/recruit.
  Production UX and integrated user acceptance remain outside this developer verdict.

## Deviations, observations, limitations and handoff

- No numerical/scope/canon deviation. One coherent local commit is appropriate because
  incident scheduler, bridge, save DTO and UI are interdependent; no empty A–F milestones.
- Stage F controlled M1–M5 and supplements are complete. Coordinator acceptance
  and integrated **04** player KEEP remain **PENDING**. Earlier02+03 KEEP is unaffected.
- Observations above are from bounded controlled runs, not final balance acceptance. No retuning.
- Explicit Continue World Phase exposes stable continuation boundaries. This is graybox
  UX, not automatic world AI/framework. No online/fog, cities/research, full portal travel,
  Loot/repair, new classes, production save profiles or unrelated systems.
- Nine foreign files preserved byte-for-byte against preflight SHA-256 manifest
  `/private/tmp/cp04-20260929/preexisting.json`: scene, two settings, six old .meta.
  Their existing scene diff includes trailing whitespace; it is not part of this package.
- Local implementation commit title: `World: add Crossroads Incident 04 lifecycle and persistent aftermath`.
  Read actual final hash with `git rev-parse HEAD`; report does not invent its own hash.
- Stage-F commit title: `Docs: complete Incident 04 controlled validation`; documentation/manual fixtures only, implementation remains20473ec.
- **NO PUSH. STOP** for coordinator review and one integrated04 user playtest before new systems.

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
