# HW_TI Engine Integration Proof — 2026-10-01

Status: **ENGINE INTEGRATION TECHNICAL PASS / NEEDS PLAYER VISUAL REVIEW** (validation totals below). Not production-ready, PLAYER ACCEPTED, a final LOD standard, or a shared animation library.

Base: `develop c35fb00c1406fffe8fe70b08751230a17716a29f`. Implementation: `feature/hw-ti-engine`, worktree `Strategic-Map-UX`. Main remains at the base; this proof has **not** been merged into My project. The commit containing this report is the delivery commit. Existing EngagementCasting/gameplay was already committed; no unfinished gameplay diff was carried into the asset package. Main/origin-main merge contents were checked before work.

## Scope and provenance

Read current Drive55 §§20–21, current48/46, provider completion report `1-eJC1Sdh09PNIsSWzbxuLXWIkRdilG-F`, archive manifest `14zH5KNjHcePnQAvWi3v6JgFDr4XEACUM`. Downloaded only four required outputs:

| Output | Drive ID |
|---|---|
| Remesh GLB | `1nwrGA_cqMH0d0kbHP9MITxrSrg4RS2c2` |
| Remesh textures/evidence archive | `1f8yEH0aUAvawoLn1uyXsAnWWQzdlW6J6` |
| Rig/free locomotion archive | `1Zc0tVVl9COoE3jiWcryERyFnG_T2Itvx` |
| Preset animation archive | `1jikYbrZmeB4TCO3RL7Af5yEuRUPxjW-L` |

Original archives remain unchanged in `/private/tmp/hw-ti-engine/source`, outside Assets/Git. Drive remains their master. SHA256/bytes/source IDs are in [source-provenance.json](Evidence/HW_TI_ENGINE/source-provenance.json). Provider animation-inspection archive was not downloaded: completion-report findings were read and playback was independently tested in Unity. **Zero Meshy calls, credits, new generations or purchased animations. Zero manual sculpt/weight-paint/rig repair interventions.** Engineering socket/import corrections are recorded below.

## Derived pipeline

Reproduction: [Tools/HW_TI/README.md](../../Tools/HW_TI/README.md), `normalize.py`, `pack_pbr.py`, and editor method `RPG.VisualTrial.Editor.HwTrialBuild.Build`.

Blender Python 5.2.2 LTS imports existing rigged GLB and animation GLBs; removes the provider's 80-triangle root marker from derived output; normalizes to 1.8m measured body height (source 1.74449134m, scale1.0318194); exports FBX with consistent axes/root. Original geometry is preserved as LOD0. The editable derived `.blend` is an external reproducible cache at `/private/tmp/hw-ti-engine/derived/HW_TI_canonical.blend`, not another raw archive inside Assets.

One canonical body asset with three mutually exclusive LOD meshes, **one 24-bone skeleton**, one Humanoid Avatar and one Animator. Five skeleton-only animation FBXs contain no duplicated character meshes. Unity mapping uses 22 humanoid bones; the two provider head end bones remain in the hierarchy. No finger bones were added. Avatar is valid/human. Provider spine names are reversed relative to anatomical height, explicitly mapped lower `Spine02` → Spine, middle `Spine01` → Chest, upper `Spine` → UpperChest. `preserveHierarchy` is required on body and clips so CopyFromOther retains the same root. No silent Generic fallback.

Only `Assets/_Project/VisualTrials/HW_TI/**` and its new metadata are added. Existing battle prefabs/scenes, Core rules, stats, timing and tactical input are unchanged. Generated materials/controller/prefab/scene are reproducible through Build. No provider archives or unrelated settings are committed.

## PBR restoration

All **125,041 unique rig/Idle UV coordinates** match the remesh UV set at 6-decimal precision; detailed inspection is [uv-rig-inspection.json](Evidence/HW_TI_ENGINE/uv-rig-inspection.json). Thus the selected texture source is remesh-compatible, not an unverified pre-remesh texture set.

Base Color sRGB and tangent Normal restored. Remesh combined map blue metallic and green roughness were verified pixelwise against the separate maps. URP packed texture is R=Metallic, A=255−Roughness, linear data; normal imported as NormalMap. Smoothness multiplier1 preserves this map. Textures remain uncompressed for this proof. This is not an arbitrary constant-smoothness fix.

