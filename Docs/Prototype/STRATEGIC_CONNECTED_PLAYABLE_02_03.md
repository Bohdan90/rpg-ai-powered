# Connected Playable 02 + 03 — Crossroads Duel

Authority: Drive document 47 (`1E18nxF0LKPgXESMTun0ZXFv_wy-GE3Mn-y5YB4kHyOY`),
read in full, latest 44/46 append; task-local owner17 §1.3/§9.6/§9.8 and command
capacity, owner05 recruitment/Provisions, owner26 HP recovery. Prototype-only.
Start: develop / `55a269a4768ffb284804bbe81bf5ec63ac9eb985` (2026-09-29).
Current single-player slice KEEP; no reopening combat or Mission01 tuning.

## Part A implementation

Separate data-driven Crossroads Duel: document47's exact symmetric 13-node,
16-edge graph. Two persistent human-controlled 6-body armies (HW Commander,
3HW,2HA), L1/0XP/full HP/Armor, stable side-specific IDs. Existing tactical
Hotseat/resolver/journal, no tactical AI substituted for either side.

Fixed StartingSide (West or East); active-side authority and explicit handoff;
one global Refresh after both activations. Claim physical nodes06/07/08 on
activation end, persistent ownership, +1 Pressure per objective at Refresh;
target8, simultaneous threshold uses higher total, tie continues. No continuing
formation loses. Open strategic information is scenario-only.

Tempo100, existing 50/remaining/0 Attack Cost, defender no cost. Existing40
Withdrawal debt and deterministic <=2-hop displacement (max separation, then
movement cost, then node ID). Tactical physical evacuation still required after
battle starts; negative Tempo disables own tactical Retreat edge. Attacker victory
advances only when target vacated; defender holds. Persistent results/XP applied
once, no battle-exit healing/reset/replacements. Commanderless survivors continue.

Provisions30/30 each, living body cost1, consumption then own-Keep replenish<=6.
Part A Keep stock scenario-sufficient. Completed Refresh HP field15%/ownKeep40%,
no stacking; Armor/dead/XP unchanged. Enemy Keep presence disables owner service.

Core snapshot/hash + JSON transport follows existing failure-safe pattern:
validate separate candidate before replacement, atomic file replacement,1MB limit.
Crossroads one slot (`Application.persistentDataPath/Crossroads/manual.json`);
Mission01's existing separate slot/schema remain supported. Stable map only,
including handoff. Snapshot includes both formations, objective/Pressure,
StartingSide/ActiveSide/completed activation, debt, supply, seed/battle counter,
events and result. No campaign event-sourcing or mid-battle save.

## Validation / execution log

Part A focused: 17 EditMode +3 PlayMode PASS (0 failed/skipped). Tests cover
mirroring, authority/handoff, claims/recapture, tie, attack/debt, physical escape,
post-battle placement/IDs/XP/no heal, Commanderless, deterministic round-trip and
failure isolation. Initial local compile error (node vs ID in HUD) corrected.
Full regression: **388 EditMode +55 PlayMode =443 PASS**, zero failed/skipped. Controlled mouse match PASS: West9/East8 at end R6 (screen advances to R7).
All three objectives recaptured; East declines adjacent attack R2 to claim Mine;
R3 West attacks, all six East characters physically Escape, West advances onto
Mine. Same IDs/XP return, East40 Tempo cost. Saved at R2 handoff, recreated scene/
session, loaded exact hash, continued. Tactical replay12/12 matches.
Mirrored East-first R1 smoke: both move/claim, score1:1, R2 begins with East.
Screenshots `a-*.png`, state/trace and replay in evidence root. No AI commands.

Observations: Pressure ended this deliberately contested match in6 global cycles;
late waiting reached Hungry in Part A without forward supply. Ownership persists
without garrison, so recapture matters; no deadlock in this run. One scripted match
and short mirror do not establish severity of first-mover advantage. Node-ID retreat
tie-break can choose a lateral/westward escape for East; it follows the existing
separation/cost deterministic contract, not a homeward bias. Open information and
handoff are readable in graybox; no production vision inferred.

Part A local commit: `Strategy: add Crossroads persistent strategic Hotseat`.
Part B follows this green checkpoint; not yet implemented in this commit.

Evidence root: `/private/tmp/cp0203-20260929/results/`. Runner source results:
`/private/tmp/wp01-20260928/results/cp02-*.{xml,log}`. External mouse/inspection
helper stays outside repo; it does not modify gameplay rules/state or enable AI.

