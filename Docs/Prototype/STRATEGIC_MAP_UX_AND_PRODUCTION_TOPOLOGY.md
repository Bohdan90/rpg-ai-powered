# Strategic Map UX + Production Topology Graybox

Status: **TECHNICAL PASS — READY FOR PLAYER MAP REVIEW**. PLAYER ACCEPTED is not claimed.

## Baseline and authority

Live starting main: `My project/develop@26e0f9a0f76c8bf68df286a15a1bf6451a8de1fc`.
Own branch/worktree: `feature/strategic-map-ux`, `Convergence/Strategic-Map-UX`.
Implementation commit: `b526beb892fdc466b3c8d392e3d0a92322b05551`; subsequent local report/evidence commit contains this report.
Main is not changed/merged/restarted by this package. NO PUSH. No Meshy/provider calls or credit spend.

Read live Drive48 (especially32–35), latest46 queue,54 (especially14–15) and53 section15 on 2026-10-01. Latest topology/navigation authorization supersedes historical NEXT/3D suggestions. References:
- 48: `1pGDVZaWYKsPbCm7Gl9bnYdDGnlPBR3xX62buiordrTA`.
- 46: `1qOjTdki-rJrNUJLsBzWeSyJg4c5gfHtrNnnVExFcl84`.
- 54: `16vnMExDSDytEcAMMeOFFJThNGnKxHd2TPbI84kUmYw4`.
- 53: `1xfjigiPQfZwJsg-YfbVoBDV_GmMorOFXoPkM5Fs2Xak`.
Historical712 PASS is not this package's regression.

## Delivered behavior

Core `StrategicJourney` stores army identity, destination/world, committed known route, cursor, formation signature, issued Refresh and pause reason. `SetDestination` accepts a known farther same-world destination, follows only affordable complete edges and retains the remainder. Every leg uses the same authoritative movement implementation as immediate07 movement: actual graph cost, Hungry multiplier, occupancy check, anti-relay ceilings, per-step sight/events and local-service departure handling. There is no future borrowing, free movement, hidden reroute or real-time motion.

Automatic continuation happens once when the side accepts its next lawful handoff, in stable army-ID order. It never selects/replaces an army or moves on the opposing side's activation. New observation pauses side orders; already-known hostile contact, committed encounter, withdrawal, formation/Commander changes, illegal route/position and portal arrival also pause. Unaffordable next edge merely waits. Cancel removes the order. Explicit new destination replaces it. A paused order never resumes by itself; the player deliberately reissues a destination. Arrival at a portal stops even with spare Tempo, including an intermediate portal on a land route. Issuing a fresh land destination from an occupied portal explicitly leaves along land; it does not traverse.

Portal UI now has a contextual purple **Traverse Portal** action above the map at the selected living army's portal anchor. It presents known/Unknown destination, phase and20-Tempo attempt cost, then changes to **Confirm Traverse Portal**. First click is pure preview; the next matching confirmation executes existing paid success/PassageBlocked semantics. Ordinary invalid commands spend nothing. Hidden exit occupancy remains absent from preview. Outside an anchor the action is hidden; insufficient/closed conditions remain disabled with a reason.

Map UI has Set/Change Destination and Cancel Destination, named target, retained road/cursor and pause/wait explanation. Teal segments are affordable this activation; gold are later segments. The stored route takes priority over incidental fresh preview when inspecting its own destination, so it is not silently visually rerouted. Other destination selection previews an alternative; only explicit command commits it. Both07 and08 use this navigation adapter. Older01–06 movement/topology is not retrofitted.

## New scenario: Production Roads 08

Launch the isolated project with `bash Tools/launch-production-roads.sh`, then **Gate C → Production Roads 08 → West starts** (or **East starts**). The ordinary07 menu remains available for the UX patch. Do not use the old Tactical Graybox-only entry for a strategic map.

One authored map, not procedural worldgen. West March, Pine Uplands, Iron Ridge, Crosswater, Reed Lowlands, East Downs and Ash Pass form the larger mainland; Stone Valley is the eighth geographic zone and reuses the small existing portal-linked world. These labels do not change economic Parent/Region.

- 8 macro zones:7 mainland +1 linked valley.
- 67 traversal nodes:60 mainland +7 valley.
- 83 Land edges:75 mainland +8 valley;2 additional explicit Portal links.
- 22 meaningful POIs:17 mainland (two Keeps, Mine/Beacon/Waystation, two minors/eight existing sources, two portals) +5 valley (two portals, two supply caches, Beacon).
- Technical anchors normally have no icon. Selected/occupied own or legitimately observed/historical enemy locations may gain a necessary marker. Meaningful junctions use small diamonds; roads remain actual graph edges. Crossed lines without a junction do not create an extra connection.
- Explicit per-edge prototype costs:15/20 on local mainland roads,25 on authored cross-zone links; the deliberate Ash Portal spur costs20. Existing valley costs and20-Tempo explicit traversal are unchanged.
- Realm06/07 armies, persistent characters, two human sides,100 base Tempo/global Refresh, supply/HP recovery, local city queues/services and Pressure24 use their real existing rules and values. No economy/roster/combat retune. A is initially charted like07; dynamic enemy/site state still uses ordinary filtered sight. B is not revealed by camera/topology until legitimately discovered.

