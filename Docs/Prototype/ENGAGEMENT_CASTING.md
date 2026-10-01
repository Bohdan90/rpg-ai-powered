# Engagement Casting

Baseline: main/develop `f8b8229cc7fadfea8694a5082e0bbb3c3e2f9cf4`; isolated source `5a45bbbe32f65bbafc17d861b256cf9c61ef4e45` (already included in main; Core/Presentation/tests verified identical before edits). Branch `feature/engagement-casting`, existing `Convergence/Strategic-Map-UX` worktree, isolated settings/data/preferences. Main integration separately authorized by user after validation. Latest Drive48§44,51§23,46queue read before changes. Prior strategic and tactical work retained.

## Data and resolver

`EngagementCasting { Allowed = 1, Blocked = 2 }` in `CombatSpells.cs`. Explicit private typed dictionary keyed by every `SpellId`, exposed through `SpellRules.EngagementCastingFor`. No zero/default value, shape/range/school inference or fallback. `ValidateData` runs in the static initializer and tests every enum member; a newly added SpellId with no explicit assignment fails initialization/validation. Unknown lookup throws; invalid player commands still return normal illegality without mutation.

| Spell | EngagementCasting |
|---|---|
| Fire Stream | Allowed |
| Fire Armor | Allowed |
| Fireball | Blocked |
| Ice Shard | Blocked |
| Ice Shield | Allowed |
| Freeze | Blocked |
| Close Heal | Allowed |

`BattleResolver.IsSpellEngagementBlocked` uses the existing `ZoneOfControl.Sources` truth. No plain-adjacency approximation, no new ZoC, and no dependence on whether the engager has an OA left. The archer-only helper is intentionally not reused for a non-archer caster. `CastBlockers` adds `CommandError.BlockedWhileEngaged`; preview and commit share this path. No Action/budget/RNG/state mutation from rejection. Other blockers are still reported independently. Staff Basic, Movement and existing movement OA remain unchanged; no casting OA or concentration added.

AI already validates each candidate through Core; no AI scoring/architecture change. Tests prove that Ice Shard/Freeze are excluded while Ice Shield remains a candidate, and the chosen command is legal. Fire/Ice damage, range, Magic Power, Initiative, budgets, Exertion and strategic systems untouched.

UI shows disabled spell buttons with wrapped **Blocked while Engaged** and tooltip, hides blocked primary aiming envelope, and includes the reason in selected-spell details/target preview. Staff stays available under its ordinary rules. Dropdown inspection cannot bypass resolver validation.

## Rules and replay compatibility

New tactical battles use existing version field `FireRulesVersion=4`, with config `GateC-v0.1-51-spells4-engagementcasting`. Clone/snapshot/hash preserve the version; existing hash serialization already records it. Old rules1–3 replays retain their previous permissive casting legality rather than silently receiving this change. No format rewrite or removal of legacy fixtures. New rules4 rejected cast→lawful movement/OA→cast journal verifies; old rules3 engaged-Ice journal verifies. Stable strategic saves/persistent records are not migrated or modified by this patch.

## Validation provenance

Focused Core73 PASS: new17 classifier/legality/AI/replay cases plus existing spell/Fire-targeting scopes. Focused PlayMode17 PASS (new2 + existing15), then the final two touched presentation cases repeated after wrapping the blocker text. Fresh final regression: **708 EditMode + 94 PlayMode = 802 PASS, 0 failed/skipped**. Final XML: `Evidence/ENGAGEMENT-CASTING/tests/EditMode-20261001-130705.xml` and `PlayMode-20261001-130851.xml`; gzip logs alongside. Source hashes unchanged through both suites. Commands: `bash /private/tmp/strategic-map-ux/run-tests.sh EditMode` and `PlayMode`; Unity6000.6.2f1, isolated preferences. Focused runner filters: `EngagementCastingTests|CombatVarietyTests|FireTargetingTests|SpellUxTests` (73); `EngagementCastingPresentationTests|SpellUxPresentationTests` (17); final presentation recheck2; updated legacy fixtures + new classifier38. These overlapping focused runs are not summed as unique tests.

Development diagnostics: first attempt to run Unity under sandbox failed on UPM socket EPERM and produced no tests; rerun used the approved runner outside sandbox. Initial new multiple-engager fixture put a unit outside the default small board; corrected its explicit23×17 battlefield, not game rules. Existing two-click Ice attack fixture used melee-adjacent targets, now intentionally illegal; moved those targets to legal ranged positions while retaining every pin/change-target/one-commit assertion. A test launch immediately after normal editor quit saw the still-closing project lock; retried only after shutdown/lock removal. First full EditMode found two further legacy adjacent-cast fixtures (Exhausted Ice Shard and mutual-elimination Fireball): moved the hostile target outside Engagement while preserving Staff/Exhausted and actual mutual-elimination/replay assertions. Focused recheck38/38 PASS. These are recorded separately from final suite results.

## Controlled GUI smoke

Temporary fixture initialization only; all EndActivation, targeting, confirmation and movement are real OS-input to the separate test window. No commands sent to the user's main Unity.

- Fire TII at(5,8), hostile HW at(6,8): Fire Stream, Fire Armor and Staff available; Fireball disabled with explicit reason. Two clicks commit Fire Stream: HW40→29HP, Armor16 unchanged, caster Action spent.
- Ice TII at(5,8), hostile HW at(6,8): Ice Shard/Freeze disabled, Ice Shield/Staff enabled, no primary spell range envelope. Movement first click previews without mutation. Second click lawfully moves to(4,8), ordinary OA reduces HP32→20, Movement4→3, Action remains available. Ice Shard/Freeze become enabled; two clicks commit Ice Shard.
- First smoke exposed truncation of the long disabled-button label. Wrapped label/minimum height fixed; final screenshot rechecked before full regression.

Evidence contains screenshots, observer snapshots, OS-input trace, XML/logs and final source hashes. Helpers used only authored initial fixtures, view focus/scroll and read-only observation; not represented as production UI fixture controls.

## Launch and boundaries

Validated feature checkout: open `Convergence/Strategic-Map-UX`, **Gate C → Combat Lab → Fire vs Ice (near contact)** or **Support vs Fire (near contact)**. Current battles use rules4. Controlled adjacent proof above is a developer fixture, not an instant-win or persistent gameplay change.

User subsequently authorized main integration. Main observed outside Play Mode before transfer; no restart needed. No push, no new balance verdict, no PLAYER ACCEPTED. Nine protected main files and22baseline saves/replays tracked by before/after hashes; main editor remains open and untouched. Temporary observer removed before final full suites. Legacy six metadata files and local EditorBuildSettings remain uncommitted.

## Final status

**ENGAGEMENT CASTING — TECHNICAL PASS.** All requested classifier/legality/AI coverage and controlled GUI smoke passed. Implementation source commit is the commit containing this report. Main delivery is recorded separately after the authorized merge; no new full run claimed at integration.
