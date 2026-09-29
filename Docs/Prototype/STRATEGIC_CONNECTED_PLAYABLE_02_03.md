# Connected Playable 02 + 03 — Crossroads Duel

Authority: Drive document 47 (`1E18nxF0LKPgXESMTun0ZXFv_wy-GE3Mn-y5YB4kHyOY`),
read in full, latest 44/46 append; task-local owner17 §1.3/§9.6/§9.8 and command
capacity, owner05 recruitment/Provisions, owner26 HP recovery. Prototype-only.
Start: develop / `55a269a4768ffb284804bbe81bf5ec63ac9eb985` (2026-09-29).
Current single-player slice KEEP; no reopening combat or Mission01 tuning.

## Part A implementation

Separate data-driven Crossroads Duel: document47's exact symmetric 13-node,
16-edge graph. Two persistent human-controlled 6-body armies (HW Commander,
3HW,2HA), L1/0XP/full HP/Armor, stable side-specific IDs. Existing tactical
Hotseat/resolver/journal, no tactical AI substituted for either side.

Fixed StartingSide (West or East); active-side authority and explicit handoff;
one global Refresh after both activations. Claim physical nodes06/07/08 on
activation end, persistent ownership, +1 Pressure per objective at Refresh;
target8, simultaneous threshold uses higher total, tie continues. No continuing
formation loses. Open strategic information is scenario-only.

Tempo100, existing 50/remaining/0 Attack Cost, defender no cost. Existing40
Withdrawal debt and deterministic <=2-hop displacement (max separation, then
movement cost, then node ID). Tactical physical evacuation still required after
battle starts; negative Tempo disables own tactical Retreat edge. Attacker victory
advances only when target vacated; defender holds. Persistent results/XP applied
once, no battle-exit healing/reset/replacements. Commanderless survivors continue.

Provisions30/30 each, living body cost1, consumption then own-Keep replenish<=6.
Part A Keep stock scenario-sufficient. Completed Refresh HP field15%/ownKeep40%,
no stacking; Armor/dead/XP unchanged. Enemy Keep presence disables owner service.

Core snapshot/hash + JSON transport follows existing failure-safe pattern:
validate separate candidate before replacement, atomic file replacement,1MB limit.
Crossroads one slot (`Application.persistentDataPath/Crossroads/manual.json`);
Mission01's existing separate slot/schema remain supported. Stable map only,
including handoff. Snapshot includes both formations, objective/Pressure,
StartingSide/ActiveSide/completed activation, debt, supply, seed/battle counter,
events and result. No campaign event-sourcing or mid-battle save.

## Validation / execution log

Part A focused: 17 EditMode +3 PlayMode PASS (0 failed/skipped). Tests cover
mirroring, authority/handoff, claims/recapture, tie, attack/debt, physical escape,
post-battle placement/IDs/XP/no heal, Commanderless, deterministic round-trip and
failure isolation. Initial local compile error (node vs ID in HUD) corrected.
Full regression: **388 EditMode +55 PlayMode =443 PASS**, zero failed/skipped. Controlled mouse match PASS: West9/East8 at end R6 (screen advances to R7).
All three objectives recaptured; East declines adjacent attack R2 to claim Mine;
R3 West attacks, all six East characters physically Escape, West advances onto
Mine. Same IDs/XP return, East40 Tempo cost. Saved at R2 handoff, recreated scene/
session, loaded exact hash, continued. Tactical replay12/12 matches.
Mirrored East-first R1 smoke: both move/claim, score1:1, R2 begins with East.
Screenshots `a-*.png`, state/trace and replay in evidence root. No AI commands.

Observations: Pressure ended this deliberately contested match in6 global cycles;
late waiting reached Hungry in Part A without forward supply. Ownership persists
without garrison, so recapture matters; no deadlock in this run. One scripted match
and short mirror do not establish severity of first-mover advantage. Node-ID retreat
tie-break can choose a lateral/westward escape for East; it follows the existing
separation/cost deterministic contract, not a homeward bias. Open information and
handoff are readable in graybox; no production vision inferred.

Part A local commit: `Strategy: add Crossroads persistent strategic Hotseat`.
Part B follows this green checkpoint; not yet implemented in this commit.

Evidence root: `/private/tmp/cp0203-20260929/results/`. Runner source results:
`/private/tmp/wp01-20260928/results/cp02-*.{xml,log}`. External mouse/inspection
helper stays outside repo; it does not modify gameplay rules/state or enable AI.

## Boundaries / delivery

No stat/AI tuning, Mission01 changes, production fog, city/research/culture,
networking, captivity/resurrection, real siege or additional resources.
Pre-existing scene/ProjectSettings edits and six persistence .meta files preserved
by SHA-256 manifest `/private/tmp/cp0203-20260929/preexisting.json`.
NO PUSH. After Part B, stop for integrated user playtest. Developer-controlled
validation is not player balance acceptance.
