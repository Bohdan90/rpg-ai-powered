# Gate C — working implementation checkpoint

Updated 2026-09-29. Single implementation handoff; Google Drive thematic owners retain
priority for canon. Follow repository `AGENTS.md` and the efficiency protocol.

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
