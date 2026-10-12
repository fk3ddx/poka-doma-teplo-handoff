# Blender 4.x: prepares the ready Guest creature (rigged, animated) and the close-up claw hand for Unity.
#
#   blender --background --python tools/art/prepare_guest.py -- <body file> --source <url> --author <name> --license <id>
#           [--claw <hand file> | --claw from-body] [--claw-source <url> --claw-author <name> --claw-license <id>]
#
# <body file>: .glb/.gltf/.fbx/.dae of a downloadable, licensed creature (itch.io / OpenGameArt / Sketchfab).
# --claw from-body: the right hand + forearm is cut out of the creature itself (vertices weighted to hand/finger/forearm
#   bones), so the close-up claws match the body. Use a separate hand model only if the creature has no usable hand.
# Rigged bodies are exported WITHOUT touching transforms, rest pose or bind poses; Unity scales the instance.
# Output: unity-game/Assets/_Project/Art/ReadyModels/Sketchfab/GuestBody|GuestClaw/ (FBX, Textures, manifest, SOURCE).
import bpy,bmesh,json,re,sys
from pathlib import Path
from mathutils import Vector,Matrix
argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
def opt(name,default=None):
 return argv[argv.index(name)+1] if name in argv and argv.index(name)+1<len(argv) else default
body=Path(argv[0]) if argv and not argv[0].startswith('--') else None
outroot=Path(__file__).resolve().parents[2]/'unity-game/Assets/_Project/Art/ReadyModels/Sketchfab'
SIDE=re.compile(r'(^|[._\- ])(r|right)([._\- ]|$)|right',re.I)
PART=re.compile(r'hand|finger|thumb|index|middle|ring|pinky|little|forearm|lowerarm|lower_arm|wrist|claw',re.I)

def load(path):
 bpy.ops.wm.read_factory_settings(use_empty=True);ext=path.suffix.lower()
 if ext in ('.glb','.gltf'):bpy.ops.import_scene.gltf(filepath=str(path))
 elif ext=='.fbx':bpy.ops.import_scene.fbx(filepath=str(path))
 elif ext=='.dae':bpy.ops.wm.collada_import(filepath=str(path))
 else:raise SystemExit('Unsupported '+ext)
def bounds(objs):
 pts=[o.matrix_world@Vector(c) for o in objs for c in o.bound_box]
 return Vector(tuple(min(p[i] for p in pts) for i in range(3))),Vector(tuple(max(p[i] for p in pts) for i in range(3)))
def save_materials(label,objs,tex):
 mats=[];seen=set()
 for o in objs:
  for slot in o.material_slots:
   m=slot.material
   if not m or m.name in seen or not m.use_nodes:continue
   seen.add(m.name);m.name=label+'_Material_'+str(len(mats));p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
   if not p:continue
   e={'name':m.name,'color':list(p.inputs['Base Color'].default_value),'roughness':p.inputs['Roughness'].default_value,'texture':'','normal':''}
   for n in m.node_tree.nodes:
    if n.type!='TEX_IMAGE' or not n.image:continue
    fn=re.sub('[^a-zA-Z0-9_]+','_',n.image.name)+'.png';n.image.filepath_raw=str(tex/fn);n.image.file_format='PNG';n.image.save()
    for l in n.outputs['Color'].links:
     if l.to_node==p and l.to_socket.name=='Base Color':e['texture']='Textures/'+fn
     if l.to_node.type=='NORMAL_MAP':e['normal']='Textures/'+fn
   mats.append(e)
 return mats
def write_meta(out,label,source,author,license,extra):
 (out/'manifest.json').write_text(json.dumps(dict({'id':label,'source':source,'author':author,'license':license},**extra),indent=1,ensure_ascii=False),encoding='utf-8')
 (out/'SOURCE.md').write_text('# '+label+'\n\nAuthor: '+str(author)+'\nSource: '+str(source)+'\nLicense: '+str(license)+'\nPrepared by tools/art/prepare_guest.py\n',encoding='utf-8')
