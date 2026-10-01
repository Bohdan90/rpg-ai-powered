# Tactical UX Companion

Status: **TACTICAL UX COMPANION — TECHNICAL PASS**. No final balance or PLAYER ACCEPTED claim.

## Baseline / scope

Started from main `26e0f9a0f76c8bf68df286a15a1bf6451a8de1fc` in `feature/strategic-map-ux`, `Convergence/Strategic-Map-UX`. Strategic map work reached its safe checkpoint first (650 EditMode passed; G1–G4 complete). Same responsible writer, no parallel agent, Meshy use or main integration. User explicitly permits one final combined regression.

Read live48§36, latest46 tactical queue,51 current5–7/11/20, relevant02 Fire/Ice/HH,04 protection/scaling,12 Exhausted. Later visual queue does not expand this task; STOP after this delivery.

## Actual changes

- `HealApproachPreview` is a pure Core query composing existing Pathfinder, movement/OA validation and projected Close Heal validation. It only handles a living friendly target outside adjacency. Only range/LoS blockers may be solved by walking; no Action, budget, Silence, Exhausted, frozen/movement or useless-heal restriction is bypassed. Shortest legal complete path wins; fewer OA exposures and line deviation break ties deterministically. Retreat cells cannot be cast destinations.
- Selected Close Heal previews **Move → Heal**, path, final cast cell, current movement cost, OA count, target and actual capped healing. First click pins; second same target confirms. Changed target replaces the plan; cancellation/selection changes clear it. No partial or future order is issued if the full plan cannot finish now.
- Confirmation records ordinary Move, applies its actual OA/terrain/death consequences, then verifies actual arrival, same living target, and existing Cast legality. Only then records ordinary Close Heal. No shortcut command, teleport, new replay schema, free healing or free budget. State-hash changes require renewed preview/confirmation. Self/adjacent casts remain direct.
- Unit labels show purple **Temp B N** whenever present; active/hover/inspection text explicitly says **Temporary Barrier**, separate from HP/Max and Armor/Max. Zero optional protection is hidden. Fire Armor's independent status remains visible even when its Barrier is spent.
- Fire description explicitly says **SHORT LINE, range3, all units / Friendly Fire**; Ice says **LONG SINGLE TARGET, range8, contact roll, no built-in Freeze/Slow**. Existing Core-exact envelopes/footprints remain unchanged.
- `CombatOutcomeText` groups resolved events by actor/target/action: Miss (failed contact), Guard, Temporary Barrier absorbed, Armor loss, HP loss/heal, Burn, Freeze, cleanse/death/escape. Both damage pools remain visible. Latest result is concise; recent readable outcomes are newest-first. Full developer Core event log remains accessible in a collapsed foldout; replay/session telemetry is unchanged. Initiative/retreat details are collapsible.
- Reproduced null-target protection-expiry bug in old log formatting is fixed: expiry legitimately has Actor but no Target. GUI also reproduced stale hover protection after leaving the board; leave now clears inspection. Controlled custom fixtures exposed legacy names assigned by numeric ID despite different side/class; fallback now uses actual side/profile/ID, retaining matching named legacy fixtures.

## Protection boundary / explicit applicability

Actual UnitState, UnitProfile, persistent records and resolver have **no Ward or Persistent Barrier fields**, and no implemented profile grants either. Document51§5 defines permanent Ward/Barrier maxima as zero for this roster. The patch does not invent a defense pool or routing system to display it. Ward-only and Ward→HP GUI/automated cases are **N/A: no implemented source/state**, not PASS. Persistent Barrier display is likewise N/A. Existing elemental damage still consumes Temporary Barrier then HP and leaves Armor unchanged; Physical consumes Temporary Barrier then Armor/HP. No math, damage, Magic Power, Accuracy, Guard, Burn, spell duration, budgets, Exertion, profile or map-size tuning changed. No new Layer Break mechanics.

Temporary protection is battle-local and clears through existing expiry/death/battle boundaries; UI stores no duplicate protection truth. Existing persistent save/recreate/return tests cover the unchanged HP/Armor/source-budget bridge.

## Focused automated evidence

Commands (isolated company/product + CFFIXED_USER_HOME):
```
bash /private/tmp/strategic-map-ux/run-tests.sh EditMode RPG.Tests.HealApproachTests
bash /private/tmp/strategic-map-ux/run-tests.sh PlayMode 'RPG.Presentation.Tests.TacticalCompanionTests|RPG.Presentation.Tests.SpellUxPresentationTests|RPG.Presentation.Tests.EssentialReadabilityTests|RPG.Presentation.Tests.GrayboxPlayModeTests|RPG.Presentation.Tests.EngagementPresentationTests'
```
13 new EditMode cases: exact Movement boundary, deterministic/pure preview, ordinary move then heal, no Action/budget/Silence/Exhausted/Freeze/movement/useful effect, self/adjacent unchanged, sealed target, target death after move, fatal OA and deterministic outcome. PlayMode covers real presenter confirmation, target change/cancel, no partial movement, replay, protection damage/expiry/reset, readable outcomes, Fire/Ice shapes and ally warning. Existing input/bow/melee/engagement tests remain part of regression.

