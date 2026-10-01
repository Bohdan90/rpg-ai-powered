# Strategic Travel Scale 08B + Mage Initiative

**STRATEGIC SCALE 08B — TECHNICAL PASS / READY FOR PLAYER SCALE REVIEW.** Player acceptance remains pending.

Starting feature: `feature/strategic-map-ux@6178e1be3351040c595e6ef8d8483eaf3af2290a`, `Convergence/Strategic-Map-UX`. Live main is `develop@3fa4a570413643a19ea2cf02e37d8fb186f5ef24`, following the explicitly requested earlier delivery. The prompt/Drive main-26e0f9a statement was stale. This package does not merge to develop or touch Meshy.

## Implementation

Production Roads 08 menu now creates version8/dense 08B. Original version7/coarse 08 saves retain their graph and route identity; 01–07 unchanged. `ProductionRoads.Map/Create` retain original coarse fixture; `TravelScale08.Create` is new playable entry through the existing menu.

Mainland's60 macro nodes/75 roads/16 cycles retained, with original POIs/junctions. Static authored geography expands each edge into ceil(3 × original straight-line geographic length) segments (minimum2),10 Tempo each. Shorter roads cost less. These are scenario-local geographic inputs; no camera transform is used. IDs encode normalized macro endpoints and segment index, independent of input enumeration. No random subdivision. Intermediate anchors are ordinary Core positions.

World B expands7→15 macro nodes,20 roads,6cycles. Existing portal/cache/Beacon IDs preserved; added nodes are forks/approaches, not new gameplay object types. Two portal links unchanged. B has154 positions/159 segments; A513positions/528segments. Hidden anchors: A453 +B139 =592; total667 positions,687 Land segments. Total macro nodes75, meaningful POIs22, geographic zones8. Ratio relative to old67 positions:9.96×.

Depth: A Keep→Keep800Tempo; A western Keep→Ash Portal900; B portal→portal450 (56.25% of Keep-depth;50% of outer Ash-depth). Ordinary startA2→Ash A25 measured through existing supply/Refresh:10activations, remainingTempo22,Provisions0. No global Tempo/food/terrain retune. Dense segments retain existing Hungry surcharge and physical sight/contact rules.

Persistent routes use existing resolver per segment; affordable legs only, same retained destination/cursor, manual cancel/override, hostile/contact/blocker/portal pauses. UI hides ordinary technical positions and compresses saved path text to progress/remainingTempo. Own armies and lawful frontier exploration endpoints remain selectable. B exploration frontier positions are shown only while observed and adjoining unexplored space, enabling lawful exploration without permanent dot spam.

Save schema8 stores dense discriminator and exact positions/orders/knowledge; array bounds follow actual map dimensions. Version7/07 hashes remain unchanged. ReplayUnit now records authored Initiative; absent field selects old11HOM/12HH profiles during replay, preserving old hash and activation semantics. New current profiles: Fire/Ice TI/TII8, HH9, HW10, HA12, EW TI14; EW TII15 unchanged. No other stat changed.

## Validation provenance

Focused final41Edit tests pass (19new +22retained journey tests), including an actual AI-resolved intermediate-road battle with deterministic replay and same-refresh aftermath. Focused PlayMode7/7; full EditMode682/682,0failed/skipped. Final full regression **682 EditMode +91 PlayMode =773PASS;0failed;0skipped**, after the test-only baseline correction. Exact inputs/XML/logs in `Evidence/SCALE-08B/validation-manifest.json`. Two initial controlled-test mistakes (selected too-short test edge; treated arrival portal pause as unexpected) corrected in tests, not rules.

GUI instrumentation is temporary, removed before final tests. Initial observer compile/local-name issue and Play-domain-reload staleHUD issue were instrument problems, not gameplay defects. Editor input was confirmed; runtime input resumed after normal stop/start of own test Play session. Main editor not controlled.

## Route diagnostics

| Graph | Nodes | Land edges | Components | Cycle rank |
|---|---:|---:|---:|---:|
| A macro |60|75|1|16|
| A travel |513|528|1|16|
| B macro |15|20|1|6|
| B travel |154|159|1|6|

Mainland articulation59 and six intermediate positions on59–25: all seven bridge segments belong to the previously deliberate portal spur. No new accidental mainland bottleneck. B articulation2/6 and their portal approach chains: deliberate entry spurs1–2 and6–7; central network has six independent cycles. Subdividing an intentional bridge necessarily creates intermediate articulation positions; it does not remove mainland loops.

