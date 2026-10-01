# WP-03B — RETURN-RECOVERY-01

Starts only after green WP-03A commit `8ed1480253159ccb558e4648c3d38fcccd887b1b`:
350 EditMode +45 PlayMode PASS and actual application-restart save/load smoke.
Batch starting HEAD was `d91702937238cddb8c02b4c10b5137f5ac9ff20a` on develop.

Authority: latest44/46 batching append and explicit user contract; owner26
"PASSIVE HP RECOVERY RATE" / "BETWEEN-BATTLE DEFENSE RECOVERY" and Healing Building
baseline; owner17 Commanderless continuity; owner05 existing Provisions cadence.
No new rates, repair services, recruitment, settlement UI or economic system.

## Implemented behavior

- `PersistentFormation.cs`: same deterministic hundredths carry; pure HP preview;
  field15% and Healing Building40% refresh entry points. Cap Max HP, clear carry
  at cap, dead excluded. Armor, identities, statuses and progression untouched.
- `StrategicScenario.cs`: Mission01 Keep is the authorized friendly functioning
  Healing Building adapter. At the existing recovery checkpoint, after autonomous
  actors/battle aftermath and supply, physically present player formation gets
  Keep40% OR field15%, exactly once. Enemy proxy formations retain existing field
  behavior. No recovery on movement, Save/Load or tactical exit alone.
- `StrategicHud.cs`: expected HP after the next completed Refresh at this position,
  rate/context, no-Armor-repair warning and actors/supply time-cost warning. Actual
  per-character HP changes are recorded by the existing world event log.
- Returning does not clear retreat debt, Commanderless or roster lock, revive
  dead members, reset XP, replenish Armor/Ward/Barrier or add units. Field/Safe
  members use the same recovery arithmetic; no replacement identities.
- Existing supply stock/rates and A/B/Patrol cadence are unchanged. Waiting at
  Keep can lose Waystation or Village. An interrupted tactical encounter defers
  the checkpoint until the world phase resumes; repeated Load grants no extra
  recovery. WP-03A already stores position and fractional carry, so no new save
  schema or duplicated recovery state is needed.
- Waystation stays the current Mission01 supply proxy; the explicit package says
  every non-Keep position uses field15%. No Field Hospital rate is invented from
  owner26's still-tunable broader Minor Settlement catalogue.

## Tests / evidence

Isolated pinned Unity6000.6.2f1, copied repository Assets, external manual helper.

```sh
bash /private/tmp/wp01-20260928/run-tests.sh wp03b-focused EditMode -testFilter 'StrategicRecoveryTests|StrategicSaveTests|PersistentFormationTests'
bash /private/tmp/wp01-20260928/run-tests.sh wp03b-focused-ui PlayMode -testFilter 'StrategicRecoveryPresentationTests|StrategicSavePresentationTests'
bash /private/tmp/wp01-20260928/run-tests.sh wp03b-full-edit EditMode
bash /private/tmp/wp01-20260928/run-tests.sh wp03b-full-play PlayMode
```

Core coverage: exact15/40 and no stacking, cap/dead/Armor/Commanderless/Safe,
fractional carry across locations and save/load, battle-exit no-heal, return/redeploy,
withdrawal debt/physical placement, actor-phase pause/resume, no-mutating preview,
world threats worsening during Keep recovery. PlayMode uses actual ordered Core
combat for bridge+Area battles, returns damaged formation to Keep, saves/recreates/
loads, advances the same world to B emergence and redeploys with exact HP/Armor.
Focused results: **34 EditMode PASS** (9 recovery cases +21 save +4 existing
persistent-formation cases), **7 PlayMode PASS** (1 actual combat/recovery flow +6
save/load cases), zero failed/skipped.

### Controlled Unity mouse sequence — PASS

Seed20260929; actual OS clicks with Core AI suggestions as tactical driver, no
HP/result injection. This is technical validation, not human player acceptance.

1. Central bridge battle R1, then Area Guard battle R2. After Area Guard,
   baron-1=20/40 HP, baron-3=12/40 HP, both Armor0. All six stable IDs survived;
   Personal XP2.091 and Command XP1.792285… persisted.
