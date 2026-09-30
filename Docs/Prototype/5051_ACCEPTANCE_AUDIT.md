# 50 + 51 acceptance audit — 2026-09-30

## Scope and provenance

Audit starts at feature/50-51-connected `d84571dc8dc11a2c119045c8d8ae514ae2c8e2d4`. Main checkout starts at develop `045707f81847fb88ac34c724d5f87ce30fba5c39`. Read live reports/checkpoint/manifest, document 50 §§11–12 plus addendum and document 51 §§12–13; checked relevant numeric contract sections against source.

The user explicitly overrode NO MERGE after clarification: “Да, объединить после проверки”. This authorizes delivery to the main checkout, **not coordinator technical acceptance or waiver of N/V**. Both packages remain PARTIAL while required coverage/manual gaps below remain. No new package, rules or tuning.

Original full regression was genuinely run against gameplay `3d58f35` (not against a later UI fix). The committed XML SHA256 values match the historical manifest: 489 EditMode, 64 PlayMode, zero failed/skipped; UTC 2026-09-30 03:44:09–03:45:24 and 03:46:22–03:48:08. Its 48 package EditMode cases and 3 package PlayMode cases were individually enumerated and assertions read. A successful test name is not evidence for unasserted parts of its title.

## Requirement-to-evidence audit

Names below refer to tests in Assets/_Project/Tests (Presentation tests in PlayMode). PARTIAL means some required branches lack direct assertions; it does not assert that the corresponding implementation is broken.

| Requirement | Concrete existing evidence | Remaining gap |
|---|---|---|
| 50 output normalization, treasury/backlog, fixed parents | CityFoundationsTests.OutputCreditsOnceAndRegionalDoesNotCountTreasury; CapturedSourceKeepsParentAndStockWithoutForeignRegional; BoosterStartsFollowingProductionCycleAndCountsActualRate | No independently varied reference-yield coupling fixture. |
| Storage, capture, fractions | SaturatedSourceStopsOutputWithoutOverflowOrDiscard; FractionalProductionSurvivesStorageSaveAndRegionalAccounting; MinorCaptureRetainsPaidProgressAndDropsOnlyUnpaidQueue | Source saturation covered; isolated Minor payment and full downstream Food saturation not explicitly asserted. Capture tests call the controlled Core seam, not a played capture. |
| Paid construction, pauses, completed levels | ConstructionPaysOnceNeedsSubsequentCyclesAndPausesOnActualLoss; StartValidationUsesCurrentProjectionNotOldCycle; DeepReservationSurvivesPauseAndCancelReleasesOnlyIncrement | No complete occupied/context-change and repeated Institute replacement scenario. |
| Deep accounting | DeepAccountingKeepsPhysicalLoadWhenContextIsLost verifies capacities 0/0/4/6, wing IV2, increment, reservation overflow, Walls0, RIV affinity77 | Does not actually change an existing capability's context, despite the title; full row matrix not directly asserted. |
| Research | ResearchSingleActiveActualProvenanceAndNoDoublePaymentOnRestart; ResearchLastTickSharesOnlyAcceptedWorkAndContextIsExplicit | Partial-tick calculation uses a static two-contributor fixture. No City joining a live nearly-complete project or explicit universal-trace assertion. |
| Forge | ForgeQuoteCompletesOnceOnlySameAliveIdsAndDoesNotHealDirectly; ForgeDepartureCancelsWithoutRefundAndNoFieldArmorRepair | Quote, delay, one settlement and departure covered. No quoted character dying/replacement or extra battle damage before fulfillment; HP not asserted by the first test. |
| Economic save/load | SaveRoundTripAllEconomicStateAndContinuesDeterministically; CorruptLoadRejectsWithoutChangingLiveWorld; CityUiPaidQueuesSaveRecreateLoadAndContinuation | Construction/research and Forge continuation covered; not before/after **every** economic boundary. PlayMode recreation case is 05B, not the specifically required manual 05A. |
| Elemental/physical, contact, FF, geometry | CombatVarietyTests.StreamBypassesArmorAndStopsAtThreeCells; FriendlyFireRequiresExplicitConfirmation; StreamSealedCornerAndWallBlockPropagation; PositiveDirectElementalDamageBreaksFreezeAndBarrierAbsorbsFirst; CombatVarietyPersistenceTests.FreezeMissStillCommitsBudgetAndExertionWithoutSecondRoll | Stream sealed corner covered, not Fireball propagation around corners or directed spell Cover. No nonzero persistent Ward/Barrier exists in authorized profiles. |
| Magic Power | OldProfilesAndNewKitAreSeparate checks MP1.15; FF test checks TII Fireball16 | No direct assertions for every scaled/unscaled magnitude. |
| Burn | HealClampsAndCleansesBurnFirst; BurnTicksTwiceNotOnApplicationAndCannotBreakFreeze | Ticks test seeds stacks directly. Application cap/reapplication, Burn death and nonrecursive Fire Armor retaliation lack dedicated assertions. |
| Freeze | PositiveDirectElementalDamageBreaksFreezeAndBarrierAbsorbsFirst; BurnTicksTwiceNotOnApplicationAndCannotBreakFreeze; FreezeMissStillCommitsBudgetAndExertionWithoutSecondRoll | FreezeDeniesOnlyUntilEndOfNextActivation only checks applied command/use counter; does not assert expiry. Zero damage/no bonus activation not directly asserted. |
| Exertion, shields | ExhaustionBlocksExertionButNotOrdinarySpellOnNextActivation; ShieldExpiresAtSecondSubsequentActivationAndDoesNotRepairArmor; ShieldReplacementClearsFireRetaliationAndDoesNotStack | No complete ordinary staff/Defend/Reaction restriction matrix. |
| EW II | DualHitStopsAfterLethalFirstAndOaRemainsSingleHit; GracefulExitDoesNotSuppressSecondEnemy; DualHitUsesTwoContactChecksButOaUsesOne | Two hits, first lethal and other enemy OA covered; repeated re-entry/second exit and miss qualification not directly asserted. |
| Healing/budgets | HealClampsAndCleansesBurnFirst; SourceBudgetIsValidatedBeforeCommit; SourceBudgetsOnlyResetAtGlobalRefreshNotHandoffOrSave; TwoActualBattlesNoRefreshCarryIdentityDamageAndSourceUseWithoutTemporaryEffects | Two-battle test is Fireball, **not Close Heal**. Full/invalid/dead heal and healing-budget continuation lack dedicated assertions. |
| AI/replay/purity | Three ControlledLabAiMatchCompletesLegallyAndReplays cases; SpellAiUsesResolverWithoutPreviewMutation; PreviewAndInvalidCastPreserveStateAndRng; SpellSequenceReplaysBudgetsAndStatuses; existing retreat regressions | Aggregate cast count/replay does not prove every new action was usefully chosen; no coverage claim for each AI decision branch. |
| Recruitment/training | PaidMageRecruitRequiresVacancyDirectionAndGetsUniqueIdentity; TrainingKeepsSameIdAndCurrentPoolsNoImmediateCompletion; TrainingPausesAwayAndDeathCancelsRatherThanReplacingIdentity | Training HP assertion is only <=32, so no-heal not proved. No training cancel/Commanderless/reload continuation matrix, or paid recruit later fighting in the scripted full match. |
| Connected new combat | Two ControlledMirroredMatchPaidEconomyRealBattlePersistentResultAndLegalEnding cases | Real battle/casualties/same IDs/World return/legal ending are automated. They do not exercise new Mage recruitment then deployment, or substitute for N1/V3 mouse matches. |
| Presentation | ThreeDirectEntriesLaunchAndSpellCommitsThroughCore; CityUiPaidQueuesSaveRecreateLoadAndContinuation; AuthoredReady05BStateAndVisualEvidence | Calls presenter methods, not OS input. Last test does not assert screenshot content/existence; no visual PASS inferred. |

