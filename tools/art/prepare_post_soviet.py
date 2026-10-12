"""Extract authored furniture from licensed Sketchfab GLB; no generated furniture geometry."""
import bpy,json,struct,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
source=Path('U:/ImportInbox/SketchfabSources/PostSovietFurniture/post-soviet_furniture_set_with_dithering.glb')
out=Path('U:/unity-game/Assets/_Project/Art/ReadyModels/PostSoviet');out.mkdir(exist_ok=True);tex=out/'Textures';tex.mkdir(exist_ok=True)
b=source.read_bytes();n=struct.unpack_from('<I',b,12)[0];doc=json.loads(b[20:20+n]);binary=b[28+n:]
for i,im in enumerate(doc['images']):
 v=doc['bufferViews'][im['bufferView']];raw=binary[v.get('byteOffset',0):v.get('byteOffset',0)+v['byteLength']];ext='png' if raw[:8]==b'\x89PNG\r\n\x1a\n' else 'jpg';(tex/f'author_{i}.{ext}').write_bytes(raw)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(source))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
def bounds(obs):
 pts=[o.matrix_world@v.co for o in obs for v in o.data.vertices];return Vector([min(p[i] for p in pts) for i in range(3)]),Vector([max(p[i] for p in pts) for i in range(3)])
for o in objects:
 lo,hi=bounds([o]);print('AUTHOR_OBJECT',o.name,tuple(round(x,3) for x in lo),tuple(round(x,3) for x in hi-lo))
# Inventory first; extraction selections are explicit and reviewed against author scene.
(source.parent/'inventory.json').write_bytes(json.dumps([{'name':o.name,'min':list(bounds([o])[0]),'size':list(bounds([o])[1]-bounds([o])[0])} for o in objects],indent=2).encode())

recipes=[('Stove',['oven_'],.85),('Fridge',['fridge_'],1.55),('KitchenCabinet',['kitchen shelf_'],.82),('Sideboard',['closet_'],1.9),('DiningTable',['table_'],.74)]
material_info={}
for i,m in enumerate(doc['materials']):
 entry={'name':'PS_'+str(i),'glass':m.get('alphaMode','OPAQUE')=='BLEND'};pbr=m.get('pbrMetallicRoughness',{});legacy=m.get('extensions',{}).get('KHR_materials_pbrSpecularGlossiness',{});entry['color']=pbr.get('baseColorFactor',legacy.get('diffuseFactor',[1,1,1,1]));image=pbr.get('baseColorTexture',legacy.get('diffuseTexture',{})).get('index')
 if image is not None:
  image=doc['textures'][image]['source'];entry['texture']=next(tex.glob('author_'+str(image)+'.*')).name
 material_info[m.get('name','')]=entry
for m in bpy.data.materials:
 if m.name in material_info:m.name=material_info[m.name]['name']
report=[];preview=[]
for idx,(name,prefixes,height) in enumerate(recipes):
 selected=[o for o in objects if any(o.name.startswith(p) for p in prefixes)];lo,hi=bounds(selected);scale=height/(hi.z-lo.z);origin=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));copies=[]
 for original in selected:
  o=original.copy();o.data=original.data.copy();bpy.context.collection.objects.link(o);mw=original.matrix_world.copy();o.parent=None
  for v in o.data.vertices:v.co=(mw@v.co-origin)*scale
  o.matrix_world=Matrix.Identity(4);copies.append(o)
 bpy.ops.object.select_all(action='DESELECT')
 for o in copies:o.select_set(True)
 bpy.context.view_layer.objects.active=copies[0]
 bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
 report.append({'id':name,'height':height,'dimensions_blender':list((hi-lo)*scale),'author_meshes':[o.name for o in selected]})
 for o in copies:o.location.x=(idx-2)*1.65;preview.append(o)
for o in objects:o.hide_render=True
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=12;scene.render.resolution_x=1280;scene.render.resolution_y=500;scene.render.resolution_percentage=100;scene.world=bpy.data.worlds.new('Preview world');scene.world.color=(.25,.25,.25)
bpy.ops.object.camera_add(location=(4,-11,5));camera=bpy.context.object;target=Vector((0,0,.7));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=9.4;scene.camera=camera
for location,power,size in [((0,-4,7),1800,7),((-5,1,5),800,5)]:
 bpy.ops.object.light_add(type='AREA',location=location);l=bpy.context.object;l.data.energy=power;l.data.shape='DISK';l.data.size=size;l.rotation_euler=(target-l.location).to_track_quat('-Z','Y').to_euler()
(out/'manifest.json').write_bytes((json.dumps({'assets':report,'materials':list(material_info.values()),'source_sha256':hashlib.sha256(b).hexdigest()},indent=2)+'\n').encode())
(out/'SOURCE.md').write_bytes((source.parent/'SOURCE.md').read_bytes())
scene.render.filepath=str(source.parent/'furniture-preview.png');bpy.ops.render.render(write_still=True)
