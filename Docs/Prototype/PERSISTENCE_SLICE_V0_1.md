# Persistence Slice v0.1

Gate C passed its current prototype scope through user Player-vs-AI playtest. This
slice proves one persistent formation through Battle 1 → no Strategic Refresh →
Battle 2 → one field Strategic Refresh → Battle 3. It is not a campaign, world
map, save/load system, recruitment simulation, recovery service or resurrection flow.

Implementation started from `64b6f17`; persistent roster/attrition continuity is
`625ff80`. The progression/harness follow-up is recorded in the repository history.

## Core state

`PersistentCharacter` owns a stable character ID, profile, Commander flag, current
HP/Armor, Alive/Escaped-Safe/Dead state, Personal XP/Level and, for a Commander,
Command XP/Level/derived Rank. `PersistentFormation` owns the persistent roster and
Commanderless/roster-lock state. Core converts persistent members to tactical units
and resolves the tactical end state back to the same records.

- Dead members remain records but do not deploy and have no replacement.
- Escaped/Safe members retain exact pools, return to the continuing roster before the
  next battle, and receive no recovery merely for escaping.
- One field Refresh adds exactly 15% Max HP. Tactical HP pools are integral, so Core
  carries fractional hundredths across Refreshes rather than discarding them. Armor
  never recovers in the field.
- A dead Commander makes a surviving formation Commanderless and roster-locked. No
  Acting Commander, numerical Morale system, free replacement or doctrine is added;
  the remnant can still fight.

## Progression and valuation

All current slice profiles are Tier I. Base Power is `10 + 0.2 × Personal Level`.
Battle-start HP/Armor calculate readiness and Effective Power. XP weight is `20%` of
Base Power. Victory uses the owner-17 underdog multiplier; defeat uses 50% of earned
enemy weight. Personal shares reserve against all eligible participants at battle
start, paying 100% alive, 25% escaped and 0% dead without redistribution. Command XP
is a separate `pool / 7` share for the single participating army. Personal Level is
one per ten XP; Command Level is one per ten Command XP with a one-level-per-battle
resolution cap and retained overflow. Rank derives I 1–3, II 4–9, III 10–19, IV 20+.
Level thresholds do not promote class/Tier or add doctrines.

## Deferred

World map, settlements, city recovery/Armor repair, recruitment/replacements,
economy, strategic AI/movement, graves, remains cargo, resurrection, capture,
Morale numbers, doctrines, promotion/training, artifacts, magic, campaign saves and
real siege systems remain out of scope.

## Developer flow

`Start Persistence Slice v0.1` starts the West persistent formation on the accepted
23×17 ordinary field. After a terminal battle the HUD shows the resolved persistent
roster. `Continue Persistence Battle` starts Battle 2 with zero Refresh, then Battle
3 after exactly one field Refresh; it is enabled only after the current battle resolves.
The opposing side is a fresh deterministic scenario force per battle. This is the
intentional minimum needed to test one persistent player formation rather than a
campaign-persistence system.

## Validation

The scenario starts with the existing nine-figure ordinary-field reference roster on
each side. Core tests cover exact no-refresh attrition, Safe return, dead slots,
Commanderless remnants, the one field refresh, battle-start readiness, victory and
defeat pools, retained personal/command overflow and derived Rank. Presentation tests
cover the 23×17 entry point and prove preview/rejected commands leave both persistence
summary and RNG unchanged. Existing replay remains per-battle: the persistence layer
only supplies the deterministic next initial state and does not alter tactical command
or replay truth.

Presentation smoke coverage confirms the new start control, compact roster summary and
accepted 23×17/18-figure framing. The complete three-step transition was exercised
through the deterministic Core scenario harness, including a prepared attrition result
(one survivor at 17/40 HP and 5/16 Armor), zero recovery into Battle 2 and 23/40 HP
after the one field Refresh into Battle 3. A natural full three-battle mouse campaign
was not used as a balance verdict; its dedicated coordinator playtest remains the
appropriate next observation.

Focused validation: **9 EditMode + 2 PlayMode**, all passed. Final regression on Unity
6000.6.2f1 in an isolated project copy: **289 EditMode + 36 PlayMode = 325 passed;
0 failed; 0 skipped**. No tactical replay format or resolver changed; the deterministic
next-battle-state regression uses equal prior persistent state and result to produce an
equal initial-state hash and persistence summary.
