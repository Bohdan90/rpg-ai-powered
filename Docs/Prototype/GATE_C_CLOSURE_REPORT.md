# Gate C — closure preparation

Status: implementation capabilities ready for user evaluation; **NOT a Gate C PASS**.
AI commit: `926e28c`. Telemetry/replay committed with this report; live hash from Git.
Baseline before this task: `fb95710`. No push.

## Implemented scope and evidence

- Player and minimal one-ply AI use the same Core resolver, legality and preview math.
  AI can control either selected side; Hotseat remains available. Evaluation does not read future
  rolls or advance RNG. Legal commands, deterministic decisions, kill/Defend/Retreat,
  engaged Archer fallback, safer OA routes and multiple activations are tested.
- Local JSONL export contains initial state/seed/deployment, config/controllers,
  attempted and successful commands, diagnostic events/resources and state hashes.
  Separate session metrics do not infer fun/depth. Debug load/verify reruns Core and
  identifies the first state/legality/RNG divergence without changing the live battle.
- Replay tests reproduce ordinary combat/elimination, interrupted movement/OA death,
  physical withdrawal, pools/dead/escaped sets, RNG progression and every state hash;
  preview/invalid attempts do not change successful-command truth. AI sequences replay
  on both provisional 9v9 baselines. Real JSONL disk round-trips cover the same flows.
- Field 23×17 and siege proxy 35×27 support 9v9 loading/reset, movement, combat and
  retreat. Existing corner/cover/action rules and all previous tests remain green.
  No real siege mechanism was introduced.

Previous full tests: **266 EditMode + 30 PlayMode = 296 passed; 0 failed; 0 skipped**.
Part A checkpoint: 257 + 28 = 285 passed. Part B adds nine Core replay cases and two
PlayMode transport tests. Test runner used Unity 6000.6.2f1 in an isolated project copy
with the same project sources/packages; generated logs and probes stay outside Git.

## Physical mouse validation

Fresh-launch isolated Unity checks passed. Part A: Player-vs-AI enabled by mouse on
both 9v9 boards; prepared ranged attacks, field OA exit, siege crossing/contact and
low-HP physical evacuation executed through Core. Part B: mouse Export/Load-verify
round-trips matched for actual Field_23x17 move (1 command), prepared OA (1), physical
Escape (4 including preceding activation ends) and Player-vs-AI shooting (10).
All retained the 18-unit roster until Escape removed a view. These were short scenarios,
not completed 9v9 battles. JSONL/footer/session files were inspected; OA/escape metrics
and Core event diagnostics were present. Output location on this machine:
`~/Library/Application Support/DefaultCompany/My project/GateC/Replays/`.
Manual artifacts stay outside Git; in-memory/disk tests also verify elimination and
OA-kill interruption. No broad combat-fun claim follows from these checks.

## Current prototype tuning

| Profile | HP/Armor | Move/Init | Accuracy/Dodge/Guard | Damage/Range |
|---|---|---|---|---|
| HW TI | 40/16 | 4/10 | 85/5/20 | 12/1 |
| HA TI | 28/4 | 4/12 | 80/5/0 | Bow 10/10; engaged Melee Strike 5/1 |
| EW TI | 32/6 | 6/14 | 85/10/0 | 11/1 |

Distance penalty `5×max(0,d−4)` pp; Steady Aim +15 Accuracy only without prior Movement,
committing remaining Movement. Valid hostile melee ZoC locks Bow; HA gains no ZoC/OA.
EW frontal evasion +15 Dodge; Light Cover −15 Accuracy; Defend 25% Physical Resistance.
AI weights/25%-HP evacuation are contract prototype tuning, not Morale or final canon.
Ordinary 23×17 and siege 35×27 remain provisional, with no further size iteration absent
new user playtest evidence. Synthetic 9v9 roster makes no strategic Capacity claim.

## DoD and remaining evidence

The requested technical capabilities exist and their automated checks pass: shared
resolver, legal AI, deterministic RNG, export/replay, both spatial baselines, 9v9 and
physical Retreat. **Strict contract §11 technical DoD is not yet fully closed**: every
fixture has not been played to a result or documented stalemate. Full-battle duration,
density, dominant openers and human prediction of tactical consequences still require
user playtest. Short prepared smoke scenarios prove integration, not combat quality.

Preserved findings: field 13×9/17×11 cramped; latest user choice is provisional 23×17;
siege 23×17/27×21 cramped considering moat space, latest choice 35×27. Range 10 felt
better; fallback is available for further user evaluation. No combat rebalance performed.

