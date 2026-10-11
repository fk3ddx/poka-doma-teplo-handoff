import bpy,json
from pathlib import Path
from mathutils import Vector
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='U:/unity-game/Assets/_Project/Art/ReadyModels/Sketchfab/Slippers/Slippers.fbx')
for obj in list(bpy.context.scene.objects):
 if obj.type!='MESH':continue
 bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.separate(type='LOOSE');bpy.ops.object.mode_set(mode='OBJECT')

selected=[]
for o in bpy.context.scene.objects:
 if o.type!='MESH':continue
 pts=[o.matrix_world@v.co for v in o.data.vertices]
 if (min(v.x for v in pts)+max(v.x for v in pts))/2<0:selected.append(o)
assert len(selected)>1
bpy.ops.object.select_all(action='DESELECT')
for o in selected:o.select_set(True)
bpy.context.view_layer.objects.active=selected[0];bpy.ops.object.join();shoe=bpy.context.object;shoe.name='Single authored slipper'
pts=[shoe.matrix_world@v.co for v in shoe.data.vertices];lo=Vector(tuple(min(v[i] for v in pts) for i in range(3)));hi=Vector(tuple(max(v[i] for v in pts) for i in range(3)));center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));scale=.28/(hi.y-lo.y)
from mathutils import Matrix
world=shoe.matrix_world.copy()
for v in shoe.data.vertices:v.co=(world@v.co-center)*scale
shoe.matrix_world=Matrix.Identity(4)
out=Path('U:/unity-game/Assets/_Project/Art/ReadyModels/Sketchfab/Slippers')
bpy.ops.export_scene.fbx(filepath=str(out/'SingleSlipper.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
(out/'SingleSlipper-preparation.json').write_text(json.dumps({'original':'Slippers.fbx','operation':'Retain complete disconnected components with negative-X bounding-box centre; join unchanged author geometry and UV; uniform scale to length .28 m. No newly modelled geometry.','parts':len(selected),'dimensions_blender_xyz':list((hi-lo)*scale),'vertices':len(shoe.data.vertices)},indent=2))
print('EXPORTED parts',len(selected),'dimensions',list((hi-lo)*scale),'vertices',len(shoe.data.vertices))
