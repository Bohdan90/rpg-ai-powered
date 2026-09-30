# Gate C — working implementation checkpoint

Main integration: feature `f3a3ccc` (tested gameplay `a82e98e`) delivered by explicit user authorization. Original main product/settings are retained; package acceptance remains PARTIAL. Resolve final merge HEAD with `git rev-parse HEAD`.

Latest 50+51 follow-up: **PARTIAL**, not technically/manual/player accepted. User explicitly authorized main integration after audit on 2026-09-30. Ability/command discoverability and spell preview wording fixed; no Core/tuning changes. Current test provenance and partial V1/V2 GUI results: [acceptance audit](5051_ACCEPTANCE_AUDIT.md), `Evidence/5051/validation-manifest.json`. N1–N5/V3–V5 remain NOT RUN; no current GUI-permission blocker is claimed.

Updated 2026-09-29. Single implementation handoff; Google Drive thematic owners retain
priority for canon. Follow repository `AGENTS.md` and the efficiency protocol.

## Connected Playable 04 — World Dynamics (document49; controlled stage F complete)

- Starting `develop @ 4fe7851`; document49 A–F authorized, latest48/44/46 read. Prior02+03 personal user **KEEP** is closed and unchanged.
- A–E implemented: separate **Crossroads Incident 04 · West first / East first** selectors in TacticalGraybox Play-mode panel, plus no-incident control. Same original graph plus axis nodes14–16/edges20; target16 only here (original03 stays8).
- R1 Precursor / R2 A / R3 B / R4–5 Whiteout2/1 / R6 Closure / R7 Recovery / R8 Stable. Persistent staged/pending/unreleased/exited/defeated/stranded records, physical raid/return and lasting Waystation supply disable. Resources/combat/recovery inherited unchanged.
- Resumable world phase after both humans; stable actor cursor, one initiation/slot; explicit Continue World Phase. Target-centered multi-formation participation, per-army Attack/Withdrawal, directional deployment, same IDs/XP/Commanderless, human Hotseat versus raider AI; pre-battle human Fight/Withdrawal.
- Shared continuous Hold: same physical owner for2 COMPLETE cycles (first R2), resets on break;150Gold once; ravage fails / R6 expires. No contract spawns or bonus Pressure/healing.
- Incident schema3/separate slot includes all actor/cursor/Hold/service/economy state; original Crossroads schema2/Mission01 unchanged. Invalid loads preserve live state; battle/modal save disabled. Core previews/replay/invalid-command invariants retained.
- Automated final: **70 focused EditMode +9 focused PlayMode PASS; full441 EditMode +61 PlayMode =502 PASS, 0 failed/skipped**. Replay/preview/invalid/save continuation checks PASS; see report for XML from that run. These are historical results for implementation20473ec, not a fresh run in the documentation-only stage-F follow-up. No runtime/test-code changes.
- Stage F resumed from live `develop @ 20473ec`; current48/latest44/46 and49 §§17–20 read. **M1–M5 controlled mouse PASS**: fullR10 West17:3, two PvE defenses/3 persistent losses, PvP tacticalEscape, Hold150 once; actual post-Hold save → Keep recovery35→40/Armor0 → paid HW recruit → later battle. Ravage/Withdrawal reset/blocked return→Stable remnant, world-cursor/reward/raid/recruit save continuation, short East-first/control comparison PASS. Grouped12-body battle and two battles atR3 with exact32HP/0Armor carry-over PASS. Seven fresh controlled replays match84/145/8/8/21/90/15 commands.
- Input interruption resolved using existing helper/one Play Mode re-entry after fixture script reload; user clarified manual takeover. Two-monitor incompatibility not proven. No current manual blocker, no game code/tuning fix. Report/launch/evidence and labelled manual JSON fixtures: [STRATEGIC_CONNECTED_PLAYABLE_04_WORLD_DYNAMICS.md](STRATEGIC_CONNECTED_PLAYABLE_04_WORLD_DYNAMICS.md). Coordinator acceptance and integrated04 user playtest remain PENDING; earlier02+03 KEEP unchanged.
- Nine unrelated scene/settings/six.meta preserved by SHA-256; own diff isolated. Stage-F documentation/manual-fixture commit title `Docs: complete Incident 04 controlled validation`. NO PUSH. STOP for coordinator acceptance and integrated04 user playtest; no Civilization Depth/other systems.

## Connected Playable 03 — economy/recruitment extension (document47 Part B)

