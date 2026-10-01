# 50+51 acceptance completion — 2026-09-30

Main `develop`, starting HEAD `73caac95ad7eeb4bb8e79b5b109d2d2ea81fa11c`. User explicitly authorized the main merge and subsequent main-project GUI work. Prior integrated feature commits and history retained; no reset/reimplementation or new package. Final commit is the commit containing this report (obtain with git log); no hash-only follow-up commit.

## Evidence method

All N/V actions below used macOS pointer/key input into the ordinary Unity HUD. The reused task-local observer exported state, replay and screen coordinates. Tactical control sometimes followed read-only TacticalAi/Pathfinder suggestions; these are agent-driven GUI controlled runs, **not a human user's balance acceptance**. The observer did not apply battle commands or inject results. Scene recreation was explicit. Rare initial states for cleanse, Freeze, Graceful Exit, training and two-heal encounters were authored and are labelled in their origin files. Those are not earned campaign history. Full05A/05B starts, construction, losses, payments and recruitment were played from new scenarios.

Snapshots, selected screenshots, replay verification and source hashes are in `Evidence/5051/manual-complete/`; exact index/provenance in `validation-manifest.json`. Large raw diagnostic traces remain under `/private/tmp/convergence-5051/manual-complete/results` and are not the sole evidence.

## Actual manual matrix

| Gate | Result and observations | Evidence prefix |
|---|---|---|
| N1 | PASS controlled05A match: paid DevII, Forge Organization and Forge; full Hotseat fight 10 rounds/222 commands; 8 dead,1 escaped. Return R12 without free healing, Commanderless roster. West legally reached Pressure24 atR35. Opponent intentionally inactive after the fight: this is no competitive balance verdict. | n1-, n4-05a-paid |
| N2 | PASS: actual battle casualties; paid100G HW replacement, later battle; returned living HW Armor0/HA Armor0; paid20G quote grants8/2 Armor only after full following Refresh. HP recovery is separate Keep40%. Save/reload order; another paid quote cancelled by ordinary departure without refund; repaired same-ID survivors enter another actual battle. | n2-, n4-05a-forge |
| N3 | PASS: East physically captured West Minor; pending paid DevIII steps stayed0 while Regional0; Gold source stock30→60. West recaptured, backlog+fresh90 Gold exported but Regional40 only; project completedIII; later capture left completedIII intact. | n3- |
| N4 | PASS: actual05A save/recreate/load during paid construction/research, Forge and HW recruit; identical hashes and visible HUD; completion grants once, no duplicate ID or repair. | n4-05a-, n2-complete |
| N5 | PASS short mirrored East-first alternative: Extraction→Depot→DevII; boosted Gold45/source contribution15 and overallRegional45. Forge mattered inN2. Institute economic value was not player-evaluated; accounting/construction checked in focused tests. | n5- |
| V1 | PASS controlled GUI Lab: PlayerEast vsAIWest Fire/Ice, 4 rounds/45 commands; IceShield10, directedShard, AIStream/Fireball, Burn, legalWest win. Separate GUI FF cast damaged ownTII32→16 and alliedTI26→10. | v1- |
| V2 | PASS: Support-vs-Fire full3round/30command GUI fight; heal26→40. Authored cleanse20→34 and Burn2→0/Armor16 unchanged; EWII11+6 reducesArmor16→0/HP40→39, Graceful Exit suppresses only attacked enemy, other enemyOA triggers. Freeze denies nextactivation/ends normally and breaks on directFire damage. | v2- |
| V3 | PASS fresh05B: paidDevII/TowerI/Drills; actualTII death, CommanderEscape, returnKeep,150G newTI `duel-West-recruit-1`, save/reload pending order, recruit deployed nextbattle. Secondbattle7rounds/146commands; East legalelimination victoryR10; bothCommanders dead, persistentCommanderless. | v3- |
| V4 | PASS labelled initial training fixture: missing confirmation rejected,75G paidonce, separate150G recruit, recreate/load with depletedFireball2; TI ID1 becomesTII once, ID2 recruit once. Two GUI-resolved battles inR1: CloseHeal0→1→2, allyHP17→31→38/Armor5; exact save/reload between; only completedRefresh resets budgets. | v4- |
| V5 | PASS short East-first Fire vs WestIce mirror: paidTowerI/Drills, R6 contactEast6/West4. MobileBlades PlayerWest-vsAIEast full8round/93command win, two usefulHeal casts, legal approach/flank/OA; replay exact. | v5- |

## Reproduced fixes

- Combat Lab ordinary HW/EW/HA no longer inherit legacy fixture names such as W HA-Left. Returning to a normal fixture clears Lab naming mode.
- Construction, Research, Mage and trainee labels/confirmation have readable contrast.
- Current Core blocker is explicitly shown for automatically paused construction; paid progress retention is stated.
- Source panels now show production/reference/export reasons and City projected per-source Regional. Values come from a detached Core production preview; no Presentation economic truth or state mutation.

