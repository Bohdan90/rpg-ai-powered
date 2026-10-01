# Gate C — Milestone 3A: ZoC, opportunity attacks, physical retreat and outcome

2026-09-21. Pure Core implementation; Unity remains **6000.6.2f1**. No package changes.
Starting point: 2B.1 `578605e`, followed by the user-approved compact-path correction
`a9aa029` (129 tests green). This milestone does not introduce Presentation, AI or new units.

## Files and architecture

Added, with required Unity meta files:

- `Assets/_Project/Core/ZoneOfControl.cs`: pure geometry and ordered reactor query.
- `Assets/_Project/Core/OpportunityAttackPreview.cs`: immutable path exposures and responder availability.
- `Assets/_Project/Core/BattleOutcome.cs`: immutable tactical outcome value; ongoing or victory/defeat with reason.
- `Assets/_Project/Tests/OpportunityAttackTests.cs` and `RetreatOutcomeTests.cs`: focused contract tests.

Changed:

- `UnitProfile.cs`, `UnitState.cs`, `BattleState.cs`: explicit melee eligibility, OA availability,
  startup/activation refresh and outcome snapshot copying.
- `Battlefield.cs`: bounded side-specific retreat-edge query.
- `BattleResolver.cs`: shared contact/damage routine, stepwise OA/escape execution, outcome evaluation,
  automatic advancement after the current actor's removal, terminal command rejection.
- `BattleEvent.cs`, `BattleResult.cs`: prototype-specific events and terminal-command error.
- `Pathfinder.cs`: stops route expansion after entering own retreat edge and rejects terminal-battle
  queries; retains cost → line deviation → turns → ordinal direction tie-break.
- `Tests/BattleTestFixtures.cs`: snapshots now include OA availability and full outcome.
- `Tests/ActivationTests.cs`: the dead-unit skip scenario keeps a surviving opponent so it tests
  activation skipping rather than attempting to continue a completed battle.
- `Tests/DeterminismTests.cs`: command recording stops when Core reports battle completion.
- This implementation report.

No Presentation scripts, scene assets, profile tuning, ranged Cover calculations, assemblies,
Unity settings or packages changed. No generic reaction/event framework or service infrastructure.

## ZoC and response order

HW and EW have `HasMeleeBasic`; HA does not. `ZoneOfControl.Exerts` uses eight-neighbour
Chebyshev adjacency and the existing melee solid-corner check. Solid destination cells do not
receive ZoC. Occupying bodies are not extra ZoC blockers. ZoC does not spend or change resources.

`Sources(state, threatenedSide, cell)` returns active enemy melee units exerting ZoC, including
those whose OA is spent. `Reactors(state, mover, from, to)` returns only available enemies whose
ZoC contains the departure cell but not the arrival cell, for a statically legal step.

**P — interpretation of “current initiative priority”:** reuse the existing battle priority:
Initiative descending, persistent seeded tie key, stable numeric UnitId. It is not a fresh roll,
not dictionary order and not a rotation based on the current queue cursor. Thus seeded activation
priority also determines simultaneous responses; ID remains the final tie-break. No extra RNG draws.

Active melee units start with one `OpportunityAttackAvailable`; their own activation refreshes it.
An OA consumes it even on miss or Guard success. Normal Action, Movement and Guard do not spend it.
Dead/Escaped units cannot react; HA availability is false.

## Stepwise execution and preview

The full explicit path is validated before copying state or drawing RNG, including its tail beyond
an eventual escape: adjacency, bounds, solids, occupancy, diagonal corners, initial Movement budget.
Invalid commands return the original state, no events and unchanged RNG.

For each validated step:

1. Get ordered available responders for this particular departure/arrival pair.
2. Before each OA, recheck mover survival and responder activity, geometry and availability.
3. Emit exit/trigger/spent events, spend OA and call the shared ordinary contact → eligible Guard →
   resistance → Armor → HP routine. Coefficient is 1.0; there is no ranged Cover for melee OA.
4. Resolve against the mover's **pre-step facing**. Never turn the responder, consume its Action,
   apply Steady Aim, or recursively invoke movement/reactions.
5. If dead, emit interruption and stop. No triggering-step cost or later responder spend.
6. Otherwise move, pay exactly one Movement, record spend, set step facing and emit StepMoved.
7. On entering the mover's own retreat edge, mark Escaped and stop the remaining path immediately.

Hit, miss and successful Guard all allow a surviving mover to continue. Earlier executed steps are
retained if a later OA kills it. **P — resource history:** Dead/Escaped records retain their unspent
Movement as history so an unexecuted step is never charged; inactive status and cleared Action/OA
prevent spending it. EndActivation continues to discard resources normally for living active units.

