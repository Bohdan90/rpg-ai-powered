# Gate C — closure preparation

Status: **Gate C PASS for the current prototype scope** after user Player-vs-AI field
playtest. The user lost a normal engagement, considered AI behavior normal and reported
no critical tactical-loop blocker. Deterministic replay/technical DoD, field 23×17,
siege 41×39 and physical Retreat are accepted. This does not claim final balance or
close later content playtests.

Persistence follow-up: Gate C remains closed for its accepted prototype scope. The
only retained watch item is later-content passive/stalemate behavior: the deliberately
minimal roster has limited intrinsic reason for every unit to advance, while ranged
asymmetry already supplies some approach pressure. Future classes, magic, objectives
and richer kits should be playtested before adding any anti-stalemate rule. Retreat was
not attractive in the isolated Gate C finale because no persistent consequence followed
it; Persistence Slice v0.1 supplies the first reason to preserve damaged veterans.
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
Ordinary 23×17 remains provisional; new approach-buffer evidence supersedes siege
35×27 with the targeted 39×37 candidate described below. Neither is universal canon. Synthetic 9v9 roster makes no strategic Capacity claim.

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

## Ranged reach presentation correction

Cyan inset L marks show the active player's geometric Bow reach from its actual cell,
derived from UnitProfile.Range and Core GridPosition.DistanceTo. Green movement fill,
red ZoC borders and gold path remain independent. Solids inside reach can be marked:
this deliberately does not claim clear LoS or a legal target. Core preview and resolver
still decide eligibility. Engaged/Action-spent HA hides marks with a HUD reason; melee
units/AI turns show no Bow overlay. No destination planner or enemy threat map added.

Three focused PlayMode scenarios test range10/11, synthetic profile range7 (test-only
construction, no production tuning API), movement coexistence/update, solid LoS rejection,
state/RNG/hash purity, selection/cancel replay, engagement/fallback, spent Action, reset,
melee and AI visibility. Full final suite: **280 EditMode +33 PlayMode =313 passed;
0 failed; 0 skipped**. Part B modifies only Presentation/tests/docs; Core combat tuning
and real siege mechanics remain unchanged.

Final mouse checks: range10 preview/execution, range11 rejection, Cover + distance
penalty, Action-spent hiding, engaged Melee Strike, OA disengagement restoring Bow
marks, and post-selection/attack/OA export/replay all passed. Cyan marks were visually
inspected alongside movement and ZoC on field and siege boards. Prepared full-roster
west/east/north/south crossings all executed by mouse; north/south used free interior
cells (19,21)/(19,15), since deeper cells contain defenders. Core correctly rejected
the initial occupied target. Final HUD review corrected attacker rear-edge labels on
defender turns to list actual approach edges, covered by the existing fixture test.
Manual scenarios were short integration checks, not Gate C PASS or battle-duration
validation. New format v2 cannot load old v1 logs; new exports replay with matching hashes.

## User-requested 41×39 expansion

All six directional siege fixtures now use `Siege_41x39_*` names and a 41×39
board, replacing their 39×37 versions at the same selector indices. Center (20,19);
fortress x15..25/y15..23, moat envelope x14..26/y14..24. Fortress, crossings,
rosters and combat tuning retain their existing shape/values. Defenders shift
(+1,+1); attacking sectors retain their depth from their own board edge:
West x1..3/y15..23, East x37..39/y15..23, North x16..24/y35..37,
South x16..24/y1..3. Minimum wall separation is now 12 on every approach.
Earlier 39×37 validation above is historical; full-battle user evaluation remains open.

Validation: 14 focused geometry/AI/retreat/replay cases passed, then full
280 EditMode + 33 PlayMode = 313 passed, zero failures/skips, in an isolated
Unity project copy. PlayMode exercised selector loading, deployment, movement
and reset across all fixtures. No new mouse-driven or completed-battle playtest.


## Final selected Gate C siege baseline — 41×39

Starting HEAD: `462bd87` (the geometry expansion was already implemented).
The user selected **41×39 from manual playtest**, superseding **39×37** for the
Gate C siege spatial baseline. Ordinary field remains **23×17**. 43×41 was discussed
but was not selected. Battlefield-size exploration is closed again; this delta
validates the selected size and does not compare alternative maps.

Actual fixture measurements use `GridPosition.DistanceTo` (Core Chebyshev distance)
from every deployment-sector cell to the existing conservative fortress-perimeter
reference cells, including openings. Runtime probe and focused Core tests agree:

| Approach | Sector (3 deep ×9 wide) | Minimum wall separation |
|---|---|---:|
| West | x1..3, y15..23 | 12 |
| East | x37..39, y15..23 | 12 |
| North | x16..24, y35..37 | 12 |
| South | x16..24, y1..3 | 12 |

Each sector holds its original nine-unit army. W+E and N+S each hold 18 attackers
plus nine defenders simultaneously, with distinct cells and no sector overlap.
All spawns are walkable, outside retreat zones and outside the fortress/moat proxy
for attackers; defenders retain their legal interior formation. Minimum separation
**12 > Bow Range 10** on every approach; no real wall firing mechanic is implied.

Only validation and documentation changed in this finalization. Accepted cyan/blue
ranged reach and green Movement visuals are unchanged, including engagement/spent
Action hiding. Combat tuning, Range 10, Movement, AI weights, geometry semantics,
Retreat edges, moat/crossings and simultaneous army support are unchanged. No real
siege mechanics, reserves or reinforcement waves were added.


Mouse validation in a fresh isolated Unity Editor: all four single approaches and
both paired deployments loaded with 18/27 unit views respectively. Physical mouse
movement/confirmation and attacker AI advances succeeded in all six variants.
W+E and N+S retain separate, visually understandable formations on opposite sides;
nine-unit detail is readable with zoom. All four crossings executed in prepared
full-roster states: W (13,19)→(17,19), E (27,19)→(23,19), N (20,25)→(20,22),
S (20,13)→(20,16). North/South stop at free interior cells before defenders.

On the actual 41×39 board, prepared Range-10 preview/fire succeeded; Range 11 was
rejected, and engagement/spent Action hid the reach marks. Preview preserved the
runtime state hash and RNG. Cyan reach and green Movement were also visually
inspected on the full W+E roster: the initial archer's reach ends short of the
fortress. Focus, wheel zoom and Fit worked. N+S movement and prepared archer-fire
Export/Load-verify each reported matching replay (one successful command each).
Automated cases additionally cover directional physical Retreat and deterministic
replay. Temporary probes/scripts/screenshots/logs remain outside Git.

The technical mouse checks found no new map implementation problem. Overall
approach-distance/emptiness preference is established by the user's selection;
this short smoke check does not independently establish battle pacing or a
completed-battle result. Existing limitations remain: overview labels need zoom,
27-unit queues need HUD scrolling, long fixture names clip, and hover text can
persist across reset. Unity Editor Search indexing and AI account/subscription
messages occurred outside project runtime code. No new runtime exception observed.

Final verification: **14 focused EditMode + 4 focused PlayMode passed**; then
**280 EditMode + 34 PlayMode = 314 passed, 0 failed, 0 skipped**. The added
PlayMode regression covers all six selected siege fixtures, Range-10 cell sets,
Movement coexistence, preview/selection state/RNG/hash purity and replay after
accepted movement. Production code is unchanged from `462bd87`.

Return control to the coordinator for final Gate C playtest / PASS-FAIL decision.
This baseline finalization does **not** declare Gate C PASS. No push.
