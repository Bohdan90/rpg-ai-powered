# Combat Variety 51 + Connected 05B — isolated implementation evidence

**Latest follow-up (2026-09-30): N1–N5 and V1–V5 controlled GUI checks completed.** Main integration was explicitly authorized. Current measured regression, deviations and evidence are in [acceptance completion](5051_ACCEPTANCE_COMPLETION.md). Earlier NOT RUN statements below are historical delivery records, superseded by that report. Coordinator/player acceptance remain separate.

## Status and commits

- Starting live baseline: `develop @ 045707f81847fb88ac34c724d5f87ce30fba5c39`; gameplay `20473ec`. No 50 implementation existed at entry.
- Worktree: `/Users/bohdanskrypka/UnityProjects/Convergence/Isolated-50-51`, branch `feature/50-51-connected`.
- `3541376`: early Combat Lab, separate profiles/spell resolver, states, previews, AI and replay.
- `3d58f35`: 50 economy/queues/research/Forge, integrated 05B, stable saves, source budgets, recruitment/training, launchers, regression fixes.
- Final evidence/checkpoint commit changes documentation and insignificant whitespace in newly generated metadata only. Tested gameplay is `3d58f35`.
- **50: IMPLEMENTED; automated regression PASS; manual N1–N5 NOT RUN.**
- **51: IMPLEMENTED; automated regression PASS; manual V1–V5 NOT RUN.**
- Not a coordinator acceptance or player balance KEEP. 04 player acceptance remains PENDING; prior feedback about repetitive Warrior/Archer battles is preserved. No develop merge/cherry-pick; NO PUSH.

## Launch without touching the user's original game

Run from Terminal:

```sh
bash '/Users/bohdanskrypka/UnityProjects/Convergence/Isolated-50-51/Tools/launch-isolated-5051.sh'
```

This opens **only the isolated project**, with its own editor preference root. In that Editor:

- `Gate C → Combat Lab → Fire vs Ice (near contact)`
- `Gate C → Combat Lab → Support vs Fire (near contact)`
- `Gate C → Combat Lab → Mobile Blades (near contact)`
- `Gate C → City Foundations 05A`
- `Gate C → City and Combat 05B (Fire vs Ice)`

Alternatively use `Gate C → Play Tactical Graybox` and the on-screen launch buttons. The launcher allows permanent West/East Fire/Ice/Support presets and StartingSide selection before starting 05B. Lab `controller-mode` supports Hotseat and either human side versus AI. Select Ability, click a target/direction/ground cell, confirm allied fire if required, then Confirm. Magenta cells show the Core spell footprint. Select Basic/Move for ordinary movement. End Activation advances the turn, including a frozen turn.

Three 23×17 Lab layouts show open, offset-cover and flank-wall situations. Near-contact fixtures are explicitly authored veterans; Support includes a wounded Warrior to make Close Heal useful immediately. These are not extra Movement or campaign-earned deployments. Strategic Hotseat never silently assigns human units to AI.

## Implemented combat scope / prototype numbers

| Profile | HP / Armor | Move / Init | Accuracy / Dodge / Guard | Additional |
|---|---|---|---|---|
| Fire / Ice HOM TI | 26 / 0 | 4 / 11 | 90 / 5 / 0 | MP1.00; staff Physical5, range1; no ZoC/OA |
| Fire / Ice HOM TII | 32 / 0 | 4 / 11 | 90 / 5 / 0 | MP1.15; permanent school major spell |
| HH TI | 28 / 0 | 4 / 12 | 90 / 5 / 0 | MP1.00; staff5; no ZoC/OA |
| EW TII | 38 / 8 | 6 / 15 | 90 / 10 / 0 | frontal evasion+10; Basic11 then6; OA11 only |

Original HW/HA/EW TI tuning, HA Range10, map sizes, movement geometry and WP-03E retreat rules remain unchanged. EW II uses remaining legal Movement after its Basic for Graceful Exit; suppression belongs only to its attacked target's first adjacency exit, not another enemy. Two contacts/Guards resolve separately; lethal first hit prevents the second.