### Actual deterministic graph diagnostics

| Graph | Nodes | Land edges | Components | Average degree | Cycle rank |
|---|---:|---:|---:|---:|---:|
| Mainland A |60|75|1|2.50|16|
| Valley B |7|8|1|2.286|2|
| Land-only combined |67|83|2|2.478|18|
| Including two explicit portal links |67|85|1|2.537|19|

Mainland articulation point: **A59**. Its only bridge edge: **A59–A25**, the intentional single approach to the valuable Ash Portal. Every other mainland edge can be removed without disconnecting the graph. There are no decorative empty dead-end spurs. Portal links supplement16 mainland cycles rather than create the only branching.

Representative graph-only shortest paths (full lawful occupancy/knowledge can impose additional limits); alternate removes the second edge of the first path and recomputes without hidden information:

| Origin → destination | Edges | Normal Tempo | Alternative Tempo |
|---|---:|---:|---:|
| West Keep A01 → Mine A06 |8|145|180|
| East Keep A13 → Mine A06 |6|125|180|
| West Keep A01 → Beacon A07 |5|85|110|
| East Keep A13 → Beacon A07 |7|125|130|
| West Keep A01 → Waystation A08 |5|85|125|
| East Keep A13 → Waystation A08 |7|115|115|

Examples of ordinary macro approaches: Uplands→Iron Ridge→Crosswater; West March→Crosswater; Reed Lowlands→East Downs→Crosswater. East has shorter Mine access, West shorter Beacon/Waystation access; Keep neighborhoods/path costs differ rather than form mirrored halves. Comparable opportunity is an authored hypothesis for player review, not a proven balance acceptance. No universal numeric map standard is introduced.

## Persistence and regression boundaries

08 uses separate scenario ID `ProductionRoads-08`, outer save schema7 and `ProductionRoads08/manual.json`. 07 retains schema6 with versioned Seamless rules2 for journeys. Actual legacy rules1 saves restore and retain their old hash representation until a new journey upgrades the state. Corrupt/inconsistent data fails candidate restoration before replacing the live scenario. Save contains full committed order/cursor, pauses, topology selection, all existing realm/knowledge/economy/character state and deterministic continuation. Load does not auto-resume or charge; the lawful side handoff does.

Tactical command/replay format and combat rules are unchanged. New-map controlled contact uses the existing battle bridge, actual AI commands, verified journal, same persistent IDs and exactly-once aftermath with no free Refresh. Old01–07 graph fixtures remain unchanged. Static07 map APIs remain intact; runtime scenario graph dispatch chooses08 only for the new scenario. No production city/fog/quest framework was added.

## Automated validation

Final focused: **42 EditMode +4 PlayMode PASS**,0 failed/skipped. Focused Edit includes22 new journey/topology tests and20 existing07 tests; Play includes2 new UI tests and2 existing07 tests. No artificial test quota.

New coverage includes partial/wait/own-handoff, cancel/override, pure preview/invalid/copy, new-hostile and already-visible-contact interruption, hidden blocker without reroute, invalid leg, formation change, explicit portal stop, deterministic save continuation, actual battle/replay/aftermath, separate scenario IDs, topology integrity/bridges and six path alternatives. Existing07 tests retain paid blocked/unknown/closed/insufficient portal behavior, per-step observation, privacy, layout invariance, global supply, real legacy saves and mirrored full matches. New PlayMode exercises contextual portal action + two confirmations and production presentation + retained-order recreate/load.

Commands:
```
bash /private/tmp/strategic-map-ux/run-tests.sh EditMode 'StrategicJourneyTests|SeamlessWorldsTests|SeamlessContinuityTests'
bash /private/tmp/strategic-map-ux/run-tests.sh PlayMode 'StrategicMapUxPresentationTests|SeamlessPresentationTests'
bash /private/tmp/strategic-map-ux/run-tests.sh EditMode
bash /private/tmp/strategic-map-ux/run-tests.sh PlayMode
```
Portable repo equivalents: `Tools/run-strategic-map-tests.sh`.
Final full results and exact source/config fingerprint: `Evidence/STRATEGIC-MAP-UX/validation-manifest.json` and `combined-final-inputs.json`. Final combined regression: **663 EditMode + 91 PlayMode = 754 PASS**, 0 failed/skipped. Covers map08 and tactical companion, exact implementation commit `b526beb`; later evidence-only commit does not change validated code.