- Continues green Part A commit `d3dd271`; same Crossroads graph/Pressure/human Hotseat, no combat or Mission01 changes. Start from tactical screen **Crossroads economy Hotseat · West first / East first**; separate Crossroads load button/slot.
- Prototype-only: Gold300/side, ownKeep Food36, shared Waystation Food24, Mine75Gold/Refresh; Beacon position/Pressure only. Provisions30/30, body cost1, supplies<=6 limited by actual Food and physical ownership; fieldHP15/ownKeep40, Armor unchanged.
- HW/HA replacement100Gold, L1/XP0/full baseline pools, free Native capacity6, Commander-led at ownKeep; one order/side/Refresh ends activation. Paid pending retained if completion illegal; joins once after current Refresh supply/recovery, consumes next Refresh. Stable new IDs; Dead records retained. Rank capacities32/38/44/50, Commander exempt. No resurrection/research/reorganization.
- Crossroads schema2 includes both sides' economy/queue/ID sequence; internal Part-A schema1 safely rejected, Mission01 unchanged. Mid-Refresh queued save/recreate/load exact, no duplicated income/charge/supply/recruit.
- Controlled mouse economic match PASS: West8:East6 afterR6, Mine income450; Waystation supplied18; real death and Commander16HP/0Armor -> field22 -> Keep38; paid `duel-West-recruit-1` deploys in second battle. East Retreat debt85 blocks100-cost return, legal two-activation return used. Replays175 and12 commands match; both-seat human controller, no tactical AI. East-first economic smokePASS.
- Focused38 EditMode +5 PlayMode PASS; full409 EditMode +57 PlayMode =466 PASS, 0 failed/skipped. Source/evidence/observations: [STRATEGIC_CONNECTED_PLAYABLE_02_03.md](STRATEGIC_CONNECTED_PLAYABLE_02_03.md). First-mover severity/Mine necessity/excessive turtling remain player watch items; no retuning. Local Part-B commit title `Strategy: add Crossroads economy and persistent replacements`.
- **STOP after this batch for integrated user playtest.** No Civilization Depth, Cities, Research, Dominion/Convergence, full Siege or broader systems. Unrelated scene/settings/six.meta preserved; NO PUSH.

## Connected Playable 02 — Crossroads Hotseat (document47 Part A)

- Starting `develop` / `55a269a`. Coordinator accepts Gate-C connected single-player slice **KEEP**. Latest Drive44/46 authorize document47 Part A → Part B autonomously; prior stop notices below are historical.
- Part A implemented: exact symmetric13-node graph, two human6-body persistent formations, fixed configurable StartingSide, authority/handoff, global Refresh, contested claims, Pressure8/tie, current Attack/Withdrawal/debt, tactical Hotseat bridge, attacker advance/defender hold, stable two-sided save/load. No Mission01 or combat tuning change.
- Provisions30/30, living body cost1, ownKeep supply6; HP field15/ownKeep40 at completed Refresh only, Armor unchanged. Separate Crossroads manual slot preserves Mission01.
- Validation: focused17 EditMode +3 PlayMode PASS; full388 EditMode +55 PlayMode =443 PASS, zero failed/skipped. Mouse match West9:East8 afterR6, three recaptures, decline attack, tactical physical escape, same persistent IDs/XP, mid-Refresh save/recreate/load exact; replay12commands matches. East-first mirror smokePASS.
- Report: [STRATEGIC_CONNECTED_PLAYABLE_02_03.md](STRATEGIC_CONNECTED_PLAYABLE_02_03.md). Commit title `Strategy: add Crossroads persistent strategic Hotseat`. Unrelated scene/settings/six.meta preserved; NO PUSH. **Continue directly Part B**, then integrated user gate; no broader systems.

## WP-03E — tactical AI retreat / wall stall

- From `develop` / `e7036eb15a8ae2b929d3c5f06514512cfe61d2f5`. User accepts current graybox combat, Provisions and threat readability; only tactical AI defect addressed.
- Reproduction-first: **D confirmed**, healthy Warrior indefinitely Defend/End behind a wall despite safe detour; straight-line approach scoring penalized the necessary detour. Also confirmed missing multi-activation low-HP retreat approach (AI approached the enemy when exit was beyond current Movement). Original user replay unavailable; these are controlled analogues.
- **A legal**: Movement exhaustion one cell short now explicitly explained; next activation evacuates. **B not reproduced** (adjacent safe reachable exit already worked); **C stale target not reproduced** (stateless AI), actual blocked target/alternate exit verified.
- Two Core AI files only: pure legal goal-distance query; existing <=25% HP retreat preference continues over current-budget route prefixes/replans each command. Wall approach potential applies only when all enemies are occluded and no useful attack is reachable; existing risk scoring and weights remain. No combat/Movement/Retreat canon, strategic state/tuning, map, persistence or replay-format change.
- Controlled Unity mouse retreat, damage-to-retreat transition, blocked exit, healthy wall detour and useful archer cover **PASS**; all five exported replays match. Healthy Warrior reaches attack by R4; exhausted Warrior escapes next activation. Screenshots/traces and limitations in [TACTICAL_AI_RETREAT_STALL_01.md](TACTICAL_AI_RETREAT_STALL_01.md).
- AI command/casualty changes alter some old seed outcomes: former North optional-Bridge return loses final B fight; existing southern return succeeds R6/5 Provisions. Recovery regression now uses a real third battle to retain actual Keep-healing coverage; withdrawn-intel coverage has an independent fixture. No compensating balance changes.
- Final automated validation: **21 focused EditMode +16 focused PlayMode PASS; full371 EditMode +52 PlayMode =423 PASS, 0 failed/skipped**. Replay/preview/invalid/RNG regression green. Own commit title `AI: complete retreat approaches and escape wall stalls`; starting scene/settings diffs and six untracked persistence `.meta` are preserved. NO PUSH. Stop for coordinator/player review; no further systems authorized.