- Fire Stream: 3-cell ray,10 Fire; Fire Armor:6 temporary Barrier and direct-positive-melee retaliation; Fireball TII: range8, radius1,14 Fire.
- Ice Shard: range8,12 Water/Ice, directed contact without Guard/bow penalty. Ice Shield: range4,10 temporary Barrier. Freeze TII: range6, directed contact, no damage, denies movement/actions/reactions through the next own activation; direct positive damage breaks it after the hit.
- Close Heal: self/adjacent friendly,14 HP; cleanse one whole Burn then Poison then Bleed; full HP requires an actual cleanse. No Armor repair/revival.
- Fireball/Freeze/Close Heal: respectively2/2/3 committed uses per persistent character per completed global Refresh. Misses still commit use/Action/Exertion. Handoff, battle exit, load and tactical escape do not replenish uses. New paid recruits start with full use allowance.
- Exertion blocks another Exertion through the end of the caster's next activation, but ordinary spells remain legal. Shields expire at the start of recipient's second subsequent activation. The package's shield slot replaces rather than stacks; Fire Armor's retaliation remains independent of remaining points until its duration expires/replacement.
- Burn: cap3; 2 Fire per stack for two subsequent target activations; positive eligible fire hits have30% application; refresh at cap. No immediate/world/battle-exit tick. Freeze does not break from DoT.
- Temporary conditions/Barrier clear between battles; HP/Armor/death/XP/identity/profile/school/major spell/source-use history persist. Current authorized profiles have no nonzero Ward or independent persistent Barrier; no new recovery service or unrelated shield system was invented.

The existing tactical integer-pool policy is retained: Magic Power scales direct damage/healing once, then final integer truncation (TII Fireball16, Ice Shard13, Fire Stream11). It does not scale fixed protection or Burn ticks. This is a prototype numeric adapter, not new universal balance canon.

AI uses legal Core spell candidates and pure previews, scores affected allies/self, real healing/cleanse, protection replacement and Freeze, avoids zero-value casts, and preserves wall-detour/retreat behavior. Unit labels/staff icons, separate HP/Armor bars, temporary Barrier/status labels, ability limits, affected allies and actual Core event logs provide graybox readability.

Confirmed simultaneous lethal friendly AoE now yields mutual elimination with no winning side. Invalid preconstructed no-active-unit scenarios still throw; terminal replay snapshots can restore actual mutual elimination. No side receives a victory multiplier on that outcome. This is a bounded necessary resolution case, not surrender/morale content.

## 05B integration

05A economics are detailed in `CITY_REGION_RESEARCH_FOUNDATIONS_05A.md`. 05B reuses that 23-node scenario, Pressure24, paid construction/research, Forge, Hotseat bridge and permanent attrition.

Each side starts with HW Commander, HW, HA, EW II L3, and Fire HOM II L3 / Ice HOM II L3 / HH TI L1. Capacity use27/32: three native subordinates6 each and one unfamiliar Elf9; Commander0. No Elf replacement wing exists. Realm HOM/HH direction is fixed by the starting preset; individual Fire/Ice school and major spell are profile-backed permanent identity, not a prebattle selector.

Mage Tower I:100G/10W/2 steps. Tower II:120G/10W/5I/2 steps plus DevII, TowerI and Elemental Drills. Drills:6 Work/40G, Human foundation provenance, HOM-only. Legal L1 Mage recruitment costs150G and6 capacity in the existing one-order recruitment queue at own functioning Tower; Commanderless is prohibited. IDs use the persisted deterministic recruit counter.

Training: same living HOM TI, Personal L3+, own TowerII/Drills, explicit permanent Fireball/Freeze confirmation,75G once, full following Refresh. Departure/Commanderless pauses; death cancels without replacement/refund; cancellation does not refund. Completion changes that character's profile once and does not itself heal or replenish Armor; ordinary global HP recovery is a separate existing step.