POI route / shortest Tempo / alternate after removing one ordinary segment:
- A1→6:480/670; A13→6:390/880.
- A1→7:350/380; A13→7:450/670.
- A1→8:290/640; A13→8:620/630.
- A2→25:870/950.

## Real GUI evidence

- S1 PASS: ordinary A2→A25 command, retained87segment route. R1stops304101, R2stops315106; opponent handoff causes no own movement. Later R3–R9 intermediate stops; R10arrives25,Tempo22,Provisions0. No early ending while an affordable next segment remained.
- S2 PASS: own army on Road30–31,1/5 visible; UI states destination,10/87progress and770remainingTempo. No permanent hidden-anchor cloud.
- S3 PASS (controlled initial fixture): opposing armies at real consecutive315102/315103 positions on31–41. Actual marker selection→Attack→Defender Fight opens existing tactical Hotseat with proper participants; four actual EndActivation clicks. Full battle/replay/aftermath is automated evidence, not a claimed mouse-completed battle.
- S4 PASS: R10paid20Tempo traverseA25→B7; normal exploration through currently observed frontier anchors. R11reachesB6 with9Tempo, three legal branch options (roads6–10,6–13,6–15). R12central choice reaches15 with9Tempo; further alternatives toward4or12. R13continues to42505. Portal-only transfer, same IDs/supplies. Whole B crossing was measured analytically/Core; GUI demonstrates multi-turn exploration and actual choices, not a claimed full B match.
- S5 PASS: actual normal A2→A25crossing10activations matches Core. Mainland comparisons800/900Tempo; B450/ratio56.25%or50%. No arbitrary reduced armyTempo.
- T1 PASS: final standalone controlled fixture, actual EndActivation clicks EW14→HA12→HW10→HH9→FireHOM8. Ice and both mage tiers covered by authored-profile tests. Initial fixture request while a connected encounter was active was correctly ignored; those captures are excluded from final T1 evidence.
- Save/recreate/load: actual Save button on earned B route state; public recreate/load helper restores identical checksum, actual next cycle moves to42505. Exact intermediate save/cursor equality additionally automated. Helper-load is not represented as mouse-only Load proof.

Reproduced gameplay fix: an explicit new land destination issued at a portal with insufficientTempo was incorrectly paused on the nextRefresh by a broad old portal-origin check. Removed that redundant origin check. Arrival still pauses inside the movement loop; no portal edge appears in a land path. GUI replayed the genuinely earned B7/Tempo2state: next own activation reaches61705with74Tempo; test locks the behavior. No new portal rules.

## Limits and launch

Supply remains real: the long expedition becomes Hungry after starting30Provisions are consumed; cost per segment rises10→13. Do not infer accepted logistics/balance from the successful crossing. Two-hop sight now covers two short segments; B exploration is more incremental, with explicit currently-observed frontier endpoints. This is the inherited sight rule, not new fog/scouting. Tiny fit-map labels still require zoom. No additional art or frame-rate guarantee.

Open **Convergence/Strategic-Map-UX**, use **Gate C → Production Roads 08 → West starts** (orEast) for a NEW dense08Bsession. Load of a version7save intentionally remains coarse08. Main **My project/develop** has NOT received08B. Controlled earned inspection snapshot: `Evidence/SCALE-08B/earned-portal-arrival.json` (actual R10traversal, B7Tempo2, not authored victory). Public Load can inspect it only in isolated save namespace.

Regression correction: first full PlayMode90/91 failed only `GroundMovementFriendlyInspectionSpecialCancelAndStaffStaySeparate`: its absolute journal-count0 assertion ignored two legal EndActivation records now required before IceMage's slower turn. Updated to capture pre-preview record count, assert no delta while previewing and exactly+1 BasicAttack after confirmation. No gameplay was changed for this test correction. Fresh final suites passed; GUI gameplay evidence remains valid.

## Final provenance and safety

Final focused:41EditMode,7map/companionPlayMode,15SpellUxPlayMode after test correction (overlapping regression scopes, not a separate unique total). Final full XML: `EditMode-20261001-110213.xml` and `PlayMode-20261001-110411.xml`. SHA256 source/config fingerprint unchanged across final suites. Exact implementation-file list: `Evidence/SCALE-08B/changed-gameplay-files.json`. All files are local to feature; use git log for resulting commit SHA rather than a self-referential report hash.

Nine main protected paths and22baseline save/replay files verified byte-identical before/after. Main remains3fa4a57. Feature retained only existing modifiedEditorBuildSettings and six historical untracked persistence .meta outside own commit. Independent Unity data/preferences roots maintained; temporary observer removed and own editor closed normally for regression. No main control/restart and no Meshy spend. NO MERGE / NO PUSH.