2. Move18→10, complete one field Refresh: HP26 and18. Waystation Ravaged while
   the player is away; its supply remains unavailable. Return10→Keep costs95 Tempo,
   arrives R3 with Tempo5 and Provisions24. Arrival itself does not heal.
3. Save at Keep, Load through UI. Canonical checksum before/after equal
   `FvUTCmbGmRztU/SV6/n1QiVbkOmcb8tTvgcwqo/eS9c=`. UI correctly forecasts
   baron-1 26→40 (+14 capped), baron-3 18→34 (+16); no Armor repair.
4. End activation completes exactly one Keep Refresh: HP40 and34, both Armor0;
   XP/IDs unchanged. Supply consumes6 and refills6 (Provisions24). Patrol moves06→05,
   Incursion B activates at11 on Refresh4. World did not freeze for healing.
5. Redeploy Keep→Village12 for40 Tempo, arrives R4/Tempo60 with the healed HP and
   unchanged Armor/progression. UI switches to field15% preview. Portal remains
   uninvestigated: unlike the original Central rush, return/recovery deferred that
   objective while B's deadline continued. This is the visible opportunity cost.

Evidence under `/private/tmp/wp03-20260929/results/`:
`recovery-{bridge,area}-{start,end,world}.{png,json}`,
`recovery-keep-arrival`, `recovery-before-refresh`, `recovery-reloaded`,
`recovery-after-refresh`, `recovery-redeploy` PNG/JSON pairs. `recovery-manual.log`
retains unrelated Unity Search indexing/account startup warnings; no game
exception was observed. Manual helper is outside repo and removed before suites.
The manual disk slot now contains the damaged Keep/Refresh3 state from step3;
Load can revisit it, or Save replaces it.

Final fresh full regression: **359 EditMode +46 PlayMode =405 PASS**, zero
failed/skipped. EditMode ended 2026-09-29 15:34:38 UTC; PlayMode ended
15:36:07 UTC. XML/log artifacts: `/private/tmp/wp03-20260929/results/`
`wp03b-{focused,focused-ui,full-edit,full-play}.{xml,log}`. Tested Core/Presentation/
test source checksums: `wp03b-source.sha256` in the same directory. Existing
replay, preview, invalid-command and RNG regressions remain green. No new baseline
failures or game runtime errors were observed.

## Limits / handoff

Baron Keep is a scenario adapter, not a constructed building or new city feature.
No Gold, recruitment, resurrection, repair-service tuning, Ward/Barrier system,
Hotseat, economy or research. Existing WP-01 combat values and scenario enemy
composition remain unchanged. User-requested weapon-only icons were included in
WP-03A; no further visual redesign here.

Own coherent commit: `Strategy: add Keep recovery with world time pressure`;
exact delivered hash comes from Git/final coordinator report. Preserve all starting
scene/settings changes and the six unrelated `.meta` files. NO PUSH.
Remaining pre-existing working-tree entries (excluded from both commits and
verified unchanged against task-start SHA-256):

```text
 M Assets/_Project/Scenes/TacticalGraybox.unity
 M ProjectSettings/EditorBuildSettings.asset
 M ProjectSettings/ProjectSettings.asset
?? Assets/_Project/Core/PersistenceSliceScenario.cs.meta
?? Assets/_Project/Core/PersistentFormation.cs.meta
?? Assets/_Project/Tests/PersistenceProgressionTests.cs.meta
?? Assets/_Project/Tests/PersistenceSliceScenarioTests.cs.meta
?? Assets/_Project/Tests/PersistentFormationTests.cs.meta
?? Assets/_Project/Tests/PlayMode/PersistenceSlicePresentationTests.cs.meta
```

No contract deviation or unresolved game blocker was found. The additional weapon
icon correction came from the user's explicit in-task steering. Production save
migration, mid-battle resume and any recovery service beyond the approved Keep
adapter remain out of scope. Player balance/fun/return-decision acceptance remains
PENDING despite the controlled technical demonstration.

After this package: **STOP — WP-03C INTEGRATED PLAYER GATE requires actual user
playtest**. Technical controlled validation is not PLAYER ACCEPTED.
