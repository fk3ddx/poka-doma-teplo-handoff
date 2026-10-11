"""Bake the original CC0 alarm-clock rig to rigid meshes; keep author geometry/UV.
Hands retain pivots from the author's deform bones, for scenario-time animation in Unity.
"""
import bpy,json
from mathutils import Matrix,Vector
from pathlib import Path
root=Path('U:/unity-game/Assets/_Project/Art/CC0/alarm_clock_01')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'alarm_clock_01.fbx'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');deps=bpy.context.evaluated_depsgraph_get()
mapping={'alarm_clock_01_hour_hand':'def_hours','alarm_clock_01_minute_hand':'def_minutes','alarm_clock_01_second_hand':'def_seconds'}
original=list(bpy.context.scene.objects);parts=[];notes=[]
for source in original:
 if source.type!='MESH':continue
 evaluated=source.evaluated_get(deps);mesh=bpy.data.meshes.new_from_object(evaluated,preserve_all_data_layers=True,depsgraph=deps)
 bone=mapping.get(source.name);pivot=rig.matrix_world@rig.pose.bones[bone].head if bone else Vector((0,0,0))
 for vertex in mesh.vertices:vertex.co=evaluated.matrix_world@vertex.co-pivot
 obj=bpy.data.objects.new(source.name+'__rigid',mesh);bpy.context.collection.objects.link(obj);obj.location=pivot;parts.append(obj)
 notes.append({'part':source.name,'pivot_bone':bone,'pivot_blender':list(pivot),'vertices':len(mesh.vertices),'triangles':sum(len(poly.vertices)-2 for poly in mesh.polygons)})
for source in original:bpy.data.objects.remove(source,do_unlink=True)
for obj in parts:obj.name=obj.name.replace('__rigid','');obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(root/'alarm_clock_01_rigid.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
(root/'rigid-preparation.json').write_bytes(json.dumps({'scope':'Original rig evaluated to rigid meshes; UV/materials retained; no remodelling. Unity hand direction still requires validation.','parts':notes},indent=2).encode('utf-8'))
print(json.dumps(notes,indent=2))