## WP-03D — player-feedback Provisions / essential readability

- From `develop` / `2e8c454`. First integrated user playtest: combat feels acceptable for graybox; Central being easier is explicitly not a blocker. Only supply/readability adjusted; combat profiles, topology/edge Tempo, enemy compositions/timing/AI, recovery and XP unchanged.
- New Mission01 **Starting/Max Provisions30/30**, provisional tuning. Figure cost1, supply6, Waystation Food12, existing ordering/Hungry unchanged. Old saves with max36 fail safely with a new-mission message; version1 format/transport unchanged and current live state preserved. Start a new mission for the new tuning.
- Tactical units: distinct green HP / blue Armor fill and current/max values, HW/EW/HA and weapon icons preserved; DEAD explicit. Direct Core projection, including Armor-only/spill and no damage animation on failed contact/Guard. Compact cell-width layout; use existing zoom for small overview numbers.
- Mission01-only scout/civilian information: known actor name/count/actual living composition + current role; no dormant B roster until its existing R4 report. Withdrawn survivors update count before exit; removed actors leave markers. Keep supply/recovery, consumption, Refresh/Tempo and Waystation Food/state clarified.
- Controlled Unity Central R5/8 Provisions, North R5/10, South R6/12 (12 replenished), Central+1 delay R6/4: all success, no Hungry. All match real-command/replay automated ledgers. Details/screenshots/limits: [PROVISIONS_READABILITY_01.md](PROVISIONS_READABILITY_01.md).
- Validation: **65 focused EditMode +21 focused PlayMode PASS; full363 EditMode +48 PlayMode =411 PASS, 0 failed/skipped**. Replay, preview/invalid/RNG and save/refresh regressions green. Own commit title `Prototype: tune Provisions and add essential readability`; preserved starting scene/settings changes and six untracked persistence `.meta`. NO PUSH.
- **STOP after this package**; return to coordinator/user playtest. No Hotseat/Economy/City/Research expansion. New tuning/player acceptance remains to be evaluated by the user.

## WP-03B — Keep recovery / mandatory next player gate

- From green WP-03A `8ed1480`: Mission01 Keep is the approved functioning Healing Building adapter. At the completed Refresh checkpoint, the player's physical location selects **Keep40% OR field15% Max HP**, never both. Fractional carry/cap preserved. No Armor repair, resurrection, replacement, Commanderless/XP reset or battle-exit healing.
- UI shows expected per-character HP and no-Armor-repair/time-cost warning; event log shows actual changes. Actors/supply process unchanged while recovering. Existing version1 save stores all needed location/HP/fractional state; no new schema needed.
- Controlled mouse sequence PASS: two real battles leave HWs20/40 and12/40, field Refresh makes26/18, return to Keep R3 then Save/Load checksum unchanged. One Keep Refresh gives40/34, Armor stays0, Patrol moves and B emerges R4. Redeploy to Village with same IDs/XP. Portal investigation was deferred by return/recovery. Details: [RETURN_RECOVERY_01.md](RETURN_RECOVERY_01.md).
- Validation: **34 focused EditMode +7 focused PlayMode PASS; full359 EditMode +46 PlayMode =405 PASS, 0 failed/skipped**. Fresh artifacts and commands in RETURN_RECOVERY_01.md. Deterministic continuation, replay and preview/invalid-state regressions green.
- Own commit title `Strategy: add Keep recovery with world time pressure`. Starting scene/settings diffs and six unrelated untracked `.meta` preserved. NO PUSH.
- **STOP CODING: next is WP-03C INTEGRATED PLAYER GATE**, actual user playtest and KEEP / ITERATE / CONTRACT DECISION verdict. WP-02 and integrated player acceptance remain PENDING. No Strategic Hotseat, economy/settlement expansion, cities or research is authorized by this batch.

## WP-03A — stable strategic save/load

