# Seamless Worlds 07 — document 54 implementation

Status: connected implementation present; automated validation below; **required real-input S1–S5 NOT RUN, S6 PARTIAL**. This is not whole-package acceptance or player KEEP. No next package started.

## Baseline and delivery

Actual starting main and feature base: `20c7f76347a7fd39c45c54542443f0113c7880c5`, develop. Both accepted52 `8c25cc8` and Fire targeting `7ae088c` are verified ancestors. Existing warrior straight approach and two-click archer approach were preserved, not reimplemented. Historical683/658 are not this run.

Implementation lives in `../Seamless-Worlds-07`, branch `feature/seamless-worlds-07`. The local implementation commit contains this report; resolve exact hash with `git log -1`. **Main remains develop@20c7f76; no merge, no push.**

## Implemented scope / exact adapters

- World address is `(WorldId, NodeId)`. Frontier A keeps all23 original06 nodes/edges, adds A24(3,6) and A25(5,6), connected to A4/A9 for20Tempo. Two adjoining visual patches have no extra movement/Refresh boundary.
- Stone Valley B has7 exact contract nodes and8 Land edges: 1–2,2–3,2–5,3–6,5–6,6–7 cost20;2–4,4–6 cost35. A24↔B1 and A25↔B7 are separate stable Portal links. A anchor route80; B detour120 including two passages. Camera layout is never a rule input.
- Explicit two-step Traverse confirmation; legal attempt requires20 and costs20. Occupied friendly/hostile exit produces paid PassageBlocked without moving, alternative landing, composition disclosure or discovering the destination. Invalid/stale/closed/insufficient/debt attempts are pure. Selection/focus is not travel.
- Two human sides, both StartingSides and Fire/Ice permutations, multiple06 armies, Reserve/Commission, physical transfers, capacity, source budgets, service queues and anti-relay are reused. Same IDs, damage, deaths, XP and signed Tempo cross portals. No portal/view tick.
- One global Refresh. B north/south caches start24 each and replenish at most6/Refresh from finite stock; no Parent income. B4 adds1 Pressure/full cycle to its owner. A objectives and target24 unchanged. Keep40%/field15% HP, no Armor repair except existing paid service; existing consumption/recruit/research remain global.
- Sight is two unobstructed local Land hops from own continuing armies and functioning rear City. No portal, objective or camera observer. A terrain charted; B absent from bounds/labels/picking until discovered. Dynamic state uses per-side observed snapshots; last-known armies do not track hidden movement.
- Every committed Land step captures only lawful visible fragments. Movement stops on newly discovered hostile contact and charges only traversed edges. Appearance/lost-contact records never join across hidden nodes. Consecutive visible steps group only within one command, so reading cannot alter future history grouping.
- Per-side/per-world unread cursors, historical Show Observation, timed fragment display, pause/fast-forward/clear. Viewing/read marks never execute commands or change simulation/knowledge hashes. Handoff hides private content before changing viewer. Side-private messages avoid exposing another side’s movement, service completion or hidden aftermath placement.
- Existing tactical bridge uses the actual world, one-hop local participation, strategic directions, per-army retreat/debt and original Lead placement. An A army with the same local NodeId cannot join B. Resolve once, no exit healing/Refresh. Committed contact brings its defender response forward regardless of camera position.
- Controlled temporary fixture: West pair Red R1–3, Whiteout R4/R5 (two full cycles), Closure/Recovery fromR6; East disabled. B armies remain real, selectable, visible locally and saveable after closure. No rescue/return link.
- Stable-world save schema6 stores both worlds, actor addresses, existing queues/resources/IDs, links/phases, knowledge/snapshots/history/cursors. Candidate restore validates before replacement. Legacy strategic formats and recorded tactical rules remain supported. Camera transforms are view-only; no mid-battle save.

## Input companion

See [TACTICAL_INPUT_03.md](TACTICAL_INPUT_03.md). Existing two-click attacks/approaches retained. A reproduced Core rejection of remaining Movement after casting was corrected under recorded tactical rules3; rules1/2 replay their original behavior. Focus/right-click/Escape cancellation, changed-state reconfirmation, outlined pinned waypoint and actual healing/Barrier summary added. No damage, range, school, economy or map retune.

## Validation and provenance

Fresh final **628 EditMode +84 PlayMode =712 passed;0 failed;0 skipped**. Focused63 EditMode+17 PlayMode (overlapping coverage). Full EditMode ran2026-10-01 01:40:00–01:41:19UTC; PlayMode01:41:38–01:44:01UTC. These are new07 runs, not the old Fire628 total. Commands/counts, source/config hashes and XML/log paths: [validation-manifest.json](Evidence/54/validation-manifest.json). Full runs use the final combined physical source tree, before its local commit; evidence links that exact tree fingerprint. No temporary GUI observer is included in that tree.

Concrete coverage:

