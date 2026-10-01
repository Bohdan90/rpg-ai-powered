# HW_TI visual correction — 2026-10-01

Status: **HW_TI VISUAL FIX TECHNICAL PASS / NEEDS PLAYER FINAL VISUAL REVIEW**. No PLAYER ACCEPTED, final LOD winner, production asset or Shared promotion claim.

Base `97fd3d18b41fe11ef9e252fe1bd2ee076712c03d`, `feature/hw-ti-engine`, `Convergence/Strategic-Map-UX`. Delivery is the commit containing this report. Main `My project/develop c35fb00` remains untouched; no merge/push. Read latest55 §§22–24,48 visual-review/fix append and46 queue. Current player-authorized correction supersedes the earlier generic attachment proof.

## Corrective implementation

One scoped script-assisted **Grip_R corrective asset edit**, zero manual sculpt strokes/weight painting, zero new bones, zero Meshy calls/credits. Original GLB/archives unchanged. Script changes623 of125,937 vertices, only right-hand/finger region; original vertex count/topology/UV and existing skin weights preserved. The non-destructive master keeps Basis + Grip_R. Curve-based finger flexion was calibrated against the actual28mm temporary handle, with a bounded thumb correction; the first candidate remained visibly open at Attack and was revised before final proof. This is not per-clip deformation. Final game export bakes the visually inspected Grip_R before every LOD; no runtime blend shapes, extra hand renderer or finger subsystem.

