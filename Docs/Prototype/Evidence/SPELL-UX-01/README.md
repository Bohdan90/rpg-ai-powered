# SPELL-UX-01 evidence

2026-09-30, isolated `Spell-UX-01`, base `da9f776`, Unity 6000.6.2f1.
Screenshots + read-only GUI state captures accompany **real macOS mouse input**, not Core-authored final outcomes. The temporary observer only exports UI/state/coordinates and read-only legal escape suggestions; fixture initialization is explicitly labelled. It is removed from the production project before regression/commit. Main editor was never targeted.

- 01–05: actual Fire vs Ice Lab, Self refusal and empty-center Fireball hover/cast.
- 06–13: labelled four-unit initial fixture (`Fire TII (8,8)`, enemy HW `(11,11)`, friendly HW `(8,9)`, enemy Ice TII `(12,8)`); diagonal primary, Ice primary, Friendly Fire, pointer transit to Confirm. No battle outcome injected.
- 20–24: fresh normal 05B Fire/Ice presets, legal West→6 / East→9, attack, Fireball, East physical Escape, return, Save → recreate → Load. R2 retained; all 10 persistent IDs/pools retained, West caster FireballUsed=1; loaded checksum equals saved checksum. Short transport smoke, not a full match/balance claim.
- 30–32: actual Support vs Fire Lab, obstructed ray / off-ray / fourth-cell explanations.
- 33–39: labelled Ice fixture `(8,8)`, enemy HW `(10,8)`, friendly Fire TI `(8,9)`. Cancel, movement, friendly inspection, explicit staff, shield and handoff.
- 40–41: actual Fire vs Ice Player West vs AI East; AI moves and casts Ice Shard through the ordinary Core resolver.

Some legacy read-only observer HUD dumps include inactive labels; screenshots establish what was visible. Export directories may contain multiple snapshots of the same short sequence; each JSONL has its own header/footer. Historical 585 PASS is not reused as a new run.

Raw full-resolution screenshots/trace/helpers and failed development logs: `/private/tmp/spell-ux-01/`. Curated images, state captures, replays and final automated XML/logs are retained here. 50–53 recheck the final camera-fit and hover cleanup, Self and successive-activation primary clicks after the full suite. Earlier GUI images may show the superseded generic movement-hover label. All17 stored accepted-baseline replays were reverified (898 commands), in `accepted-baseline-replays.txt`.
