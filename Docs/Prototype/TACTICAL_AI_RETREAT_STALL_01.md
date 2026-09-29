# WP-03E — Tactical AI retreat / wall stall

Starting develop / `e7036eb15a8ae2b929d3c5f06514512cfe61d2f5`.
User accepted graybox combat, Provisions and threat readability after WP-03D.
This is a reproduction-first local AI fix, not a balance redesign.

## Reproduction and classification

The local user replay directory contains only older September22 recordings, not
this reported battle. The precise original seed/commands therefore remain unknown;
these are reproducible analogues, not a claim to have recovered the user's match.

At the unmodified baseline, fresh `wp03e-reproduce` EditMode run: **1 passed,
2 failed** (intentional new regression tests; existing tests not changed to fail).

- **D confirmed.** Seed2,13×9 control board, HW(5,4), opposing HW(7,4), solid
  wall x6/y1..7. Eight consecutive activations choose Defend/End at(5,4), no
  attack. Safe legal detours exist above/below. Raw Chebyshev approach reward
  penalizes the necessary initial detour, so the one-ply policy waits forever.
- **Multi-activation retreat planning gap confirmed.** HP8/40 HW(5,4), own West
  edge x0, Movement4, distant opponent(11,4). Baseline chooses(7,2), away from
  evacuation. Existing25% HP priority only considers exits reachable *this*
  activation, so there was no long-range retreat approach/continuation.
- **A is legal, not a rules defect.** Once Movement is exhausted one cell before
  the zone, no extra move is granted. With refreshed Movement, the AI exits on
  the next activation. Automated and controlled Unity evidence below.
- **B not reproduced:** baseline adjacent, eligible, safe reachable exit already
  evacuates. Keep that behavior and existing expected-OA safety preference.
- **C not reproduced as stale state:** baseline AI has no cached target. A real
  blocker movement changes the next query; an alternate exit is selected. New
  long-distance query also replans every command, with no target cache.

## Exact implementation

Only production Core files changed: `TacticalAi.cs`, `TacticalAiPaths.cs`.
No stats, Movement/ZoC/Retreat rules, maps, strategic AI, Provisions, recovery,
XP/rewards, persistence fields or replay format changed.

- Pure reverse multi-source BFS computes legal distance to goal cells using
  `MovementRules.ValidateStep`, active occupancy and corner rules. Own retreat
  cells terminate a route rather than becoming transit shortcuts. It does not
  grant Movement; executable prefixes still come from current-budget Pathfinder/
  existing safe-route/OA queries and execute through the same Core resolver.
- Existing HP<=25% immediate evacuation remains first. If no safe immediate exit
  is available, choose a nonlethal-expected-OA, distance-reducing current-budget
  prefix toward a reachable legal exit; prefer maximum progress then risk/cost.
  Tactical HP does not regenerate, so the same existing threshold maintains this
  decision on subsequent activations. Blockers/targets are queried afresh.
  If no route exists, use the ordinary legal policy; no teleport/new morale state.
- Only when **all enemies are hidden by solid LoS and no useful attack is reachable**,
  use distance to legal attack setups for the existing capped approach term.
  Open approaches, useful attacks, incoming-damage/OA scoring and all weights are
  unchanged. This permits wall detours without forcing suicidal charges or
  removing meaningful cover. No global anti-camping mechanic or AI memory added.
- Existing explanation text clarifies exhausted Movement / re-evaluation next
  activation; no new UI/persistence truth.

An initial broad setup-distance experiment changed ordinary fights unnecessarily;
it was narrowed before delivery to the wall-occluded case above.

## Regression fixture adjustments / observed impact

Fixing an AI policy can change ordered commands and casualties for a fixed seed,
without changing combat RNG rules. Do not present the old tactical result as an
invariant of recovery, intel or strategic path tests.

- Original automated North return via optional Bridge now loses its final B fight
  (one persistent survivor; historical failing XML retained). The northern outward
  route with return over existing southern edges avoids that optional fight and
  succeeds R6 with5 Provisions. Graph, costs, enemies and timing are untouched.
  Central, South and +1 Central delay still succeed; no balance tuning was used.
  This is a visible policy consequence for coordinator/player review, not proof
  that every formerly winning sequence will still win.
- Bridge's controlled AI result is now eliminated instead of one escaped HW.
  The UI test checks actual removal; a separate deterministic withdrawn-roster
  fixture retains the1HW/withdrawal information check without relying on AI seed.
- After the original two battles, surviving HP damage is now fully covered by the
  intervening field Refresh. Recovery UI regression therefore plays an actual
  third B battle (leaves Portal uninvestigated so Keep return does not end mission),
  then saves/loads, heals at Keep and redeploys. It explicitly requires actual HP
  gain, unchanged Armor/identities, deterministic continuation and Patrol movement.
  No synthetic healing/damage injection or relaxed no-op-healing assertion.

