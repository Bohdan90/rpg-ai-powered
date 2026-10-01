# FIRE-TARGETING-02 — document 51 §16

2026-09-30. Implemented bounded follow-up; coordinator/player acceptance remains pending.
Starting main and feature base: `008e58f2a94a6eda0b79f25a2330ba5a3e64d482`. Worktree `Convergence/Fire-Targeting-02`, branch `feature/fire-targeting-02`. The containing local implementation commit is the delivery commit (`git log -1`); no push. `My project / develop` remains `008e58f` with its active editor preserved; this patch is **not integrated into main**.

Authority: current48/latest44/46 and document51 §16 (accepted, superseding historical v1 restrictions), affected51 §§5–7/11–12/14–15, task-local shared04/12 and44 LoS. No new balance or economy decision.

## Actual implementation

- **Stream:** cell-centered direction; Chebyshev rings1–3; integer nearest/half-away raster at each of3 steps. `(2,1)` gives `(1,1),(2,1),(3,2)`; `(3,1)` gives `(1,0),(2,1),(3,1)`. All48 nonzero offsets, mirrors/rotations and `(3,3)` supported. No cone/extra side cells. Units never stop the stream; caster excluded, one payload per affected unit.
- Obstruction follows the real straight line to the third-ring outer boundary, using rational open-cell intersections and sealed vertices. A solid omitted by raster samples still blocks. Single-corner touch remains legal; movement's stricter diagonal rule is not used. Same Core cells feed preview, execution and AI.
- **Fire Armor:** self or one active friendly of any current race/class, range3 ordinary spell LoS. Recipient receives6 Temporary Barrier and independent FireProtection; only caster pays Action/Exertion. Timer2 decrements at recipient activation starts, including both initiative orders. Recipient distance/caster Silence/death does not maintain or cancel it. Barrier depletion does not cancel fiery state; death/battle completion cleans protection.
- Fire Armor/Ice Shield share the recipient's package slot. `PackageBarrier` tracks its remaining share within total TemporaryBarrier; replacement/expiry removes only that share. An unrelated independent amount remains. Existing total-pool absorption consumes package share first; no independent-barrier producer, duration or new defense system was added. No caster-wide one-recipient cap.
- Existing qualifying positive direct Physical melee retaliation and survivor rule retained; no ranged/DoT/miss/zero/recursive trigger. No healing, Armor repair, stat, Magic Power, Burn, budget, roster, school or strategic tuning changes.
- **UI:** existing amber range outlines and magenta exact cells; actual friendly recipient, range and concrete blocker text; self/ally button label. Primary hostile-click spells, ordinary empty-ground Move/friendly inspection, explicit specials/staff, cancel/reset and single-action click retained. New rules no longer show obsolete self-only/eight-ray restrictions. A ring4 refusal reports range, without an invented additional LoS failure.
- **AI:** legal off-axis centers and friendly shield recipients through the same commands. Existing friendly-fire penalty and opportunity/replacement score retained; replacing a larger slot has no artificial positive grant value. Focused tests exercise actual `TacticalAi.Choose`, not only candidate availability. Retreat/detour behavior and architecture untouched.

## Replay / persistent save boundary

Replay container remains format2; config now explicitly identifies `spells2-firetargeting02`. Snapshot captures `fireRulesVersion=2`, package share and new effect recipient; Cast still records aim cell. Helpful commands record resolved recipient ID, verified on replay. Unknown/mismatched versions or changed recipient fail verification.

Missing snapshot version means **v1**, with recorded eight-ray/self-only rules and old state hashing, shield overwrite/cleanup behavior. No silent replay upgrade. Retained recorded v1 JSONL files are verified directly by the PlayMode test; originals unchanged. Current records reproduce new casts and physical Escape. Stable strategic save schema is unchanged: it carries permanent IDs, HP/Armor/death/XP/source-use budgets, not temporary battlefield shields. Subsequent battles use current rules; loading a strategic save does not replay an old battle or grant Refresh/healing/refill.

## Existing magic arithmetic (documentation, not redesign)

`SpellRules.Magnitude` uses decimal `base × MagicPower`, then C# integer cast (positive values truncate/floor). Fire HOM II: `10 × 1.15 = 11.5 → 11 Fire`. With TemporaryBarrier6,6 is absorbed and5 reaches HP; ordinary Armor is unchanged by this elemental payload. These profiles have no nonzero Ward or independent persistent Barrier layer. Physical damage instead uses remaining Armor before HP. A qualifying fire hit retains the existing30% Burn application; a Burn stack ticks2 and is not multiplied by Magic Power. Fixed Fire Armor grant stays6.

## Automated evidence

Fresh focused: **77 EditMode +6 PlayMode, 0 failed/skipped**. Full regression results and exact paths are in [validation-manifest.json](Evidence/FIRE-TARGETING-02/validation-manifest.json); source/config bytes in [tested-source-sha256.json](Evidence/FIRE-TARGETING-02/tested-source-sha256.json). Fresh full: **558 EditMode +70 PlayMode =628 PASS; 0 failed; 0 skipped**. No historical609 result is represented as this run.

Coverage: all offsets/third-fourth boundary; empty/occupied/through-unit/FF/exact damage; off-raster obstruction/sealed/single corner/map edge; recipient profiles/invalids/range/LoS; caster-only cost; both recipient initiative orders and independent source loss; depleted barrier/Burn/death/end cleanup; slot replacement both ways/independent share; AI actual choice; legacy/current replay, recipient identity, source-budget/persistence suites. PlayMode covers primary off-axis single click, selected ally/invalid enemy, exact UI cells, ordinary ground/ally inspection, special/staff/cancel/handoff and no state/RNG mutation. Existing focused/full combat tests retain melee trigger exclusions and no-recursion coverage.