## Ready-to-load fixture

`Assets/StreamingAssets/CityCombat05B/authored-ready.json` is loaded by **Load AUTHORED 05B inspection fixture (not a played match)** on the tactical launcher. It is also validated through actual PlayMode JSON load.

**Origin: controlled authored snapshot, not a played match.** West has DevII, Forge, TowerII and completed Drills; one HW is Dead; the Commander has21 HP /3 Armor. Other prescribed units retain their persistent identities/levels. The event log identifies this provenance. A new L1 Mage still requires the real150G payment, local presence, vacancy and Refresh; it is not spawned by research. This fixture is ready for repair/recruit/economy inspection; near-level training is covered separately by explicitly controlled automated fixtures, not claimed as earned player progress.

## Actual automated evidence

Unity6000.6.2f1, isolated project and namespaces. Final gameplay `3d58f35`:

| Scope | Passed | Failed | Skipped | Fresh XML |
|---|---:|---:|---:|---|
| Full EditMode | 489 | 0 | 0 | `Evidence/5051/final-editmode.xml` |
| Full PlayMode | 64 | 0 | 0 | `Evidence/5051/final-playmode.xml` |
| Total | **553** | **0** | **0** | manifest records source logs/commands |

New coverage:48 EditMode +3 PlayMode. Focused runs included33 combat/replay/retreat;38 legacy Crossroads;18 initial economy;41 expanded economy/combat;21 final Lab/connected-match;4 consecutive-battle persistence;31 final retreat/persistence/replay;3 PlayMode Lab/City save-load. These overlap and are not additive. Full regression covers prior01/03/04, ordinary/siege fixtures, old ranged UI, preview/invalid/RNG and replay tests.

Final controlled AI Lab results (not manual): FireVsIce5 rounds/56 commands/12 casts, elimination; SupportVsFire3 rounds/30 commands/7 casts, withdrawal; MobileBlades8 rounds/93 commands/2 casts, elimination. All commands legal and replays matched.

Two scripted 05B matches exercised paid DevII/TowerI/Drills, movement, a real resolver battle, casualties, persistent return, saved-vs-unsaved continuation and legal Pressure ending. West-first:15 tactical rounds,8 deaths,1 escape. East-first mirrored presets:11 rounds,6 deaths,2 escapes. Both ended at R31 with the scripted first side winning. This script deliberately leaves one side holding the objective; it is **not evidence of first-mover balance**, camping quality or human acceptance.

Separate real two-battle retreat fixtures verify exact HP/Armor and same IDs, Fireball use1→2 without Refresh, no carried Exertion/temporary state, and rejection of a third use. Full save/recreate/load PlayMode checks use actual JSON/file transport and reject corrupt data without live-state mutation.

Failures found and fixed during work: JsonUtility null nested DTO handling (explicit active-project flag); missing Editor assembly reference; regression in the legacy invalid-empty-battle guard; terminal mutual-elimination replay restore. Test-harness issues corrected: a spent-Movement test loop and a seed whose Burn legitimately killed the target before the intended retreat. Earlier failed runs are retained in `/private/tmp/convergence-5051/logs`; only the final runs above are claimed green.

## Manual gates / remaining gaps

**N1–N5 and V1–V5: NOT RUN as manual mouse-driven playthroughs.** No independent GUI/input session is exposed; macOS input is shared with the user's open original game. No user-window clicks, focus changes, Play-state changes or Unity termination were performed. Automated scene/command tests do not count as manual play. A batch ScreenCapture attempt did not produce a usable image; no screenshot/visual PASS is claimed.

N1–N5 residual instructions are in the 05A report. V residuals:

