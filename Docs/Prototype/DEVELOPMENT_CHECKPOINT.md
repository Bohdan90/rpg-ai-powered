# Gate C — compact development checkpoint

Updated: 2026-09-21. Local implementation snapshot; Drive remains authoritative for canon.
Read repository `AGENTS.md` for the user-approved efficient workflow.

## Verified baseline

- Repository: `/Users/bohdanskrypka/UnityProjects/Convergence/My project`.
- Latest gameplay commit: `3bb1164` — Archer Range 10, Accuracy-only Steady Aim.
- Unity `6000.6.2f1`, C#, URP. Core is deterministic pure C#; Presentation consumes Core queries/results.
- Last runtime validation: **214 EditMode + 24 PlayMode = 238 passed, 0 failed, 0 skipped**.
- No push performed. Inspect current Git status/HEAD rather than assuming this snapshot is still current.
- Launch: **Gate C → Play Tactical Graybox → Fixture → Field_19x13_ExpandedV2**.

## Current implemented behavior

Hotseat, activation/Action/Movement, deterministic RNG, readable deterministic paths,
Basic Attack, Defend, Armor → HP, facing/Frontal Evasion, unit Cover, ZoC/OA,
physical Retreat, Escaped/Safe and Withdrawal/Eliminated outcomes are implemented.

- HA: HP 28, Armor 4, Movement 4, Initiative 12, Accuracy 80%, Dodge 5 pp, Guard 0%, damage 10, Range 10.
- Steady Aim: +15 pp Accuracy only if no Movement spent; commits remaining Movement; never extends range.
- Distance penalty: `5 × max(0, distance − 4)` pp. Light unit Cover: −15 pp, directional, nonstacking.
- Melee diagonal contact: one open orthogonal side suffices; two solid sides block. Shared with ZoC/OA.
- Ranged LoS: solid interior blocks; single-corner/boundary-only touch is legal; shared vertex of two diagonally touching solids blocks.
- Movement remains stricter: either orthogonal side solid/occupied blocks a diagonal step.
- HW/HA Movement 4, EW Movement 6. Other tuning unchanged by the latest task.
- Bow tuning, distance coefficients and battlefield sizes are provisional, not final global canon.

## Fixtures and pending decisions

Available: Field 13×9, 17×11, 19×13; static siege proxies 23×17 and 27×21.
19×13 feels approximately suitable to the user as a provisional open-field candidate.
Its solids are (9,5), (9,6), (9,7); rear Retreat edges x=0/18; existing 5v5 deployment unchanged.
Siege sizes felt too small; no new siege-size experiment is authorized by this checkpoint.

Next design decision awaits user playtesting of the new bow envelope. Adjacent/point-blank
archer behavior is explicitly deferred: no minimum range, adjacent penalty, sidearm,
bow disable in ZoC or disengage mechanics have been added. Do not implement them unasked.
No AI, campaign, real siege systems or other deferred content is authorized here.

## References and limitations

- Local contract: `GATE_C_TACTICAL_PROTOTYPE_CONTRACT_v0_1.md` — read relevant sections, respecting Drive priority.
- Latest tuning and exact changed files: `ARCHER_RANGE_TUNING.md`.
- Geometry correction: `RANGED_CORNER_LOS_CORRECTION.md`.
- Last mouse validation covered distances 7/8/10/11, movement/Aim, long-range Cover, corner LoS, EW approach and 19×13 framing.
- Existing HUD limitation: nonadjacent melee preview may also say `corner: blocked` alongside the correct `OutOfRange`.
- Long fixture names clip in the dropdown; detailed preview requires scrolling. Existing zoom/focus controls remain available.

For a new chat: ask the agent to read `AGENTS.md` and this checkpoint, inspect current
HEAD/status, and perform the specific new task. Reuse relevant source documents rather
than copying the entire chat history.