## Numeric and semantic comparison

Checked profile chassis, 11+6 dual hit/OA11, spells 10/6/14/12/10/14, ranges3/8/8/4/6/1, MP1.00/1.15, Burn3×2×2, budgets2/2/3; economy starting300G/30W/20I, Food36/90, supply30/30, production30/6/6/4, storage3×/4×, recipes/durations, Research2/6 and6/12 Work/40G, Mage costs100/120/150/75, load27/32 and recovery15%/40%. **No numeric mismatch found in those checked tables.** This is not a claim that untested semantics all passed.

Explicit adapters retained: integer final spell magnitude truncation (TII Stream11/Shard13/Fireball16); profile-backed permanent school/selected spell; package-local temporary shield slot with zero permanent Ward/Barrier maxima; mutual elimination for actual same-command deaths; explicit rejection of old tactical replay configuration; schema4 for foundations. These were already documented, not newly introduced tuning. Excluded cultural context/deep capabilities remain rule fixtures, not implemented campaign systems.

Actual acceptance deviations: incomplete required automated branch coverage listed above; incomplete N/V manual evidence; ability label contrast/discoverability defect reproduced in GUI. No blanket “no deviations” claim. Directed-spell Cover has no dedicated test and source currently applies Accuracy−Dodge−frontal without a Cover term; contract §6 specifies applicable contact semantics but does not separately fix spell Cover. No probability change is made on this audit; coordinator/owner clarification may be needed before claiming full geometry coverage.

## Reproduced UI issue and bounded correction

