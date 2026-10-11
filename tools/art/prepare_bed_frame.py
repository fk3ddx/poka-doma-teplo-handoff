import bpy,json,re,math
from pathlib import Path
from mathutils import Vector,Matrix
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath='U:/game-v2/src/assets/models/old_bed_frame.glb')
obs=[o for o in bpy.context.scene.objects if o.type=='MESH']
points=[o.matrix_world@Vector(v) for o in obs for v in o.bound_box];lo=Vector(tuple(min(v[i] for v in points) for i in range(3)));hi=Vector(tuple(max(v[i] for v in points) for i in range(3)))
print('SOURCE_BOUNDS',list(lo),list(hi))
# Orient the long horizontal dimension along Y (Unity Z).
rot=Matrix.Rotation(math.pi/2,4,'Z') if hi.x-lo.x>hi.y-lo.y else Matrix.Identity(4)
points=[rot@v for v in points];lo=Vector(tuple(min(v[i] for v in points) for i in range(3)));hi=Vector(tuple(max(v[i] for v in points) for i in range(3)));center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));scale=2/(hi.y-lo.y)
out=Path('U:/unity-game/Assets/_Project/Art/CC0/old_bed_frame');out.mkdir(parents=True,exist_ok=True);tex=out/'Textures';tex.mkdir(exist_ok=True)
for o in obs:
 mw=o.matrix_world.copy();o.parent=None
 for v in o.data.vertices:v.co=(rot@mw@v.co-center)*scale
 o.matrix_world=Matrix.Identity(4)
 # Retain authored rails; source has roughly 50k triangles. No automatic collapse of thin ironwork.
mats=[]
for i,m in enumerate(bpy.data.materials):
 if not m.use_nodes:continue
 m.name='BedFrame_'+str(i);p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
 if not p:continue
 d={'name':m.name,'texture':'','normal':''}
 for n in m.node_tree.nodes:
  if n.type!='TEX_IMAGE' or not n.image:continue
  im=n.image;fn=re.sub('[^a-zA-Z0-9_]+','_',im.name)+'.png';im.filepath_raw=str(tex/fn);im.file_format='PNG';im.save()
  for link in n.outputs['Color'].links:
   if link.to_node==p and link.to_socket.name=='Base Color':d['texture']='Textures/'+fn
   if link.to_node.type=='NORMAL_MAP':d['normal']='Textures/'+fn
 if not d['texture']:d['texture']='Textures/old_bed_frame_diff.png'
 mats.append(d)
bpy.ops.object.select_all(action='DESELECT')
for o in obs:o.select_set(True)
bpy.context.view_layer.objects.active=obs[0]
bpy.ops.export_scene.fbx(filepath=str(out/'old_bed_frame.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
(out/'manifest.json').write_text(json.dumps({'dimensions_blender':list((hi-lo)*scale),'materials':mats,'vertices':sum(len(o.data.vertices) for o in obs)},indent=2))
(out/'SOURCE.md').write_text('# Old bed frame\n\nLuca B / Poly Haven / CC0. https://polyhaven.com/a/old_bed_frame\nExisting game-v2 GLB imported, oriented lengthwise, uniformly scaled to 2m, exported FBX with author UV and textures. Verified primary page 2026-10-11. No newly modelled furniture.\n')
print('BED_EXPORTED',list((hi-lo)*scale),mats)
