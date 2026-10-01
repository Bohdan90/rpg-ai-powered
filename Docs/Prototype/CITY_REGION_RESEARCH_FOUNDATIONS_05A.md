# City / Region / Research Foundations 05A — documents 50 + 51 batch

**Latest follow-up (2026-09-30): N1–N5 and V1–V5 controlled GUI checks completed.** Main integration was explicitly authorized. Current measured regression, deviations and evidence are in [acceptance completion](5051_ACCEPTANCE_COMPLETION.md). Earlier NOT RUN statements below are historical delivery records, superseded by that report. Coordinator/player acceptance remain separate.

## Authority and isolation

Based on live `develop @ 045707f81847fb88ac34c724d5f87ce30fba5c39`, gameplay `20473ec`. No prior 50 implementation existed at entry. Work is confined to `../Isolated-50-51`, branch `feature/50-51-connected`; no integration into develop, no push. Document 50 plus its addendum owns 05A; document 51 independently authorizes Combat Lab and 05B. 04 player acceptance remains PENDING, with the reported combat-variety feedback preserved.

## Implemented

05A uses a separate 23-node graph, protected rear Cities, no incidents, Pressure target 24. The old 01/03/04 maps/rules remain separate. Nodes 14/15 are fixed-parent Minors; 16–19 and 20–23 are their Gold/Food/Wood/Iron sources. Capture changes controller and keeps parent links, stocks, completed infrastructure and paid project history. It pauses the paid project and removes the former owner's unpaid queue.

Per-side initial resources: Gold300, Wood30, Iron20; City Food36/cap90; Provisions30/30. Source baselines are 30/6/6/4 per Refresh, source buffers 3×baseline, Minor buffers 4×baseline per resource. Decimal accounting preserves fractions through save/load. Gold/Wood/Iron enter global pools only on legal City delivery; Food remains local. Existing North Mine +75, Waystation24/up to6 and Keep supply up to6 are unchanged.

Production drains downstream stores, admits only output with actual available capacity, then transfers admitted output. Full stores stop production. Regional output is normalized against the same resource baseline and counted once at the fixed Region under common control. UI distinguishes current projection from the last resolved cycle; project starts use projection, advancement rechecks actual output. Source condition is a saved numeric fixture seam, initially full; no raid/damage/repair gameplay was added to 05A.

Paid City growth II/III, Forge, Research Institute and two Minor boosters have the exact document-50 costs/times. One active construction per location plus unpaid ordered queue; activation pays once, no work on activation Refresh, paused progress/reservations persist, cancellation gives no refund. Institute replaces Center and reserves +2 physical Deep Load. The generic accounting-only owner matrix (including IV capacity6 and cultural thresholds) has explicit rule tests; excluded buildings/culture systems are not playable additions.

One active realm technology plus queue, Center2/Institute6 Work, 40 Gold once. Extraction/Assay cost6 Work; Research Method/Forge Organization12. Stop/requeue retains accepted work, payment and per-City provenance. Final partial Work is proportionally credited with deterministic remainder. Research-completion access and new infrastructure do not retroactively contribute to the current cycle. Universal technologies do not acquire invented racial trace.

Forge quotes each living persistent ID for `min(missing Armor, ceil(.5 MaxArmor))`, costs2 Gold/point, pays once and requires the full following Refresh at that City's functioning Forge. Departure cancels without refund. Completion applies the original quote only to the same living IDs; no HP, Max Armor, Ward, revival or replacement effect. Commanderless repair is allowed. Existing ordinary HP recovery remains independent.

05A/05B reuse the existing tactical bridge, recruitment queue and Hotseat. Stable-state schema4 snapshots persist locations, exact quantities, project payment/steps/reservation, research/provenance, Forge quotes, training, direction, budgets and all existing formation/world state. Restore builds and validates a candidate before the live presenter changes. 01/03/04 retain their namespaces and legacy schema handling.

## Validation and manual boundary

See `COMBAT_VARIETY_AND_CONNECTED_05B.md` and `Evidence/5051/validation-manifest.json` for final run counts, exact implementation commit and fresh XML/log paths. Focused economic tests exercise capture, saturation, fractional production, projection, paid queues, reservation, research credit, Forge continuity, recruitment/training, corruption rejection and deterministic continuation. Automated PlayMode tests perform actual JSON save → scene/session destruction → load → continue.

N1–N5 are **NOT RUN as mouse-driven human matches**. No independent desktop/input session is exposed; the available macOS GUI belongs to the user's running game. No clicks were delivered to that window. Programmatic controlled matches/fixtures are not substituted for manual passes.

Manual residual procedure:

| Case | Starting state and actions | Observe / expected |
|---|---|---|
| N1 | Fresh 05A; both humans leave protected Cities, contest Mine/Beacon/Waystation, pay one construction and technology, fight with actual casualties, finish by Pressure24 or elimination. | Costs/progress visible; persistent losses return to same World; legal winner. |
| N2 | Earn DevII + Forge Organization, build Forge; after a battle return damaged survivors, inspect/pay quote, stay for the full next Refresh and re-enter battle. Repeat with departure before completion. | Same-ID Armor only, one charge, no departure refund or field repair. |
| N3 | Start DevIII/Institute; opponent captures sufficient sources/Minor. End global cycles, recapture and resume. | Lost support pauses steps; retained paid history/stock/parents; completed City does not downgrade. |
| N4 | Save during nondefault paid construction/research/Forge/recruit state; stop/recreate this isolated session, load and continue same commands. | No double charge, income, progress, repair or recruit; same IDs. |
| N5 | Mirror StartingSide and compare alternative development/booster/Forge/research spending. | Record opportunity cost and strategic choices; no balance acceptance inferred from tests. |

No production city system, Cultural Influence/Dominion, siege, resource conversion, research UI beyond this tranche, or recruitment economy outside the authorized profiles was added.

## 2026-09-30 coordinator follow-up

Both contracts remain **PARTIAL**. See [requirement-level acceptance audit](5051_ACCEPTANCE_AUDIT.md) for named-test coverage, unasserted branches, checked numeric values and manual evidence. The prior 553 PASS are valid historical evidence, not complete coverage of every required branch. Main integration was explicitly authorized by the user after the original NO MERGE instruction; integration does not grant technical/manual/player acceptance. Runtime changes in this follow-up are limited to the reproduced tactical ability/command discoverability defect. No rules or balance changes. Fresh validation and manual results are in `Evidence/5051/validation-manifest.json`.
