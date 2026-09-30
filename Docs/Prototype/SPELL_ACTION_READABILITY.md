# Spell Action readability — 2026-09-30

Starting main `develop @52827b5`; isolated validation in Realm-Operations-06. User reports Fire Stream still appears available after casting.

Reproduced in actual Presentation PlayMode: Core Action becomes unavailable and rejects a repeat Cast with NoAction, but the geometric spell envelope and actionable-looking buttons remain. The new regression fails on the old UI (nonempty range after Action). No repeated-cast Core defect was found.

Presentation-only correction: hide spell aim range/footprint/recipient highlight after Action is spent, disable spell/primary-spell/staff controls and show explicit Action-spent/end-activation guidance. Invalid clicks remain read-only; next activation restores range/control eligibility. Non-spell Basic/Move selection retains existing behavior. No combat values, movement rules, spell geometry, source budgets, replay format or saves changed. Ordinary mage movement remains forbidden after Action under existing Core rules.

Regression covers default hostile click and explicit Stream selection, Core NoAction, disabled actual UI controls, hover/inspection, repeat click+confirm no state/RNG/journal mutation, existing movement restriction, next-activation recovery and replay. A development test initially assumed movement after Action was legal; corrected to the existing Core rule without changing gameplay.

Focused final:7 PlayMode PASS. Fresh full585 EditMode +74 PlayMode =659 PASS,0 failed,0 skipped. Full results and source hashes: `Evidence/SPELL-ACTION-READABILITY/validation-manifest.json`. Manual mouse validation NOT RUN for this bounded fix; automated checks instantiate the real UI. Existing user Unity was not clicked or restarted for testing.
