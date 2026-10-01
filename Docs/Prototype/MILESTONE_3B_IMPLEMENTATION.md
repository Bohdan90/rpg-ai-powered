# Milestone 3B — engagement and withdrawal presentation

Implemented 2026-09-21 against HEAD `97f66b1`, which includes compact-path correction `a9aa029`. Unity remains **6000.6.2f1**. No Core, combat rules, contract, packages, scene, or project settings changed.

## Files and boundary

- `Assets/_Project/Presentation/BattlePresenter.cs`: consumes `ZoneOfControl.Sources`, `OpportunityAttackPreview.Query`, `Battlefield.IsRetreatZone`, Core availability and outcomes; presents exact paths, named step risks, conditional escape and chronological command events.
- `Assets/_Project/Presentation/BattleGridView.cs`: threat borders, retreat stripes, risk segments and distinct unit silhouettes. Views refresh from returned Core state; dead/escaped units disappear without simulating movement or attacks.
- `Assets/_Project/Presentation/BattleHud.cs`: readiness labels, risk confirmation/cancel, own retreat edge, Safe roster, outcome panel and restart.
- `Assets/_Project/Tests/PlayMode/EngagementPresentationTests.cs` and its `.meta`: nine focused integration tests.
- This implementation report.

`RPG.Presentation -> RPG.Core` remains the only dependency direction. Core source is unchanged, contains no Unity imports/references, and its assembly retains `noEngineReferences: true`.

## Presentation behavior

Enemy ZoC cells have thin red borders when a Core source has OA available, gray when all sources are spent. Hover identifies sources individually; archers have no ZoC. Tokens and initiative queue show OA ready/spent/no OA directly from Core state.

The exact Core-selected path remains gold, with eligible OA exit segments red. Preview lists each exit step and responders in Core order, including spent or already-used-on-this-path responders. Eligible risks change the existing explicit confirmation button to `Confirm Move — accept N OA risk(s)` and expose Cancel. Safe paths retain their ordinary single confirmation. No path rerouting or random-outcome prediction is performed.

West/East retreat stripes remain visible; `YOUR ESCAPE` and HUD text identify the active side's valid edge. Preview queries Core retreat cells and shows current pools. With OA risk it explicitly conditions escape on survival and says pools after OA are preserved. Escaped units have a separate Safe roster and leave the board/activation queue.

The outcome panel uses only `BattleState.Outcome`: winner, loser, Withdrawal/Eliminated, Dead, Escaped/Safe and surviving active rosters. It scrolls into view on battle completion, disables combat controls and offers Restart Same Seed. Commander removal does not independently create any result.

Unit shapes: **HW square; HA circle; EW diamond**. Blue West/orange East remain side colors; facing arrows remain independent of body shape; `*` marks Commander.

## Automated validation

Unity 6000.6.2f1, isolated project copy with current Assets:

| Suite | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| EditMode (all existing Core tests) | 158 | 0 | 0 |
| PlayMode (11 existing + 9 new) | 20 | 0 | 0 |
| Total | 178 | 0 | 0 |

New tests cover Core preview equality/no mutation, safe/spent risks, Core ZoC sources and archer exclusion, responder/event order, death interruption, both retreat sides, wrong-edge movement, conditional risky escape and Safe status, Commander partial retreat, terminal controls and UI-button restart restoring initial state.

XML evidence for this local run: `/private/tmp/gate-c-m1-4dkkwcy6/TestResults.xml` and `PlayModeResults.xml`. No generated validation artifacts are committed.

## Mouse-driven Play Mode validation

Performed in a separate Unity Editor instance/project copy. A temporary editor-only helper installed deterministic Core fixtures and recorded state/UI text; actual cell selections, scrolls and confirmations were physical macOS mouse events. The helper did not submit movement or attack commands. Test fixtures/helper are outside this repository, not new gameplay content.

| Scenario | Observed result |
| --- | --- |
| 1. Enter enemy ZoC | Step executed, no OA warning or attack. |
| 2. Remain adjacent | Step executed, no OA. |
| 3. Leave one enemy | One named exit risk; confirmation; OA before step. |
| 4. Leave two enemies | Both named; EW then HW, matching Core priority. |
| 5. Spent responder | Return/exit preview marks spent; no second OA. |
| 6. OA miss | Roll 61 vs 60; movement continued, pools unchanged. |
| 7. OA hit, survive | Roll 54 vs 75; Armor 6 to 0, HP 32 to 26; step executed afterward. |
| 8. OA death | First path step executed; OA killed mover before second step; remained at pre-exit (2,3); remainder cancelled; view removed. |
| 9. Safe own-edge entry | Escaped/Safe; view removed; HP 32 / Armor 6 preserved. |
| 10. OA before retreat | Conditional risk + escape preview; OA resolved, then escape with HP 26 / Armor 0. |
| 11. Enemy edge | Unit remained Active at (12,4); no escape. |
| 12. Partial retreat | Safe roster updated, next actor activated, battle ongoing. |
| 13. Commander retreat | Commander Safe; ally remained active, no defeat panel. |
| 14. Last active retreats | East Victory / West Withdrawal, Safe roster distinct from Dead. |
| 15. Last active dies | Ordinary attack produced West Victory / East Eliminated and correct Dead roster. |

Local screenshots: `/private/tmp/gate-c-3b-base.png`, `gate-c-3b-oa-preview.png`, `gate-c-3b-risky-retreat.png`, `gate-c-3b-withdrawal.png`, `gate-c-3b-eliminated.png`. Text snapshots are `/private/tmp/gate-c-3b-*.txt`. These are temporary evidence, not portable repository assets.

## Assumptions and limitations

Presentation-only choices: reuse the existing single explicit confirmation for risk acceptance; show full enemy ZoC with restrained borders; snap to final Core state and explain intermediate execution in chronological text; retain the latest 200 event lines. No new gameplay prototype assumption or contract deviation.

The narrow graybox HUD remains scrollable: longer previews place confirmation/actions below the fold. No animation is added; interruption details are visible in event text and final state rather than animated frames.

No gameplay/presentation fault was observed in these checks. The temporary Editor emitted unrelated Unity SearchDatabase initialization and Unity account/subscription service errors; these did not prevent compilation, tests, or Play Mode interaction. Existing GrayboxPlayModeTests also retain obsolete Unity object-search API warnings; no new compiler warnings were introduced by this milestone. This pass does not fix Unity service/editor internals.

## Launch

Open this project in Unity **6000.6.2f1**. Open `Assets/_Project/Scenes/TacticalGraybox.unity` and press Play (or use `Gate C > Play Tactical Graybox`). No new scene setup is required. Click a cell/target, inspect the preview, then confirm; scroll the right panel for actions. Use Restart Same Seed to reset. Both sides remain locally controlled.

Stop here: no AI, telemetry, new mechanics, production assets, or strategic systems.