## Validation

Fresh final-source focused runs: **21 EditMode PASS +16 PlayMode PASS**, zero
failed/skipped. Full EditMode: **371 PASS**, zero failed/skipped (19:13:11–19:14:10Z).
Full PlayMode: **52 PASS**, zero failed/skipped (19:14:23–19:15:55Z).
Total **423 PASS** on the final source diff; no code changes after these runs. Baseline reproduction failures above are intentional new
bug assertions; no unrelated baseline failure was identified.

Executed from the isolated project synced with `rsync -a Assets/_Project/`:

```sh
bash /private/tmp/wp01-20260928/run-tests.sh wp03e-focused-delivery EditMode -testFilter 'TacticalAiRetreatTests|TacticalAiTests|StrategicRouteTests'
bash /private/tmp/wp01-20260928/run-tests.sh wp03e-focused-ui-verified PlayMode -testFilter 'TacticalAiRetreatPresentationTests|TacticalAiPresentationTests|StrategicPresentationTests|StrategicRecoveryPresentationTests|EssentialReadabilityTests|StrategicSavePresentationTests'
bash /private/tmp/wp01-20260928/run-tests.sh wp03e-full-edit EditMode
bash /private/tmp/wp01-20260928/run-tests.sh wp03e-full-play PlayMode
```

Fresh XML/logs: `/private/tmp/wp03e-20260929/results/wp03e-{focused-delivery,focused-ui-verified,full-edit,full-play}.{xml,log}`
(copied from the runner's `/private/tmp/wp01-20260928/results/`). That directory
also retains baseline `wp03e-reproduce`, old North `wp03e-focused-local`, and
`tested-source.sha256`. Tests include existing preview/invalid/RNG, OA/Escape,
strategic save/continuation, 23x17/41x39 and ranged-overlay regressions.
Required Core cases:
adjacent exit; full prefix one activation short; next-activation exhaustion;
actual blocker movement/alternate exit; sealed exits/pure query; bounded wall
progress/no oscillation; useful attack from cover; wall retreat and replay.
PlayMode covers actual damage crossing25%, wall escape, one-cell-short next turn,
blocked exit and healthy wall approach through Presenter/Core.

Controlled Unity mouse validation (6000.6.2f1, isolated project, real Presenter AI
and Core commands): **all five PASS**. Screenshots were inspected; traces and
exported tactical replays were verified, not substituted for UI interaction.

| Fixture | Observed result |
|---|---|
| Short, seed2 | East HW(17,8) -> (21,4), Movement0, one cell short; next activation (22,5), Escaped. No extra Movement. |
| Damage/obstacle transition, seed1 | Real HA attack takes HW11->1HP; wall x19/y5..11; remaining legal distance8->4->Escape over two activations. |
| Blocked, seed2 | Player physically moves HA onto previously preferred exit(22,9); AI chooses(22,8), Escaped. |
| Healthy wall, seed2 | HW(10,8) detours around wall x11/y3..13, reaches(12,7) and attacks by R4. |
| Useful cover, seed2 | HA stays(4,4) behind wall and shoots exposed opponent(4,8); no reckless charge. |

External setup/mouse driver lives in `/private/tmp/wp03e-20260929/`; it is not
production code. It supplies controlled fixtures, clicks real player controls and
allows the existing AI Presenter to act. The screenshot header/fixture selector
can retain the base Hotseat/control-fixture label after this injected fixture;
actual controller is PlayerVsAI East and traces record the 23x17 custom board.
Evidence: `/private/tmp/wp03e-20260929/results/`, each fixture's `*-initial.png`,
`*-final.png`, intermediate activation/prepared screenshots, `*-trace.jsonl`,
`*-replay/` and `*-verification.txt`. All five exported replays match (2/3/5/6/15
commands respectively; see individual files). Editor Search indexing exception and Account API/licensing/subscription warnings
remain outside gameplay; no Core/Presenter failure observed. This is controlled developer
validation, not a new user balance acceptance.

## Handoff

Own local commit title: `AI: complete retreat approaches and escape wall stalls`.
Exact delivered hash from Git/final coordinator report. NO PUSH.
Starting scene/ProjectSettings changes and six persistence `.meta` files remain
untouched/excluded; exact final status will be recorded below.
Original user-match classification remains unverified without its replay; reproduced
D and the long-retreat gap are bounded, testable defects. New player acceptance
remains pending. STOP after this package; no Hotseat/Economy/City/Research work.


Preserved starting/final foreign status (own diff committed separately):

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

All nine files match preflight SHA-256; no reset/stash/cleanup of user work.