One development run had37/38 due solely to the old test's lowercase `new hostile` message expectation; preserved compatible wording, then all42 focused checks passed. Temporary GUI observer initially lacked an assembly reference and was corrected outside production code. One editor-startup SearchDatabase indexing exception and unrelated Unity Generators `NoSubscription` message appeared in the independent editor; no gameplay exception/failure was attributed to them. Diagnostic startup correction was limited to that separate editor. No provider generation was requested.

## Real GUI G1–G4

**PASS as controlled real OS-input developer checks, not player acceptance.** Normal08 seed20261001, two human slots, independent Unity6000.6.2f1 on macOS. The read-only observer recorded state and UI coordinates; public start/recreate/load/view helpers initialized scenes, restored saves and focused/scrolled views. All travel/turn/cancel/portal decisions used actual physical mouse input. Core assertions and those helper calls are not called mouse-driven actions. The observer was removed before final regression.

- G1: from ordinary start, target W Iron A19; path2→3→30→31→41→42→37→19. R1 moved toA42,Tempo5; opposing activation unchanged; next own R2 reachedA19,Tempo55. Destination/partial route visible. Repeated on final runtime after UI refinement.
- G2: changed target toA08, moved toA42, cancelled, selectedA24. Later A25→A12 paused atA55 on first observed enemy and remained there through the next own activation. New southward order paused again atA54 after a second distinct hostile became visible; player deliberately reissued. Additional already-visible-contact branch is automated coverage, not a separate mouse claim.
- G3: R3 arrivalA24 with30Tempo pauses at portal. Contextual Traverse/Unknown/20 preview left hash unchanged; second click arrivedB01 with10Tempo. R4 explicit return toA24. Final short07 repeat verified new Confirm label, A24/40→B01/20. Paid blocked semantics are unchanged and tested automatically rather than repeating the entire previous S2 batch.
- G4: ordinary connected tour includes Iron, portal/valley return, complete Pine loop A24→18→14→5→4→31→30→29→24, Mine, Iron/Ash approaches, deliberate A59→A25 spur, visible hostile interruption, then alternate A54→57→56→9→43→50→51→48→8. It crosses multiple zones/junctions rather than only portals. EndR13 at Waystation; real stock24→18 and carried Provisions0→6. Real pan/zoom changed no simulation hash.

Extra continuity evidence: physical Save buttons, scene recreation/public Presentation load helpers, exact hashes before/after; retained journey resumed toA50 thenA08 without duplication. These helper-assisted load checks are explicitly not described as mouse-only Load UI. `gui/g3-saved-world.json`, `g4-pending-save-world.json`, `g4-waystation-supply-world.json` are earned states from that ordinary tour, not authored completion snapshots. Last state also loaded on final runtime. Screenshots, actual input/pointer logs, state traces and `gui-summary.json` give provenance.

## Limits and next review

This remains graybox: Fit shows the topology, while POI labels require zoom; unmarked road crossings do not imply connectivity. No terrain/3D/art/rotation is added. The long deliberate tour reached Hungry before Waystation; this is a measured opportunity cost, not evidence to silently rebalance supply. The asymmetric resource access and whether the map feels sufficiently spatial require actual user review. No full08 competitive balance match or PLAYER ACCEPTED is claimed/required by this package.

The only meaningful scope extension is the new authored topology and its versioned adapter; no deviations from the bounded navigation/portal contract identified. Old07 direct single-activation Core movement remains available for legacy tests/callers; its UI now uses the persistent-order command. Interrupted orders retain target plus pause reason and require deliberate reissue, satisfying the allowed pause policy.

## Safety and delivery

Main remains26e0f9a; no merge/restart of its active session. Live preflight had8 dirty/untracked entries: the historical ProjectSettings modification had already disappeared; all9 protected paths were nevertheless fingerprinted. Preserve the original main scene/build settings, six metadata files and unchanged current PlayerSettings. Check final protection audit for all22 existing saves and exact remaining statuses.

Feature uses its own company/product (`RPGPrototypeIsolated/Strategic-Map-UX`), separate CFFIXED_USER_HOME roots for GUI/tests/player, independent Library/logs/replays/saves. Its local pre-existing App UI build-setting dependency was copied from main without modifying main; it remains uncommitted alongside the six generated legacy persistence metadata counterparts. The feature-only PlayerSettings delta sets the isolated namespace and APP_UI_EDITOR_ONLY; it is not permission to copy whole settings over main on a future delivery.

STOP after coordinator review/player map review. No3D, Meshy, economy expansion or next package begins automatically. NO PUSH.

## Safe checkpoint before tactical companion (2026-10-01)
Map implementation and G1–G4 complete; focused 42 EditMode + 4 PlayMode passed. Full map EditMode rerun: 650/650, 0 failed/skipped (`EditMode-20261001-003723.xml`). No final map PlayMode claim yet. User authorized the sequential tactical UX companion in this same worktree; final full regression will cover both packages. No concurrent writer or main integration.