- From `develop` / `d917029`: version1 single-slot Mission01 JSON, Core snapshots/validation/checksum, independent candidate restore then atomic Presenter replacement. Captures world/actor cadence, seed+battle counter, Tempo/debt, supply/sites/results, persistent identities/HP/Armor/dead/Safe/Commanderless/progression/fractional carry and result history. Stable map only; no mid-battle resume.
- Startup **Load saved Mission01**; World **Save Mission01 / Load saved Mission01**. Slot `Application.persistentDataPath/Mission01/manual.json`. Failed load preserves live state; save overwrites one slot through temp+atomic replace. No cloud/autosave/profiles/replay redesign.
- Full application-restart mouse smoke PASS: saved node05/Refresh2 after actors moved; closed/reopened Unity; loaded checksum/state identical; moved06→10 and entered Area Guard battle. Fixed JSON absent-resolution decoding found in first smoke; added four explicit regression cases. Details/evidence: [SAVE_LOAD_01.md](SAVE_LOAD_01.md).
- User's in-task visual correction applied: tokens now show **weapon icons only**, sword/shield for HW/EW, bow/arrow for HA; no people. Labels/facing/side colors remain.
- Validation: **21 focused Core +6 focused PlayMode PASS; full350 EditMode +45 PlayMode =395 PASS, 0 failed/skipped**.
- Local commit title `Persistence: save and load stable Mission 01 state`; exact hash from Git/final report. Starting scene/ProjectSettings diffs and six untracked `.meta` remain unchanged. NO PUSH. WP-03B follows only after this package's green validation; then mandatory WP-03C user gate.

## Visual readability delta — warrior / archer tokens (prior implementation)

- From `7069a80`: two shared flat vector silhouettes in Presentation replace cube/cylinder class tokens: sword/shield for both HW and EW; bow/arrow for HA. Race stays in existing HW/EW labels. Side-color base, Commander asterisk, active marker and HP/Armor labels remain. Facing arrow sits on the token edge; HUD legend updated. No Core/combat/persistence changes or asset-package dependency.
- Unity visual checks: field23×17 and siege41×39 fit/focus/zoom show both types, side colors and HW/EW/HA labels; dense siege still needs zoom for detail. Evidence `/private/tmp/unit-silhouettes-20260929/`. Existing editor Search indexing/account warnings are unrelated to runtime silhouettes.
- Validation: focused16 PlayMode PASS; full329 EditMode +39 PlayMode = **368 PASS, 0 failed/skipped**. Fresh XML/logs: `/private/tmp/unit-silhouettes-20260929/silhouettes-{focused,full-edit,full-play}.{xml,log}`. Tested code checksum manifest is in the same directory.
- User's starting changes to TacticalGraybox scene and two ProjectSettings files plus six untracked persistence `.meta` files are preserved; only this visual diff is committed. Local commit title `Presentation: distinguish warrior and archer silhouettes`; no push.

## WP-02 — current connected implementation delta

- Starting accepted baseline: `develop` / **79ac6257ceb76478d057ed8e95a5761cf5a32b38**. WP-00 DONE and WP-01 TECHNICAL PASS accepted by coordinator; provisional combat values remain unchanged.
- **Strategic Connected Playable 01 implemented**: authored 18-node graph, Tempo/Attack/Withdrawal debt, Refresh, Provisions/Hungry, Waystation supply, deterministic Hard Guard/Area Guard/Patrol/A/B, Portal/Village/Keep mission state and low-fi UI. World → chosen encounter → actual tactical battle → same persistent World.
- Same player/enemy IDs carry HP/Armor, death/Safe, Commanderless/roster lock, XP/levels/rank and recovery carry. No replacement/exit healing/Armor repair. One authorized field Refresh at world boundary. Adjacent coalition snapshot and per-army approach/Retreat edges; negative-Tempo defense has no Retreat edge. Fixed XP enemy valuation to use battle-start Base Power.
- Final mouse routes: Central R5, North R5, South R6 succeed. Delayed North changes Patrol contact; ignored A disables supply, ignored B causes Village defeat; guard removal/early death persist; physical Withdrawal gives node03→01, Tempo−40 then60 after Refresh. Controlled OS-click driver uses Core AI tactical suggestions; human balance acceptance remains **PENDING**.
- Evidence-based mission-local tuning: B **1 HW + 1 EW + 1 HA**, one EW fewer after actual initial South loss/withdrawal/Village failure. Combat profiles/AI weights/timing/topology unchanged. Final South keeps six; Central/North four, Commanderless. Supply margins exceed approximate paper estimates; watch in later player evaluation.
- Launch: existing TacticalGraybox → **Start Connected Mission 01**. No strategic Hotseat, save/load, cities/research/economy expansion or new combat content. Do not start another package automatically.
- Fresh final validation: **329 EditMode + 39 PlayMode = 368 PASS; 0 failed/skipped**. Focused final35 Core/persistence +2 PlayMode PASS. Replay, preview/invalid-state and RNG regression PASS. Full validation/delivery results: see [WP-02 report](STRATEGIC_CONNECTED_PLAYABLE_01.md). This delta and implementation belong to local commit `Strategy: connect Mission 01 world and persistent battles`; obtain live hash with `git rev-parse HEAD`. Six pre-existing persistence `.meta` files remain untracked and SHA-256 unchanged. NO PUSH.

## Baseline (prior milestone evidence retained)

