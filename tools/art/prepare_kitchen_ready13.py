import bpy,json,math,hashlib,shutil
from pathlib import Path
from mathutils import Vector,Matrix
sources=Path('U:/ImportInbox/SketchfabSources');out=Path('U:/ImportInbox/PreparedKitchen13');out.mkdir(exist_ok=True)
report=[]
for name,source,rot,width in [('SovietSink',sources/'SovietSink/original/source/Sink.fbx',math.pi/2,.65),('WoodenWallCabinet',sources/'WoodenWallCabinet/wooden_wall_cabinet_-_pbr__game-ready.glb',0,.82)]:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 if source.suffix=='.fbx':bpy.ops.import_scene.fbx(filepath=str(source))
 else:bpy.ops.import_scene.gltf(filepath=str(source))
 obs=[o for o in bpy.context.scene.objects if o.type=='MESH'];rotation=Matrix.Rotation(rot,4,'Z')
 for o in obs:
  world=rotation@o.matrix_world.copy();o.parent=None
  for v in o.data.vertices:v.co=world@v.co
  o.matrix_world=Matrix.Identity(4)
 pts=[v.co for o in obs for v in o.data.vertices];lo=Vector([min(p[i] for p in pts) for i in range(3)]);hi=Vector([max(p[i] for p in pts) for i in range(3)])
 scale=width/(hi.x-lo.x);origin=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
 for o in obs:
  for v in o.data.vertices:v.co=(v.co-origin)*scale
 folder=out/name;folder.mkdir(exist_ok=True);mats=[]
 for i,m in enumerate(sorted({m for o in obs for m in o.data.materials if m},key=lambda m:m.name)):
  m.name=name+'_'+str(i);textures=[]
  if m.use_nodes:
   for node in m.node_tree.nodes:
    if node.type=='TEX_IMAGE' and node.image:
     image=node.image;filename=m.name+'_'+image.name.replace('.','_')+'.png';image.filepath_raw=str(folder/filename);image.file_format='PNG';image.save();textures.append(filename)
  mats.append({'name':m.name,'color':list(m.diffuse_color),'textures':textures})
 bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.select_set(True)
 bpy.context.view_layer.objects.active=obs[0]
 bpy.ops.export_scene.fbx(filepath=str(folder/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
 entry={'name':name,'size_blender_xyz':list((hi-lo)*scale),'uniform_scale':scale,'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'rotation_z':rot,'materials':mats,'status':'Prepared only, not installed or visually accepted in Unity'}
 (folder/'manifest.json').write_text(json.dumps(entry,indent=2),encoding='utf-8');shutil.copy2(sources/name/'SOURCE.md',folder/'SOURCE.md');report.append(entry)
(out/'README.md').write_text('# Prepared kitchen assets 13\n\nAuthor meshes preserved, uniform scaling only; no generated furniture. SovietSink width 0.65m, cabinet width 0.82m; cabinet depth ~0.327m. Sink missing authored textures: material preparation required before installation. Not yet placed in Unity.\n',encoding='utf-8')
print(json.dumps(report,indent=2))
