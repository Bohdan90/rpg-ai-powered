# Seamless Worlds 07 — document 54 implementation

Status: implementation `1d101cf`; **S1–S6 controlled real-input checks completed** in the follow-up below. Coordinator acceptance/player KEEP remain separate. No next package started.

## Baseline and delivery

Actual starting main and feature base: `20c7f76347a7fd39c45c54542443f0113c7880c5`, develop. Both accepted52 `8c25cc8` and Fire targeting `7ae088c` are verified ancestors. Existing warrior straight approach and two-click archer approach were preserved, not reimplemented. Historical683/658 are not this run.

Implementation lives in `../Seamless-Worlds-07`, branch `feature/seamless-worlds-07`. Game implementation is `1d101cf436fd66546550584577ea145fbf938136`; the later evidence-only commit does not change gameplay. **Main remains develop@20c7f76; no merge, no push.**

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

**Manual follow-up pre-commit rerun:** same unchanged implementation `1d101cf`, fresh628 EditMode (02:27:06–02:28:29UTC) +84 PlayMode (02:28:51–02:30:58UTC) =712 passed,0 failed/skipped. Temporary observer removed, source/config fingerprint unchanged. Earlier focused63+17 are retained checks on that implementation, not renamed as fresh focused runs.

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

## Real GUI follow-up — 2026-09-30

User explicitly requested the missing clicks. Independent Unity6000.6.2f1 was driven by real OS mouse/key input, using normal presentation controls. Ordinary scenario initialization and explicitly labelled rare initial fixtures are separate from earned actions. Tactical choices in the ordinary Hotseat match used read-only AI suggestions, followed by physical target/button clicks; no direct resolver execution, autoresolve, outcome injection or human balance acceptance is claimed.

The previous input blocker is superseded. Actual input worked on the external display and Retina display. Diagnostic focus/reload interference was bounded; only the independent editor was restarted. A temporary observer also briefly attempted a stable-world hash during contact, so its telemetry became stale; its guard was corrected and only that affected fixture repeated. No production-code defect or intrinsic two-monitor incompatibility was established. Temporary diagnostics were removed before regression.

| Check | Actual result and evidence |
|---|---|
| S1 | **PASS**: ordinary West army A2→A24→B1→A24, two paid20 passages; zero-Tempo attempt pure. Unknown B absent initially. Real pan/wheel, both known worlds, army focus, Previous; no simulation mutation. |
| S2 | **PASS**: occupied exit paid20, stayedA24, no B/occupant reveal; closed exit pure. Ordinary B battle95 commands, five deaths, same-ID aftermath. Labelled joint fixture: B3/B2 defenders6 versus B6 attackers3, A6 force excluded; actual deployment and14-command lawful withdrawal match. |
| S3 | **PASS**: labelled opaque probe commits B1→B2→B5 while A is focused. West gets only Appeared/Lost atB2, not hidden route/destination. Real handoff curtain, history pause/fast/skip; unchanged simulation hash; history save/recreate/load and next side continuation. |
| S4 | **PASS**: ordinary West-first match ended R14, East24:West13 Pressure. Army1s usedB, army2s claimed A Mine/Waystation. Real B battle killed both participating commanders and three other figures; East’s one surviving Warrior continued Commanderless and claimedB4. Short East-first mirror reached both portals/B andR2. |
| S5 | **PASS**: labelled temporary probe R4 Whiteout2, R5 Whiteout1, R6 closed; army remained inB, movedB1→B2 and continuedR7 after load. Seven exact full-state save/recreate/load pairs include normal aftermath, victory, blocked passage, Whiteout, stranded, observations and joint aftermath. |
| S6 | **PASS, bounded measurements**: actual OS pan/zoom samples below; no display-FPS or production guarantee. Fresh view creation measured separately through public initializer, not input-to-photon. |
| INPUT03 | **PASS short controlled GUI**: Fire primary pin/cast leaves4 Movement then legal move leaves3; Ice after movement leaves3 after cast, then2; Exhausted Fire Armor caster moves; HH self-heal10→24 with one use; Frozen has0 Movement/used Action and repeated movement clicks are pure. Pinned route survives hover, Escape cancels. Joint Fireball→Escape→World→save/load keeps use1. Focus-loss/material-state reconfirmation remain automated coverage, not separately claimed GUI. |

Joint aftermath retains both West armies separately inB, all six characters Escaped/Safe,60Tempo each; the attacker has50. FireMage use1 persists exactly. No exit healing/Refresh or free source refill. Seven scenario replay verification summaries are successful (normal95, joint14, Fire2, Ice3, FireArmor2, HH2, Freeze9 commands); retained partial Freeze journals also remain. The full suite separately checks32 historical rules1/2 recordings.

Performance: Apple M4 Pro CPU/GPU, Game render2800×1732, two editors running with read-only observer sampling.20 physical drag/wheel sequences per condition. A-only sampled unscaled frame median/max0.997/6.610ms, allocated395,973,919→395,977,319B; A+B1.097/6.331ms,399,062,579→399,064,243B. GameObjects882→882 and one simulation hash in each run. No obvious sustained pan stall or object growth observed. New view initialization in a recreated presentation reached a fresh observer snapshot in~25ms; includes scheduler/100ms sampling, not measured screen presentation latency. Prior100+100 API camera measurements remain historical supporting evidence, not new physical clicks.

Observations: fitting both worlds makes small node labels require zoom. B is a useful second approach/objective, not a faster portal shortcut. In this deliberately prolonged holding match all continuing armies eventually reached Hungry; no supply/Pressure/combat retune made. One controlled game cannot establish first-mover or camping balance.

Evidence: [manual summary](Evidence/54/manual-followup/manual-summary.json), screenshots, compressed full observer states/traces, actual saves/replays and diagnostic provenance under `Evidence/54/manual-followup/`. Read-only snapshots are intentionally omniscient developer evidence; the player-facing information filter was checked separately through rendered UI. No unresolved gameplay defect from this follow-up.

## Launch / inspection

Open isolated `Seamless-Worlds-07` with `bash Tools/launch-seamless-worlds-07.sh`. Menu **Gate C → Seamless Worlds 07 → West starts** (or East starts / Ice-vs-Fire variants). Existing TacticalGraybox scene is the host; ordinary Play alone does not choose07.

Additional labelled entries: **Temporary Route - CONTROLLED**, **Opaque edge - CONTROLLED**, **Inspection - AUTHORED portal arrival**. Inspection JSON: `Assets/StreamingAssets/SeamlessWorlds07/authored-portal-arrival.json`. It was generated by initial Core commands A2→A24→B1 (R1,20Tempo left), not earned by mouse play. It supplies known West portal mapping and an immediate view of both worlds without claiming match completion.

## Isolation / deviations / remaining blockers

Nine original main scene/settings/metadata files are byte-identical to their captured baseline; main saves are audited in the manifest. Separate company/product `RPGPrototypeIsolated/Seamless-Worlds-07`; independent batch/GUI/player preference roots under `/private/tmp/seamless-worlds-07`. Main settings were never overwritten. The main EditorBuildSettings scene entry was copied only as an uncommitted local test dependency; six legacy generated metadata counterparts remain untracked. Isolated ProjectSettings/company/product are deliberate environment configuration; do not copy them wholesale to main.

No deliberate numerical/gameplay deviation from54 identified. Main delivery is pending by design. Required controlled S1–S6 and short INPUT03 GUI checks are completed above. Coordinator acceptance, main delivery and player evaluation remain pending; independent validation is not permission to overwrite the active main session. Coordinator review precedes player evaluation; no next package, no push.