First focused PlayMode was20/21: one obsolete assertion expected the previous verbose `HP healed +14` string. Updated it to require Close Heal + actual HP+14; next23/23 passed. The expiry regression is also covered. Initial sandbox-only Unity launch failed UPM socket permission and produced no test result; rerun with existing approved isolated runner succeeded. Final source/config and combined full results are recorded in the manifest; historical712 is not reused.

## Controlled real GUI T1–T4

Separate Unity6000.6.2f1 window, isolated saves/preferences, actual OS mouse clicks. Public `ConfigureBattle` creates clearly labelled controlled initial fixtures; it does not alter an outcome or submit gameplay commands. Read-only observer provides UI/cell coordinates and records Core state. All attack/cast/move/activation decisions below were real clicks. Screenshots/input logs and fixture provenance are in `Evidence/TACTICAL-UX-COMPANION`.

- T1: no optional pool initially; actual Fire Armor on HW grants6 Temporary Barrier separate from16Armor. First recipient activation retains6; second expires to0 and removes optional label. A separate Fire Stream consumes6Barrier +4HP while16Armor remains. Ward is N/A as above.
- T2: Fire aim at(8,8) from(5,8) displays exactly(6,8),(7,8),(8,8), ally warning and range3; Ice displays only target(8,8), range8 envelope, directed contact. No damage/range retune.
- T3: HH(5,8), wounded ally(10,8), Movement4. First click pins path(6,8)→(7,8)→(8,8)→(9,8), with no hash/resource change. Ally(12,8) is unreachable: two clicks consume nothing. Reselect(10,8), confirm: actual HH arrives(9,8), ally HP10→24, Move4→0, one use, caster Exhausted. Journal adds exactly Move+Cast. No future order.
- T4: seeded frontal HW probes5/31/1 visibly resolve Miss, Guard, and Armor4→0 plus HP40→32. Fire probe shows Barrier absorption + HP loss and Burn+1. Heal shows actual+14. Actor/target/action attribution is retained. Controlled fixture names use actual profile/side after the final local correction.

Remaining HUD debt: full-board tiny unit labels still benefit from zoom; long ability explanations and developer controls still occupy a scroll panel. No production HUD, new icon framework, Ward source, or broad redesign was added. Unity's editor SearchDatabase startup exception and unrelated Generators NoSubscription banner were observed, not gameplay failures; no generation/provider call was made.

## Delivery / safety

Final full results, source hashes, commits and exact worktree status are recorded in the checkpoint/manifests. Main is untouched, not automatically merged. Existing22 main save/replay files and9 protected paths are fingerprinted in the combined protection audit. Feature keeps its local App UI build-setting dependency and6 pre-existing generated metadata files uncommitted. Own isolated PlayerSettings namespace must not be copied wholesale over main.

Launch this feature project; **Gate C → Combat Lab → Support vs Fire (near contact)** for HH (or Fire vs Ice / Mobile Blades). Existing Lab buttons and Hotseat/Player-vs-AI remain available. Production Roads08 is separate. Confirm exact menu labels in `Editor/CombatLabLaunch.cs`.

NO PUSH. STOP for coordinator/player review; no Air/Earth redesign, 3D or next package.

GUI provenance note: the final two-line inspection reset correction (clear null hover; record clicked cell) was validated by focused/full automated tests after the screenshots. T1–T4 gameplay interactions were not reclassified as a new GUI run for that final text-refresh change. One focused test initially constructed an invalid Unity PointerLeave event without pointer data; replaced with a direct presentation refresh check. This was a test-driver error, not a gameplay failure.

Final transition audit: clear the readable feed on each new connected encounter, matching existing detailed-log reset. Expanded focused PlayMode44/44 passed; targeted connected/feed10/10 passed after this correction. Final combined suites follow it.

Full pre-final run:663 EditMode passed,90/91 PlayMode passed; the sole failure was the old Lab-name prefix assertion after adding actual side. Updated assertion still requires actual side and profile. Final readable-event audit also distinguishes reversed-source Fire Armor retaliation from Basic Attack; focused synthetic event coverage verifies the attribution. Neither change retunes Core. Final full rerun below supersedes these intermediate results.

## Final verified result
Implementation `b526beb892fdc466b3c8d392e3d0a92322b05551`; later evidence-only commit. Fresh combined **663 EditMode + 91 PlayMode = 754 PASS**, 0 failed/skipped. Final source/config matches the manifest fingerprint. Focused:13 EditMode;44 expanded PlayMode;11 final changed-area PlayMode (overlapping scopes). T1–T4 real-input evidence above; Ward/Persistent Barrier N/A. No gameplay blocker remains.
