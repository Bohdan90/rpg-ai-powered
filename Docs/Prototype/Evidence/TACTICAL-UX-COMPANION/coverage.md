# Task-local coverage and applicability

- Protection absent/present/damage/reset: `TacticalCompanionTests.TemporaryProtectionDisplayedAbsorbedExpiredAndResetWithoutFakeWard`; actual Fire Armor and Fire Stream, no fabricated Ward.
- Recipient expiry and missing event Target: `RecipientExpiryUpdatesUiAndDoesNotRequireAnEventTarget`.
- Both protection/HP deltas, failed contact/Guard: `ReadableEventsRetainActorTargetContactAndBothDamagePools`, existing `GrayboxPlayModeTests.AttackOutcomeHudDistinguishesContactGuardArmorHpAndSpillFromCoreEvents`, `EssentialReadabilityTests`.
- Fire footprint, allies, Ice reach/one target: `SchoolsKeepDifferentFootprintsAndFriendlyFire` plus retained SpellUx/FireTargeting geometry tests.
- Adjacent/self Heal: `HealApproachTests.AdjacentAndSelfRemainDirectAndSealedRecipientIsUnreachable`, retained self-heal SpellUx test.
- Full reachable plan / exact limit / insufficient Movement / pure preview: parameterized `PurePlanExactLimitAndRealMoveCast`.
- Action, budget, Silence, Exhausted, Freeze, movement and no useful effect: parameterized `BlockedPlanNeverMutates`.
- No adjacent cell: sealed-recipient test; no partial movement/order produced.
- OA fatal consequence: `ApproachOaMayKillCasterBeforeAnyHealAndReplayStaysExact`, existing resolver; seed search is test-only.
- Target invalidation after move: `DeathOrChangedTargetAfterMovementCannotCreateAFreeHeal` (controlled Core test; not a mouse claim).
- Two-click/target change/cancel/confirmed Move+Cast/replay/inspection reset: `HealTwoClicksChangeCancelUnreachableAndReplay`.
- Existing ordinary movement/approach/bow/selection: retained SpellUxPresentation/Graybox/Engagement suites and MeleeApproach tests; no new input resolver replaces them.
- New connected encounter resets feed: extended `CrossroadsPresentationTests.TacticalHotseatPhysicalEscapeReturnsSameRosterAndSaves`; existing return/IDs/save/determinism assertions preserved.
- Ward-only, elemental Ward→HP, persistent Barrier: N/A. No runtime fields/source/profile exists. No skipped NUnit case disguises this applicability boundary.
- Whole suite retains all prior strategic/tactical/persistence/legacy-replay tests, plus map08 and companion coverage.