`OpportunityAttackPreview.Query(state, MoveCommand)` validates first and returns zero-based step
indices, from/to cells and immutable `OpportunityThreat` records. `AvailableNow` reflects the input
snapshot; `WouldReact` additionally accounts for earlier hypothetical expenditures along this path.
Later exposures are conditional on survival, never predicted hits. Even spent sources can appear
with false availability so UI can explain them. Preview stops at own-edge escape, uses no RNG and
mutates no state. HashSet is used only for membership, never response ordering.

## Retreat and battle completion

West escapes by entering x=0; East by entering x=12, at any valid y. Merely occupying an initial
edge cell does not cause spontaneous escape. Production fixtures begin outside their own edges.
The opposite edge remains ordinary terrain. HP/Armor are preserved exactly as they stand after OA;
escape frees occupancy and removes the unit from activation and targeting.

`BattleState.Outcome` is separate from the existing command `BattleResult`:

- Ongoing: no winner/loser; reason None.
- Completed: VictorySide, DefeatedSide, reason Withdrawal or Eliminated.

After an actual removal command, if allies of the removed side remain, battle continues. If its last
active unit escaped, reason is Withdrawal; if it died, Eliminated. Earlier evacuees remain Escaped/Safe.
Commander ID/profile has no special defeat behavior. Both sides empty is a scenario exception, not Draw.

**P — fixture compatibility:** existing one-sided geometry/unit fixtures remain executable, with no
invented victory at startup over a side never fielded. Completion follows an actual last-unit removal.
A one-sided fixture whose last unit escapes raises the both-empty scenario error; the source snapshot
is unchanged. This does not affect the Gate C two-sided fixture or add a production game rule.

**P — lifecycle integration:** when the current actor dies or escapes and the battle remains ongoing,
Core emits ActivationEnded and starts the next eligible unit itself. A completed battle retains
`CurrentUnitId` as the historical last actor for the existing thin view, but `Outcome.IsEnded` gates
all subsequent commands and path queries. No further activation is started. M3B must display outcome
instead of presenting that ID as a playable activation. This avoids changing Presentation in M3A.

Events added: ZoCExitDetected, OpportunityAttackTriggered, OpportunityAttackSpent,
OpportunityAttackResolved, MovementInterruptedByDeath, UnitEscaped, BattleEnded. Existing roll,
Guard, damage, loss, death, facing and movement events describe the details. BattleEnded embeds the
value outcome (winner, loser, reason); separate redundant Victory/Withdrawal events are unnecessary.

## Validation

**158 EditMode + 11 PlayMode = 169 passed, 0 failed, 0 skipped.**
Forty new focused Core test cases were added to the 129-test compact-path baseline.
Runs used Unity 6000.6.2f1 in /private/tmp/gate-c-m1-4dkkwcy6; no C# compiler warnings/errors.
The full diff and new files were inspected; no generated folders are included.

Previous tests are preserved; only the
activation/replay fixtures described above were adapted to respect the newly executable terminal state.
Existing Core architecture tests and source inspection protect Unity independence.

Coverage includes: all three ZoC profiles, eight neighbours and solid corners, enter/stay/exit,
pre-step resolution, hit/miss/Guard behavior, Action separation, one OA and refresh, inactive responders,
multiple priority-ordered responders and stop-on-death, old facing and no responder rotation, preview
availability across repeated exits, invalid full-path atomic rejection, no ranged Cover in melee OA,
physical escape for both sides, wrong-edge nonescape, pool preservation, freed occupancy/activation
skip, partial/Commander retreat or death, last-unit withdrawal/elimination, safe earlier evacuees,
death before the safety step, later-step interruption accounting, post-escape preview termination,
terminal query/command rejection, scenario errors and identical movement/OA/escape replay events/state.

No mouse-driven M3A validation is claimed: this milestone is Core-only. Existing PlayMode integration
is run as regression coverage, not as an OA/retreat UI acceptance test.

## Limits and exact next step

No combat contract deviation for the two-sided Gate C fixture. The explicit prototype interpretations
above cover response ties, inactive resource history, synthetic fixture compatibility and lifecycle.
OA uses the same existing combat math; Light Cover −15 pp and distance tuning remain unchanged.

Milestone 3B should expose these Core facts in the current thin graybox:

- show ZoC sources and remaining OA;
- annotate the exact previewed path with per-step responders and explicitly confirm exposure;
- mark West/East retreat edges and warn when the path terminates in Escape;
- show interrupted movement, OA rolls/damage, removed units and automatic actor advancement;
- show Victory/Withdrawal/Eliminated from Outcome and disable gameplay commands when ended.

Do not calculate these rules in MonoBehaviours. Do not begin AI, deployment editing, telemetry,
spells, morale/capture, strategy consequences, persistence, campaign or final UI in this milestone.
