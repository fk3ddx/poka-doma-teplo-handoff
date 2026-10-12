"""Convert existing Poly Haven rubber boots; preserve author geometry/UV, no new model."""
import bpy,json,re,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
bpy.ops.wm.read_factory_settings(use_empty=True)
source=Path('U:/game-v2/src/assets/models/rubber_boots.glb')
bpy.ops.import_scene.gltf(filepath=str(source))
obs=[o for o in bpy.context.scene.objects if o.type=='MESH' and 'dirty' in o.name.lower()]
assert len(obs)==1,'Expected author dirty boots pair, not both preview variants'
points=[o.matrix_world@Vector(v) for o in obs for v in o.bound_box]
lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
# Only centre the author mesh and put soles on floor. Height chosen uniformly, not per-axis.
scale=.37/(hi.z-lo.z);origin=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
out=Path('U:/unity-game/Assets/_Project/Art/CC0/rubber_boots');out.mkdir(parents=True,exist_ok=True)
tex=out/'Textures';tex.mkdir(exist_ok=True)
for o in obs:
 mw=o.matrix_world.copy();o.parent=None
 for v in o.data.vertices:v.co=(mw@v.co-origin)*scale
 o.matrix_world=Matrix.Identity(4)
materials=[]
for i,m in enumerate(dict.fromkeys(m for o in obs for m in o.data.materials)):
 if not m.use_nodes:continue
 p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
 if not p:continue
 m.name='Boots_'+str(i);entry={'name':m.name,'texture':'','normal':''}
 for n in m.node_tree.nodes:
  if n.type!='TEX_IMAGE' or not n.image:continue
  im=n.image;fn=re.sub('[^a-zA-Z0-9_]+','_',im.name)+'.png';im.filepath_raw=str(tex/fn);im.file_format='PNG';im.save_render(str(tex/fn)) # save() can copy packed WebP bytes despite a .png suffix
  for link in n.outputs['Color'].links:
   if link.to_node==p and link.to_socket.name=='Base Color':entry['texture']='Textures/'+fn
   if link.to_node.type=='NORMAL_MAP':entry['normal']='Textures/'+fn
 materials.append(entry)
bpy.ops.object.select_all(action='DESELECT')
for o in obs:o.select_set(True)
bpy.context.view_layer.objects.active=obs[0]
bpy.ops.export_scene.fbx(filepath=str(out/'rubber_boots.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
report={'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'original_dimensions_blender':list(hi-lo),'dimensions_blender':list((hi-lo)*scale),'uniform_scale':scale,'materials':materials,'objects':[{'name':o.name,'dimensions':list(o.dimensions),'materials':[m.name for m in o.data.materials],'bounds':[[min(v.co[i] for v in o.data.vertices) for i in range(3)],[max(v.co[i] for v in o.data.vertices) for i in range(3)]]} for o in obs],'scope':'Prepared author mesh, not integrated or visually approved in Unity.'}
(out/'manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
(out/'SOURCE.md').write_text('# Rubber boots\n\nSource: https://polyhaven.com/a/rubber_boots\nLicense: CC0; primary page verified2026-10-12. Reused existing game-v2 GLB. Selected authored dirty pair (clean pair is an alternate display variant, excluded). Preserved geometry/UV, uniform scale to37cm height, centred soles on floor. FBX and extracted author textures. Not yet installed in hallway.\n',encoding='utf-8')
print(json.dumps(report,indent=2))
