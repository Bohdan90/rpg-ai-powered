# SPELL-UX-01 — document 51 §14

2026-09-30. Bounded targeting/input follow-up; no 50+51 reimplementation or balance changes.

## Baseline and authority

Main `My project / develop` was and remains `da9f776eafc9939ffe27bf1b042bf16c3043a562`. Read current48, latest44/46, Doc51 §§5–8/11–12/14 and task-local owner12 Silence restriction. Prior technical acceptance/N/V history is retained; historical585 is not this run. Positive user feedback is not blanket balance acceptance.

Implementation is isolated on `feature/spell-ux-01` in `Convergence/Spell-UX-01` because the main Unity/editor session was open. The user subsequently granted shared-input availability; only the separate Spell-UX-01 window was targeted. Nine main scene/settings/meta files are SHA256-identical to preflight. Their verified copies were used locally for scene/build/settings references and excluded from the commit. Saves/preferences/logs use `/private/tmp/spell-ux-01/{gui-preferences,test-preferences,logs,results}`, with separate Library/Temp. GUI confirmed persistentDataPath under the isolated home. Main integration/session reload is deliberately not performed while its active session must be preserved; this is a new follow-up branch, not a reopening of the completed 50+51 merge.

## Reproduced causes and changes

| Observation | Reproduction / classification | Correction |
|---|---|---|
| Fire Armor ally rejection | Core checked range0 before recipient; ally produced misleading OutOfRange. Self-only itself is lawful. | SelfOnly diagnosis before range; Self button/detail and one-cell envelope. Invalid attempt does not cast on self or spend. |
| Fireball ground/hover | Core already accepted empty centers. UI only prepared spell previews after a click; pointer motion showed movement/inspection. | Live Core footprint/blocked cells/affected allies; separate center envelope and radius1 effect. Empty/occupied center retained, no snap/extension. |
| Fire Stream complaint | Existing Core already permits eight length3 rays including `(3,3)`. Actual user coordinates were not recorded. Controlled off-ray, fourth-cell, solid and sealed-corner refusals reproduced as lawful geometry, previously poorly explained. | Exact ray and obstruction display; specific reasons. No invented diagnosis of user's particular rejection and no changed ray/movement semantics. |
| Repeated primary selection | Old ordinary path selected Basic/Move/staff and reset explicit spell after commands. | Fire/Ice hostile hover and ordinary attack click issue existing FireStream/IceShard CastCommand. Staff is explicit; specials override; ground remains Move and friendly units inspect. |
| Silence contract gap | No Silence condition existed in old Core despite §11/14 restriction. | Minimal source-owned tactical IsSilenced gate for Spell actions and replay snapshot/hash. No producer, duration, new ability, persistent ailment or numeric effect introduced. Staff/Move/Defend remain legal. Exhausted does not block ordinary primary spells. |

Core owns aim geometry, exact affected/excluded cells and ordered complete blockers; UI consumes them. Amber aim brackets, magenta footprint/center, red obstruction marks are accompanied by target/shape/distance/ally/blocker text. Inspection does not replace the explicit choice. Casts retain original Action, Exertion, Magic Power, damage, status, budgets and source-reset boundary. No profile, range, roster, economy, recovery or AI scoring was retuned.

During implementation GUI exposed loss of invalid Friendly Fire preview when leaving the board for its checkbox, causing layout shift. Clicked aim now stays fixed while moving to controls, until a new click/cancel/resolution/handoff. A late Fit-board refresh regression used previous-battle threat IDs; corrected by recalculating derived overlays before rendering. Both have focused regression coverage. No failure was suppressed.

## Automated evidence

**Fresh focused:59 EditMode +13 PlayMode PASS. Full:540 EditMode +69 PlayMode =609 PASS,0 failed,0 skipped.** Final counts/artifact manifest are in `Evidence/SPELL-UX-01/validation-manifest.json` (fresh local runs; source hashes bind results to committed implementation). Runner: `bash /private/tmp/spell-ux-01/run-tests.sh <EditMode|PlayMode> [filter]`; Unity6000.6.2f1, batchmode, EditMode additionally nographics, explicit testResults/logFile and isolated preferences.

- `SpellUxTests` (19 cases): Self refusal/no spend; all8 rays/third vs fourth; off-ray, solid, single/sealed corners; empty/occupied Fireball; center vs blast/FF; four directed/adjacent range edges; all-blocker collection; Silence vs staff/Move/Defend; Exhausted primary legality; state/hash and replay.
- `SpellUxPresentationTests` (5): actual scene/presenter defaults, one click/one journal command, explicit/staff/movement/friendly-inspection separation, hover exact footprint/inspection, click lock/leave-to-confirm, cancel/camera/handoff, replay.
- Existing CombatVariety*, CombatLabPresentation, CrossroadsPresentation and CityFoundationsPresentation focused coverage plus full regression retain AI/legal resolver, old scenarios, pools/source budgets, same-ID return and save/load checks.
- Development-only failures: test compilation used an unsupported NUnit containment form; one fixture placed an ally on a solid and one claimed an occupied Lab cell was empty. These tests were corrected. A test launch overlapping previous process shutdown was refused by Unity's project lock and produced no result; it is not counted. The reproduced Fit-board regression was corrected. All earlier logs retained under `/private/tmp/spell-ux-01/logs`.

