# Blender 4.x: prepares the ready Guest body (rigged, animated) and the close-up claw hand for Unity.
# Run: blender --background --python tools/art/prepare_guest.py -- <downloads folder>
# Sources (CC-BY 4.0, credit in CREDITS.md):
#   GuestRake  - "The Rake" by SealifeFan3      https://sketchfab.com/3d-models/92898b04b24e4315b0673fcfe307f64e
#   GuestClaw  - "creepy hand" by barbodoji      https://sketchfab.com/3d-models/a7039d6d69764149a65ffd3bbffc5c6d
# Download "glTF (glb)" from each page while signed in; keep the original file names below or edit SOURCES.
import bpy,json,re,sys
from pathlib import Path
from mathutils import Vector,Matrix
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
downloads=Path(args[0] if args else 'C:/Users/User/Downloads')
outroot=Path(__file__).resolve().parents[2]/'unity-game/Assets/_Project/Art/ReadyModels/Sketchfab'
SOURCES=[ # label, file, target size (m), rigged, uid, author
 ('GuestRake','the_rake.glb',2.35,True,'92898b04b24e4315b0673fcfe307f64e','SealifeFan3'),
 ('GuestClaw','creepy_hand.glb',.46,False,'a7039d6d69764149a65ffd3bbffc5c6d','barbodoji')]
for label,filename,size,rigged,uid,author in SOURCES:
 src=downloads/filename
 if not src.exists():print('MISSING',src);continue
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(src))
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
 arm=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None)
 pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
 lo=Vector(tuple(min(p[i] for p in pts) for i in range(3)));hi=Vector(tuple(max(p[i] for p in pts) for i in range(3)))
 extent=hi-lo;scale=size/(extent.z if rigged else max(extent))
 out=outroot/label;out.mkdir(parents=True,exist_ok=True);tex=out/'Textures';tex.mkdir(exist_ok=True)
 if rigged and arm:
  # Keep the skeleton and every action: scale the whole hierarchy from its root, feet at the origin.
  root=arm
  while root.parent:root=root.parent
  center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
  root.location=(root.location-center)*scale;root.scale=root.scale*scale
  bpy.context.view_layer.update()
 else:
  center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,(lo.z+hi.z)/2))
  for o in meshes:
   mw=o.matrix_world.copy();o.parent=None
   for v in o.data.vertices:v.co=(mw@v.co-center)*scale
   o.matrix_world=Matrix.Identity(4)
 mats=[]
 for i,m in enumerate([m for m in bpy.data.materials if m.users]):
  if not m.use_nodes:continue
  m.name=label+'_Material_'+str(i);p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
  if not p:continue
  entry={'name':m.name,'color':list(p.inputs['Base Color'].default_value),'metallic':p.inputs['Metallic'].default_value,'roughness':p.inputs['Roughness'].default_value,'alpha':p.inputs['Alpha'].default_value,'texture':'','normal':'','alphaTexture':''}
  for n in m.node_tree.nodes:
   if n.type!='TEX_IMAGE' or not n.image:continue
   fn=re.sub('[^a-zA-Z0-9_]+','_',n.image.name)+'.png';n.image.filepath_raw=str(tex/fn);n.image.file_format='PNG';n.image.save()
   for link in n.outputs['Color'].links:
    if link.to_node==p and link.to_socket.name=='Base Color':entry['texture']='Textures/'+fn
    if link.to_node.type=='NORMAL_MAP':entry['normal']='Textures/'+fn
  mats.append(entry)
 actions=[a.name for a in bpy.data.actions]
 bpy.ops.object.select_all(action='DESELECT')
 for o in bpy.context.scene.objects:
  if o.type in ('MESH','ARMATURE'):o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(out/(label+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',
  bake_anim=rigged,bake_anim_use_all_actions=rigged,bake_anim_use_nla_strips=False,add_leaf_bones=False,apply_scale_options='FBX_SCALE_ALL')
 manifest={'id':label,'source':'https://sketchfab.com/3d-models/'+uid,'author':author,'license':'CC-BY-4.0','size':size,'rigged':rigged,'actions':actions,'materials':mats,
  'changes':['glTF to FBX, uniform scale to '+str(size)+' m','author meshes, UVs, textures and animations unchanged']}
 (out/'manifest.json').write_text(json.dumps(manifest,indent=1,ensure_ascii=False),encoding='utf-8')
 (out/'SOURCE.md').write_text('# '+label+'\n\nAuthor: '+author+'\nSource: https://sketchfab.com/3d-models/'+uid+'\nLicense: CC-BY-4.0 https://creativecommons.org/licenses/by/4.0/\nPrepared by tools/art/prepare_guest.py (scale only).\n',encoding='utf-8')
 print('PREPARED',label,'actions=',actions)
