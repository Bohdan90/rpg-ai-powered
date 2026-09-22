# Gate C — minimal tactical AI

Contract §10 prototype policy; no combat/tuning change. Hotseat remains default.
Controller dropdown selects Player West vs AI East. AI submits one ordinary Core
command at a time, reevaluating after every applied command (including OA interruption).
Player command controls are disabled on the AI turn. No production/strategic AI.

Candidates: stay and Core-reachable endpoints with readable shortest paths, plus
minimum expected-OA-HP-loss routes within current Movement. Safe routing keeps
non-dominated cumulative damage distributions by position, facing, already-reacted
IDs and cost. Geometry comes from MovementRules; reaction order from ZoneOfControl;
probabilities/damage from the same resolver calculation exposed as a pure OA preview.
Damage distributions cap at death and account for armor, Guard, facing and one OA
per responder. Retreat endpoints stop path expansion. No random draws in evaluation.

At HP <=25%, reachable escape with expected loss below current HP is preferred;
lowest expected OA HP loss, then cost and stable traversal. Otherwise candidates use
exact contract weights: enemy HP + .5 armor + 12 kill probability - OA HP - .5 incoming
HP + nearest-enemy Chebyshev improvement clamped -2..2. Healthy-unit escape endpoints
are excluded from ordinary positional candidates (P: evacuation is the explicit
low-HP policy, not an accidental incoming-score optimization).

Incoming is a conservative independent next-activation estimate: each living enemy
gets its best legal single attack from reachable cells, using refreshed action/movement
and normal bow/fallback/LoS/Cover previews. Other units remain at visible positions;
no coordinated moves. It uses current pools, without speculative post-hit/kill removal
or future OA damage. Score components are separate estimates, not a full probabilistic
battle rollout. This is a prototype AI approximation, not new combat truth.

Ties: less Movement, then stable traversal: endpoint X/Y, enemy ID attacks, Defend,
End; ordinary path before equal-scoring safe path. Safe path expansion uses clockwise
Core-compatible directions and ordinal sorted label keys. Neither RNG state nor future
rolls influence decision evaluation. Current board has no hidden-information layer.

Explainability: compact AI score/components, endpoint, planned follow-up target/action
in HUD and recent log; no candidate dumps. Move commands contain the exact chosen path;
normal step/OA events explain execution. Expected endpoint benefits/incoming are weighted
by probability of surviving the preceding OA path; expected OA loss includes lethal branches. Controller tick delay is cosmetic .4 seconds.

Validation: nine focused Core cases (including both 9v9 maps), plus PlayMode controller
integration. Full suite results and mouse smoke evidence recorded in checkpoint.
Limitations: one ply can stalemate at obstacles or choose weak positional plans; it is
not evidence that combat is good/bad. No coordinated tactics or strategic retreat.

Final validation: **257 EditMode + 28 PlayMode = 285 passed, 0 failed/skipped**.
Mouse checks in isolated Unity: both 9v9 boards, prepared 18-unit shooting/contact
and low-HP retreat situations; selector enabled Player-vs-AI, ranged attacks occurred,
field exit produced ordinary OA, siege melee crossed the west opening and attacked,
both field rear and siege perimeter escapes removed the unit. No invalid loops.
Prepared positions isolate behavior; no full battle/stalemate claim. A hot-reload UI
artifact in the test Editor required a fresh launch; fresh-launch checks passed.
