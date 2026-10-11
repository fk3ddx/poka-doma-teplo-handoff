import bpy,addon_utils,importlib,json,math,shutil
from pathlib import Path
from mathutils import Vector,Matrix
project=Path(__file__).resolve().parents[2]
root=project/'ImportInbox/MPFB';data=root/'assetdata';out=project/'unity-game/Assets/_Project/Art/Characters/NinaPreview';out.mkdir(parents=True,exist_ok=True);tex=out/'Textures';tex.mkdir(exist_ok=True)
bpy.context.preferences.use_preferences_save=False
repo=bpy.context.preferences.extensions.repos.new(name='Ataka isolated MPFB',module='ataka',custom_directory=str(root));addon_utils.modules_refresh();addon_utils.enable('bl_ext.ataka.mpfb',default_set=True,persistent=False)
H=importlib.import_module('bl_ext.ataka.mpfb.services.humanservice').HumanService
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
macro={'gender':0.,'age':.9,'muscle':.15,'weight':.88,'proportions':.5,'height':.35,'cupsize':.5,'firmness':.1,'race':{'caucasian':1.,'african':0.,'asian':0.}}
h=H.create_human(macro_detail_dict=macro);h.name='Nina'
T=importlib.import_module('bl_ext.ataka.mpfb.services.targetservice').TargetService
for fragment,value in {'chin/chin-width-decr':.28,'chin/chin-bones-decr':.3,'cheek/l-cheek-volume-incr':.45,'cheek/r-cheek-volume-incr':.45,'ears/l-ear-scale-decr':.25,'ears/r-ear-scale-decr':.25,'neck/neck-double-incr':.35,'neck/neck-scale-vert-decr':.15,'hip/hip-scale-horiz-incr':.15}.items():
 T.load_target(h,str(root/'mpfb/data/targets'/(fragment+'.target.gz')),weight=value)

H.set_character_skin(str(data/'skins/old_caucasian_female/old_caucasian_female.mhmat'),h,skin_type='MAKESKIN')
rig=H.add_builtin_rig(h,'game_engine')
def add(sub,typ='Clothes',alt=None):
 folder=data/sub;f=next(folder.glob('*.mhclo'));return H.add_mhclo_asset(str(f),h,asset_type=typ,subdiv_levels=0,alternative_materials=alt,set_up_rigging=True)
eyes=add('eyes/low-poly','Eyes',{'1cc97a30-85a1-42e6-a9f7-b3e753732baa':'materials/grey.mhmat'})
hair=add('hair/ponytail01','Hair');brows=add('eyebrows/eyebrow001','Eyebrows');dress=add('clothes/elvs_simple_60s_dress');glasses=add('clothes/toigo_round_glasses_leopard');apron=add('clothes/elvs_ladies_apron');veil=add('clothes/elvs_charity_veil1');socks=add('clothes/joepal_crude_high_socks')
# Lower the existing rig arms for an ordinary fitting pose.
for name in ['upperarm_l','upperarm_r']:
 pb=rig.pose.bones[name];bpy.context.view_layer.update();head=pb.head.copy();direction=(pb.tail-pb.head).normalized();target=Vector((.16 if head.x>0 else -.16,0,-.985));delta=direction.rotation_difference(target);pb.matrix=Matrix.Translation(head)@delta.to_matrix().to_4x4()@Matrix.Translation(-head)@pb.matrix
bpy.context.view_layer.update()
for name in ['lowerarm_l','lowerarm_r']:
 pb=rig.pose.bones[name];head=pb.head.copy();target=Vector((.07 if head.x>0 else -.07,-.18,.68));delta=(pb.tail-pb.head).normalized().rotation_difference((target-head).normalized());pb.matrix=Matrix.Translation(head)@delta.to_matrix().to_4x4()@Matrix.Translation(-head)@pb.matrix;bpy.context.view_layer.update()
