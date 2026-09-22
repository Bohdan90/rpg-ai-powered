# Persistence Slice v0.1

Gate C passed its current prototype scope through user Player-vs-AI playtest. This
slice proves one persistent formation through Battle 1 → no Strategic Refresh →
Battle 2 → one field Strategic Refresh → Battle 3. It is not a campaign, world
map, save/load system, recruitment simulation, recovery service or resurrection flow.

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
