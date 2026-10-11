import bpy,json,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
source=Path('U:/art-sources/BedsidePills/original/complexPill/complexPill.fbx')
out=Path('U:/unity-game/Assets/_Project/Art/ReadyModels/BedsidePills');out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(source))
objects=[o for o in bpy.data.objects if o.type=='MESH'];points=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
lo=Vector(tuple(min(p[i] for p in points) for i in range(3)));hi=Vector(tuple(max(p[i] for p in points) for i in range(3)));dims=hi-lo
axis=max(range(3),key=lambda i:dims[i]);unit=Vector((int(axis==0),int(axis==1),int(axis==2)));rotation=unit.rotation_difference(Vector((1,0,0)));scale=.018/dims[axis];center=(lo+hi)*.5
points=[rotation@(p-center)*scale for p in points];minz=min(p.z for p in points);offset=Vector((0,0,minz));cursor=0
for o in objects:
 for v in o.data.vertices:v.co=points[cursor]-offset;cursor+=1
 o.matrix_world=Matrix.Identity(4);o.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
bpy.ops.export_scene.fbx(filepath=str(out/'ReadyCapsule.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
report={'author':'mxc2 / Marcus-Indrek Simmer','source':'https://opengameart.org/content/pills-3d-models','license':'CC0-1.0','source_file_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'size_blender_m':[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)],'triangles':sum(len(f.vertices)-2 for o in objects for f in o.data.polygons),'materials':[m.name for o in objects for m in o.data.materials],'adaptation':'Original geometry/UV/material assignment preserved; uniformly scaled18mm capsule, rotated to lie flat, base at0. No medicine brand or invented interaction.'}
(out/'preparation.json').write_bytes((json.dumps(report,indent=2)+'\n').encode());print(json.dumps(report,indent=2))