Development failures were investigated: two old self-only/off-axis expectations are preserved as explicit **v1** tests; new v2 tests cover the accepted rules. One new-test compile assertion overload fixed before execution. Sandbox UPM socket/temporary project-lock startup failures are not counted as test passes. Final runs use the isolated permitted runner.

## Real GUI evidence (not autoresolve)

User granted free input for a separate window. Only `Fire-Targeting-02` was focused; main PID70828 was left open. Reused bounded OS-input helper, checked foreground identity and visible Lab-button response. Temporary observer exports state/coordinates and authors initial fixtures only; it never supplies a battle result. It was removed before final suites/commit. Initial fixture provenance and raw input observer/driver are retained in evidence.

| Check | Actual result |
|---|---|
| Default off-axis | PASS: authored FireII `(8,8)` → enemy `(10,9)`, preview `(9,9),(10,9),(11,10)`; one ordinary click, exactly one Cast, HP40→29. |
| Empty center / full beam / FF | PASS: empty `(10,10)` selects3 diagonal cells beyond center; ally `(9,9)` named. Unconfirmed click spends nothing; explicit FF confirmation deals11 once to ally. |
| Allied armor | PASS: select Fire Armor, target HW `(9,9)`, preview names HW; HW Barrier0→6, FireProtection timer2, caster alone Exhausted. |
| Trigger / expiry | PASS: enemy HW contact34<85, Guard31 fails15; damage12 consumes Barrier6 and Armor6; attacker gains Burn1. Recipient still fiery at Barrier0; first recipient start timer1, second start clears. |
| 05B smoke | PASS: ordinary new05B launch, West→Mine6/East→Ridge9, attack atRefresh2; Fire Armor on allied persistent member; Fireball after lawful Exhausted cooldown; five East units physically evacuate.35 successful tactical commands. Return preserves all10 IDs/pools and FireballUsed1, Refresh remains2; protection cleared. Save→scene recreation→load restores identical checksum. No authored world outcome or instant win. |
| Old replay compatibility | PASS:17 retained50/51 actual GUI replays verified; PlayMode also reads SPELL-UX-01 recorded replays. |

Screenshots: [off-axis](Evidence/FIRE-TARGETING-02/01-default-offaxis-preview.png), [empty aim / FF](Evidence/FIRE-TARGETING-02/03-empty-cell-full-stream-friendly-fire.png), [recipient](Evidence/FIRE-TARGETING-02/05-allied-recipient-preview.png), [melee trigger](Evidence/FIRE-TARGETING-02/07-melee-fire-trigger.png), [expiry](Evidence/FIRE-TARGETING-02/09-second-recipient-start-expired.png), [loaded05B](Evidence/FIRE-TARGETING-02/24-05b-recreated-loaded.png). JSON observations and new-rule replay files sit beside them.

The smoke initially attempted Fireball during the expected next-activation Exhausted window; it was legally refused and continued after cooldown. A read-only observer serializes null world snapshots as zero-filled JSON objects; the driver initially compared against that placeholder. Comparing real pre-battleRefresh2 against returnedRefresh2 resolves the instrumentation assertion; no game code or result was changed to pass it.

## Launch / isolation / limitations

From `Convergence/Fire-Targeting-02`: `bash Tools/launch-fire-targeting-02.sh`. Unity6000.6.2f1; then **Gate C → Combat Lab → Fire vs Ice** (or Support vs Fire/Mobile Blades). Existing near-contact/Hotseat/Player-vs-AI controls remain. For world smoke choose **Gate C → City and Combat05B** or the labelled in-game05B button. Spell buttons appear on the active mage's activation. Primary hostile click casts Stream; explicit Stream selects empty-cell aim; Fire Armor requires selecting a recipient then Confirm.

Launcher uses `/private/tmp/fire-targeting-02/player-preferences`; GUI validation used `gui-preferences`, batch tests `test-preferences`, each with distinct saves and logs. Main save namespace is untouched. Verified copies of the original scene/settings/six persistence metadata were used as worktree dependencies; SHA256 and originals retained under `/private/tmp/fire-targeting-02/backup`, never committed as own work. All nine main originals remain byte-identical. Unity normalized only the isolated ProjectSettings copy by removing SENTIS_ANALYTICS_ENABLED (actual tested define: APP_UI_EDITOR_ONLY); this dependency copy remains uncommitted, while the original main file and backup retain both defines. No reset/clean/stash/push, no forced main restart.

No unresolved gameplay contradiction found. Independent-barrier producer remains outside this prototype; preservation is covered with explicit Core fixture state. Graybox scrollable HUD and integer damage rounding remain existing limitations. Player acceptance of these new targeting rules is **PENDING**. Feature/main integration remains a delivery step; do not reload the user's live main party automatically. Stop after this package; no additional systems.

## 2026-09-30 — authorized delivery to main

User explicitly permitted ending the current party and integrating. `My project / develop` fast-forwarded `008e58f → 7ae088c`; no gameplay edits during integration. This supersedes the feature-only/main-pending delivery limitation above. All gameplay/test source hashes match the verified implementation; **628 PASS** remains the real pre-integration run, not a new run. Main editor recompiled and launched Fire vs Ice; an actual off-axis `(3,2)` primary attack dealt11 (HP26→15), and Self/Ally Fire Armor/full ring3 envelope are visible. Main Unity is left open.

All nine original scene/settings/meta files remain byte-identical and uncommitted. Existing campaign saves unchanged; Unity updated its own session-end telemetry on editor restart. Backups: `/private/tmp/fire-targeting-main-integration`. No push. Exact delivery evidence: [main-integration.json](Evidence/FIRE-TARGETING-02/main-integration.json). Player acceptance remains pending.