- Latest delta: **WP-00 + WP-01 implemented**, from live `develop` / `d41b5d5a2d161f75f5009e5d5e4295cd7ff038de`. Document 45 Phase 0 provisional values below supersede the old profile tuning. Core-derived attack result HUD/log distinguishes failed contact, Guard, Armor and HP (including spill); weak HA fallback remains Accuracy 80.
- Fresh WP-01 validation: **298 EditMode + 37 PlayMode = 335 passed; 0 failed/skipped**. Five outcome mouse smoke cases PASS. Replay/preview/invalid-command regressions PASS. Player balance acceptance **PENDING**. Evidence/commands/limitations: [COMBAT_TUNING_01.md](COMBAT_TUNING_01.md). Code and this delta share the local `Combat: apply WP-01 tuning and clarify attack outcomes` commit; resolve HEAD from Git.
- At WP-01 closure, WP-02 was the next candidate; superseded by the current WP-02 delta above. Six pre-existing persistence `.meta` files remain untracked and unchanged; no push.

- Implementation: final user-playtested siege baseline 41×39, validation/documentation committed with this checkpoint delta; geometry **462bd87**; ranged reach **25c57bd**; directional siege **7bed17e**; telemetry/replay **ec19d0b**; minimal AI **926e28c**; density fixtures **fb95710**; prior siege-scale V2 **18088fe**. Part A fallback: **5aff414**; prior bow baseline **3bb1164**. Live HEAD: `git rev-parse --short HEAD`.
- Gate C: **PASS for the current prototype scope**. User completed a Player-vs-AI field battle, lost normally to the AI, found its behavior normal and reported no critical tactical-loop blocker. Technical DoD/replay, field 23×17, siege 41×39 and physical Retreat are accepted. Persistence Slice v0.1 is the next authorized work; do not reopen tactical redesign unless persistence exposes a direct Core defect.
- Unity **6000.6.2f1**, C#, URP, Rider; no editor/package upgrade authorized.
- RPG.Core is pure C#, independent of UnityEngine/Presentation. Core owns combat truth; views consume queries/results.
- Deterministic explicit RNG; same state/seed/commands replay identically. Queries/invalid commands do not mutate state/RNG. Movement costs and legality remain Core-owned.
- Last Persistence Slice v0.1 full tests: **289 EditMode + 36 PlayMode = 325 passed; 0 failed; 0 skipped** on Unity 6000.6.2f1 in an isolated project copy. Gate C pre-slice result was 280 + 34 = 314.
- No push performed. Check actual Git status before work.
- Launch: **Gate C → Play Tactical Graybox → Fixture → Field_23x17_Full_9v9 / Siege_41x39_West_9v9**.

## Implemented systems and T — PROTOTYPE TUNING

Hotseat; activation queue/Initiative; Movement + Action; deterministic readable paths;
Basic Attack, Accuracy/Dodge/Guard, Armor → HP/death, Defend, Facing/Frontal Evasion,
Steady Aim, distance penalty, LoS/directional unit Cover, ZoC/OA, physical Retreat,
Escaped/Safe, Withdrawal/Eliminated and outcome HUD.

| TI profile | HP | Armor | Move | Init | Accuracy | Dodge | Guard | Damage | Range |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| HW | 40 | 16 | 4 | 10 | 90 | 5 | 15 | 12 | 1 |
| HA | 28 | 4 | 4 | 12 | 85 | 5 | 0 | 10 | 10 |
| EW | 32 | 6 | 6 | 14 | 90 | 10 | 0 | 11 | 1 |

- HA distance penalty: `5 × max(0, distance − 4)` pp. Steady Aim: **+15 pp Accuracy**, no Range increase; requires no prior Movement and consumes remaining Movement. Bow Range always 10 when available. Hostile existing HW/EW ZoC locks Bow even if OA spent; engaged HA gets Action-only Melee Strike (Range 1, damage 5, Accuracy 80), no Aim and no HA ZoC/OA. Surviving exit restores bow without Aim after Movement.
- EW Frontal Evasion +10 pp Dodge. Defend: 25% Physical Resistance, before Movement only; consumes Action/remaining Movement, expires at next own activation.
- Light Cover −15 pp Accuracy, directional/nonstacking; unit bodies do not hard-block LoS. Strong classification exists; numeric tuning deferred.
- Movement precedes Action, may be split; after Action no further Movement. HW/EW exert ZoC, HA does not; one OA between own activation starts, resolved before exit step, no responder rotation.
- These values, working bow distance ~7 (1.75× baseline Movement), max 10 (2.5×) and map dimensions are prototype tuning, not final global canon.

## Geometry and fixtures