| Case | Human procedure | Observe |
|---|---|---|
| V1 | Fire vs Ice, near-contact, Player-vs-AI; cast line/AoE including an explicitly confirmed allied footprint, shields and directed spell; finish. | Real FF, protection loss, Burn ticks, AI decisions and readable restrictions. |
| V2 | Support vs Fire then Mobile Blades, Hotseat; heal/cleanse, attack with EW II beside two enemies and exit; Freeze then direct-hit break/expiry. | Target-only OA suppression, no free Movement, actual cleanse, clear denial/expiry. |
| V3 | Fresh05B chosen presets; pay construction/research, fight with casualties, return/recover/repair, buy a legal Mage and deploy it, finish match. | Attrition changes subsequent investment; no free reset/replacement or infrastructure from research. |
| V4 | Save with nondefault queues/training/use counters after battle; recreate/load; fight again before Refresh, then complete Refresh. | Exact continuation, no skipped/double payments/recovery/use reset. For training use a labelled near-level fixture or an earned L3 TI. |
| V5 | Mirror StartingSide and presets; compare open/cover/flank Lab and full05B choices. | Rounds to contact/useful spells, repetitive waits, optional fights, loss/replacement/recovery tradeoffs. |

Graybox layout/readability, first-mover advantage, turtling/stalemate and economic/combat balance remain user-playtest questions. No automatic balance retuning followed scripted results.

## Original checkout / save safety

Original `My project` remains develop `045707f`; its three scene/settings changes and six untracked persistence metadata files retain preflight SHA256 hashes. No original dependencies were copied or committed as new work. Unity generated counterpart metadata for those six missing baseline files in the isolated worktree; they are deliberately left untracked.

Tests use `CFFIXED_USER_HOME=/private/tmp/convergence-5051/preferences`, independent Library/Temp/logs and `CodexPrototype/Convergence-5051-Isolated` product namespace. The supplied interactive launcher uses a separate `play-preferences` root. 05A,05B, old scenario saves, authored fixtures and replays have distinct directories. Legacy tactical replay configuration is explicitly rejected after the spell config extension rather than silently reinterpreted; old strategic scenario schemas remain loadable and tested.

Stop after this evidence commit for coordinator review and the new integrated player evaluation. No additional Civilization Depth, research expansion, Dominion/Convergence, siege, economy tranche or network multiplayer is authorized by this implementation.

## 2026-09-30 coordinator follow-up

Both contracts remain **PARTIAL**. See [requirement-level acceptance audit](5051_ACCEPTANCE_AUDIT.md) for named-test coverage, unasserted branches, checked numeric values and manual evidence. The prior 553 PASS are valid historical evidence, not complete coverage of every required branch. Main integration was explicitly authorized by the user after the original NO MERGE instruction; integration does not grant technical/manual/player acceptance. Runtime changes in this follow-up are limited to the reproduced tactical ability/command discoverability defect. No rules or balance changes. Fresh validation and manual results are in `Evidence/5051/validation-manifest.json`.


## 2026-09-30 — SPELL-UX-01 (§14), superseding follow-up status

Coordinator accepted the preceding main `da9f776` technical completion; prior N/V and585 results above are historical, not current failures/gates to repeat. The user personally tried the new combat and requested targeting/readability/default-input corrections, not a new balance pass.

Implemented on isolated `feature/spell-ux-01` while preserving the open main editor: Core-derived hover envelope/footprint/obstruction/complete blockers, correct Self-only Fire Armor diagnosis, empty/occupied Fireball, eight-direction Stream explanations, Fire/Ice primary hostile click, explicit staff/specials, stable clicked preview/FF confirmation and cancel/handoff/camera reset. Minimal previously missing Silence eligibility/snapshot state closes the existing restriction; no new source/duration/content. Combat numbers, pools, budgets, rosters and economy are unchanged.

Fresh focused59 EditMode +13 PlayMode and full540 EditMode +69 PlayMode =609 PASS (0 failed/skipped), representative real GUI examples, normal05B battle/physicalEscape/return/save-recreate-load smoke, known limitations and exact launch path: [SPELL_UX_01.md](SPELL_UX_01.md), [evidence manifest](Evidence/SPELL-UX-01/validation-manifest.json). Nine original files/saves/session preserved; no push. Main remains da9f776 until this follow-up can be loaded without resetting the user's active session. Stop for coordinator review and short user targeting check, not another full N/V gate.

