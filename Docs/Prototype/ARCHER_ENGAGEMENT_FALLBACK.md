# Gate C — Archer engagement fallback (Part A)

Baseline: 0009533 (documentation after gameplay 3bb1164). User-approved P/T experiment,
not final class/weapon canon. Read Drive checkpoint 43 in full; 02 Human Archer and
04 action/OA sections. Checkpoint 43 left point-blank behavior OPEN; this task explicitly
chooses the fallback. 02 global Steady Aim range behavior is not rewritten: Gate C's
previously approved Accuracy-only variant remains a local prototype override.

## Boundary and implementation

`BattleResolver.IsArcherEngaged` reuses `ZoneOfControl.Sources`. Hostile active HW/EW
with valid melee contact lock the bow even with spent OA; allies, HA, Dead/Escaped and
sealed corners do not. `AvailableBasicAttack` supplies the HUD's choice.
`BasicAttackCommand.Kind` distinguishes profile Basic (HA Bow Shot) from Melee Strike.
A submitted Bow while engaged is rejected with Engaged without state/RNG mutation.
Melee Strike is legal only for engaged HA: Range 1, Physical damage 5, Accuracy 80,
ordinary Action, facing, Dodge/Frontal Evasion, Guard, resistance and Armor→HP routing.
No Aim, ranged Cover/distance penalty, special effect, status or weapon identity.
Profile `HasMeleeBasic` remains HW/EW-only for ZoC/OA; fallback never grants reactions.
Ordinary movement can incur OA, and bow availability recomputes after surviving exit.
The generic friendly-fire confirmation contract remains unchanged; no new targeting system.

Presentation selects the Core-recommended variant, labels Melee Strike and
“Bow Shot — unavailable: Engaged”, and shows existing Core attack math. There is no
weapon switcher or Presentation engagement geometry. The ordinary event sequence shows
contact/Guard/damage; preview identifies which Basic variant is being submitted.

## Files

Core: BattleCommand.cs, BattleResolver.cs, BattleResult.cs, AttackPreview.cs.
Presentation: BattlePresenter.cs.
Tests: new ArcherEngagementTests.cs + .meta; SizeExperimentPresentationTests.cs;
DeterminismTests.cs legal replay now chooses the Core-recommended Basic variant;
CoverTests.cs reverse-shot screener moved from x=7 to x=6 to isolate Cover from engagement.
Docs: local contract, working checkpoint and this report.

## Validation

13 focused engagement cases passed, including positive/negative sources, spent OA,
corner geometry, ordinary Guard/resistance/damage, no HA ZoC/OA after refresh,
state/RNG purity, disengagement, multiple sources and range restrictions.
Existing replay and Cover coverage preserved under the new explicit rule.
Full suite: **227 EditMode + 25 PlayMode = 252 passed, 0 failed/skipped**.
Results: /private/tmp/gate-c-fallback-edit.xml and gate-c-fallback-play.xml.

No map, army, profile tuning, RNG, pathfinding, OA or Retreat changes in Part A.

Mouse Play Mode: passed in isolated Unity copy on Field_19x13_ExpandedV2, using real
OS clicks and temporary scenario setup outside Git. EW moved into contact, HA preview
showed unavailable Bow + Melee Strike, and confirmed hit removed 5 Armor. EW exit
produced no HA OA. HA exit showed one risk, took ordinary OA (HP 28→21, Armor 4→0),
survived, and fired Bow without Aim. Range 10 + −30 pp distance + Light Cover preview
and the exposed/interior/sealed-wall LoS cases remained correct. Screenshot inspected:
/private/tmp/gate-c-fallback.png. No new technical issues found.