## Part B implementation

Same graph/Pressure/combat, economy enabled by default in launch UI; Core can
still instantiate the Part-A fixture (`economy:false`). Prototype constants:
Gold300/side; own Keep Food36; shared Waystation Food24; Mine controller +75
Gold/global Refresh; Beacon no secondary output; supplies<=6 actual transfer,
only ownKeep or controlled physical Waystation. Food never regenerates, Gold
never substitutes for Food. Provisions30/30, body cost1, Hungry multiplier1.25
unchanged. HP field15/ownKeep40 independent of Food, no Armor repair.

HW/HA ordinary recruit100 Gold, L1/0XP/full baseline HP/Armor, no Commander.
Native load6 per subordinate; Commander exempt, rank capacities32/38/44/50.
Living roster capacity only, Dead records preserved. Own Commander-led formation
must physically occupy own functioning Keep, have free6 capacity and Gold100.
One order/side/Refresh; action pays and ends activation. Paid order persists if
completion illegal, blocks further orders, joins once when conditions return.
No refund/new charge. A permanently Commanderless pending order remains pending
in this bounded prototype (no resurrection/replacement Commander service).

Refresh order: claims already resolved, Pressure score, each side consumption /
supply / HP recovery, Mine income, legal recruit completion, next Refresh/Tempo.
New recruit consumes from following Refresh. Side-local monotonic recruit sequence
produces new persistent IDs; never reuse a Dead record. Field deployment supports
up to the existing rank-IV9 bodies without changing board size or combat tuning.

Crossroads schema2 adds economy flag, Gold/Food, paid queue/profile/ID, last-order
Refresh and next-ID sequence. Intermediate Part-A schema1 is safely rejected;
Mission01 schema/slot unchanged. Save/reload does not re-execute completed stages.

Part B focused: **38 EditMode (17 A +21 B) +5 PlayMode PASS**, zero failed/skipped.
Covers finite stock, owner income/recapture, no double processing, own-resource
isolation, cost/locality/capacity/Commanderless invalid purity, paid pending across
blocked service, exact persistent recruit deployment, rank-IV capacity, recovery,
recreated session and incompatible/tampered save isolation.
Controlled economic mouse match **PASS**, West8/East6 after completed R6
(screen R7). Both seats used existing human Hotseat controls through OS mouse;
no tactical AI/auto-resolution or injected HP/deaths/Gold in either manual match.
Developer control is not an independent two-user balance playtest.

| Checkpoint | West HP/Armor (Commander) | West roster | Gold W/E | Provisions W | Other evidence |
|---|---|---|---|---|---|
| R2 real battle return |16/0|5 living +1 Dead|375/300|24|Physical Escape; debt25->-15; displaced7->4|
| R3 Keep arrival |22/0|same identities|450/300|19|Field15%; travel60 of refreshed85 Tempo|
| R3 paid pending save |22/0|5 living +1 Dead|350/300|19|100 paid, handoff/session recreated/load same hash|
| R4 after Keep checkpoint |38/0|6 living +1 Dead|425/300|20|Keep40%, Food36->30; new HW L1/XP0/full40HP16Armor|
| R4 second tactical battle |38/0 at start|recruit deployed|425/300|20|`duel-West-recruit-1` in initiative/persistent mapping|
| R7 victory screen |40/0|6 living +1 Dead|650/300|2|Pressure8:6; Waystation Food6; Keep Food30 each|

West's original `duel-West-2` remains Dead; recruit is a separate record. End
battle1 grants no recovery. West chose return/recruit over pushing; East chose
forward supply. First battle replay175/175 commands matches; second12/12 matches.
The scripted direct East return at R5 was rejected correctly: route100 >85 Tempo
because its second-battle Withdrawal created -15 debt. Continued via node12 at
cost80, then Keep13 next activation at cost20; no rule/tuning change to make the
script pass. `b-debt-blocked-home.png` records this real opportunity cost.

Economy ledger after6 Refreshes: West income450, recruit cost100, Keep supply6,
consumption34, final Provisions2. East Waystation supply18 +Keep6, consumption36,
final Provisions18. Shared Waystation24->6, each Keep36->30. All transfers actual.
Mirrored East-first economic R1 smoke PASS: East owns Mine/Gold375, West owns
Waystation/real6 transfer, Pressure1:1, R2 starts East.
Screenshots `b-*.png`, matching JSON, `b-trace.jsonl`/`b-second-trace.jsonl`, two
replay directories/verification files in evidence root. Screenshots inspected.