| Requirement | Evidence |
|---|---|
| Exact topology, typed addresses, portal costs/blocked/invalid | `SeamlessWorldsTests` |
| Save, Whiteout, stranded control, shared supply tick | `SeamlessWorldsTests`, `SeamlessContinuityTests` |
| Visible fragments, hidden information, per-world unread, opaque fixture | same two Core suites |
| B battle, real tactical commands, same-ID aftermath, exactly-once result/replay | `RealValleyBattleReplayReturnsSameIdsOnceWithoutRefresh` |
| Normal starts, both worlds, battle, save continuation, legal Pressure24 end | `OrdinaryLegalTwoWorldMatchCanFinishAndMirrorWithoutPresentation(West/East)` — **automated, not mouse-driven** |
| Camera rotation/translation, hidden/culled view, history order, privacy, recreate/load | `SeamlessPresentationTests` |
|06 conservation/anti-relay/Commission/queue regression | existing `RealmOperationsTests`, `RealmContinuityTests`, complete suites |
| Post-spell movement/versioned replay, no repeated Action | `TacticalInput03RulesTests` |
| HH self-target, pinned input/state change/focus cancellation, legacy journals | `SpellUxPresentationTests` |

Development failures were resolved: a view method name shadowed the UI positioning enum; two test scripts initially attempted to use an uncharted route / skipped the mandated new-contact stop. The tests now issue lawful deliberate follow-up commands; no rules were changed to make a route pass. A screenshot exposed scaling around the default canvas center; transform origin is now top-left. Fog review also removed undiscovered B terrain bounds and global-message leaks.

## Real GUI and performance — exact limits

Independent Unity6000.6.2f1 window was launched with isolated roots and its actual rendered map inspected. OS helper was restricted to its window title/PID. One bounded diagnostic checked both monitor placements, Game focus and current coordinates. Accessibility reported trusted; `Application.isFocused=true`, Editor focused window `Game`, Play not paused. OS click commands returned but runtime received no PointerDown and a lawful Move left the strategic hash unchanged. **No cause beyond failed event delivery is claimed.** Main editor was not stopped or driven. Repeated blind attempts were stopped; missing runtime input remains a developer-validation blocker, not a user permission refusal.

| Check | Actual status |
|---|---|
| S1 | PARTIAL: real rendered A inspected and canvas-origin defect fixed; actual pan/control/discovery clicks NOT RUN |
| S2 | NOT RUN by GUI; Core/B bridge/replay tests PASS separately |
| S3 | NOT RUN by GUI; filtered snapshots/history/handoff tests PASS separately |
| S4 | NOT RUN by GUI; two automated legal full matches are not a substitute |
| S5 | NOT RUN by GUI; Whiteout/save/recreate/continuation tests PASS separately |
| S6 | PARTIAL: measured API-driven view exercise in actual editor; no mouse-driven latency or cold-entry guarantee |

Hardware: Apple M4 Pro CPU/GPU. Game view reported1400 logical points wide /2800 render pixels; UI root1485.85×871.34 logical units.100 API-driven focus/Fit changes per condition: one-world median/max view callback0.0384/0.1807ms; two-world0.03825/0.0738ms. GameObject count882→882 in both. Allocated memory380,767,319→380,787,400B (A),380,991,823→381,003,207B (A+B). One unique simulation hash per run. Reported Unity unscaled frame delta median~0.896/~0.892ms, maxima5.069/1.216ms; these sampled Editor values are **not measured display FPS or a production guarantee**. First cold entry and human-visible input latency remain unmeasured. Raw samples retained.

Exact pending procedures: [MANUAL_REMAINDER.md](Evidence/54/MANUAL_REMAINDER.md). Automated assertions and authored inspection are never labelled GUI PASS.

## Launch / inspection

Open isolated `Seamless-Worlds-07` with `bash Tools/launch-seamless-worlds-07.sh`. Menu **Gate C → Seamless Worlds 07 → West starts** (or East starts / Ice-vs-Fire variants). Existing TacticalGraybox scene is the host; ordinary Play alone does not choose07.

Additional labelled entries: **Temporary Route - CONTROLLED**, **Opaque edge - CONTROLLED**, **Inspection - AUTHORED portal arrival**. Inspection JSON: `Assets/StreamingAssets/SeamlessWorlds07/authored-portal-arrival.json`. It was generated by initial Core commands A2→A24→B1 (R1,20Tempo left), not earned by mouse play. It supplies known West portal mapping and an immediate view of both worlds without claiming match completion.

## Isolation / deviations / remaining blockers

Nine original main scene/settings/metadata files are byte-identical to their captured baseline; main saves are audited in the manifest. Separate company/product `RPGPrototypeIsolated/Seamless-Worlds-07`; independent batch/GUI/player preference roots under `/private/tmp/seamless-worlds-07`. Main settings were never overwritten. The main EditorBuildSettings scene entry was copied only as an uncommitted local test dependency; six legacy generated metadata counterparts remain untracked. Isolated ProjectSettings/company/product are deliberate environment configuration; do not copy them wholesale to main.

No deliberate numerical/gameplay deviation from54 identified. Main delivery is pending by design. Real GUI S1–S5, cold-entry/interactive S6, and short TACTICAL-INPUT-03 GUI/connected smoke remain pending. Whole-package54 is therefore **PARTIAL**, despite connected implementation and automated results. Coordinator review precedes player evaluation; no next package, no push.