[Raw gloss](Evidence/HW_TI_ENGINE/01-raw-gloss-close.png) versus [corrected close](Evidence/HW_TI_ENGINE/02-corrected-close.png): corrected cloth is visibly matte with restored surface detail. Raw comparison is a **URP approximation** of provider metallic1/roughness0.41008 with missing maps, not an exact renderer of glTF specular/IOR/emissive extensions. No claim of exact cross-renderer material parity.

## Playback, attachments and camera

| Clip | Result |
|---|---|
| Idle | Actual Humanoid playback, looping; supported stance |
| Walk | Actual limb motion, loops; Idle→Walk→Idle works |
| Sword Slash / Attack | Actual playback, transitions to Idle; temporary sword follows right hand |
| Hit | Actual playback, transitions to Idle; shield remains attached |
| Death | Actual playback to grounded terminal pose; no automatic revival |

Locomotion is deliberately in-place; root motion is not introduced into combat. Shoulder/elbow/hip/knee motion and feet were inspected in rendered close/tactical frames; no catastrophic skinning collapse. Cloth stretches and open fingers remain visible close-up. Temporary cuboid sword/shield use right/left hand sockets; source FBX bone scale0.01 initially made props tiny, corrected by socket scale compensation. No mesh/weights changed. Approximate grip/orientation is adequate for socket proof, **not an authored closed-hand grip**. Props stay bound through animation; no catastrophic detachment. Manual art intervention count remains0.

Actual Unity Game View captures, not Blender renders, are in [Evidence/HW_TI_ENGINE](Evidence/HW_TI_ENGINE). `close-*` and `tactical-*` cover all five clips; Attack/Hit/Idle also show the sockets. `Close-LOD*`, `Tactical-LOD*`, `Far-LOD*` compare each candidate. `field-18-LOD*` shows representative full-field count. Frames were produced by labelled automated controlled playback in a real rendered Unity Play session, then visually inspected; **not claimed as mouse-driven gameplay**. LOD captures are consecutive animated frames, not pixel-identical poses.

Normal camera uses the existing overhead90° **23×17** fit formula; far is1.35×. At that full-field view the body is small: sword/shield silhouette and motion are readable, face/quilting/finger articulation are not. No major deformation was apparent there. Missing finger articulation is conspicuous close-up, not apparent at this normal tactical framing. Camera/art redesign was intentionally excluded.

## LOD and performance evidence

| Candidate | Triangles/body | Blender vertices | Unity vertices/body |
|---|---:|---:|---:|
| Source LOD0 | 102,053 | 125,937 | 126,006 |
| Medium LOD1 | 50,000 | 85,023 | 76,956 |
| Low LOD2 | 25,000 | 61,045 | 37,474 |

Unity/import vertex welding/splitting differs from Blender counts. Automated decimation preserved rig/weights/UVs and gross silhouette, with **zero unweighted vertices**. It introduces visible fine cracks/holes on clothing/hair/limbs in close view, especially25k. Neither candidate is approved for production close-up. At normal/far overhead framing the difference is much harder to distinguish; this warrants player review rather than declaring100k necessary or25k final. No manual retopology was attempted.

Apple M4 Pro,48GiB, Unity6000.6.2f1 Editor/Metal; real120-frame wall-time samples after40 warmup frames, all characters animated Walk, same camera/scene. Includes Editor/vsync/UI/grid/temporary props, **not a standalone GPU benchmark or FPS guarantee**. Mesh triangle totals exclude props/ground. Full data: [performance.json](Evidence/HW_TI_ENGINE/performance.json).

| Count | LOD0 mean/p95 ms | LOD1 mean/p95 ms | LOD2 mean/p95 ms |
|---:|---:|---:|---:|
| 1 | 1.57 /1.91 | 1.51 /1.89 | 1.56 /1.90 |
| 9 | 2.71 /3.38 | 2.21 /2.70 | 2.06 /2.50 |
| 18 | 4.77 /8.37 | 3.91 /6.12 | 2.86 /3.72 |

