# Blender 4.x: prepares the ready Guest creature (rigged, animated) and the close-up claw hand for Unity.
#
#   blender --background --python tools/art/prepare_guest.py -- <body file> --source <url> --author <name> --license <id>
#           [--claw <hand file> | --claw from-body [--hand-bones <forearm bone>]] [--claw-source <url> --claw-author <name> --claw-license <id>]
#           [--claws <length factor>,<tip width> --wrists <left wrist bone>,<right wrist bone>]
# --claws: the author's distal finger phalanges are lengthened along the finger and tapered into black claws
#   (mesh vertices + a 'Claw' material only; joints, weights and bind poses unchanged).
# --hand-bones: rigs with generic bone names (joint12.001…): the named bone and all its descendants are the hand+forearm.
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
   # Identity, not name: the name changes on the next line, so a shared material must not be renamed twice.
   if not m or m.as_pointer() in seen or not m.use_nodes:continue
   seen.add(m.as_pointer());m.name=label+'_Material_'+str(len(mats));p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
   if not p:continue
   e={'name':m.name,'color':list(p.inputs['Base Color'].default_value),'roughness':p.inputs['Roughness'].default_value,'texture':'','normal':''}
   for n in m.node_tree.nodes:
    if n.type!='TEX_IMAGE' or not n.image:continue
    fn=re.sub('[^a-zA-Z0-9_]+','_',n.image.name)+'.png';img=n.image
    if max(img.size)>1024:img.scale(*(max(1,v*1024//max(img.size)) for v in img.size))
    # save_render re-encodes (a packed JPEG would otherwise be written as-is under a .png name)
    st=bpy.context.scene.render.image_settings;st.file_format='PNG';st.color_mode='RGBA' if img.channels==4 else 'RGB';st.color_depth='8';img.save_render(str(tex/fn))
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
def subtree(arm,root):
 names=set();stack=[arm.data.bones[root]] if arm and root in arm.data.bones else []
 while stack:b=stack.pop();names.add(b.name);stack.extend(b.children)
 return names
def cut_right_hand(meshes,bones=None):
 # Right hand + forearm by skin weights, in bind pose (armature modifier removed = rest geometry).
 best=None
 for o in meshes:
  keep={g.index for g in o.vertex_groups if (g.name in bones if bones else PART.search(g.name) and SIDE.search(g.name))}
  if not keep:continue
  dup=o.copy();dup.data=o.data.copy();bpy.context.collection.objects.link(dup)
  bm=bmesh.new();bm.from_mesh(dup.data);deform=bm.verts.layers.deform.verify()
  kill=[v for v in bm.verts if sum(w for g,w in v[deform].items() if g in keep)<.5]
  bmesh.ops.delete(bm,geom=kill,context='VERTS');bm.to_mesh(dup.data);bm.free()
  for m in list(dup.modifiers):dup.modifiers.remove(m)
  # The claw gets its own material copies, so renaming them for GuestClaw never renames GuestBody's materials.
  for slot in dup.material_slots:
   if slot.material:slot.material=slot.material.copy()
  if len(dup.data.vertices)<20:bpy.data.objects.remove(dup);continue
  if not best or len(dup.data.vertices)>len(best.data.vertices):best=dup
 return best

def lengthen_claws(arm,mesh,K,TW,nail_from=0.0,wrists=('joint13','joint13.001')):
 # Author-mesh edit, no new geometry: the distal phalanx of every finger is lengthened K× along the finger and
 # tapered to TW at the tip; its outer part gets a near-black glossy 'Claw' material. Joints, weights, bind poses unchanged.
 inv=mesh.matrix_world.inverted();mw=arm.matrix_world
 def head(b):return inv@(mw@arm.data.bones[b].head_local)
 groups={g.name:g.index for g in mesh.vertex_groups};count={};dom={}
 for v in mesh.data.vertices:
  if v.groups:g=max(v.groups,key=lambda x:x.weight).group;dom[v.index]=g;count[g]=count.get(g,0)+1
 tips=[]
 for w in wrists:
  for f in arm.data.bones[w].children:
   b=f;last=None
   while True:
    if count.get(groups.get(b.name),0)>=5:last=b
    if not b.children:break
    b=b.children[0]
   if last and last.parent:tips.append(last)
 claw=bpy.data.materials.new('Claw');claw.use_nodes=True;p=claw.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(.012,.011,.010,1);p.inputs['Roughness'].default_value=.25
 mesh.data.materials.append(claw);ci=len(mesh.data.materials)-1;moved=set();nailv=set();far=set()
 for t in tips:
  gi=groups[t.name];h=head(t.name);d=(h-head(t.parent.name));L=d.length;d.normalize()
  vs=[i for i,g in dom.items() if g==gi];smax=max(((mesh.data.vertices[i].co-h).dot(d) for i in vs),default=0)
  if smax<=0:continue
  for i in vs:
   co=mesh.data.vertices[i].co;rel=co-h;s=rel.dot(d)
   if s<=0:continue
   u=s/smax;radial=rel-d*s;mesh.data.vertices[i].co=h+d*(s*K)+radial*(1-(1-TW)*u)
   moved.add(i)
   if u>nail_from:nailv.add(i)
   if u>.5:far.add(i)
 faces=0
 for f in mesh.data.polygons:
  # claw faces: mostly claw vertices, or any face reaching the outer half (long faces from the last joint to the tip)
  if sum(v in nailv for v in f.vertices)*2>=len(f.vertices) or any(v in far for v in f.vertices):f.material_index=ci;faces+=1
 return {'verts':len(moved),'faces':faces,'tips':[t.name for t in tips]}

if not body or not body.exists():raise SystemExit('Body file missing: '+str(body))
load(body)
arm=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None)
# Only meshes driven by the rig (a stray importer mesh such as a glTF Icosphere is not part of the creature).
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and (not arm or o.find_armature()==arm)]
out=outroot/'GuestBody';(out/'Textures').mkdir(parents=True,exist_ok=True)
claws=None
if opt('--claws'):
 k,tw=(float(x) for x in opt('--claws').split(','));skinned=max((o for o in meshes if o.vertex_groups),key=lambda o:len(o.data.vertices))
 claws=lengthen_claws(arm,skinned,k,tw,wrists=tuple(opt('--wrists','joint13,joint13.001').split(',')));print('CLAWS',claws)
mats=save_materials('GuestBody',meshes,out/'Textures');actions=[a.name for a in bpy.data.actions]
claw=None
if opt('--claw')=='from-body':
 claw=cut_right_hand(meshes,subtree(arm,opt('--hand-bones')) if opt('--hand-bones') else None)
 if not claw:print('WARNING no right hand/forearm vertex groups found; use --claw <hand file>')
export(meshes+([arm] if arm else []),out/'GuestBody.fbx',bool(arm))
write_meta(out,'GuestBody',opt('--source'),opt('--author'),opt('--license'),{'rigged':bool(arm),'actions':actions,'materials':mats,
 'changes':['format conversion; transforms, rest pose and bind poses untouched (scaled in Unity)']+(['distal finger phalanges lengthened x%s and tapered into black claws (%d vertices moved, Claw material on %d faces; joints, weights, bind poses unchanged)'%(opt('--claws'),claws['verts'],claws['faces'])] if claws else [])})
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
# Verify: every exported FBX re-imports with exactly the material names its manifest lists (GuestInstaller maps by name).
for folder in [p for p in (outroot/'GuestBody',outroot/'GuestClaw') if (p/(p.name+'.fbx')).exists()]:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(folder/(folder.name+'.fbx')))
 got=sorted({s.material.name for o in bpy.context.scene.objects if o.type=='MESH' for s in o.material_slots if s.material})
 want=sorted(e['name'] for e in json.loads((folder/'manifest.json').read_text(encoding='utf-8'))['materials'])
 print(('VERIFY OK ' if got==want else 'VERIFY FAIL ')+folder.name+' fbx='+str(got)+' manifest='+str(want))
