# HW_TI integration proof (derived files only)

Current visual correction supersedes the original normalization output: run
`correct_visuals.py SOURCE_DIR OUTPUT_ASSET_DIR DCC_CACHE_DIR` with Blender Python,
then `HwTrialBuild.Build`. Original `normalize.py` is retained for reproducing97fd3d1;
do not run it over the corrected export unless intentionally reproducing that historical proof.
`correct_visuals.py` preserves Basis + Grip_R in its external `.blend` master, bakes the
single right-hand correction into game geometry, and generates102k/75k/uniform50k/weighted50k/25k.
The weighted direction is selected from an explicit invertFalse/invertTrue boot-retention experiment.
Both raw provider inputs and historical97fd3d1 assets/evidence remain available unchanged.

Current prefab has **one** SkinnedMeshRenderer; `HwMeshVariants` swaps candidate mesh/bone references
on that renderer and keeps the same Humanoid skeleton/Avatar. No runtime hand morph or finger rig.
Socket calibration references are exported from the DCC bind pose. Build resolves Humanoid
RightHand/LeftLowerArm, converts those calibrated world frames once into bone-local sockets,
and aligns the explicit GripPoint/ShieldMountPoint. No per-animation attachment adjustments.
Prop convention: local +Z = blade direction / shield outward normal; +Y = calibrated up;
GripPoint is center of the28mm diameter,130mm-long temporary handle. Weapon root is deliberately
offset from GripPoint, exercising alignment instead of depending on an accidental mesh origin.
See `Docs/Prototype/HW_TI_VISUAL_FIX.md` for the corrective-source Drive archive and evidence.

Inputs: Drive IDs/hashes in `Docs/Prototype/Evidence/HW_TI_ENGINE/source-provenance.json`.
Download/extract untouched provider outputs outside Unity. Expected staging:
`rig/rigged.glb`, `rig/walking.glb`, `animations/{idle,attack,hit,death}.glb`,
`textures/textures/{base_color,normal,metallic,roughness,metallic_roughness}.png`.
No provider calls or credentials in these scripts.

1. With Blender Python (`bpy 5.2.2 LTS` used), run `normalize.py SOURCE_DIR OUTPUT_ASSET_DIR DCC_CACHE_DIR`.
   Output asset directory: `Assets/_Project/VisualTrials/HW_TI`.
   The canonical `.blend` remains in DCC cache, reproducible from original Drive sources.
2. Python with Pillow: `pack_pbr.py SOURCE_DIR/textures/textures OUTPUT_ASSET_DIR/Textures`.
3. In the isolated Unity project, run editor method `RPG.VisualTrial.Editor.HwTrialBuild.Build`.
   This regenerates model settings, materials, controller, prefab and dedicated scene without touching ordinary battle scenes.
4. Launch menu **Gate C / Visual Asset Lab / HW_TI Meshy Trial 01**.
   Controls: five animation buttons, close/hand/shield/boots/tactical/far views,1/9/18 units,
   explicit102k/75k/uniform50k/weighted50k/25k candidates, raw-gloss comparison.
   For a separate editor with isolated preferences/data, run `bash Tools/HW_TI/launch-lab.sh`.
   It refuses to start a second editor on an already open project; it never closes another editor.
5. Optional developer evidence run: `RPG.VisualTrial.Editor.HwTrialBuild.LaunchProof`.
   It plays the actual Animator in a rendered Unity editor, records camera frames and wall-frame statistics
   under `/private/tmp/hw-ti-fix/results`. This is an automated controlled playback, not mouse-driven gameplay.

Tactical view copies existing overhead rotation90° and23×17 fit formula; far=1.35×fit.
No camera/art upgrade is hidden inside the proof. Locomotion is deliberately in-place;
attack/hit return to Idle through Animator transitions, Death stays terminal until a developer button resets it.
No combat timing/Core integration. Props are temporary geometry, not final shared sword/shield art.

PBR pack: remesh glTF blue=metallic→URP red; smoothness=255−green roughness→alpha.
Normal imported as tangent normal; color map sRGB, packed data linear. Raw comparison is a URP adapter
of the provider's metallic default1/roughness0.41008 without recovered normal/MR maps, not a full glTF
specular/IOR/emissive extension renderer. Imported texture maps remain uncompressed for this proof.