No combat stats, school choices, economic numbers, recovery rates, RNG policy, replay/schema format or old scenarios retuned. Source projection uses the existing production algorithm; tests assert nonmutation and backlog accounting.

## Observations and limitations

Near-contact Fire/Ice and Support Labs produce useful spells in round1; they remove the strategic/research wait. Full-field05B includes substantial approach movement around the central obstacle but already has useful attacks in round1; no round was purely approach/pass in its second battle. Fire/Ice and MobileBlades also had attacks/casts in every round; Support ended with an evacuation/pass-only round3. Exact command/round counts are in `battle-observations.json`. Casting does not by itself prove combat pacing balanced. AI actually used elemental attacks/AoE; Freeze was manually exercised but no claim that AI chose every ability in these runs. All kit actions have legal AI candidates checked separately; AI preference tuning remains a playtest observation.

LostTII and150G replacement have visible cost; replacementTI has noFireball, same persistent death remains. Forge delayed repair changed laterArmor; Keep waiting healedHP but did not repairArmor. Investment and optional fight choices were controlled, not randomized balanced comparisons. No first-mover/turtling verdict. Graybox panel remains long and scrollable.

## Regression / safety

Final measured suite is recorded below and in the manifest. Historical553 on a82e98e is retained with original provenance, never relabelled as this run. Temporary observer removed before tests/final commit. Original nine files preserved by SHA256; Unity-normalized local scripting defines restored to their captured value after testing. Main product remains DefaultCompany/My project; GUI tests use `/private/tmp/convergence-5051/manual-complete/preferences`, batch tests `/private/tmp/convergence-5051/preferences`. User save namespace untouched. NO PUSH.

## Audit gap closure and exact semantic boundaries

| Entry audit gap | Follow-up evidence |
|---|---|
| Reference coupling, source preview/backlog, full Food chain | `ChangedReferenceYieldChangesProductionWithoutInventingMoreRegional`, `SourcePreviewUsesActualProductionWithoutMutationOrBacklogRegional`, `FullFoodChainStopsProductionUntilActualSupplyCreatesRoom`; N3 actual capture/recapture |
| Disconnected Minor payment | `DisconnectedMinorPaysOnlyItsOwnStock` distinguishes local recipe debit from ordinary global production credit |
| Deep row matrix / unavailable context / Institute stacking | Eight `DeepAccountingRowsAreAccountingOnly` cases; `InstituteReplacementDoesNotStackAndUnavailableCityKeepsPhysicalLoad` checks reservation, unavailable pause, completion, rejected duplicate activation/no charge, retained physical load and reload |
| Contributor joins near completion / Universal eligibility | `NewlyEligibleCityContributesOnlyRemainingResearchWork`: real work4 then context-enabled secondCity, final credits4.5/1.5, no overrun/duplicate; Universal eligibility independent of racial context. Multi-City ownership is labelled rule fixture, not new City capture gameplay |
| Forge dead/recruited identity and extra damage | `ForgeQuotedIdentityDoesNotTransferToRecruitAndExtraDamageDoesNotIncreaseQuote`; N2/N4 actual quote/repair/departure/redeploy/load |
| Burn reapply/cap/death/retaliation | `BurnReapplicationAtCapRefreshesTicksWithoutImmediateTick`, `BurnDeathSkipsDeadActivationAndPreservesArmor`, `FireArmorRetaliatesOnceAfterPhysicalHitEvenWithoutBarrierAndDoesNotRecurse` |
| Freeze denial/end/zero direct damage | `FreezeActuallyDeniesActivationThenExpiresWithoutBonusTurn`, `FailedDirectedContactDoesNotBreakFreezeOrSpendAnotherRoll`, existing positive-damage break test; V2 actual denial/expiry/break |
| Fireball corner/scaling | `FireballCannotPropagateThroughSealedCorner`, three `TierTwoScalesOnlyDirectMagnitude` cases; existing unscaled shield/Burn fixtures |
| Exertion / ordinary commands / EW repeated exit | `ExhaustionAllowsOrdinaryStaffDefendAndSpellButBlocksExertion`, `GracefulExitAfterMissIsConsumedAndSecondExitCanProvoke`, prior other-enemy/OA tests and V2 |
| All AI kit commands / purity | `EveryKitActionHasLegalAiCandidatesWithoutStateOrRngMutation`, existing AI choice/replay/invalid preview tests; actual elemental/AoE/heal/shield choices in GUI replays. No assertion that the heuristic prefers every spell in every situation |
| Heal invalid/dead/full/cleanse and two actual encounters | `HealRejectsFullDeadHostileAndEmptyTargetsWithoutMutation`, `FullHpCleanseUsesOneBudgetAndLeavesArmorAndOtherConditions`, `CloseHealBudgetAndDamagedArmorCarryThroughTwoResolvedBattlesWithoutRefresh`, V4 two completed GUI battles |
| Training no healing/cancel/Commanderless/reload / recruit fights | `TrainingPromotionItselfKeepsIdentityXpAndCurrentPools`, `PaidTrainingCancellationAndCommanderlessPauseSurviveReload`; V3 earned recruit later fights, V4 paid training/reload completes once |