Known limitations: one-ply AI has conservative independent incoming estimates and no
coordinated tactics; it can make weak plans or stalemate. Healthy escape endpoints are
excluded from ordinary movement scoring; low-HP evacuation is explicit. HUD requires
scrolling for 18-unit queues/debug controls; large-board detail benefits from zoom/focus.
Replay is a versioned diagnostic format, not a durable save format or authenticated log.
Optional player decision-time telemetry is omitted. Existing corner-label/hover-reset
cosmetic issues remain. A Unity hot-reload UI artifact during manual setup required a
fresh Editor launch; fresh-launch integration was used for validation.

## Still deferred

Real Gates, Wall Platforms, elevation, ladders, Siege Towers, breaches, Structure HP,
real moat/bridge mechanics, racial siege systems; strategic AI, coordinated multi-ply
AI, reinforcements/reserves/strategic multi-army systems, deployment/roster editors, new
classes/tiers, spells/Ward/Barrier/Living Armor, Morale/surrender/capture, campaign,
persistence, economy, production art/animation/audio and production replay UI.

Next decision belongs to the user: play the current AI/hotseat 9v9 baselines, export
representative sessions, inspect tactical predictions and outcomes, then decide Gate C.
No automatic new combat features or progression to persistence.

Details: [AI](MINIMAL_TACTICAL_AI.md), [Replay/telemetry](TELEMETRY_REPLAY.md),
[Density geometry](ARMY_DENSITY_VALIDATION.md), [working checkpoint](GATE_C_WORKING_CHECKPOINT.md).

## Directional siege approach correction (after ec19d0b)

New user evidence invalidates 35×27 as an already-validated approach envelope.
Selected proxy: **39×37**, center (19,18), same fortress x14..24/y14..22;
moat outer envelope x13..25/y13..23. Four existing three-cell crossings unchanged.
All coordinates refer to cell centers, using Core Chebyshev distance.

| Deployment | Legal sector (3 deep ×9 wide) | Minimum wall separation |
|---|---|---:|
| West | x1..3, y14..22 | 11 |
| East | x35..37, y14..22 | 11 |
| North | x15..23, y33..35 | 11 |
| South | x15..23, y1..3 | 11 |

Reference firing cells are every fortress perimeter cell center, including openings;
these conservative measurement points do not introduce real Wall Platforms. Smaller
centered candidates 35×33 and 37×35 give minimum 9 and 10 respectively. 39×37 is the
smallest centered odd envelope satisfying the authored sector depth, rear retreat
cell and one-cell Range-10 buffer. This is an experimental authoring choice, not canon.

Deployment formation (depth from own edge, lateral offset from sector center):
Commander (2,0); Infantry (3,-2),(3,0),(3,2); HA (1,-3),(1,3),(1,0);
EW (3,4),(3,-4). Rotate by approach. Existing defender formation translated (+2,+5)
from 35×27 unchanged. Six selectable fixtures: Siege_39x37_{West,East,North,South}_9v9,
Siege_39x37_WestEast_18v9 and Siege_39x37_NorthSouth_18v9. Paired formations do not
overlap; all 27 figures deploy simultaneously. No reserves or strategic roster logic.
Side.West denotes the attacker coalition, not its deployment direction; HUD queue
labels each approach, Army 2 has distinct IDs/names. Each attacker keeps its own
RetreatEdge; Core resolver, pathfinder, OA preview and AI use the same unit-aware query.
Defenders retain full legal perimeter. Replay v2 includes this field/hash and explicitly
rejects old v1 exports. AI scoring/combat rules unchanged; controller may select either
side to validate attacker AI.

Automated: **280 EditMode +30 PlayMode =310 passed; 0 failed/skipped**. Fourteen focused
Core cases cover all sectors/pairs, clearance, old proxy identity, deterministic spawn,
physical direction-specific escape + replay, crossings and legal AI approaches.
Existing PlayMode fixture/reset test now covers all fifteen fixtures, including 27 views.

Mouse validation: all six original directional deployments loaded; legal approach moves
and attacker-coalition AI advances executed in each. W+E and N+S displayed 27 figures,
kept distinct sectors/queue approach labels; N+S export/load regenerated matching replay.
Prepared west crossing traversed moat/opening (12,18)→(16,18); all four crossings and
directional physical retreat/replay also pass Core tests. Fit/Focus were exercised.
Overview is readable for formation geometry; individual labels require existing zoom.
A 27-unit queue requires scrolling. No completed-battle duration/empty-turn verdict;
that remains user playtest evidence, not inferred from this smoke check. Unity Editor
Search indexing/AI subscription messages occurred outside project runtime code.