Source master: [HW_TI_Grip_R_SOURCE_v01.zip](https://drive.google.com/file/d/1u95z9yi5VUtpvqcx25eQ9p1K9yqtGcPA/view), stored in the unit's existing **Source** folder. Contains editable `.blend`, reproduction script, normalization report and README; upload/readback verified,13,491,987 bytes, SHA256`f5641135830d51e1b5b706ce96ef6c40f42273b79eac4b1c6fbcd9a0f60300b7`. Original provider source remains in its existing Trial01 archives, not overwritten. [source-master.json](Evidence/HW_TI_VISUAL_FIX/source-master.json).

**Sword:** Build resolves `Animator.GetBoneTransform(HumanBodyBones.RightHand)`, creates `RightHandWeaponSocket`, and derives one bone-local frame from explicit exported DCC bind-pose calibration references. Prop-side `GripPoint` is the handle center; local+Z points toward blade, local+Y is calibrated up. Prop root deliberately differs from GripPoint; setup aligns the marker to the socket using inverse marker rotation/translation. The temporary prop now has distinguishable handle, guard, blade and pommel. No reliance on arbitrary mesh pivot and no animation-specific offsets. Scale compensation converts imported0.01bone scale into meter-sized props.

Final calibration originates from Blender world grip center(-0.793,-0.010,1.328), blade axis toward-Y, up+Z, then existing1.0318194export scale/axis conversion. Actual RightHand local position is about(1.35469,10.40677,2.77181) in imported bone units; **authoritative exact generated values are in unity-build.log.gz and the prefab**, not a presumed generic hand offset. Caster/gameplay rig is not changed.

**Shield:** resolves `HumanBodyBones.LeftLowerArm`, creates `LeftForearmShieldSocket`; bind calibration center(0.580,0.015,1.430), outward normal+Z, longitudinal up+X in Blender. This was chosen against actual forearm surface/axes, not mirrored from sword. Prop `ShieldMountPoint` aligns to the socket. Imported local position(-1.48761,14.07099,-6.19114), rotationEuler(1.082,180.940,351.987). Board is outside the forearm rather than between arm and torso; left fingers remain unchanged. No constraints/package needed.

One canonical24-bone Humanoid/Avatar and **one SkinnedMeshRenderer per character**. New small `HwMeshVariants` swaps the selected candidate mesh and corresponding bind-bone references on that same renderer. This replaces the previous three-renderer LOD storage; no second skeleton/body instance per animation. Five existing animation clips and PBR textures/material definitions preserved.

## Visual evidence

Real rendered Unity Play Mode frames, captured by controlled automated playback and visually inspected; not claimed as mouse-driven gameplay. Same frozen Idle pose is used across each LOD comparison. Five major clips each have Close/Right-Grip/front/outside/opposite/back/tactical captures. Additional Attack samples at normalized0.05/0.25/0.50/0.75/0.95 check extrema. Evidence: [HW_TI_VISUAL_FIX](Evidence/HW_TI_VISUAL_FIX).

| Required observation | Evidence / result |
|---|---|
| Hand + sword / closed grip | `Idle-Right-Grip.png`, `Attack-Right-Grip.png`: fingers wrap handle, no open-palm support pose |
| Attack wrist risk | `Attack-risk-*.png`: fixed grip remains in palm; no obvious blade/hilt crossing wrist |
| Shield front / outside | `Idle-Shield-Front.png`, `Idle-Shield-Outside.png`, opposite/back frames: outside-forearm placement |
| Shield attack / movement / hit | `Attack-Shield-Outside.png`, `Walk-*`, `Hit-*`: stable forearm attachment, no mirror flip |
| Death attached equipment | `Death-*`: both props remain bound; no detach/fabricated drop system |
| Normal closed grip | `Idle-Tactical.png`: small tactical silhouette; no clearly open sword hand |
| Boots A/B | `Boots-LOD0/1/2/3.png`:102k,75k,uniform50k,weighted50k |
| Whole unit | `Close-LOD*`, `Tactical-LOD*`, `Far-LOD*` |
| Full field | `field-18-LOD1.png`75k, `field-18-LOD3.png`weighted50k, plus other candidates |

Sword, shield and grip pass this bounded developer visual check. Close grip remains faceted with minor finger/handle contact and stylized thumb/web geometry; these are not visible at normal tactical scale. No new wrist/arm correction or severe deformation. Props are deliberately simple geometry, not final art. Player final visual review remains necessary.

## LOD comparison

| Candidate | Actual triangles | Unity vertices/body | Boot triangles (DCC z<0.43m) |
|---|---:|---:|---:|
| Corrected high | 102,053 | 126,007 | 13,779 |
| New75k | 75,000 | 108,787 | 10,528 |
| Uniform50k, grip corrected | 49,999 | 76,925 | 7,088 |
| Weighted50k | 50,000 | 70,288 | 13,779 |

25k remains optional far evidence; previous original102053/50000/25000 assets and screenshots are preserved in commit97fd3d1 and `Evidence/HW_TI_ENGINE`, not deleted to conceal comparison. Current uniform50k follows the same uniform reduction pipeline with Grip_R baked first (one triangle below target). Thus the player can compare uniform versus weighted on the same corrected hand; it is not mislabelled the unchanged old mesh.

`Preserve_Boots_Grip` protects boots with a smooth lower-leg transition and corrected hand, not half the body. Both modifier directions were executed: **invertFalse leaves61 boot triangles; invertTrue leaves13,779**. Selected invertTrue, factor100. Group is removed after decimation and does not become a skin/bone group. Every candidate has zero unweighted vertices. Actual report: [visual-fix-normalization.json](Evidence/HW_TI_VISUAL_FIX/visual-fix-normalization.json). Blender's [Decimate manual](https://docs.blender.org/UATEST/manual/en/dev/modeling/modifiers/generate/decimate.html) documents the group influence; direction here was established empirically rather than assumed.

Weighted50k visibly preserves boot form/wrinkles much better than uniform50k, approaching high detail there.75k reduces boot cracks versus uniform50k but does not preserve that region as strongly as weighted50k. Weighted reduction shifts simplification elsewhere: hair/cloth hem and some torso close-view cracks remain. Face/overall silhouette stay recognizable; corrected hand remains closed across candidates. At normal/far tactical camera differences are smaller. No automatic winner selected: weighted50k is useful for player A/B review,75k/high remain valid options.

## Comparable18-character measurement

All four candidates remeasured together: Apple M4 Pro/48GiB, Unity6000.6.2f1 Editor/Metal, same18 animated Walk characters,23×17 overhead camera, identical props/materials/grid/UI,40warmup+120sample frames each, extra settling time after screenshot capture. Real wall frame times, not GPU-only/standalone benchmark or FPS guarantee.

| Candidate | Mean ms | p95 ms | Body triangles for18 |
|---|---:|---:|---:|
| 102k | 13.31 | 14.17 | 1,836,954 |
| 75k | 12.24 | 13.16 | 1,350,000 |
| Uniform50k | 10.54 | 11.19 | 899,982 |
| Weighted50k | 10.27 | 10.76 | 900,000 |

18 stored/active SkinnedMeshRenderers, two unique shared character materials,282whole-scene draw calls reported,~581–583MB allocated including Editor/assets. Detailed [performance.json](Evidence/HW_TI_VISUAL_FIX/performance.json). These new absolute timings are higher than historical4.77/3.91ms: current fixture has changed prop geometry/draw workload, renderer organization and editor-session state. Therefore **do not infer a3× mesh regression or use old uniform50k timing as a comparable sample**. The valid comparison is the four candidates measured together here.75k is measured, not extrapolated.

## Validation / delivery

Final focused asset/import suite: **11 EditMode +1 PlayMode PASS**. Fresh complete regression: **719 EditMode +95 PlayMode =814 PASS,0failed/skipped**. XML/logs/provenance recorded in `Evidence/HW_TI_VISUAL_FIX/validation-manifest.json`. Full suites executed because repository pre-commit policy requires them despite changes remaining local to VisualTrials. No normal Core/Presentation/import hooks changed.

Checks cover actual Humanoid RightHand/LeftLowerArm resolution, socket/marker coincidence and scale, five candidate meshes/count ranges, one canonical renderer/skeleton, baked grip with no runtime blend shapes, material maps, existing clips/controller/scene, actual Walk/Attack/Hit/Death transitions and attachment stability. First batch attempt occurred before normal editor shutdown finished and aborted on project lock; no tests executed in that attempt. Retried only after editor exit was confirmed.

Exact changed systems: `Models/HW_TI.fbx`; derived prefab/controller/scene; `Editor/HwTrialBuild.cs`; `Runtime/HwVisualLab.cs`; new `Runtime/HwMeshVariants.cs`+meta; two focused test files; `Tools/HW_TI/correct_visuals.py` and README; report/checkpoint/evidence. Original clips/textures/source raw archives unchanged. Existing asset GUIDs retained.

Launch unchanged: **Strategic-Map-UX → Gate C → Visual Asset Lab → HW_TI Meshy Trial01**. Buttons now include Right Grip, Shield Front/Outside/Opposite/Back, Boots and labelled102k/75k/50k/50kweighted/25kfar. Camera presets avoid needing Scene-view orbit for review. `bash Tools/HW_TI/launch-lab.sh` opens with isolated data/preferences when the project is closed.

Protected9mainfiles/22saved-replay files remain hash-identical. Main Unity untouched. User explicitly freed the feature lab; only that editor was closed normally for import/tests. Feature's two local settings modifications+six pre-existing metadata remain excluded from commit. Separate validation data/preferences maintained. No merge, NO PUSH. Body remains unit-scoped; socket conventions are REUSE CANDIDATE only. STOP for coordinator/player final review.

## Shield height follow-up — 2026-10-01

Player requested better finger coverage, then reviewed a provisional 12 cm lowering and requested exactly half. Final delta from base `3b7e0e6`: one 6 cm socket-local +Y translation toward the hand, baked by the existing prefab builder. Shield rotation, dimensions, outside-forearm mounting, model, LODs and clips are unchanged. No per-clip offsets or additional geometry. This follows the player's positional correction; it is not a claim of final visual acceptance or complete finger occlusion in every pose. Main/develop remains untouched.

Follow-up validation: focused11EditMode+1PlayMode; fresh full719EditMode+95PlayMode=814PASS,0failed/skipped. XML provenance in Evidence/HW_TI_SHIELD_HEIGHT/validation.json. No new GUI visual PASS claimed for6cm; user review pending. Protected9mainfiles and22saves/replays hash-unchanged. No merge/push.

## 2026-10-01 — HW_TI VISUAL ACCEPTED / MERGED TO DEVELOP

Player explicitly accepted the final sword/shield/grip result (Drive55§25,48§49,46latest,53§21). Provider proof PASS; engine integration PASS; player visual review ACCEPTED; main integration COMPLETE.

Normal no-ff merge `ae64deb1ed108d599172d4a0cb5dafe2689ec753` brings feature/hw-ti-engine `c75d3f03fd7dd3dabacca40f86a97834afcb5ee1` into My project/develop from `c35fb00c1406fffe8fe70b08751230a17716a29f`. No conflicts, no re-export/regeneration/decimation. Merge tree exactly equals accepted feature tree. 201 changed/added paths listed in Evidence/HW_TI_MAIN_INTEGRATION/merged-files.txt: VisualTrials HW_TI assets/meta/scripts/tests, Tools/HW_TI, reports/evidence/checkpoint. Core, normal Presentation, Packages and committed ProjectSettings unchanged. This follow-up commit adds documentation/evidence only.

Fresh post-merge main validation: 11 EditMode + 2 PlayMode PASS, 0 failed/skipped. Asset tests verify valid Humanoid, single canonical renderer, LOD references, five Humanoid clips, real PBR maps, sockets/markers and controller. PlayMode validates animation transitions/attachments plus a temporary integration-only test that actually loads VisualLab additively and checks its spawned 102053-triangle character and camera. Temporary test source archived as evidence and removed from Assets afterward; no new runtime code. XML and compressed logs: Evidence/HW_TI_MAIN_INTEGRATION. Automated scene smoke, not a new mouse-driven visual review.

No fresh full regression claimed or required for this mechanically identical integration per current user instructions. Retained feature provenance: c75d3f0, 719 EditMode + 95 PlayMode =814 PASS; Evidence/HW_TI_SHIELD_HEIGHT/validation.json. Integration tests use separate CFFIXED_USER_HOME. Unity package import removed SENTIS_ANALYTICS_ENABLED from local defines; only that test-induced change was reverted to the exact preflight settings. All nine unrelated main files and22 saves/replays hash-identical afterward. Existing unrelated feature files untouched. Main had no open Unity editor at preflight; batch test editors exited normally; existing feature lab session was not controlled/restarted.

Launch in **My project**: Gate C → Visual Asset Lab → HW_TI Meshy Trial 01. Scene: Assets/_Project/VisualTrials/HW_TI/Generated/HW_TI_VisualLab.unity. No global Human Warrior replacement.102k remains accepted high-quality LOD0/master candidate; lower LODs are optimization evidence, not a production-wide standard. Body/head/gambeson remain unit-scoped; skeleton/avatar/socket conventions and clips remain REUSE CANDIDATES. NO PUSH. STOP; no next package begun.