- Melee diagonal: one open orthogonal side permits contact; two solid sides seal it. Basic/preview/ZoC/OA share this rule.
- Ranged: single-wall corner/boundary-only touch legal; shared vertex of two diagonal solid cells blocked; solid interior intersection blocked.
- Movement stricter: either orthogonal side solid/occupied blocks diagonal steps. Eight directions, each cost 1; Chebyshev attack distance.
- Fixtures: `Field_13x9_Control`, `Field_17x11_Expanded`, `Field_19x13_ExpandedV2`, `Siege_23x17_Tight`, `Siege_27x21_Roomy`, **`Siege_31x25_Medium`**, **`Siege_35x27_Large`**, `Field_23x17_Full_9v9`, `Siege_35x27_Full_9v9`.
- Original comparisons retain 5v5 composition: HW-Commander, HW-Infantry, HA-Left, HA-Right, EW-Flanker per side. Static siege proxy only, no real siege mechanics. 31×25/35×27 siege maps share identical centered 11×9 fortress (24 solids) + one-cell solid/opaque moat proxy (32 solids; 13×11 outer envelope), with four three-cell crossings. West deployment unchanged at x=1/2; East inside; no combat retuning between maps.
- Density fixtures: 1 HW Commander + 3 HW Infantry + 3 HA + 2 EW per side; synthetic tactical roster, no strategic Capacity legality claim. Field uses center wall (11,7..9); siege reuses existing 35×27 geometry exactly. Several deployment columns, same profiles/seed/tuning.
- 19×13 solids (9,5)/(9,6)/(9,7); West/East retreat edges x=0/18. Ordinary field retreat rear edge; siege defender retreat full legal outer perimeter.

- Directional correction: **41×39** siege proxy, four single-army fixtures and W+E/N+S 18v9 capacity fixtures; old comparisons retained. Centered fortress 11×9/moat/crossings unchanged; each attacking sector is 3 deep ×9 wide, minimum Chebyshev wall separation **12**. Per-unit own Retreat edge preserves each approach; defender all-perimeter unchanged. No combat retuning/real siege systems. Geometry/coordinates and validation: closure report.
- Final Gate C spatial decision from user playtest: **siege 41×39**, superseding 39×37; **ordinary field 23×17**. 43×41 was discussed but not selected. Battlefield-size exploration is closed; no further comparison without a direct technical contradiction or explicit new user instruction. This is the selected Gate C baseline, not universal canon or Gate C PASS.
- 41×39 final validation: 14 focused Core cases + 4 ranged-reach PlayMode scenarios passed. Mouse: all six deployments/approach moves and attacker AI advances; W+E and N+S each 18 attackers +9 defenders, no overlap; four prepared crossings; Range10/11, engagement/spent hiding, full-roster reach, Fit/Focus/zoom passed. Minimum wall separation measured 12 on every approach. N+S movement and archer-fire exports replayed with matching hashes; selection/preview preserve state/RNG. Ranged overlay/combat tuning unchanged; no full-battle verdict.
- Previous 39×37 mouse checks: all six directional deployments + attacker AI moves; W+E/N+S 27 views; prepared crossing and N+S export/replay passed. Existing zoom needed for unit detail; no full-battle timing verdict.
- Mouse range10/11, Cover, engaged fallback/OA exit, Action-spent hiding and export/replay passed; all four corrected crossings traversed. No full-battle verdict.
- Ranged reach: active player HA shows cyan cell-corner marks from current position using Profile.Range/Core Chebyshev distance; green Movement remains. Hidden with explicit HUD reason while engaged/Action spent, also hidden on AI turns and for melee units. Geometric reach is not LoS/target legality; preview/resolver unchanged.
- Replay v2 snapshots/hashes include own Retreat edge; v1 exports are explicitly unsupported.
- Persistence Slice v0.1: `PersistentCharacter`/`PersistentFormation` in Core carry stable ID, profile, Commander flag, current HP/Armor, Alive/Escaped-Safe/Dead, Personal XP/Level, Command XP/Level/Rank and Commanderless/roster lock. Dead slots stay empty; Safe returns unchanged; field refresh is +15% Max HP with deterministic fractional carry and no Armor repair. `625ff80`.
- Developer harness: `Start Persistence Slice v0.1` runs the nine-figure 23×17 field sequence Battle 1 → 0 Refresh → Battle 2 → 1 field Refresh → Battle 3; HUD exposes the persistent roster/result/Commander state. East is a fresh deterministic scenario opponent each battle, intentionally not an enemy campaign framework. Focused persistence checks: 9 EditMode + 2 PlayMode; full result 289 + 36 = 325. Part B commit to be recorded with final validation.

## Playtest findings, limitations, OPEN

