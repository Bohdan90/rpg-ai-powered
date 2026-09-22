# Gate C — working implementation checkpoint

Updated 2026-09-21. Single implementation handoff; Google Drive thematic owners retain
priority for canon. Follow repository `AGENTS.md` and the efficiency protocol.

## Baseline

- Implementation: Part B siege-scale V2, committed with this checkpoint delta. Part A fallback: **5aff414**; prior bow baseline **3bb1164**. Live HEAD: `git rev-parse --short HEAD`.
- Gate C: Milestones 1–3B implemented; post-3B battlefield/geometry corrections and bow-envelope tuning, Part A engagement fallback and Part B siege-scale V2 implemented. Next step: user playtest; no further gameplay work authorized here.
- Unity **6000.6.2f1**, C#, URP, Rider; no editor/package upgrade authorized.
- RPG.Core is pure C#, independent of UnityEngine/Presentation. Core owns combat truth; views consume queries/results.
- Deterministic explicit RNG; same state/seed/commands replay identically. Queries/invalid commands do not mutate state/RNG. Movement costs and legality remain Core-owned.
- Last full tests: **240 EditMode + 26 PlayMode = 266 passed; 0 failed; 0 skipped**.
- No push performed. Check actual Git status before work.
- Launch: **Gate C → Play Tactical Graybox → Fixture → Field_19x13_ExpandedV2**.

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
- Fixtures: `Field_13x9_Control`, `Field_17x11_Expanded`, `Field_19x13_ExpandedV2`, `Siege_23x17_Tight`, `Siege_27x21_Roomy`, **`Siege_31x25_Medium`**, **`Siege_35x27_Large`**.
- Same 5v5 composition: HW-Commander, HW-Infantry, HA-Left, HA-Right, EW-Flanker per side. Static siege proxy only, no real siege mechanics. New maps share identical centered 11×9 fortress (24 solids) + one-cell solid/opaque moat proxy (32 solids; 13×11 outer envelope), with four three-cell crossings. West deployment unchanged at x=1/2; East inside; no combat retuning between maps.
- 19×13 solids (9,5)/(9,6)/(9,7); West/East retreat edges x=0/18. Ordinary field retreat rear edge; siege defender retreat full legal outer perimeter.

## Playtest findings, limitations, OPEN

- Field 13×9 too small; 17×11 still somewhat small; **19×13 currently feels good as a provisional successful size candidate, NOT final canon**.
- Siege 23×17 too small; 27×21 probably still somewhat small considering future moat/bridges/exterior terrain. 31×25 and 35×27 now available for comparison; no user verdict yet.
- Latest mouse checks: Part A fallback/engagement, no HA OA, risky HA exit restoring Bow; Part B both maps approach/crossings/flanks, ranged/Cover/blocked LoS, OA, physical defender escape, attacker non-escape and framing/focus passed. Earlier range 7/8/10 vs 11 and corner checks preserved.
- User reports Range 10 substantially better. Point-blank decision now explicitly prototyped: engagement locks Bow and enables weak Melee Strike; no weapon identity canonized. OPEN: user evaluation of fallback.
- OPEN: user comparison of 31×25 vs 35×27 with static moat footprint. No final size/balance chosen; proxy opacity is not a canon rule for real moats.
- Existing HUD: nonadjacent melee can say `corner: blocked` alongside correct `OutOfRange`; long fixture name clips in selector; detailed preview requires scrolling (zoom/focus available). Hover text may persist across fixture reset until next cell hover.
- No AI, real siege, campaign or other deferred systems authorized here.

## Detail references (read only when relevant)

[Contract](GATE_C_TACTICAL_PROTOTYPE_CONTRACT_v0_1.md) ·
[Archer fallback](ARCHER_ENGAGEMENT_FALLBACK.md) · [Bow tuning](ARCHER_RANGE_TUNING.md) · [Ranged geometry](RANGED_CORNER_LOS_CORRECTION.md) ·
[Field V2/melee](FIELD_V2_CORNER_CONTACT.md) · [Siege V2](SIEGE_SCALE_V2_EXPERIMENT.md) · [Size experiment](BATTLEFIELD_SIZE_EXPERIMENT.md) ·
[Path/Cover corrections](MILESTONE_2B_1_CORRECTIONS.md) ·
[3A Core](MILESTONE_3A_IMPLEMENTATION.md) · [3B Presentation](MILESTONE_3B_IMPLEMENTATION.md).