def normalise_static(objs,size):
 lo,hi=bounds(objs);scale=size/max(hi-lo);center=(lo+hi)/2
 for o in objs:
  mw=o.matrix_world.copy();o.parent=None
  for v in o.data.vertices:v.co=(mw@v.co-center)*scale
  o.matrix_world=Matrix.Identity(4)
def export(objs,path,rigged):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objs:o.select_set(True)
 bpy.context.view_layer.objects.active=objs[0]
 bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH','ARMATURE'} if rigged else {'MESH'},axis_forward='-Z',axis_up='Y',
  bake_anim=rigged,bake_anim_use_all_actions=rigged,bake_anim_use_nla_strips=False,add_leaf_bones=False,apply_scale_options='FBX_SCALE_UNITS' if rigged else 'FBX_SCALE_ALL')
def cut_right_hand(meshes):
 # Right hand + forearm by skin weights, in bind pose (armature modifier removed = rest geometry).
 best=None
 for o in meshes:
  keep={g.index for g in o.vertex_groups if PART.search(g.name) and SIDE.search(g.name)}
  if not keep:continue
  dup=o.copy();dup.data=o.data.copy();bpy.context.collection.objects.link(dup)
  bm=bmesh.new();bm.from_mesh(dup.data);deform=bm.verts.layers.deform.verify()
  kill=[v for v in bm.verts if sum(w for g,w in v[deform].items() if g in keep)<.5]
  bmesh.ops.delete(bm,geom=kill,context='VERTS');bm.to_mesh(dup.data);bm.free()
  for m in list(dup.modifiers):dup.modifiers.remove(m)
  if len(dup.data.vertices)<20:bpy.data.objects.remove(dup);continue
  if not best or len(dup.data.vertices)>len(best.data.vertices):best=dup
 return best

if not body or not body.exists():raise SystemExit('Body file missing: '+str(body))
load(body)
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];arm=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None)
out=outroot/'GuestBody';(out/'Textures').mkdir(parents=True,exist_ok=True)
mats=save_materials('GuestBody',meshes,out/'Textures');actions=[a.name for a in bpy.data.actions]
claw=None
if opt('--claw')=='from-body':
 claw=cut_right_hand(meshes)
 if not claw:print('WARNING no right hand/forearm vertex groups found; use --claw <hand file>')
export(meshes+([arm] if arm else []),out/'GuestBody.fbx',bool(arm))
write_meta(out,'GuestBody',opt('--source'),opt('--author'),opt('--license'),{'rigged':bool(arm),'actions':actions,'materials':mats,
 'changes':['format conversion only; transforms, rest pose and bind poses untouched (scaled in Unity)']})
print('PREPARED GuestBody actions=',actions)
cout=outroot/'GuestClaw';(cout/'Textures').mkdir(parents=True,exist_ok=True)
if claw:
 normalise_static([claw],.46);cm=save_materials('GuestClaw',[claw],cout/'Textures');export([claw],cout/'GuestClaw.fbx',False)
 write_meta(cout,'GuestClaw',opt('--source'),opt('--author'),opt('--license'),{'materials':cm,'changes':['right hand and forearm cut from the GuestBody mesh by skin weights, bind pose, scaled to 0.46 m']})
 print('PREPARED GuestClaw from body')
elif opt('--claw') and opt('--claw')!='from-body':
 load(Path(opt('--claw')));hand=[o for o in bpy.context.scene.objects if o.type=='MESH'];normalise_static(hand,.46)
 cm=save_materials('GuestClaw',hand,cout/'Textures');export(hand,cout/'GuestClaw.fbx',False)
 write_meta(cout,'GuestClaw',opt('--claw-source'),opt('--claw-author'),opt('--claw-license'),{'materials':cm,'changes':['format conversion, scaled to 0.46 m']})
 print('PREPARED GuestClaw from separate file')