OS HID input opened Gate C and Fire vs Ice. Ability dropdown contained IceShard/IceShield on the active IceMageTI. The default label was dark against the dark panel, and ability/confirmation controls were separated by scenario launch controls. Corrected label contrast for Ability/05B preset fields, moved Ability beside active-unit information and command controls before scenario launch controls; confirmation names the selected spell. Status/protection previews no longer misleadingly display “Magnitude 0”. No combat/Core/tuning changes.

Screenshots and actual N/V results are recorded in the validation manifest. Initial PostToPid click did not open menu; existing HID posting did. Two-display coordinates were verified. An intervening user resize/input invalidated the next targeting attempt; no shield cast is inferred from that click. User then explicitly released input for checks. No generic desktop harness or direct result injection was used.

## Manual remainder and acceptance boundary

Keep N1–N5 and V1–V5 individually reported; prior automated controlled matches and authored-ready fixture remain explicitly non-manual. The procedures in both package reports remain developer-owned checks, not a demand that the user execute every one. The user's next playtest is assessment of the combined experience, not a substitute for engineering coverage.

Main integration must preserve original scene/settings/six .meta byte-for-byte and original product/save namespace. Feature retains isolated product/preferences. No push. Integration authorization does not change 50/51 PARTIAL status or invent player acceptance.

## Actual follow-up manual results

The user closed both editors, then explicitly released input. GUI access **was available**, so the historical “no independent GUI” reason is no longer the current blocker. Reused the existing stage-F mouse helper with only isolated-window selector/HID posting; no game-result injection or generic harness. Follow-up screenshots are in `Evidence/5051/manual-audit/`.

| Gate | Actual result in this follow-up | Exact remainder |
|---|---|---|
| V1 | **PARTIAL**: Gate C → Fire vs Ice via OS clicks; active IceMageTI Ability dropdown; self IceShield preview/confirm, Barrier10/Exhausted; Player East vs AI West; actual AI Fireball damages IceTII32→16 and IceTI26→20 after consuming its shield; actual IceTII Freeze preview85%, 0/2 then committed Freeze on FireTI. | Finish a real-input Lab battle, player area FF, Burn observations. No completed Lab victory claimed. |
| V2 | **PARTIAL**: actual Freeze cast and visible target FRZ state; not a complete V2 probe. | Support healing/cleanse, Mobile Blades exit beside second enemy, Freeze denial/break/expiry. |
| V3 | **NOT RUN** as manual. | Full05B match, paid new Mage later fights, casualty, return/spending, legal ending. |
| V4 | **NOT RUN** as manual. | Actual saved/recreated training/recruit/budget state; two encounters with healing-budget carryover, then Refresh. |
| V5 | **NOT RUN** as manual. | Mirrored presets/starting side and recorded strategic investment/pace observations. |
| N1 | **NOT RUN** as manual. | Specifically05A full match with paid construction/research, actual loss and legal ending. |
| N2 | **NOT RUN** as manual. | Paid Forge service and interruption, then another actual battle. |
| N3 | **NOT RUN** as manual. | Actual enemy capture/recapture pausing/resuming support; stocks and completed levels. |
| N4 | **NOT RUN** as manual. |05A recreate/load of nondefault paid services/projects/recruits and completion HUD. |
| N5 | **NOT RUN** as manual. |05A mirrored investment comparison. |

These unfinished gates are **not waived or declared blocked by unavailable GUI**. This follow-up fixes the reproduced input/readability problem and delivers the user-authorized main integration; it does not finish the broader developer acceptance matrix. The outstanding work remains with implementation/coordinator, not transferred wholesale to the player. Automated tests, authored snapshots, UI smoke and a partial battle are not complete manual matches.

## Final follow-up automated validation

Final gameplay **a82e98e6cdad582ea95d3cece78076d59a06c081**: fresh **489 EditMode +64 PlayMode =553 PASS**,0 failed/skipped. Focused CombatLabPresentationTests **1 PASS**. Commands and new XML/log paths are in the manifest; exact tested C# hashes included. No tests added or waived. Main delivery has identical gameplay sources; original product name/namespace and unrelated local settings are retained, so this is not claimed as a separate main-checkout GUI or regression run.

## User-authorized main integration

Two-parent merge includes feature `f3a3ccc`, tested gameplay `a82e98e`, into develop based on `045707f`. Both tracked project settings files retain their original baseline, allowing original local settings/scene and six metadata files to remain byte-identical. No foreign settings changes were committed as our work. Feature keeps its own isolated namespace; main keeps DefaultCompany/My project and existing saves, while05A/05B use distinct slots. No saves migrated or overwritten. Gameplay C# source hashes match the final tested manifest. This integration is authorized delivery for playtest, **not technical acceptance**; full manual and coverage gaps remain above.
