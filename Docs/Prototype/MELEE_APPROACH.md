# Melee approach + attack — 2026-09-30

User requests two-click approach-and-strike for melee Warriors. Starting main `develop @49a2a17`; isolated validation in Realm-Operations-06.

Read-only Core `MeleeApproachPreview` composes existing Pathfinder, movement legality, OA preview and attack preview. For an enemy outside legal contact, HW/EW can select a reachable adjacent legal contact cell. Choose shortest path, fewer potential OA triggers on equal length, then deterministic x/y order. No extra Movement, altered corners, teleport, occupied destinations or retreat-zone arrival. Already legal contact remains a direct attack. Archers, casters, explicit spells/staff and friendly targets do not get this automatic approach adapter.

Hover shows the Core route and conditional contact/Guard on arrival. First enemy click pins route and target; second submits normal MoveCommand. Only after the real movement/OA result, if the actor remains active at the expected destination and Core validates the same attack, submit normal BasicAttackCommand. Interrupted/dead/escaped/invalid actors never perform a follow-up strike. The deliberate two-command sequence is recorded in existing tactical replay; no new combined command or replay format. A double-hit EW basic remains its existing single BasicAttackCommand.

Preview/cancel/reselection remain read-only. UI shows cost, path and OA risk; attack preview is conditional on surviving movement. No combat stats, AI, source budgets, map or persistence rules changed.

Focused5 EditMode +12 spell/approach PlayMode +7 size/corner PlayMode PASS. Coverage: exact existing Movement4 boundary, deterministic pure query, walls/sealed target/corner-valid steps/no evacuation route, no mage/archer/spent-Action fallback, pinned UI move then attack, replay and real seeded lethal OA preventing the attack. Initial test assumed a larger HW Movement boundary and was corrected to the actual unchanged profile4. The first full PlayMode run had78 PASS/1 FAIL: the old UI test required an unreachable direct diagonal attack to disable Confirm even when the new legal detour existed. Updated that test to explicitly retain Core BlockedCorner, validate every approach step and confirm the two-command detour while preserving its OA checks. Final full590 EditMode +79 PlayMode =669 PASS,0 failed,0 skipped; see `Evidence/MELEE-APPROACH/validation-manifest.json`. Manual mouse validation NOT RUN; actual Presentation is exercised by automated PlayMode. No user Unity clicks/restart for tests.

Delivery: main `My project/develop` fast-forwarded `49a2a17 → 029a42e`; tested source hashes match. Nine unrelated files preserved; no forced Unity restart or mouse input. Final669 PASS is the fresh isolated validation of identical delivered source, not a second main run. NO PUSH.

## Straight-approach correction — 2026-09-30

User observed a Warrior stepping downward instead of approaching an aligned enemy directly. Starting HEAD `76f60eb`. Reproduced in the Core approach query: equal-cost, equal-OA-risk contact destinations were resolved by x/y enumeration, preferring an unnecessary diagonal endpoint on open ground. Four cardinal regression cases fail before the fix; four diagonal cases already pass. This is a selection defect, not extra Movement cost or a combat-rule change.

Tie-break now prefers the path with least summed cross-product deviation from the actor–target line, after shortest path and OA safety; deterministic x/y remains the final tie-break. Obstacles and safer paths still take precedence. Existing replay records the chosen ordinary movement path, so recorded command semantics are unchanged.

Focused13 PASS (before fix:9 PASS/4 FAIL); fresh full598 EditMode+79 PlayMode=677 PASS,0 failed/skipped. Evidence: `Evidence/MELEE-APPROACH/straight-line/validation-manifest.json`, including source hashes, red/green XML and compressed logs. Manual mouse validation NOT RUN. Implemented in Realm-Operations-06 feature; main remains `76f60eb` pending safe delivery, no forced import/restart of the active user session. Nine main files verified unchanged. NO PUSH.

Delivery follow-up: user explicitly requested main integration. `My project/develop` fast-forwarded `76f60eb → e823e08`. All tested source hashes match; nine unrelated files and22 non-Unity save files are unchanged. No forced restart. The677 PASS remain the actual isolated run for identical delivered code, not a new main run. See `straight-line/main-integration.json`. NO PUSH.