# The costume is a clearly documented fitting garment, not the final floral robe.
obs=[o for o in bpy.context.scene.objects if o.type=='MESH'];deps=bpy.context.evaluated_depsgraph_get()
# Keep an editable MPFB source outside Unity Assets; evaluated export has helpers masked.
bpy.ops.wm.save_as_mainfile(filepath=str(root/'Nina-editable.blend'))
for o in obs:
 old=o.data;o.data=bpy.data.meshes.new_from_object(o.evaluated_get(deps),depsgraph=deps);o.modifiers.clear()
 for p in o.data.polygons:p.use_smooth=True
points=[o.matrix_world@v.co for o in obs for v in o.data.vertices];lo=Vector(tuple(min(p[i] for p in points) for i in range(3)));hi=Vector(tuple(max(p[i] for p in points) for i in range(3)));scale=1.55/(hi.z-lo.z)
for o in obs:
 mw=o.matrix_world.copy();o.parent=None
 for v in o.data.vertices:v.co=(mw@v.co-Vector((0,0,lo.z)))*scale
 o.matrix_world=Matrix.Identity(4)
# PS1 silhouette pass on the existing artist topology, not replacement primitive parts.
for o in obs:
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 if o==dress:
  for v in o.data.vertices:
   # Lengthen the existing ready dress below the hip to an ordinary calf-length garment.
   if v.co.z<.79:v.co.z=.79-(.79-v.co.z)*1.75
   v.co+=v.normal*.004
 if o==glasses:
  import bmesh
  bm=bmesh.new();bm.from_mesh(o.data);uv=bm.loops.layers.uv.active;opaque_lenses=[f for f in bm.faces if min(l[uv].uv.y for l in f.loops)>.67];print('Removed opaque lens faces',len(opaque_lenses),flush=True);bmesh.ops.delete(bm,geom=opaque_lenses,context='FACES');bm.to_mesh(o.data);bm.free()

 if o==veil:
  # Fit an existing fabric mesh closer at the lower cheeks; no new garment geometry.
  for v in o.data.vertices:
   t=max(0,min(1,(1.37-v.co.z)/.23));v.co.x*=1-.42*t
 if o==hair:
  import bmesh
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.z<1.33 or v.co.y>0],context='VERTS');bm.to_mesh(o.data);bm.free()

 ratio=.17 if o==h else .06 if o==apron else .3 if o in [dress,veil] else .25 if o==hair else .28 if o==glasses else .6
 if ratio<1:
  mod=o.modifiers.new('PS1 appearance LOD','DECIMATE');mod.ratio=ratio;bpy.ops.object.modifier_apply(modifier=mod.name)
 if o==apron:
  fit=o.modifiers.new('Ready apron fit after simplification','SHRINKWRAP');fit.target=dress;fit.wrap_method='NEAREST_SURFACEPOINT';fit.wrap_mode='OUTSIDE';fit.offset=.018;bpy.ops.object.modifier_apply(modifier=fit.name)
 for face in o.data.polygons:face.use_smooth=True
print('PS1 triangles',sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in obs),flush=True)
# Flatten the imported MakeSkin graph to standard texture materials with a reproducible manifest.
entries=[]
for o in obs:
 for m in o.data.materials:
  if not m or any(e['name']==m.name for e in entries):continue
  images=[n.image for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image] if m.use_nodes else []
  im=next((i for i in images if 'diffuse' in i.name.lower()),images[0] if images else None)
  if o==dress:im=bpy.data.images.load(str(data/'clothes/elvs_ladies_apron/apron2.png'),check_existing=True)
  alpha=o in [hair,brows,glasses];grey=o in [hair,brows]
  m.use_nodes=True;nt=m.node_tree;nt.nodes.clear();p=nt.nodes.new('ShaderNodeBsdfPrincipled');p.inputs['Roughness'].default_value=.72;p.inputs['Specular IOR Level'].default_value=.22;nout=nt.nodes.new('ShaderNodeOutputMaterial');nt.links.new(p.outputs['BSDF'],nout.inputs['Surface'])
  file=''
  if im:
   file=Path(bpy.path.abspath(im.filepath)).name;dst=tex/file;shutil.copy2(bpy.path.abspath(im.filepath),dst) if Path(bpy.path.abspath(im.filepath)).resolve()!=dst.resolve() else None;im=bpy.data.images.load(str(dst),check_existing=True)
   n=nt.nodes.new('ShaderNodeTexImage');n.image=im
   if grey:
    bw=nt.nodes.new('ShaderNodeRGBToBW');nt.links.new(n.outputs['Color'],bw.inputs[0]);ramp=nt.nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(.16,.17,.18,1);ramp.color_ramp.elements[1].color=(.63,.64,.65,1);nt.links.new(bw.outputs[0],ramp.inputs[0]);nt.links.new(ramp.outputs[0],p.inputs['Base Color'])
   else:nt.links.new(n.outputs['Color'],p.inputs['Base Color'])
   if alpha:nt.links.new(n.outputs['Alpha'],p.inputs['Alpha']);m.surface_render_method='DITHERED'
  entries.append({'name':m.name,'texture':'Textures/'+file if file else '', 'alphaClip':alpha,'greyscale':grey,'roughness':.72})
