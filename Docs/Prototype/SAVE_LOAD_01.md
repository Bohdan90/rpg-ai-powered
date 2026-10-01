# WP-03A — SAVE-LOAD-01

Starting `develop` / `d91702937238cddb8c02b4c10b5137f5ac9ff20a`, legitimate descendant
of accepted WP-02 `7069a80`. Latest appends in Drive44/46 authorize exactly
SAVE-LOAD-01 then RETURN-RECOVERY-01, followed by mandatory integrated player gate.
Existing scene/ProjectSettings changes and six untracked persistence `.meta` files
are preserved. Unity stays6000.6.2f1; no combat tuning changes.

## Implementation

- Core `StrategicSaveData.cs`: version1 data-only snapshot, validation, canonical
  SHA-256 checksum and independent session reconstruction. `StrategicScenario`
  exposes stable-state capture. No Unity dependency in Core.
- Captures mission/version; seed and battle counter (next tactical seed); Refresh,
  world phase/cursor; player node/Tempo/debt; Provisions/max36/Hungry; Waystation
  Food and site condition; Village condition; Portal flag and mission result.
- Every actor retains position, Tempo, objective/removal/dormancy/withdraw state,
  Patrol index, raid arming and Area Guard pending intercept. No strategic RNG is
  consumed by this deterministic actor implementation; seed+counter are sufficient
  for subsequent tactical RNG initialization. No tactical state is serialized.
- Player and enemy formations preserve ordered persistent IDs/profiles/Commander
  identity, HP/Armor, Alive/Dead/Safe, Personal XP/Level, Command XP/Level/Rank,
  Commanderless/roster lock and fractional HP recovery remainder. Decimal XP is
  invariant-culture text to avoid Unity JSON decimal loss. Derived fields are
  cross-checked against canonical restored state. Dead identities remain present.
- Last battle summary/XP resolution and event history survive load. Terminal
  strategic result screens can be saved, including a world-phase failure; an
  ongoing interrupted actor phase or tactical encounter cannot.
- Presentation `StrategicSaveFiles.cs`: one `Mission01/manual.json` under
  `Application.persistentDataPath`; JSON, 1MB prototype limit. Write temp file,
  flush, replace slot atomically. Errors retain the old slot/current session.
  Load parses/validates a separate candidate before Presenter installs it.
- Minimal Save/Load buttons on World, plus Load from initial tactical screen for
  application startup. Mid-connected-battle loading is blocked. No autosave,
  profiles, cloud, networking, replay redesign or mid-battle resume.

## Verification

Evidence root `/private/tmp/wp03-20260929/`. Test runner reuses isolated import
cache `/private/tmp/wp01-20260928/project` and pinned Unity. Source sync:
`rsync -a Assets/_Project/ /private/tmp/wp01-20260928/project/Assets/_Project/`.
No test runs modify the user's open project scene/settings.

```sh
bash /private/tmp/wp01-20260928/run-tests.sh wp03a-focused EditMode -testFilter 'StrategicSaveTests'
bash /private/tmp/wp01-20260928/run-tests.sh wp03a-focused-ui-fixed PlayMode -testFilter 'StrategicSavePresentationTests'
bash /private/tmp/wp01-20260928/run-tests.sh wp03a-full-edit EditMode
bash /private/tmp/wp01-20260928/run-tests.sh wp03a-full-play PlayMode
```

Focused:21 EditMode +6 PlayMode PASS, zero failed/skipped. Coverage includes
initial/actors/intercept, both guards removed, death/Commanderless, Safe/debt,
Waystation/Village changes, changed Provisions, terminal results, XP overflow and
fractional remainder, actual ordered tactical commands and next battle seed,
rejected mid-battle capture, corrupt/incompatible/missing save rejection without
mutation. PlayMode destroys/recreates the scene/session, reads the disk save after
an actual battle, continues into the next identical battle, and checks failed-load
UI behavior. The first actual application-restart smoke found Unity JSON turns
an absent nested XP resolution into an empty object. Fixed with an explicit
`hasLastResolution` flag; added four JSON round-trip cases before any battle
(initial, actors moved, Waystation changed, Village failure). These now PASS.
Repeated application-restart mouse smoke PASS: save at node05 / Refresh2 /
Tempo100 after Patrol and Incursion A moved, close the isolated Unity process,
start a new process, click Load saved Mission01 from startup. The restored
canonical checksum and DebugState exactly match the pre-close capture. Move06→10
then Attack Area Guard enters the next battle normally. Evidence:
`results/save-before-restart`, `save-after-restart`, `save-loaded-continuation`,
`save-loaded-battle-weapons` PNG/JSON pairs. Original null-resolution failure is
retained separately as `save-initial-null-resolution-failure`.

Manual slot used: `~/Library/Application Support/DefaultCompany/My project/Mission01/manual.json`.
At WP-03A closure it contained the smoke's Refresh2 state; later manual saves
(including WP-03B validation) replace it. A new mission does not delete
the slot, and pressing Save replaces it. Source test helpers stay outside repo.
Unity Search indexing/account warnings also appeared at editor startup; no game
exception accompanied the successful corrected smoke.

Fresh full regression: **350 EditMode +45 PlayMode =395 PASS; 0 failed/skipped**.
EditMode finished2026-09-29 15:20:31 UTC; PlayMode15:22:15 UTC. Final logs/XML are
copied to `/private/tmp/wp03-20260929/results/wp03a-{focused,focused-ui-fixed,full-edit,full-play}.{xml,log}`.
No prior accepted-baseline regression remains. Existing WP-01, replay, field/siege,
persistence and connected routes stay green.

## User-requested visual correction during this package

The user clarified that the new tokens should show weapons, not people.
`UnitSilhouettes.cs` therefore now draws only a sword/shield for HW/EW and bow/arrow
for HA; existing race labels, side color, facing and selection remain. This is a
small independently requested Presentation delta, not strategic scope expansion.

## Limitations / delivery

Prototype slot is local and schema/mission-specific; no migration promise for
future incompatible scenario revisions. Checksum detects accidental corruption,
not a security/anti-cheat boundary. Save is explicit and can overwrite the one
slot; no autosave. A failed load never replaces the current scenario.

This implementation and checkpoint share local commit `Persistence: save and load
stable Mission 01 state`; resolve exact hash from Git/final coordinator report.
No push. WP-03B starts only after this package is green. WP-03C user acceptance
remains pending; no Hotseat/economy/city/research work is authorized here.
