# Gate C — working implementation checkpoint

Updated 2026-09-21. Single implementation handoff; Google Drive thematic owners retain
priority for canon. Follow repository `AGENTS.md` and the efficiency protocol.

## Baseline

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
| HW | 40 | 16 | 4 | 10 | 85 | 5 | 20 | 12 | 1 |
| HA | 28 | 4 | 4 | 12 | 80 | 5 | 0 | 10 | 10 |
| EW | 32 | 6 | 6 | 14 | 85 | 10 | 0 | 11 | 1 |

- HA distance penalty: `5 × max(0, distance − 4)` pp. Steady Aim: **+15 pp Accuracy**, no Range increase; requires no prior Movement and consumes remaining Movement. Bow Range always 10 when available. Hostile existing HW/EW ZoC locks Bow even if OA spent; engaged HA gets Action-only Melee Strike (Range 1, damage 5, Accuracy 80), no Aim and no HA ZoC/OA. Surviving exit restores bow without Aim after Movement.
- EW Frontal Evasion +15 pp Dodge. Defend: 25% Physical Resistance, before Movement only; consumes Action/remaining Movement, expires at next own activation.
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
