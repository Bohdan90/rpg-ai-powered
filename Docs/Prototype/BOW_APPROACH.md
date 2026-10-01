# Bow approach + shot — 2026-09-30

Starting main `develop @8fa995c`; implemented/tested in isolated Realm-Operations-06. User extends the existing two-click approach interaction to Archers.

Hover previews a shortest legal movement route to a valid shooting position. First enemy click pins it; second click moves and then submits the existing Bow attack after real movement/OA and Core revalidation. Selection, cancel and hover consume no state/RNG. Repeated Confirm cannot repeat the attack. A currently legal shot fires in place; no extra approach for accuracy. If no firing position is reachable this activation, no partial approach is automatically executed.

The existing approach query is shared through `QueryBow`, preserving existing melee callers and straight-line tie-break. Candidate destinations are bounded by remaining Movement and actual profile range. Core validates path, retreat-zone exclusion, LoS, engagement and attack eligibility. Shortest path wins, then fewer potential OA reactions, then direct-line alignment. An initially engaged Archer does not automatically disengage; existing explicit melee fallback is unchanged. Follow-up remains a Bow command and cannot silently become melee if movement changes eligibility.

Preview shows destination distance, range and post-move contact/Guard, explicitly without Steady Aim. Current Bow Range10, Accuracy85, movement, damage, distance penalties, combat rules and persistence are unchanged. No AI change. Commands remain ordinary MoveCommand and BasicAttackCommand; existing recorded replays retain their semantics.

Focused18 EditMode +13 PlayMode PASS. Coverage includes minimum3-step approach from range13, Movement4 boundary, unreachable target, real LoS obstruction and impassable wall, no spent-Action/Movement/engagement bypass, preview purity/determinism, pinned route/target, cancel, exactly one move/shot pair and identical replay. Full regression results are recorded in `Evidence/BOW-APPROACH/validation-manifest.json` when complete.

Mouse-driven manual validation NOT RUN. Automated PlayMode exercises actual Presentation. Tests use isolated preferences/saves; no user-window input or forced Unity restart. Delivery is recorded separately after source/hash verification. NO PUSH.

Regression follow-up: initial fullPlayMode76 PASS/4 FAIL exposed old UI expectations that blocked direct shots must always disable Confirm. Tests now preserve Core OutOfRange/BlockedLineOfSight assertions and verify the lawful movement/shot alternative. Fully sealed, unreachable geometry still disables confirmation. Current-shot refusal reason remains visible alongside the approach. An intermediate focused35 PASS/1 FAIL caught a mistaken positive expectation for that sealed fixture; corrected to retain refusal, focused corner/range7 PASS. No combat geometry was relaxed. Full suites rerun on the final sources.

Final full regression: **603 EditMode +80 PlayMode =683 PASS;0 failed;0 skipped**. Source hashes match the final tested implementation.

Main delivery: `My project/develop` fast-forwarded `8fa995c → b448334`; tested source hashes match. Nine unrelated files preserved. Save verification and exact delivery: `Evidence/BOW-APPROACH/main-integration.json`. No forced Unity restart or push.