Observations (no automatic redesign):
- **Mine:** six income ticks are visible (+450), but starting300 already funded
  the one100 replacement. This sample does not establish Mine as mandatory.
- **Waystation:**18 replenished made a material difference; finite6 remains.
  Useful forward supply, not a mandatory route proven by these controlled runs.
- **Recruitment:** restored one lost subordinate at Gold/time/capacity cost;
  no resurrection/roster normalization. Full army cannot recruit (only2 free).
- **Recovery:** Keep restored16 HP vs field6 for HW; Armor stayed0. A veteran
  returns weakened defensively; waiting is not a complete reset.
- **First mover:** both full scripts started West/won West; short opposite-start
  tests are symmetric technically, insufficient to judge competitive advantage.
- **Camping/stalemate:** both matches ended in6 cycles, no stalemate observed.
  Persisting ungarrisoned ownership can finance home recovery; opponent can
  recapture (demonstrated A). Whether this produces excessive turtling is a player
  watch item, not settled by one recovery/recruit run.
- **Pressure8:** allowed two battles and a return/replacement in B; not shown
  too early/late by this script, not final balance acceptance.
- **Optional fight:** A's East declined an available attack to claim Mine;
  damage/debt in B changed actual follow-up movement/return decisions.
- **Readability:** map, ownership, handoff and resource/roster state usable;
  right pane scroll remains graybox. Open information is explicit, no fog claim.
- **Save/load:** exact mid-Refresh/pending hash and continuation preserved; no
  repeated income/supply/charge/recruit. Mission01 save support retained.

Final full regression: **409 EditMode +57 PlayMode =466 PASS, 0 failed/skipped**.
Fresh final-source runs 20:57:59–20:58:59Z /20:59:14–21:00:46Z, 2026-09-29. Prototype numeric deviations: **none**.
Implementation choices: paid-pending handling, completion after current supply/
recovery (new consumption next Refresh), separate scenario slot and schema2 safe
rejection of internal Part-A schema1, 0..16 tactical deployment rows for rankIV.
No production system or game-rule expansion.

## Boundaries / delivery

No stat/AI tuning, Mission01 changes, production fog, city/research/culture,
networking, captivity/resurrection, real siege or additional resources.
Pre-existing scene/ProjectSettings edits and six persistence .meta files preserved
by SHA-256 manifest `/private/tmp/cp0203-20260929/preexisting.json`.
NO PUSH. After Part B, stop for integrated user playtest. Developer-controlled
validation is not player balance acceptance.


## Commands / final evidence

Unity pinned6000.6.2f1; isolated source-identical project. No engine upgrade.

```sh
bash /private/tmp/wp01-20260928/run-tests.sh cp02-focused-v2 EditMode -testFilter CrossroadsTests
bash /private/tmp/wp01-20260928/run-tests.sh cp02-focused-play PlayMode -testFilter CrossroadsPresentationTests
bash /private/tmp/wp01-20260928/run-tests.sh cp02-full-edit EditMode
bash /private/tmp/wp01-20260928/run-tests.sh cp02-full-play PlayMode
bash /private/tmp/wp01-20260928/run-tests.sh cp03-focused EditMode -testFilter 'CrossroadsTests|CrossroadsEconomyTests'
bash /private/tmp/wp01-20260928/run-tests.sh cp03-focused-play PlayMode -testFilter CrossroadsPresentationTests
bash /private/tmp/wp01-20260928/run-tests.sh cp03-full-edit EditMode
bash /private/tmp/wp01-20260928/run-tests.sh cp03-full-play PlayMode
```

Fresh XML/log copies in `/private/tmp/cp0203-20260929/results/` with these basenames;
`final-tested-source.sha256` proves committed C# matches tested sources. Existing
replay/OA/Escape/preview/invalid/RNG, siege41x39/field23x17, AI WP03E, Mission01
save/recovery/supply/route and ranged-overlay regressions included. No known
baseline test failure. Manual Editor-only Search indexing exception and account/
subscription warnings persist outside gameplay; no Core/Presenter error observed.

Two coherent commits chosen: Part A includes its playable bridge/save/UI as one
reviewable milestone; Part B extends that same scenario with economy/recruitment.
Part A `d3dd271`; Part B title `Strategy: add Crossroads economy and persistent replacements`
(resolve exact final hash from Git/coordinator report). No squash/push.

Preserved final status after own commit:

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

All nine match preflight SHA-256. Final player acceptance remains PENDING; stop
coding after this batch and return for one integrated user playtest.
