# Gate C battlefield size / geometry experiment

2026-09-21. Unity **6000.6.2f1**. This is a comparative fixture experiment, not a decision about final field/siege dimensions. No combat tuning changes, real siege mechanics, AI, telemetry or deployment editor.

## Authority

Read in full from the current Google Drive pack: 00, 07, 09, 01, 04, 12, 17, 18, 19, 02, Gate C contract, engine decision, and [42 — implementation/playtest checkpoint](https://docs.google.com/document/d/15t0Z5mCjVckjRVFdNvoJ8ucwERqbJN16s8LAA6ZQE0A/edit).

42 supersedes historical toolchain recommendations (6.3 LTS / TypeScript) and the old universal occupied-cell LoS blocker description. [17](https://drive.google.com/file/d/1U5acCffJ4y6LL6ZoSBEjVhe8rP1Zd395/view), [18](https://drive.google.com/file/d/10ogUvTEtyxNM8G3S3RC1wC829uYlbmXB/view), and [19](https://drive.google.com/file/d/1OR6fJPuDOwuubAOlJ3PWkaGAq9Wjb00O/view) own retreat/deployment rules. (See the pack for full rule context.) Siege defenders escape at the full legal outer battlefield perimeter; static proxy openings are routes, never escape endpoints.

## Fixture data

All fixtures: seed **20260921**; same ten IDs/profiles, pools, initiative, Movement, ranges, combat formulas, Cover, OA and action economy. West faces East; East faces West. IDs 1–5 West and 6–10 East, ordered: HW-Commander, HW-Infantry, HA-Left, HA-Right, EW-Flanker.

| Fixture | Dimensions | West positions in roster order | East positions in roster order |
|---|---|---|---|
| Field_13x9_Control | 13×9 | (2,4), (2,3), (1,2), (1,6), (2,5) | (10,4), (10,3), (11,2), (11,6), (10,5) |
| Field_17x11_Expanded | 17×11 | (2,5), (2,4), (1,3), (1,7), (2,6) | (14,5), (14,4), (15,3), (15,7), (14,6) |
| Siege_23x17_Tight | 23×17 | (2,8), (2,7), (1,6), (1,10), (2,9) | (12,8), (12,7), (13,6), (13,10), (12,9) |
| Siege_27x21_Roomy | 27×21 | (2,10), (2,9), (1,8), (1,12), (2,11) | (14,10), (14,9), (15,8), (15,12), (14,11) |

Field solids: F0 `(6,3),(6,4),(6,5)`; F1 `(8,4),(8,5),(8,6)`.

Both siege proxies are exactly the same translated **11×9** envelope, **9×7** interior, **24 solid cells**, centered at `(11,8)` / `(13,10)` respectively. Exact layout in offsets from the center:

- Solid if `abs(dx)==5 && abs(dy)>1`, for `dy=-4..4`.
- Or solid if `abs(dy)==4 && abs(dx)>1`, for `dx=-5..5`.
- Three-cell openings on each side: `dx=±5, dy=-1..1` and `dy=±4, dx=-1..1`.
- Every other cell is ordinary walkable ground.

S0 outer envelope: `x=6..16,y=4..12`; S1: `x=8..18,y=6..14`. Available exterior strips measure 6/4 cells (horizontal/vertical) in S0 and 8/6 in S1. Defenders use identical fortress-relative positions. Attackers retain their rear-relative positions, so the larger map increases approach distance naturally.

Field Retreat: West `x=0`, East `x=width−1`. Siege: West `x=0`; East any walkable cell with `x=0 || x=width−1 || y=0 || y=height−1`. Thus the west edge can serve both sides; entry only executes that mover's own escape. Units start outside Retreat Zones.

**P — experimental fixture choices:** West attacks / East defends, symmetric four three-cell openings, these dimensions/positions and fortress footprint. None establishes canonical gates, fortress dimensions, deployment depth, army maximum density or strategic encirclement rules. Real fortification destructibility remains deferred; this is a static neutral geometry proxy expressly requested for comparison.

## Implementation

- `Core/Battlefield.cs`: immutable instance `Columns`/`Rows`, solid data, one explicit `EastRetreatUsesPerimeter` configuration flag. Legacy `Width`/`Height` constants and default constructor remain 13×9 compatibility defaults, not universal sizes. Runtime consumers use instance dimensions.
- `Core/SizeExperimentFixture.cs`: four explicit fixture IDs, board and deployment factories; ordinary C# only.
- `Core/Pathfinder.cs`: array sizes use actual board dimensions. Cost → total straight-line deviation → turns → stable clockwise direction string policy is unchanged.
- `Presentation/PrototypeFixture.cs`: existing default roster delegates to the shared fixture definition; names/seed unchanged.
- `Presentation/BattlePresenter.cs`: enum selection, reset of the selected board, dimension-aware queries and camera framing.
- `Presentation/BattleGridView.cs`: disposable dimension-aware cell views and separate West/East retreat stripes (both remain visible where zones overlap).
- `Presentation/BattleHud.cs`: four-option selector, dynamic coordinates, Core-configured retreat descriptions, fit/focus/wheel zoom; compact labels at overview scale and detailed labels when sufficiently enlarged. Active/hover/target HUD retains full inspection information.
- `Tests/SizeExperimentTests.cs` and `Tests/PlayMode/SizeExperimentPresentationTests.cs` plus `.meta` files.

No changes to resolver, PRNG, profiles, Cover, attack preview formulas, Movement rules, OA rules or battle outcome calculation. No packages or editor version changed. No Unity dependency introduced in Core.

## Launch / interaction

If the Editor is already playing while scripts are updated, stop Play Mode first (the runtime-only HUD does not preserve a session across C# domain reload). Open the project in pinned Unity → **Gate C → Play Tactical Graybox**. In the right HUD choose **Fixture (resets battle)**. Names are the four identifiers above. Switching or Restart Same Seed resets the battle; Restart preserves the selected fixture. Every launch defaults to F0; no selection persistence.

**Fit whole board** resets the camera. **Focus active unit** enlarges around the current actor. Right-click a cell to center the camera there. Mouse wheel over the battlefield zooms; wheel over the HUD scrolls the HUD. Overview uses short type/Commander labels (side remains the token color); zoom in for per-token HP/Armor/OA text. Existing gold explicit paths, red OA segments, unit shapes, cover previews and confirmation remain. Blue stripes identify West escape cells; orange stripes identify East escape cells, including every siege outer edge.

## Validation

Automated verification in an isolated project copy using the pinned Unity editor:

- **169 EditMode passed** (158 existing + 11 new fixture/geometry cases).
- **21 PlayMode passed** (20 existing + one multi-fixture selector/reset/integration test).
- **190 total; 0 failed; 0 skipped**. Existing tests were not weakened or modified.
- New tests cover dimensions, unchanged profiles/control wall, deterministic valid deployment, deterministic executable paths, identical translated fortress, all four defender escape edges, HP/Armor preservation, attacker retreat classification, solid endpoint exclusion, blocked/clear fortress LoS, interior/opening/exterior routes, runtime selector switching, visual roster count, movement submission and restart.
- Core assembly still uses `noEngineReferences: true`, has no Unity references/imports, and no presentation dependency. Package manifest/lock and Unity version unchanged.

All four dropdown entries were selected using real mouse input; right-click centering, wheel zoom and Fit were also checked. Mouse-driven Play Mode checks used the actual board input and confirmation buttons in an isolated Unity project. Initial ten-unit fixtures were inspected; short custom initial states on the same boards exercised wall/opening/Cover/OA/escape cases without changing production fixture definitions. This is integration validation, not four complete human-played battles.

| Fixture | Mouse validation |
|---|---|
| F0 | Known compact six-step path around central wall selected and executed; original ten-unit formation preserved. |
| F1 | Compact six-step path selected and executed, Light Cover preview and fired attack, OA warning and before-step damage/continued movement, own-edge partial escape. |
| S0 | All ten initial units; camera focus/fit; wall blocks attack; Light Cover allows attack; OA warning/resolution at west opening; defender crosses south opening; six-step exterior route around southwest corner; East south-edge escape preserves pools and produces Withdrawal; West entering same edge stays Active. |
| S1 | Same checks as S0 with translated geometry; same Cover/OA math and successful opening/exterior paths; East south-edge escape versus West remaining Active. |

Observed technical issue: the temporary Editor repeats an existing `UnityEditor.Search.SearchDatabase` startup index exception and Unity AI account `NoSubscription` message. These are editor/package services, not exceptions in the graybox runtime; automated tests pass. No package upgrades were made to suppress them.

A first camera check exposed overlap between board edges and fixed legend/header at large sizes; framing margins were increased. Compact token labels avoid label overlap at overview scale; zoom/hover/attack HUD provide detailed inspection. The temporary HUD still requires vertical scrolling to reach confirmation/log controls.

No unreachable fortress interior or sealed exterior escape route was found. Paths still obey corner restrictions. Full long-form battle pacing, future 6–9-unit congestion, and platform/GPU performance beyond local macOS remain unvalidated; they are not inferred from these integration checks.

## Human comparison sheet

Play each fixture with the same seed/composition. Record observations, not a synthetic score:

| Observation | F0 | F1 | S0 | S1 |
|---|---|---|---|---|
| First meaningful ranged/position/engagement pressure: activation/round | | | | |
| Mostly empty movement rounds (two full rounds is a warning) | | | | |
| Useful flank route / excessive walking | | | | |
| Archer Near/Far bands and Steady Aim tradeoff | | | | |
| Local ZoC/OA pressure vs excessive congestion | | | | |
| Retreat distance and opportunity to pressure escape | | | | |
| Fortress exterior maneuver / defender escape routes | n/a | n/a | | |
| Apparent headroom for future 6–9 figures per side | | | | |
| Unit/path/Cover/OA/Retreat readability (try zoom before judging) | | | | |

Openings can concentrate fighting by geometry; that is an observable confound of this fixed proxy, not a new gate mechanic. Five defenders do not validate future 6–9-unit density. No final battlefield size or balance conclusion is selected by this implementation.