- Historical user/Drive 43 closure superseded the earlier 19×13 preference: **ordinary field 23×17; full siege 35×27** were provisional Gate C baselines, NOT final universal canon. Keep older comparisons; do not iterate dimensions without new playtest evidence.
- Earlier findings retained: field 13×9 too small, 17×11 somewhat small; siege 23×17 too small and 27×21 probably small considering moat/bridges/exterior terrain.
- Previous mouse checks: Part A fallback/engagement, no HA OA, risky HA exit restoring Bow; Part B both maps approach/crossings/flanks, ranged/Cover/blocked LoS, OA, physical defender escape, attacker non-escape and framing/focus passed. Earlier range 7/8/10 vs 11 and corner checks preserved.
- Latest density mouse checks: both 18-unit deployments/approach, prepared contact/OA/Cover, siege crossing/exterior flank and escape, initial field retreat, focus/fit passed. No full-battle timing verdict. Queue 18 readable but needs HUD scrolling; siege detail needs zoom. Full interior-to-perimeter route passed Core test.
- User reports Range 10 substantially better. Point-blank decision now explicitly prototyped: engagement locks Bow and enables weak Melee Strike; no weapon identity canonized. OPEN: user evaluation of fallback.
- OPEN: user 9v9 density evaluation: congestion, flank routes, ranged/ZoC pressure, retreat and battle duration. No full 9v9 battle-length verdict yet; proxy opacity is not a canon rule for real moats.
- Existing HUD: nonadjacent melee can say `corner: blocked` alongside correct `OutOfRange`; long fixture name clips in selector; detailed preview requires scrolling (zoom/focus available). Hover text may persist across fixture reset until next cell hover.
- Minimal one-ply AI now selectable: Hotseat / Player West vs AI East / Player East vs AI West. Same resolver/previews; no evaluation RNG. Contract weights, expected OA-safe routing, low-HP physical evacuation. No strategic AI or real siege/campaign systems.
- AI mouse smoke: both 9v9 boards, ranged attacks, melee/crossing movement, ordinary OA and low-HP field/siege escape passed in prepared 18-unit states.
- Replay: HUD Export battle + session / Load-verify file; local JSONL + separate session metrics under persistentDataPath/GateC/Replays. Core snapshots/commands/SHA-256; regenerated RNG, first divergent sequence. Mouse Field/OA/Escape/Player-vs-AI exports all verified. Optional decision-time metric omitted.
- Watch item for later content playtests, not a Gate C blocker: the deliberately minimal roster gives every unit limited intrinsic incentive to advance. Ranged asymmetry already creates approach pressure; future classes, magic, objectives and richer kits may add more. Do not add anti-stalemate combat rules now. Retreat was not behaviorally attractive in the final isolated battle because it had no persistent consequence; persistence now supplies that incentive.

## Detail references (read only when relevant)

[Closure report](GATE_C_CLOSURE_REPORT.md) · [Replay](TELEMETRY_REPLAY.md) · [Minimal AI](MINIMAL_TACTICAL_AI.md) · [Density validation](ARMY_DENSITY_VALIDATION.md) · [Contract](GATE_C_TACTICAL_PROTOTYPE_CONTRACT_v0_1.md) ·
[Archer fallback](ARCHER_ENGAGEMENT_FALLBACK.md) · [Bow tuning](ARCHER_RANGE_TUNING.md) · [Ranged geometry](RANGED_CORNER_LOS_CORRECTION.md) ·
[Field V2/melee](FIELD_V2_CORNER_CONTACT.md) · [Siege V2](SIEGE_SCALE_V2_EXPERIMENT.md) · [Size experiment](BATTLEFIELD_SIZE_EXPERIMENT.md) ·
[Path/Cover corrections](MILESTONE_2B_1_CORRECTIONS.md) ·
[3A Core](MILESTONE_3A_IMPLEMENTATION.md) · [3B Presentation](MILESTONE_3B_IMPLEMENTATION.md).

## 2026-09-29 isolated 50+51 early Lab (IN PROGRESS)

Feature `feature/50-51-connected` in `../Isolated-50-51`, based on `045707f`. Separate company/product/preferences/save namespace; original checkout untouched. Combat Lab three direct near-contact entries implemented; focused 33 EditMode + 1 PlayMode PASS. This is not full regression or manual acceptance. 50 economy and combined 05B remain in progress. See `COMBAT_VARIETY_AND_CONNECTED_05B.md`. Continue the authorized batch; no merge into develop or push.

## 2026-09-29 — isolated 50+51 implementation / coordinator handoff

Starting live develop `045707f` (gameplay `20473ec`); no existing 50 work. Feature worktree `../Isolated-50-51`, `feature/50-51-connected`. Local commits `3541376` (early Combat Lab) and `3d58f35` (City/Region/Research/Forge, integrated 05B, save/replay and final gameplay fixes). Evidence/checkpoint commit follows; it changes no gameplay. Do not merge/cherry-pick into develop without separate authorization. NO PUSH.

50 and 51 implementations are present. 05A remains separate with old roster. 05B reuses paid 05A economy and adds fixed Fire/Ice/Support presets, HOM/HH direction, TowerI/II,150G L1 Mage recruitment,6Work/40G Drills and75G same-ID HOM II training. New Lab Fire/Ice/HH/EW II kits, spell AI, clear source budgets/statuses and persistent results are connected. Old01/03/04 tuning and user checkout remain untouched.