## 2026-09-30 — FIRE-TARGETING-02 (§16), accepted follow-up

The historical v1 self-only/eight-direction restrictions above are superseded for new-rule play. Implemented on `feature/fire-targeting-02` from main008e58f: exact cell-directed thin range3 Stream with true-centerline obstruction; range3 self/allied Fire Armor with recipient-based duration, caster-only cost and shared shield-slot replacement; matching preview/UI/AI; explicit v1/v2 replay dispatch preserving recorded outcomes. Combat/economy/roster/source-budget values unchanged. Main008e58f and active session remain untouched.

Fresh focused77 EditMode+6 PlayMode PASS; full558 EditMode+70 PlayMode=628 PASS,0 failed/skipped. Config/XML: [FIRE_TARGETING_02.md](FIRE_TARGETING_02.md), [manifest](Evidence/FIRE-TARGETING-02/validation-manifest.json). Real short separate-window targeting/recipient/Burn/expiry and05B battle→physicalEscape→sameRefresh→save/recreate/load PASS; original full N/V runs were not repeated.24 retained v1 replay files still verify. Source rounding example and exact isolated launch path are in the report. Feature not yet delivered to main; player targeting acceptance PENDING. No push; stop for coordinator review.

Delivery update: FIRE-TARGETING-02 gameplay7ae088c is now fast-forwarded into `My project/develop` with explicit user permission to end the old party. Main Lab off-axis attack/UI smoke passed;628 PASS retains its original pre-integration provenance. Nine unrelated files and campaign saves preserved. No push. The feature-only limitation above is superseded.


## 2026-09-30 delta — TACTICAL-INPUT-03 with isolated Seamless Worlds07

Existing two-click attacks/warrior and Bow approach preserved from actual main20c7f76. Completed the remaining post-spell Movement correction under recorded rules3, plus stale/focus/cancel guards, outlined waypoint and actual heal/Barrier outcome summary. HH self targeting was already canonical and implemented; new Presentation test verifies two-click self heal and one source use. No spell damage/range/kit/economy retune. Rules1/2 hashes and32 actual stored journals remain verified.

Fresh combined07 regression628 EditMode+84 PlayMode=712PASS,0failed/0skipped; exact source/provenance in `Evidence/54/validation-manifest.json`. This is feature-only, not a main delivery. Short INPUT03 GUI/connected smoke NOT RUN due independent runtime input delivery; no repeat of old N/V claimed. See `TACTICAL_INPUT_03.md` and `SEAMLESS_WORLDS_07.md`. NO PUSH.


### INPUT03 GUI follow-up with document54

At unchanged game implementation `1d101cf`, controlled real OS-input Fire/Ice/HH, Exhausted/Freeze and pin/cancel probes completed. Connected B joint battle retained Fireball use1 through Escape, aftermath and save/recreate/load. Previous INPUT03 NOT RUN is superseded; no repeat of old N/V, no new spell/economy changes. See `TACTICAL_INPUT_03.md` and `Evidence/54/manual-followup/`. Main remains20c7f76; no merge/push.


## 2026-10-01 — Tactical UX companion delta

Implemented in isolated `feature/strategic-map-ux@b526beb` alongside Production Roads08; main26e0f9a not changed. Protection/readable outcomes and deterministic two-click Move→Close Heal use existing rules. Fire3-line/Ice8-target asymmetry preserved. No Ward/persistent Barrier runtime source exists, so those cases are N/A; no class/number/routing changes. Details and actual GUI/focused coverage: `TACTICAL_UX_COMPANION.md`. Fresh combined full regression: 663 EditMode +91 PlayMode =754 PASS,0failed/skipped; exact XML/config provenance in `Evidence/STRATEGIC-MAP-UX/validation-manifest.json`. No final balance/player acceptance. NO PUSH.
