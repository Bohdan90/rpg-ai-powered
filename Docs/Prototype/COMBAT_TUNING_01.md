# WP-00 + WP-01 — COMBAT-TUNING-01

Started 2026-09-28; completed 2026-09-29 (America/New_York). Scope: document 45 Phase 0 only.
Player balance acceptance remains **PENDING**. WP-02 has not been started.

## WP-00: live preflight

- Repo: `/Users/bohdanskrypka/UnityProjects/Convergence/My project`, branch `develop`.
- Starting HEAD: `d41b5d5a2d161f75f5009e5d5e4295cd7ff038de`.
- Unity installed and pinned: `6000.6.2f1 (770e33f6875c)`.
- No staged/unstaged tracked changes at entry. Six pre-existing untracked `.meta`
  files were preserved: Core `PersistenceSliceScenario`, `PersistentFormation`;
  Tests `PersistenceProgressionTests`, `PersistenceSliceScenarioTests`,
  `PersistentFormationTests`; Tests/PlayMode `PersistenceSlicePresentationTests`.
  Their SHA-256 manifest is in the evidence directory below.
- Existing: pure `RPG.Core` (`noEngineReferences: true`), Presentation, separate
  EditMode/PlayMode tests, tactical resolver/AI/replay, persistence and three-battle
  developer flow. No strategic graph/Tempo/Provisions/actor bridge found.
- Missing at entry: all WP-01 tuning values, prominent attack-result summary,
  representative combined-probability coverage and dedicated outcome UI coverage.
- Fresh baseline suites: **289 EditMode + 36 PlayMode passed**, zero failed/skipped.
  These runs used a fresh isolated copy of Assets/Packages/ProjectSettings before
  modifications, not historical XML. No baseline failures.

## Authority and implementation

Read current Drive 00 navigation/authority, 07/09 current deltas, 44 technical handoff,
45 in full, 46 working contract/WP-00/WP-01, task-local 04/15 protection rules and
30 Level-C risks. Document 45 Phase 0 governs the provisional numbers; no thematic
owner or Drive document was edited. Retain positional defenses and distinguish
readability problems from empirical balance claims.

| Profile | Accuracy | Base Dodge | Guard | Frontal Evasion |
|---|---:|---:|---:|---:|
| HW | 90 | 5 pp | 15% | 0 |
| HA Bow | 85 | 5 pp | 0 | 0 |
| EW | 90 | 10 pp | 0 | +10 pp |

`UnitProfile.cs` owns these values. HP/Armor/Movement/Initiative/Damage/Range are
unchanged. HA melee fallback remains Accuracy **80**, damage 5, Range 1, with no
Aim/ZoC/OA. Its old accuracy implicitly used the Bow profile; `CalculateAttack`
now keeps its accepted 80 explicitly so Bow tuning cannot silently strengthen it.
No probability semantics, RNG, AI weights, replay format, persistence identity or
field 23×17 / siege 41×39 geometry changed.

## Outcome correctness and readability

Inspection found no extra Dodge roll or routing defect. `CalculateAttack` includes
Dodge/Frontal Evasion in contact; `ResolveContactAndDamage` rolls contact once,
then at most one eligible Guard, then applies Armor followed by HP. Failed contact
does not roll Guard. Armor-only damage is a successful hit.

Previously the actionable message said only `BasicAttackCommand applied`; distinct
outcomes existed as technical event names at the bottom of a long scroll panel.
`BattlePresenter.Append` now derives a concise last-result summary only from actual
Core events, and `BattleHud` shows it near the active unit. Both HUD and detailed log
name failed contact, Guard blocked, Armor damage and HP damage. Spill retains both
pool deltas and before/after values; no invented causal Dodge explanation is added.
The summary survives activation changes and preview/rejections, and clears on restart
or loading a new persistence battle. It stores display text, not combat truth.

## Analytical probabilities (not observed frequencies)

`damage-producing % = ContactChance × (100 − eligible GuardChance) / 100`.
Ordinary Basic, relevant facing, no Cover/Defend/additional modifiers; HA no-Aim
cases use actual prior Movement. Contact clamps at 5–95 as before.

| Case | Contact | Guard | Damage-producing chance |
|---|---:|---:|---:|
| HW → HW frontal | 85% | 15% | 72.25% |
| HW → EW frontal | 70% | 0 | 70% |
| HW → EW flank | 80% | 0 | 80% |
| HA → HW d4 | 80% | 15% | 68% |
| HA → HW d4 + Aim | 95% | 15% | 80.75% |
| HA → EW frontal d4 | 65% | 0 | 65% |
| HA → EW frontal d4 + Aim | 80% | 0 | 80% |
| HA → HW d10 | 50% | 15% | 42.5% |
| HA → HW d10 + Aim | 65% | 15% | 55.25% |

The approximate 72.2/80.8/55.2 figures in Phase 0 represent these products. Tests
assert exact decimal products, without changing RNG to match rounded prose.

## Reproducible automated evidence

Evidence directory: `/private/tmp/wp01-20260928/results/`.
Isolated project: `/private/tmp/wp01-20260928/project/`.
Runner: `/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity`.