Reviewed numbers remain the tables in the original audit: no numeric deviation found, and this follow-up does not retune them. Owner01 says Facing/Cover remains source-governed; 51§6 specifies directed Accuracy−Dodge−applicable frontal, without Bow penalties. Current directed spell behavior follows that specification; no new Cover roll or Bow penalty was invented. Solid/corner propagation and existing Bow Cover regressions remain tested. Nonzero persistent Ward/Barrier and live cultural erosion are absent from the authorized scenario: they are not silently claimed as playable features.

Existing adapters remain: final integer magnitude truncation, profile-backed permanent school/selected spell, temporary shield slot with zero permanent Ward/Barrier maxima, simultaneous-death mutual elimination, explicit old replay incompatibility, schema4 foundations. No new adapter/rule exception was introduced here. Manual fixtures supplement rare state setup only.

## Reproducibility, observed failures and launch

Initial focused compile attempt failed because this Unity NUnit lacks `NonParallelizable`; removed that unsupported test annotation. A first invalid-heal test used a nonadjacent target; corrected its test geometry. The final added tests passed without gameplay corrections. GUI observer initial fixture used an inaccessible serializer helper; replaced it with explicit initial DTO construction before the run. A late EndActivation snapshot arrived after the driver's immediate assertion; the command had applied, verified visually, and the same battle continued. None of these was hidden as a green run or blamed on the user's input. Unity restart was requested by the user; no user save was loaded/overwritten.

Open `My project` with Unity6000.6.2f1; stop Play before choosing a menu entry:
- `Gate C → Combat Lab → Fire vs Ice (near contact)`
- `Gate C → Combat Lab → Support vs Fire (near contact)`
- `Gate C → Combat Lab → Mobile Blades (near contact)`
- `Gate C → City Foundations 05A`
- `Gate C → City and Combat 05B (Fire vs Ice)`

The ordinary launcher exposes controller/preset choices. `Assets/StreamingAssets/CityCombat05B/authored-ready.json` and its labelled load button remain an **authored inspection state**. Additionally `Evidence/5051/manual-complete/v3-earned-ready-save.json` is a real GUI-earned R10 strategic save afterTII loss, paid infrastructure and150G replacement recruitment, before the replacement's battle. It is evidence/ready-to-load Core format, not silently installed over a user's manual slot. No new file-browser/save-profile system added.

Coordinator acceptance and PLAYER ACCEPTED remain PENDING. Controlled technical checks do not constitute a user balance verdict. No new package follows this report; NO PUSH.

## Final measured test results

- Focused EditMode: **54 passed,0failed,0skipped**, `EditMode-20260930-105047.xml`; focused PlayMode **3 passed,0failed,0skipped**, `PlayMode-20260930-100427.xml`.
- Full final combined implementation in main: **521 EditMode +64 PlayMode =585 passed;0failed;0skipped**. EditMode `20260930-105150`, PlayMode `20260930-105331`; exact commands, source/repo XML+logs and timestamps in the manifest. No gameplay/source change after these runs.
- Historical full553 is preserved as historical evidence, not counted again. Earlier focused48 is superseded by54 after six targeted acceptance cases.
- Final source/config SHA256 provenance covers128 files. Unity normalized local defines to APP_UI_EDITOR_ONLY during compilation; after tests the exact original SENTIS_ANALYTICS_ENABLED;APP_UI_EDITOR_ONLY file was restored unstaged. RPG sources do not depend on either package define; all nine protected originals match their preflight hashes.
- Existing01/03/04/05A, tactical replay, invalid/preview/RNG and WP-03E retreat tests remain in the full run. All exported GUI replays verify exactly; training/recruit/Forge and between-battle snapshots retain exact load hashes.

50: IMPLEMENTED, automated PASS, controlled N1–N5 PASS. 51: IMPLEMENTED, automated PASS, controlled V1–V5 PASS. No remaining required GUI check is NOT RUN. User balance acceptance and coordinator acceptance remain PENDING; Institute gameplay value and heuristic use of Freeze are explicitly not established by these checks. Recommended next step: coordinator review and one integrated user playtest, not another implementation package.
