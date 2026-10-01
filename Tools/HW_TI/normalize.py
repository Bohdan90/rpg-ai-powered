"""Derived HW_TI trial exports. Run with Blender bpy Python; originals are read only."""
import bpy,sys,json,math,hashlib
from pathlib import Path
from mathutils import Vector
source=Path(sys.argv[1]);out=Path(sys.argv[2]);evidence=Path(sys.argv[3]);evidence.mkdir(parents=True,exist_ok=True)
for d in ('Models','Animations','Textures'): (out/d).mkdir(parents=True,exist_ok=True)
report={'blender':bpy.app.version_string,'manualArtRepairs':0,'sourceHashes':{},'lods':[],'clips':[]}
def load(p):
 report['sourceHashes'][str(p.relative_to(source))]=hashlib.sha256(p.read_bytes()).hexdigest()
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(p));bpy.context.scene.render.fps=24
 arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');arm.name='HW_TI_Root'
 for o in list(bpy.context.scene.objects):
  if o.type=='MESH' and not o.vertex_groups:bpy.data.objects.remove(o,do_unlink=True)
 return arm
arm=load(source/'rig/rigged.glb');body=next(o for o in bpy.context.scene.objects if o.type=='MESH');body.name='HW_TI_LOD0'
arm.animation_data_clear();arm.data.pose_position='REST';bpy.context.view_layer.update()
coords=[body.matrix_world@v.co for v in body.data.vertices];height=max(v.z for v in coords)-min(v.z for v in coords);scale=1.8/height
report['normalization']={'sourceHeightMeters':height,'targetHeightMeters':1.8,'exportGlobalScale':scale,'forward':'-Z FBX / +Z Unity','up':'Y FBX / Unity','root':'HW_TI_Root','bones':[b.name for b in arm.data.bones]}
# Preserve UV corner data and skin weights; automated trial candidates, not production retopology.
for name,target in [('LOD0',102053),('LOD1',50000),('LOD2',25000)]:
 mesh=body if name=='LOD0' else body.copy()
 if name!='LOD0':
  mesh.data=body.data.copy();bpy.context.collection.objects.link(mesh);mesh.name='HW_TI_'+name
  bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);bpy.context.view_layer.objects.active=mesh
  mod=mesh.modifiers.new('Trial automated reduction','DECIMATE');mod.ratio=target/len(body.data.polygons);mod.use_collapse_triangulate=True
  bpy.ops.object.modifier_apply(modifier=mod.name)
 report['lods'].append({'name':name,'vertices':len(mesh.data.vertices),'triangles':sum(len(p.vertices)-2 for p in mesh.data.polygons),'unweighted':sum(not v.groups for v in mesh.data.vertices),'materials':len(mesh.data.materials)})
 # Unity supplies the restored material; do not embed provider gloss/textures in FBX.
 mesh.data.materials.clear()
bpy.ops.wm.save_as_mainfile(filepath=str(evidence/'HW_TI_canonical.blend'))
def export(path,animation=False):
 bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'ARMATURE','MESH'},global_scale=scale,apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_armature_deform_only=False,bake_anim=animation,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='STRIP',use_mesh_modifiers=True)
bpy.ops.object.select_all(action='SELECT');export(out/'Models/HW_TI.fbx')
for label,path in [('Idle','animations/idle.glb'),('Walk','rig/walking.glb'),('Attack','animations/attack.glb'),('Hit','animations/hit.glb'),('Death','animations/death.glb')]:
 arm=load(source/path);action=arm.animation_data.action;action.name=label
 lo,hi=action.frame_range;scene=bpy.context.scene;scene.frame_start=math.floor(lo);scene.frame_end=math.ceil(hi)
 for t in arm.animation_data.nla_tracks:t.mute=True
 bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);bpy.context.view_layer.objects.active=arm
 export(out/('Animations/'+label+'.fbx'),True)
 report['clips'].append({'name':label,'frameStart':scene.frame_start,'frameEnd':scene.frame_end,'fps':24,'seconds':(scene.frame_end-scene.frame_start)/24,'skeletonOnly':True})
(evidence/'normalization.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