## Real GUI evidence

User confirmed input was free. Mouse helper checks the exact Spell-UX-01 window; initial Lab button visibly changed scenario before further input. Temporary read-only observer and existing task-local driver recorded coordinates/state and clicked ordinary controls. No result injection or automatic win. Temporary observer removed before final regression/commit.

| Check | Result / evidence |
|---|---|
| Self-only Fire Armor | PASS: actual Fire/Ice Lab ally `(9,8)` reports Self only, not range; no new command/Action spend. `03-self-invalid`. |
| Empty Fireball | PASS: actual Lab `(13,6)` empty center, center range4/8, nine-cell radius1 preview, affected Ice unit, one cast. `04–05`. |
| Primary Fire/Ice / FF | PASS on explicitly authored four-unit initial fixture: diagonal `(8,8)→(11,11)` full3-cell FireStream, IceShard primary, R2 occupied-center FF preview/refusal, explicit checkbox then one Fireball. `06–12`. |
| Pointer transit | PASS: clicked empty Fireball target preserved while cursor crossed another cell toward Confirm; fixed FF checkbox interaction. `13`. |
| Stream refusal / obstruction | PASS in actual Support/Fire Lab: blocked `(12,6)→(9,9)`, off-ray `(10,5)`, fourth-cell `(12,10)` with full legal3-cell ray shown. `30–32`. |
| Specials/cancel/staff/ground/friendly | PASS on labelled Ice initial fixture: Freeze select/cancel; ground movement preview without cast; friendly inspection; legal move then explicit Staff; handoff clear; friendly IceShield preview/cast. `33–39`. |
| Existing AI | PASS: actual Fire/Ice Lab Player West vs AI East; AI moved and cast IceShard for **12 HP**, contact50<85; no new AI rule. `40–41`. |
| 05B battle→sameWorld→save | PASS: fresh normal Fire/Ice05B, West→6 / East→9, attack, Fireball, physical East Escape, return atR2 with all10 same IDs and exact HP/Armor; West caster use1 retained. Save→recreate→Load checksum identical. `20–24`. Short smoke, no full match/balance claim. |

Actual short exported replays verify deterministically, including14-command05B physical Escape and spell-use carryover. Optional Silence snapshot field defaults false for earlier files; no-Silence hash serialization remains unchanged, no format version bumped. Save schema unchanged. Preview/cancel/invalid checks compare authoritative state hashes including RNG; journal UI counters are not simulation truth.

See [evidence index](Evidence/SPELL-UX-01/README.md). Rare fixtures are labelled at initialization, not misrepresented as played history. Exact eight-direction and directed-range boundary matrices, Silence/Exhausted and sealed-corner cases are automated checks; the GUI walkthrough is representative and does not claim every matrix cell was manually played. N1–N5/V1–V5 were not repeated. Final camera-fit/readout and successive-activation primary clicks were also rechecked in GUI after the full suite (`50–53`), without further production-code changes. All17 accepted-baseline replay files (898 commands) were read-only reverified successfully on the final implementation: `accepted-baseline-replays.txt`.

## Launch and limitations

Open `Convergence/Spell-UX-01` in Unity6000.6.2f1. With that editor out of Play mode: `Gate C → Combat Lab → Fire vs Ice (near contact)` or `Support vs Fire (near contact)`. Fire/Ice primary: hover/click hostile; explicit special: button, hover/click cell, Confirm; Cancel restores primary. Empty ground in ordinary mode remains movement. `Gate C → City and Combat 05B (Fire vs Ice)` launches the affected strategic scenario. These menu entries are unchanged.

This follow-up is not yet loaded into the main open editor; main remains da9f776 with its original nine unrelated files. No active user session/saves were reset, no global kill/restart, no push. Graybox UI is text-heavy and large envelopes can be busy; subjective ease-of-use remains for a short user check. No new Silence source or full ailment framework is included. The unrecorded original Stream rejection remains unclassified beyond the controlled lawful analogues.

Stop for coordinator review and short user targeting/default-attack check. No broader implementation started.

## Local delivery

One own commit: `Spell UX: clarify targeting and make school spells primary`; final hash is the commit containing this report. Validated gameplay SHA256s are in the manifest. Remaining task worktree status (same nine protected dependency copies as main):

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

All nine originals and task copies match preflight SHA256. No own uncommitted implementation or temporary observer remains after the commit. Main stays `develop @ da9f776`; NO PUSH.