18 units:1,836,954/900,000/450,000 body triangles. Per character3 stored SkinnedMeshRenderers,1 active at forced LOD;18 active at18 units. Two unique shared character materials (body, placeholder props), one material per body renderer. Whole-scene UnityStats draw calls about149/194–197/248 for1/9/18; these include scene overhead and do not measure unit-only draws. Allocated-memory counter~565–568MB, includes all imported LODs/textures/editor assets; not isolated mesh memory. Lower candidates reduce measured frame cost, but their close quality remains a limitation.

## Validation and launch

Focused final import/data checks **8 EditMode PASS**; actual Animator/playback/socket transitions **1 PlayMode PASS**,0failed/skipped. Fresh full regression: **716 EditMode +95 PlayMode =811 PASS,0failed/skipped**. Results and source/config fingerprints are in `Evidence/HW_TI_ENGINE/validation-manifest.json` (populated from final XML, not prior802PASS).

Coverage: valid shared Humanoid/root, three expected LODs, five humanoid reusable clips without meshes, material maps/keywords/color spaces, controller references, prefab/scene references, socket world scale, actual Walk motion, Attack/Hit return to Idle, terminal grounded Death. Renderer output and map appearance were additionally inspected through real Unity screenshots.

Launch **Unity project `Convergence/Strategic-Map-UX` → Gate C → Visual Asset Lab → HW_TI Meshy Trial 01**. Five animation buttons, Close/Tactical/Far,1/9/18 units, LOD0/1/2 and raw/corrected controls. Or open `Assets/_Project/VisualTrials/HW_TI/Generated/HW_TI_VisualLab.unity` and Play. This is a dedicated fixture; normal Human Warriors remain graybox. Main My project/develop does **not** contain this feature yet.

## Safety, reuse and limitations

Main9 protected files and22 saved/replay files remain SHA256-identical ([protected-check.json](Evidence/HW_TI_ENGINE/protected-check.json)). Main active editor was not controlled/restarted. Validation editor used `CFFIXED_USER_HOME=/private/tmp/hw-ti-engine/preferences`; batch tests used separate `/private/tmp/strategic-map-ux/test-preferences`. Actual recorded persistent path is in performance.json. No writes to main saves/preferences. Own validation editor closed normally.

Feature's existing EditorBuildSettings and6untracked persistence metadata remain uncommitted; Unity additionally adds local `APP_UI_EDITOR_ONLY` scripting symbol in feature ProjectSettings, excluded from delivery. Main retains its exact9 unrelated entries. NO PUSH, no merge.

Body/head/gambeson remain **unit-scoped**. Skeleton/Avatar and clips are **REUSE CANDIDATE** only: Humanoid mapping and shared-avatar clip reuse work; retargeting onto a second distinct consumer is NOT RUN. Nothing promoted to Shared. Run not integrated (optional). No production animation polish, final props, gameplay timing or mass replacement. Primary remaining review questions: small tactical silhouette, existing face/cloth quality, open grip close-up, decimation cracks. No substantial manual art repair was needed for this engine proof.

## 2026-10-01 player-review correction

The preceding report is historical proof97fd3d1. Player rejected wrist penetration, wrong-side shield and open sword hand; its generic attachment assessment is **superseded**. Bounded follow-up now uses Humanoid RightHand+GripPoint, outside LeftLowerArm+ShieldMountPoint, one baked Grip_R correction (same topology/weights/24bones), one runtime skinned renderer,75k and empirically verified boot-preserving50k candidates. Raw source and this original evidence remain intact. Actual correction, new performance and validation provenance: [HW_TI_VISUAL_FIX.md](HW_TI_VISUAL_FIX.md). No main merge or PLAYER ACCEPTED claim.

## Final acceptance and delivery

HW_TI VISUAL ACCEPTED / MERGED TO DEVELOP. See HW_TI_VISUAL_FIX.md final integration entry and Evidence/HW_TI_MAIN_INTEGRATION. Accepted feature c75d3f0 merged normally as ae64deb; no asset regeneration or global roster replacement. Fresh post-merge11Edit+2Play PASS; prior814full PASS retained, not rerun.
