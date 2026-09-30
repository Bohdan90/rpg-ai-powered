# Two-click tactical interaction — 2026-09-30

User explicitly replaces the former primary-spell one-click adapter. Starting main `develop @3271802`; implementation/tests in isolated Realm-Operations-06.

Hover computes the existing Core path/attack/spell preview without arming execution. Leaving the cell changes the transient preview; leaving the board clears it. First left click pins the destination/target and exact path/area. Hovering elsewhere, leaving the board or inspecting an ability does not replace a pinned choice. A click on a different cell replaces the pin without execution. A second click on the same cell submits exactly the pinned legal command once. This is two sequential clicks with no double-click timing requirement.

Applies to melee, Bow, primary Fire Stream/Ice Shard, explicit spells and movement. Plain ground remains movement unless a ground spell was explicitly selected. Ally inspection does not turn into an implicit cast. Confirm remains an optional equivalent second step and cannot execute hover-only previews. Cancel/action switch/activation change clears the pin. Existing camera zoom/recenter also cancels the preview. Friendly Fire still needs explicit checkbox consent; repeating a blocked target does not grant consent. No automatic walking/attack fallback.

Live movement previews now render the actual Core path/OA risk. Basic attack target cells are highlighted; spell footprints remain Core exact. Spent-Action UI fix retained. No Core combat/path/range/Action/RNG/replay/persistence rules changed.

Focused10 PlayMode PASS: prior spell UX/Fire Armor/spent-Action/replay coverage plus movement hover/pin/reselection/leave/commit, melee/ranged/Fire/Ice target pinning, empty ground spell, cancel/action changes and explicit Friendly Fire. Hash/journal checks distinguish preview from committed commands. Fresh full585 EditMode +77 PlayMode =662 PASS,0 failed,0 skipped. Full suite provenance is in `Evidence/TWO-CLICK-TACTICAL/validation-manifest.json`. Mouse-driven manual validation NOT RUN; actual Presentation/UI is covered in automated PlayMode. No user window clicks/restart for testing.