# Bake hair colour to its existing UV, keeping source opacity; Unity gets an ordinary albedo texture.
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=8
for o in [hair,brows]:
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 for m in o.data.materials:
  nt=m.node_tree;p=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED');src=next(n for n in nt.nodes if n.type=='TEX_IMAGE');im=bpy.data.images.new(o.name+'_Grey',1024,1024,alpha=True);dest=nt.nodes.new('ShaderNodeTexImage');dest.image=im;nt.nodes.active=dest
  bpy.ops.object.bake(type='DIFFUSE',pass_filter={'COLOR'},margin=8)
  # Retain source alpha via compositor-free image pixel channel transfer from the existing texture.
  src.image.scale(1024,1024);a=list(src.image.pixels);v=list(im.pixels);v[3::4]=a[3::4];im.pixels.foreach_set(v);im.filepath_raw=str(tex/(o.name+'_Grey.png'));im.file_format='PNG';im.save();nt.links.new(dest.outputs['Color'],p.inputs['Base Color']);nt.links.new(dest.outputs['Alpha'],p.inputs['Alpha'])
  for e in entries:
   if e['name']==m.name:e['texture']='Textures/'+o.name+'_Grey.png';e['greyscale']=False
bpy.ops.object.select_all(action='DESELECT')
for o in obs:o.select_set(True)
bpy.context.view_layer.objects.active=h
bpy.ops.export_scene.fbx(filepath=str(out/'Nina-preview.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
(out/'manifest.json').write_text(json.dumps({'height':1.55,'macro':macro,'materials':entries,'temporary':['Ready 60s dress lengthened/retextured; robe fastening not finished','Ready veil adapted as head covering; tied chin knot not finished','glasses have no chain','static preview, animation and rig not approved']},indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(root/'Nina-preview.blend'))
print('NINA_EXPORT_OK',[(o.name,len(o.data.vertices)) for o in obs],flush=True)
# Neutral inspection only; final user previews will be rendered inside Unity.
w=bpy.data.worlds.new('QA');w.use_nodes=True;w.node_tree.nodes['Background'].inputs[0].default_value=(.16,.18,.22,1);w.node_tree.nodes['Background'].inputs[1].default_value=.5;sc.world=w
cam=bpy.data.cameras.new('QA');cg=bpy.data.objects.new('QA',cam);sc.collection.objects.link(cg);sc.camera=cg;cam.type='ORTHO';cam.ortho_scale=.61
ld=bpy.data.lights.new('QA','AREA');lg=bpy.data.objects.new('QA',ld);sc.collection.objects.link(lg);lg.location=(-1,-2,2.5);ld.energy=100;ld.size=2;lg.rotation_euler=(Vector((0,0,1.3))-lg.location).to_track_quat('-Z','Y').to_euler()
sc.cycles.samples=24;sc.render.resolution_x=800;sc.render.resolution_y=900;sc.render.resolution_percentage=100
for name,pos in [('front',(0,-2,1.4)),('three-quarter',(.95,-1.6,1.42)),('profile',(2,-.05,1.4))]:
 cg.location=pos;cg.rotation_euler=(Vector((0,-.01,1.32))-cg.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(root/('qa-'+name+'.png'));bpy.ops.render.render(write_still=True)
