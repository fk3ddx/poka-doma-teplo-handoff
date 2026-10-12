"""Convert licensed authored model; preserve mesh/UV and original PNG bytes. No procedural furniture."""
import bpy,json,struct,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
source=Path('U:/ImportInbox/SketchfabSources/DynamoHandheldFlashlight/dynamo_handheld_flashlight.glb');out=source.parent/'prepared';out.mkdir(exist_ok=True);tex=out/'Textures';tex.mkdir(exist_ok=True)
b=source.read_bytes();n=struct.unpack_from('<I',b,12)[0];doc=json.loads(b[20:20+n]);binary=b[28+n:]
for i,im in enumerate(doc['images']):
 v=doc['bufferViews'][im['bufferView']];raw=binary[v.get('byteOffset',0):v.get('byteOffset',0)+v['byteLength']];assert raw[:8]==b'\x89PNG\r\n\x1a\n';(tex/f'author_{i}.png').write_bytes(raw)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(source));obs=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(obs)==1
points=[o.matrix_world@v.co for o in obs for v in o.data.vertices];lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)]);scale=.12/max(hi-lo);origin=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
for o in obs:
 mw=o.matrix_world.copy();o.parent=None
 for v in o.data.vertices:v.co=(mw@v.co-origin)*scale
 o.matrix_world=Matrix.Identity(4);o.name='Dynamo handheld author mesh'
 for m in o.data.materials:m.name='DynamoAuthor'
bpy.ops.object.select_all(action='DESELECT')
for o in obs:o.select_set(True)
bpy.context.view_layer.objects.active=obs[0]
bpy.ops.export_scene.fbx(filepath=str(out/'Dynamo.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
report={'source_sha256':hashlib.sha256(b).hexdigest(),'dimensions_blender':list((hi-lo)*scale),'uniform_scale':scale,'triangles':sum(len(p.vertices)-2 for o in obs for p in o.data.polygons),'materials':[{'name':'DynamoAuthor','texture':'Textures/author_0.png','normal':'Textures/author_2.png','orm':'Textures/author_1.png'}],'scope':'Authored geometry, UV, PNGs unchanged; uniform largest extent12cm. Not installed in Unity yet.'};(out/'manifest.json').write_bytes((json.dumps(report,indent=2)+'\n').encode());print(json.dumps(report))
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=24;scene.render.resolution_x=640;scene.render.resolution_y=480;scene.render.resolution_percentage=100;scene.world=bpy.data.worlds.new('Preview world');scene.world.color=(.18,.18,.18)
bpy.ops.object.camera_add(location=(.19,-.21,.16));camera=bpy.context.object;target=Vector((0,0,.025));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=.19;scene.camera=camera
for location,power,size in [((.08,-.10,.22),12,.2),((-.14,.03,.13),5,.15)]:
 bpy.ops.object.light_add(type='AREA',location=location);l=bpy.context.object;l.data.energy=power;l.data.shape='DISK';l.data.size=size;l.rotation_euler=(target-l.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(out/'author-preview.png');bpy.ops.render.render(write_still=True)
