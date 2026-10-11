import bpy,json,re,shutil
from pathlib import Path
from mathutils import Vector,Matrix
outroot=Path('U:/unity-game/Assets/_Project/Art/ReadyModels/Sketchfab');outroot.mkdir(parents=True,exist_ok=True)
models=[('VEFPhone','phone_vef_ta_68.glb',.15,'c848036e1d5f455797476659dfd12b06','strnroman'),('Slippers','house_slippers.glb',.08,'ac25fc96e7ff4c138d8503b1072b7ea3','headless_christ'),('Wardrobe','old_soviet_wardrobe.glb',1.9,'ad0ab8f9757640d6b88fa9318d2298cb','dashkilya'),('Dresser','734b7c6aba6343518c89008ece0e58f0.glb',1.8,'734b7c6aba6343518c89008ece0e58f0','VVMGames'),('WallClock','antique_wall_pendulum_clock.glb',.70,'a1399c9b33554b9a9d25ec9e5fbdd5ca','Odin Branigan')]
for label,filename,height,uid,author in models:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.gltf(filepath='C:/Users/User/Downloads/'+filename)
 obs=[o for o in bpy.context.scene.objects if o.type=='MESH']
 points=[o.matrix_world@Vector(v) for o in obs for v in o.bound_box];lo=Vector(tuple(min(p[i] for p in points) for i in range(3)));hi=Vector(tuple(max(p[i] for p in points) for i in range(3)));center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));scale=height/(hi.z-lo.z)
 out=outroot/label;out.mkdir(exist_ok=True);tex=out/'Textures';tex.mkdir(exist_ok=True)
 mats=[];objects=[]
 for i,o in enumerate(obs):
  mw=o.matrix_world.copy();o.parent=None
  for v in o.data.vertices:v.co=(mw@v.co-center)*scale
  o.matrix_world=Matrix.Identity(4)
  if label=='VEFPhone' and len(o.data.polygons)>5000:
   mod=o.modifiers.new('Closeup silhouette LOD','DECIMATE');mod.ratio=.35;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
  objects.append({'name':o.name,'vertices':len(o.data.vertices),'bounds':[list(Vector((min(v.co[j] for v in o.data.vertices) for j in range(3)))),list(Vector((max(v.co[j] for v in o.data.vertices) for j in range(3))))]})
 for i,m in enumerate(bpy.data.materials):
  if not m.use_nodes:continue
  m.name=label+'_Material_'+str(i)
  p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
  if not p:continue
  entry={'name':m.name,'color':list(p.inputs['Base Color'].default_value),'metallic':p.inputs['Metallic'].default_value,'roughness':p.inputs['Roughness'].default_value,'alpha':p.inputs['Alpha'].default_value,'texture':'','normal':'','alphaTexture':''}
  for n in m.node_tree.nodes:
   if n.type!='TEX_IMAGE' or not n.image:continue
   im=n.image
   fn=re.sub('[^a-zA-Z0-9_]+','_',im.name)+'.png'
   im.filepath_raw=str(tex/fn);im.file_format='PNG';im.save()
   for link in n.outputs['Color'].links:
    if link.to_node==p and link.to_socket.name=='Base Color':entry['texture']='Textures/'+fn
    if link.to_node.type=='NORMAL_MAP':entry['normal']='Textures/'+fn
   for link in n.outputs['Alpha'].links:
    if link.to_node==p and link.to_socket.name=='Alpha':entry['alphaTexture']='Textures/'+fn
  mats.append(entry)
 bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.select_set(True)
 bpy.context.view_layer.objects.active=obs[0]
 bpy.ops.export_scene.fbx(filepath=str(out/(label+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
 (out/'manifest.json').write_text(json.dumps({'id':label,'height':height,'dimensions':list((hi-lo)*scale),'materials':mats,'objects':objects},indent=2))
 (out/'SOURCE.md').write_text(f'# {label}\n\nAuthor: {author}\nSource: https://sketchfab.com/models/{uid}\nLicense: CC-BY-4.0 https://creativecommons.org/licenses/by/4.0/\nDownloaded through official Sketchfab Download GLB 1K on 2026-10-11. Adapted metre scale and exported existing meshes to FBX; extracted author texture images. Phone reduced in polygon count for close-up use.\n',encoding='utf-8')
 print(label,'SCALE',list((hi-lo)*scale),'OBJECTS',objects,'MATERIALS',mats,flush=True)
 bpy.ops.wm.save_as_mainfile(filepath=str(out/(label+'.blend')))
