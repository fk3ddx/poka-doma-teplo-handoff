import bpy,json
from pathlib import Path
from mathutils import Vector,Matrix
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='U:/unity-game/Assets/_Project/Art/ReadyModels/Sketchfab/Wardrobe/Wardrobe.fbx')
for o in list(bpy.context.scene.objects):
 if o.type!='MESH':continue
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.separate(type='LOOSE');bpy.ops.object.mode_set(mode='OBJECT')
groups={'Body':[],'LeafLeft':[],'LeafRight':[]}
for o in list(bpy.context.scene.objects):
 if o.type!='MESH':continue
 pts=[o.matrix_world@v.co for v in o.data.vertices]
 lo=[min(v[i] for v in pts) for i in range(3)];hi=[max(v[i] for v in pts) for i in range(3)]
 front=lo[0]>.34 and lo[2]>.19
 key='LeafLeft' if front and hi[1]<-.30 else 'LeafRight' if front and lo[1]>.30 else 'Body'
 groups[key].append(o)
for key,parts in groups.items():
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=key
 world=o.matrix_world.copy()
 for v in o.data.vertices:v.co=world@v.co
 o.matrix_world=Matrix.Identity(4)
bpy.ops.object.select_all(action='SELECT')
out=Path('U:/unity-game/Assets/_Project/Art/ReadyModels/Sketchfab/Wardrobe')
bpy.ops.export_scene.fbx(filepath=str(out/'WardrobeSeparated.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
(out/'WardrobeSeparated.json').write_text(json.dumps({'operation':'Group unchanged disconnected author geometry into carcass, two side doors and handles. Central mirror panel stays with body. Preserve UVs; no invented furniture geometry.','groups':{k:len(v) for k,v in groups.items()}},indent=2))
