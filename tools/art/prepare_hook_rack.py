"""Deterministic conversion of downloaded CC-BY ready prop. No generative model use."""
import bpy,json,re,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
bpy.ops.wm.read_factory_settings(use_empty=True)
source=Path('C:/Users/User/Downloads/wooden_hook_rack.glb');bpy.ops.import_scene.gltf(filepath=str(source))
obs=[o for o in bpy.context.scene.objects if o.type=='MESH']
points=[o.matrix_world@v.co for o in obs for v in o.data.vertices];lo=Vector([min(v[i] for v in points) for i in range(3)]);hi=Vector([max(v[i] for v in points) for i in range(3)])
# Long axis remains authored; Unity installer measures its orientation. Uniform width90cm.
scale=.90/max(hi.x-lo.x,hi.y-lo.y);center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
out=Path('U:/unity-game/Assets/_Project/Art/ReadyModels/Sketchfab/HookRack');out.mkdir(parents=True,exist_ok=True);tex=out/'Textures';tex.mkdir(exist_ok=True)
for o in obs:
 mw=o.matrix_world.copy();o.parent=None
 for v in o.data.vertices:v.co=(mw@v.co-center)*scale
 o.matrix_world=Matrix.Identity(4)
materials=[]
for i,m in enumerate(dict.fromkeys(m for o in obs for m in o.data.materials)):
 m.name='HookRack_'+str(i);p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');entry={'name':m.name,'texture':'','normal':''}
 for n in m.node_tree.nodes:
  if n.type!='TEX_IMAGE' or not n.image:continue
  im=n.image;fn=re.sub('[^a-zA-Z0-9_]+','_',im.name)+'.png';im.filepath_raw=str(tex/fn);im.file_format='PNG';im.save()
  for link in n.outputs['Color'].links:
   if link.to_node==p and link.to_socket.name=='Base Color':entry['texture']='Textures/'+fn
   if link.to_node.type=='NORMAL_MAP':entry['normal']='Textures/'+fn
 materials.append(entry)
bpy.ops.object.select_all(action='DESELECT')
for o in obs:o.select_set(True)
bpy.context.view_layer.objects.active=obs[0]
bpy.ops.export_scene.fbx(filepath=str(out/'HookRack.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
report={'height':float((hi.z-lo.z)*scale),'dimensions_blender':list((hi-lo)*scale),'materials':materials,'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'objects':[o.name for o in obs]}
(out/'manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
(out/'SOURCE.md').write_text('# Wooden hook rack\n\nAuthor:Sousinho. https://sketchfab.com/3d-models/wooden-hook-rack-b7928bccdd1344b79c0b67e7a9878016\nLicense:CC-BY4.0 https://creativecommons.org/licenses/by/4.0/ . Free official GLB1K download12.10.2026. Source page also has NoAI tag: not used for training or generative model input; direct authored game prop conversion only. Original mesh/UV/textures retained, uniform90cm width, FBX conversion.\n',encoding='utf-8')
print(json.dumps(report,indent=2))