Commands used (replace `NAME` and `PLATFORM` using the table of runs):

```sh
Unity -batchmode -projectPath /private/tmp/wp01-20260928/project \
  -runTests -testPlatform PLATFORM \
  -testResults /private/tmp/wp01-20260928/results/NAME.xml \
  -logFile /private/tmp/wp01-20260928/results/NAME.log
```

EditMode also uses `-nographics`. Focused EditMode filter:
`RPG.Tests.BattleResolverTests|RPG.Tests.ArcherRangeTuningTests|RPG.Tests.ArcherEngagementTests|RPG.Tests.CoverTests|RPG.Tests.LineOfSightTests|RPG.Tests.OpportunityAttackTests|RPG.Tests.DeterminismTests|RPG.Tests.BattleReplayTests`.
Focused PlayMode filter:
`RPG.Presentation.Tests.GrayboxPlayModeTests|RPG.Presentation.Tests.EngagementPresentationTests|RPG.Presentation.Tests.RangedReachPresentationTests|RPG.Presentation.Tests.ReplayPresentationTests`.

Initial focused failures: three new probability fixtures inadvertently moved along
the Retreat edge (fixed by interior deployment); an old OA miss seed now hit under
the new chance (seed 1 rolls 61 vs new contact 70). The miss fixture now uses seed 5,
asserting roll 97 explicitly. No production behavior was reverted to satisfy a seed.

The first full EditMode run had one failure: the East siege test assumed EW #5
must move in its first activation. Diagnostics across two rounds showed HWs
advancing in round one and EW #5/#14 advancing in round two after first defending.
The regression now requires the same EW to approach within two own activations
and every command to remain legal. No AI policy or fixture was changed.
`siege-diagnostic` / `siege-rounds` are diagnostic runs, not acceptance passes.

| Run name | Scope | Passed | Failed | Skipped |
|---|---|---:|---:|---:|
| baseline-edit | Full EditMode, starting HEAD | 289 | 0 | 0 |
| baseline-play | Full PlayMode, starting HEAD | 36 | 0 | 0 |
| focused-edit-final | Focused EditMode | 135 | 0 | 0 |
| focused-play | Focused PlayMode | 27 | 0 | 0 |
| final-edit-verified | Full EditMode, final code | 298 | 0 | 0 |
| final-play-verified | Full PlayMode, final code | 37 | 0 | 0 |

Each row has matching `.xml` and `.log` in the evidence directory. Final full
results: **335 passed, 0 failed, 0 skipped**. Nine analytical cases and one UI
test were added; old tests remain represented. Full suites cover facing/Guard,
engagement/fallback, ranged penalties, preview/invalid command purity, deterministic
replay including OA/Escape, persistence, field and directional siege geometry.
These checks are **PASS** for the code committed with this report; locate the exact
implementation commit with `git log -1 --format=%H -- Assets/_Project/Core/UnitProfile.cs`.
No historical XML is used as current validation. `git diff --check` passed.

## Manual Unity smoke and limitations

Executed in the isolated Unity Editor, Hotseat, existing 13×9 control board, HW #1
at (2,2) facing East vs HW #7 at (3,2) facing West, HP 40. A temporary Editor
probe outside the repository configured initial states and scrolled the panel;
OS mouse clicks selected the target and pressed Confirm. No attack result was
injected. Screenshots were visually inspected against Core events/pools.

| Case | Seed / starting target Armor | Observed result | Manual status |
|---|---|---|---|
| Failed contact | 5 / 16 | No damage; HP40 Armor16 | PASS |
| Guard | 31 / 16 | Guard blocked; HP40 Armor16 | PASS |
| Armor-only | 1 / 16 | Armor −12 to4; HP40 | PASS |
| HP-only | 1 / 0 | HP −12 to28; Armor0 | PASS |
| Spill | 1 / 4 | Armor −4 to0 and HP −8 to32; both shown | PASS |

Evidence: `results/manual-0` through `manual-4`, each `.png` + `.txt`, plus
`manual-log.png`, relative to `/private/tmp/wp01-20260928/`. Readable HUD outcomes
and log records agree. `ManualProbe.cs`, `mouse.swift`, and `manual.log` there
document the setup. These are bounded mouse smoke checks, not a full player
balance playthrough. No new manual field/siege battle or persistence sequence
was run for this package; those regressions are automated.

Unity Editor emitted a SearchDatabase indexing exception and AI-generator
`NoSubscription` message in this isolated copy. Neither prevented rendering,
clicks or combat; no combat exception was observed. These environment messages
are retained in `manual.log`, not attributed to WP-01 Core.

## Delivery

Implementation **DONE**; automated validation **PASS**; required outcome mouse
smoke **PASS**; player balance acceptance **PENDING**. No unresolved contract
decision. Recommend coordinator acceptance of this technical package.
One local commit: `Combat: apply WP-01 tuning and clarify attack outcomes`.
Report/checkpoint and tested code are committed together; no empty milestone.
Starting/final tracked tree clean; the same six pre-existing untracked `.meta`
files remain byte-identical (SHA-256 verified). Push **NOT PERFORMED**.
Next candidate: WP-02 after coordinator acceptance; **not started here**.
