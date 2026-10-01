> Historical milestone report: bow range / Steady Aim range values below are superseded by [the 2026-09-21 bow tuning](ARCHER_RANGE_TUNING.md). Current range is 10; Steady Aim adds Accuracy only.

# Gate C — Implementation Milestone 1

Date: 2026-09-21. Scope: pure deterministic combat kernel and automated tests only.

## Authority and scope

Read the complete [contract v0.1](GATE_C_TACTICAL_PROTOTYPE_CONTRACT_v0_1.md)
from [Google Drive](https://drive.google.com/file/d/1j2gtEK8ay620ZEr433OwtWXCKW6fkCH6/view).
The local contract is an exact UTF-8 text copy (30,493 bytes); its rules were not edited.
The explicit implementation request selects C#/Unity over the TypeScript recommendation
in contract section 13 and restricts this work to section 14, step 1.

## Files

Relative to the Unity project root (`My project/`):

- Added `Assets/_Project/Core/UnitId.cs`, `GridPosition.cs`, `Facing.cs`, `Side.cs`.
- Added `Assets/_Project/Core/UnitProfile.cs`, `UnitState.cs`, `BattleState.cs`.
- Added `Assets/_Project/Core/CombatRandom.cs`, `BattleCommand.cs`, `BattleEvent.cs`,
  `BattleResult.cs`, `AttackPreview.cs`, `BattleResolver.cs`.
- Changed `Assets/_Project/Core/AssemblyInfo.cs`: friend access for `RPG.Tests` to
  prepare internal spent-movement fixtures without implementing movement commands.
- Added `Assets/_Project/Tests/BattleTestFixtures.cs`, `BattleResolverTests.cs`,
  `ActivationTests.cs`, `DeterminismTests.cs`.
- Added matching `.meta` files for the 17 new C# files.
- Added the unmodified contract and this report under `Docs/Prototype/`.
- All three assembly definitions and Presentation are unchanged.

## Implemented architecture

`RPG.Core` contains ordinary C# values and state, three fixed TI profiles, one explicit
resolver, and its own PRNG. It has no UnityEngine dependency or presentation objects.
No services, interfaces for effects, event bus, ability framework, or persistence layer.

Public flow:

```csharp
var started = BattleResolver.StartBattle(units, seed);
var state = started.State;
var command = new BasicAttackCommand(actorId, targetId);
var preview = BattleResolver.PreviewAttack(state, command);
var result = BattleResolver.Apply(state, command);
state = result.State;
```

`StartBattle` assigns keys and begins the first activation. `Apply` returns a new state
and read-only structured events on success. Invalid commands return the identical input
state, a typed error, and no events. Source units and previous snapshots are not modified.
State is publicly read-only; internal mutation is limited to Core and its test fixtures.
`FindUnit` returns null for an unknown ID. Malformed initial fixtures throw argument errors.

Only `BasicAttackCommand`, `DefendCommand`, and `EndActivationCommand` exist.
End optionally accepts a final facing and starts the next activation. Basic Attack and
Defend consume Action but do not automatically End. Movement remaining and historical
movement spend are separate fields. There is no Move command or movement API.

Combat is: validate -> spend Action -> face target / apply eligible Steady Aim -> contact
roll -> at most one frontal shield Guard roll -> integer resistance reduction -> Armor
then HP -> death. No damage variance, Crit, penetration, retaliation, Ward or Barrier.

The three profiles use all requested tuning values. Existing TI rules are explicit:
Archer Steady Aim adds 15 accuracy points and 1 range, and consumes remaining Movement;
Archer distance penalty is 5 points per cell beyond 4; Elf frontal evasion adds 15 Dodge
points. Defending grants 25% Physical Resistance and expires at the next own activation.
Damage uses integer floor. Side/rear attacks receive no damage bonus.

Events carry kind, round, actor/target IDs, relevant amount/before/after values, and separate
contact/Guard chance and roll values. `DamageApplied.Amount` is post-resistance damage;
`ArmorLost` and `HpLost` contain actual clamped losses. `ActivationStarted.Amount` is the
refreshed Action count (1), and `After` is refreshed Movement. Initial seed and per-unit tie
keys remain in state. Events explain execution; they are not a generalized replay system.

## P — PROTOTYPE ASSUMPTION: local executable details

These details supplement, rather than revise, the contract:

1. PRNG is xorshift32 with shifts 13/17/5 and an explicit uint state. Seed zero maps to
   `0x6D2B79F5`. Percentage rolls are 0..99 and succeed strictly below the chance.
   Values below 96 are rejected before modulo 100 to avoid bias. A rare rejection may
   advance the generator more than once for one logical roll; only the logical roll is
   an event. This algorithm/zero mapping is part of the current deterministic format.
2. Unit IDs are positive explicit integers. At battle creation, units are canonicalized
   by ascending ID and each receives one random uint tie key, including any inactive
   fixture records. The same PRNG then resolves combat. Priority is descending Initiative,
   ascending tie key, then ascending ID. Keys never reroll between rounds. Hash functions
   exist only for value equality/collections and are not used for seeds or tie-breaks.
3. Coordinates use positive Y north, with eight clockwise facings starting at North.
   Range is Chebyshev. Nearest-octant facing uses exact squared integer comparisons
   represented in decimal, avoiding floating-point angle boundaries. Nonzero integer
   coordinate vectors cannot lie exactly on an irrational 22.5-degree boundary.
4. M1 uses unbounded logical coordinates with no board, obstacles, screening, LoS or corner
   checks. It validates unit/Action/target/range and computes directional protection.
   This is an intentionally incomplete legality check until M2, not a claim that every
   in-range shot is legal on the future battlefield.
5. Isolated fixtures may contain only one active side. M1 does not declare victory or
   stop on side elimination: field-control outcomes belong to contract step 3. At least
   one active unit is required to start; dead/escaped records are retained and skipped.
   Dead positions are historical, not occupied cells. Escaped can be supplied as an
   initial fixture state; no escape command exists.

The contract already defines Defend resistance, frontal Guard, contact formula, final
facing choice and move-before-action as P/T; these are preserved rather than new local rules.
Self-attacks are invalid. Direct allied attacks require `FriendlyFireConfirmed = true`,
matching the contract's explicit hotseat/debug confirmation; there is no UI or AoE system.

## Verification

Unity **6000.6.2f1**, Unity Test Framework **1.8.0**, NUnit EditMode:
**57 passed, 0 failed, 0 skipped** (56 new parameterized cases + existing Core architecture test).
Tests ran on a temporary copy because the original project was open in the editor.
An initial test compile error using NUnit's string-oriented `Does.Not.Contain` was corrected
to collection membership syntax; the subsequent complete RPG suite compiled and passed.

Coverage of the 16 requested invariants:

| Requirement | Tests |
| --- | --- |
| Armor before HP | `ArmorAbsorbsDamageBeforeHp` |
| Defending reduces damage | `DefendingReducesTwelveDamageToNineBeforeArmor`, `ResistanceRoundsDownFractionalDamage` |
| Defend spends Action + Movement | `DefendConsumesActionAndAllRemainingMovement` |
| No Defend after movement spend | `CannotDefendAfterMovementWasSpentEvenIfRemainingWasRestored` |
| Defend expiry | `DefendingExpiresOnlyAtTheUnitsNextActivationAndResourcesRefresh` |
| Exactly one Action spent | `BasicAttackConsumesActionExactlyOnceOnHitOrMiss` |
| Dead unit skipped | `UnitKilledBeforeItsTurnDoesNotReceiveAnActivation`, `DeadAndEscapedUnitsAreSkippedWithoutChangingTheirPools` |
| HP and Armor stay nonnegative | `OverkillClampsHpAndArmorToZeroAndEmitsDeathOnce` |
| Invalid state/RNG unchanged | `InvalidCommandsPreserveEntireStateAndRng` (11 cases) |
| Deterministic replay | `SameInitialStateSeedAndCommandsProduceIdenticalEventsAndFinalState` |
| Preview/query preserves RNG | `RepeatedPreviewsAndQueriesDoNotMutateStateOrRng` |
| Guard stops damage | `GuardSuccessPreventsAllDamage` |
| Guard is a separate roll | `GuardUsesSeparateSecondRollOnlyAfterContact`, `MissHasNoGuardRollOrDamageAndConsumesOnlyOneRandomRoll` |
| No extra damage systems | `UnguardedAttackHasOnlyFixedPhysicalDamageAndNoRetaliation` asserts exact event sequence, fixed losses and no damage to actor |

Other focused coverage: profile tuning, Steady Aim, frontal protections, initiative and
seeded priority, resource refresh, final facing, explicit allied targeting, unique identities
and occupied initial positions, source-snapshot isolation, PRNG golden sequence, and safe
coordinate arithmetic. Core's compiled dependency test rejects Unity and Presentation.

Re-run in Unity via Test Runner -> EditMode -> RPG.Tests, or with this installed editor:

```sh
"/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -projectPath "/path/to/closed/project" \
  -runTests -testPlatform EditMode -testFilter RPG.Tests \
  -testResults /private/tmp/gate-c-tests.xml -logFile /private/tmp/gate-c-tests.log
```

The full suite was rerun after the repository-root correction: 57 passed, 0 failed,
0 skipped, including the Core dependency check. No gameplay source changed.

Evidence from this run: `/private/tmp/gate-c-m1-4dkkwcy6/TestResults.xml` and `tests.log`.
Do not open a second Unity process on an already-open project path.

## Known limitations and next step

- **Pinned Gate C editor: 6000.6.2f1**, explicitly approved after M1 acceptance.
  No downgrade or unapproved upgrade; package manifest and lockfile remain unchanged.
- Git now covers the Unity project root (`My project/`): Assets, Packages,
  ProjectSettings and Docs, including the tests, contract and this report.
  Original Core-only commits remain in history; infrastructure cleanup does not rewrite them.
  The accepted M1 baseline is committed locally; no push was requested.
- No movement, pathfinding, battlefield, LoS, ZoC/OA, Retreat Zone, field-control result,
  AI, UI, rendering, telemetry export, persistence or other deferred systems exist.
- This verifies combat execution, not playtest quality, tactics or class balance.

Exact next implementation step: Milestone 2, pure grid legality and movement tests.
Define board bounds/occupancy/obstacles and implement legal step/path validation, Movement
spend/history and facing, then range/LoS/corner legality through this resolver. UI/graybox
integration can follow those tests. Do not begin OA/retreat/AI as part of that first step.