Final actual combined regression for gameplay `3d58f35`: **489 EditMode +64 PlayMode =553 passed; 0 failed; 0 skipped**. Fresh XML and exact source logs/commands: `Evidence/5051/validation-manifest.json`. Focused tests exercised concrete spell/attrition/queue/save/Forge risks. Two scripted mirrored 05B matches used actual tactical commands, casualties and save continuation and ended legally; they are not manual or balance acceptance.

**N1–N5 / V1–V5 manual: NOT RUN.** No independent input session was exposed; no clicks/focus/Play changes were sent to the user's Unity. Exact residual human procedures are in the two reports. UI batch screenshot produced no usable artifact; no visual/manual PASS claimed. Coordinator review and integrated user evaluation remain required. 04 player acceptance stays PENDING with the reported repetition feedback; no invented KEEP.

Launch isolated editor using `bash Tools/launch-isolated-5051.sh`; choose `Gate C/Combat Lab` (three direct entries), `Gate C/City Foundations 05A` or `Gate C/City and Combat 05B`. On-screen launcher supports presets/StartingSide. `Assets/StreamingAssets/CityCombat05B/authored-ready.json` is explicitly authored (not earned) and loadable by the labelled inspection-fixture button.

Save/preference safety: distinct test and interactive preference roots, unique company/product, independent Library/Temp, separate05A/05B slots; old save namespaces preserved. All nine original scene/settings/persistence metadata files match preflight SHA256. The isolated worktree retains six untracked generated counterpart metadata files; these were not copied from or committed as the user's work.

Reports: `CITY_REGION_RESEARCH_FOUNDATIONS_05A.md`, `COMBAT_VARIETY_AND_CONNECTED_05B.md`. Stop coding here for coordinator review/user playtest. No further Civilization Depth, City/research systems, Dominion/Convergence or siege.

## 2026-09-30 — 50/51 acceptance audit and authorized integration

- Follow-up starts feature `d84571d`; main `develop @ 045707f`. Existing feature scene whitespace, ProjectSettings define delta, launcher executable bit and six untracked metadata preserved. User explicitly overrode pasted NO MERGE and closed both editors.
- Actual existing tests mapped to individual contract requirements; required coverage gaps found despite historical553 PASS. No numeric mismatch in the checked profile/spell/economy tables; exact adapters, untested branches and spell-Cover ambiguity recorded in `5051_ACCEPTANCE_AUDIT.md`.
- GUI: menu/Fire-vs-Ice launch, Ability selection, lawful self IceShield (Barrier10/Exhausted), AI Fireball with real damage, directed Freeze on FireTI observed through OS input. Full match, player FF/Burn and the rest of N/V are not passed.
- Reproduced unreadable Ability/preset labels and distant command controls corrected. Selected-spell confirmation and zero-damage status/protection preview wording corrected. No gameplay rules, profiles, RNG, save formats or economic tuning changed.
- Full final regression and exact source/config hashes are recorded in the manifest. Main integration keeps original product/save namespace and all nine unrelated files; test/feature preferences and saves stay isolated. User merge permission is not coordinator or player acceptance. NO PUSH. Stop for review; no next system.

## 2026-09-30 — 50/51 acceptance completion in authorized main

User authorized merging and checking main; follow-up starts `develop @73caac9` in `My project`. The feature was already integrated; no reset or duplicate implementation. Own final commit contains this delta (use live git log), no push.

N1–N5 and V1–V5 now **PASS as controlled real OS-input GUI checks**, with clearly labelled rare initial fixtures. Includes full05A Pressure victoryR35, Forge/departure/redeploy, capture/recapture, full05B eliminationR10 with150G newMage later fighting, paid75G same-ID training/reload, two battles withoutRefresh (HP17→31→38/Armor5; CloseHeal0→1→2), mirrored starts and all three Lab battles. This is not actual player balance acceptance.

Bounded fixes: Lab ordinary-unit names; readable City dropdown labels; explicit dynamic construction blocker; per-source Core production/export/Regional preview. No tuning, save-schema or combat-rule change. Targeted acceptance coverage closes identified reference/Institute/Forge/status/heal/training/AI-candidate gaps.

Fresh final **521 EditMode +64 PlayMode =585 PASS;0failed;0skipped**. Focused54EditMode+3PlayMode. Exact commands/config/source hashes/XML/logs and GUI replays: `Evidence/5051/validation-manifest.json`. Historical553 kept separately. Report: `5051_ACCEPTANCE_COMPLETION.md` supersedes old NOT RUN summaries in the original reports/audit.

User's nine unrelated scene/settings/meta files preserved byte-for-byte; temporary observer removed. GUI/batch preferences and saves separate from user namespace. Current main menus: Gate C/Combat Lab (Fire vs Ice, Support vs Fire, Mobile Blades), City Foundations05A, City and Combat05B. Authored-ready button remains labelled; genuine playedR10 save also supplied in evidence. Coordinator and player acceptance PENDING. STOP: no next package, NO PUSH.
