import bpy,json,math,sys,hashlib
from mathutils import Vector
from pathlib import Path
source=Path(sys.argv[1]);out=Path(sys.argv[2]);cache=Path(sys.argv[3]);cache.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(source/'rig/rigged.glb'))
a=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');a.animation_data_clear();a.data.pose_position='REST'
m=max((o for o in bpy.context.scene.objects if o.type=='MESH'),key=lambda o:len(o.data.vertices))
m.shape_key_add(name='Basis');key=m.shape_key_add(name='Grip_R'); changed=[]
for v in m.data.vertices:
 p=m.matrix_world@v.co;x,y,z=p
 if x<-.72 and z>1.25:
  q=p.copy()
  # Four fingers: continuous curl from metacarpal transition around 28mm diameter grip.
  if x<-.793 and y>-.065:
   t=-.793-x;length=.109-.035*max(0,min(1,(y-.01)/.03));theta=min(4.5*t/length,4.5);center=1.353+.13*(x+.793);thickness=(z-center)*.65
   q.x=-.793-(.025+thickness)*math.sin(theta)
   q.z=1.328+(.025+thickness)*math.cos(theta)
  # Thumb: flex across near side of grip; compact the extended web without touching wrist.
  elif y<-.047 and x<-.738:
   w=min(1,max(0,(-y-.047)/.055))*min(1,max(0,(-x-.738)/.024))
   q.x+=(-.789-x)*w*.8;q.y+=(-.045-y)*w*.72;q.z+= (1.304-z)*w*.78
  if (q-p).length>1e-7:key.data[v.index].co=m.matrix_world.inverted()@q;changed.append(v.index)
key.value=1
bpy.ops.wm.save_as_mainfile(filepath=str(cache/'HW_TI_Grip_R_master.blend'))

# Preserve shape master above; game export bakes this single corrective into existing topology.
weights=lambda mesh:hashlib.sha256(str([(v.index,[(g.group,round(g.weight,8)) for g in v.groups]) for v in mesh.data.vertices]).encode()).hexdigest()
before=weights(m);count=len(m.data.vertices)
coords=[v.co.copy() for v in key.data];m.shape_key_clear()
for v,p in zip(m.data.vertices,coords):v.co=p
assert weights(m)==before and len(m.data.vertices)==count
arm=a;arm.name='HW_TI_Root';arm.data.pose_position='REST'
for ob in list(bpy.context.scene.objects):
 if ob.type=='MESH' and ob!=m:bpy.data.objects.remove(ob,do_unlink=True)
scale=1.8/1.74449134035
report={'sourceSha256':hashlib.sha256((source/'rig/rigged.glb').read_bytes()).hexdigest(),'shape':'Grip_R','baked':True,'changedVertices':len(changed),'sourceVertices':count,'weightsUnchanged':True,'bones':len(a.data.bones),'manualSculptOperations':0,'targetHandleDiameterMeters':.028,'lods':[]}
def region(mesh):
 pts=[mesh.matrix_world@v.co for v in mesh.data.vertices]
 return sum(all(pts[i].z<.43 for i in p.vertices) for p in mesh.data.polygons)
def reduced(name,target,weighted=False,invert=False):
 ob=m.copy();ob.data=m.data.copy();bpy.context.collection.objects.link(ob);ob.name=name
 bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
 if target<102053:
  mod=ob.modifiers.new('Automated candidate','DECIMATE');mod.ratio=target/102053;mod.use_collapse_triangulate=True
  if weighted:
   group=ob.vertex_groups.new(name='Preserve_Boots_Grip')
   for v in ob.data.vertices:
    p=ob.matrix_world@v.co;weight=max(max(0,min(1,(.51-p.z)/.10)),.8 if p.x<-.72 and p.z>1.25 else 0)
    group.add([v.index],weight,'REPLACE')
   mod.vertex_group=group.name;mod.vertex_group_factor=100;mod.invert_vertex_group=invert
  bpy.ops.object.modifier_apply(modifier=mod.name)
  if weighted:ob.vertex_groups.remove(ob.vertex_groups['Preserve_Boots_Grip'])
 return ob
m.name='HW_TI_102K'
variants=[m,reduced('HW_TI_75K',75000),reduced('HW_TI_50K_UNIFORM',50000)]
x=reduced('WeightedFalse',50000,True,False);y=reduced('WeightedTrue',50000,True,True)
report['decimateDirectionExperiment']={'invertFalseBootTriangles':region(x),'invertTrueBootTriangles':region(y),'uniformBootTriangles':region(variants[2]),'sourceBootTriangles':region(m)}
weighted=x if region(x)>region(y) else y;discard=y if weighted==x else x
report['selectedInvert']=weighted==y;bpy.data.objects.remove(discard,do_unlink=True);weighted.name='HW_TI_50K_WEIGHTED';variants+=[weighted,reduced('HW_TI_25K',25000)]
for ob in variants:
 ob.data.materials.clear();report['lods'].append({'name':ob.name,'triangles':sum(len(p.vertices)-2 for p in ob.data.polygons),'vertices':len(ob.data.vertices),'bootTriangles':region(ob),'unweighted':sum(not v.groups for v in ob.data.vertices)})
# Explicit exported calibration references; engine resolves actual Humanoid bones, then computes local socket transforms once.
for name,pos in [('GripCalibration',(-.793,-.01,1.328)),('GripBladeAxis',(-.793,-.11,1.328)),('GripUpAxis',(-.793,-.01,1.428)),('ShieldCalibration',(.58,.015,1.43)),('ShieldNormalAxis',(.58,.015,1.53)),('ShieldUpAxis',(.68,.015,1.43))]:
 ob=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(ob);ob.location=pos
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(out/'Models/HW_TI.fbx'),use_selection=True,object_types={'ARMATURE','MESH','EMPTY'},global_scale=scale,apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_armature_deform_only=False,bake_anim=False,path_mode='STRIP',use_mesh_modifiers=True)
(cache/'visual-fix-normalization.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
