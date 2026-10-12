# Blender 4.x: story props from sources with DIRECT downloads (OpenGameArt, Poly Haven) — no Sketchfab sign-in.
#   blender --background --python tools/art/prepare_story_props_open.py -- <cache folder>
# Downloads each source into <cache folder>, keeps the author's geometry/UVs/textures, wires the author's texture files
# into a Principled material where the source uses a legacy material, uniform scale + bottom-centre pivot, FBX + manifest
# into Art/ReadyModels/Story/<id> (same layout as prepare_story_props.py, installed by StoryPropsInstaller).
import bpy,json,re,sys,os,zipfile,urllib.request
from pathlib import Path
from mathutils import Vector,Matrix
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
cache=Path(args[0] if args else 'C:/Users/User/Downloads/story-open');cache.mkdir(parents=True,exist_ok=True)
outroot=Path(__file__).resolve().parents[2]/'unity-game/Assets/_Project/Art/ReadyModels/Story'
OGA='https://opengameart.org/sites/default/files/'
PP='https://static.poly.pizza/'
# Material colour overrides (installer has no transparency): the jam jar's opaque 'glass' becomes cherry glass so the jam reads.
TINT={'JamJar':{'lambert2SG':((.32,.04,.06,1),.12)}}
ONLY=set(args[1].split(',')) if len(args)>1 else None  # e.g. JamJar,Shawl,Cans — rebuild only these
# id, download, file inside, objects (None = all meshes), longest size m (None = keep real scale), textures {diffuse, normal}, author, license, page
SOURCES=[
 ('Kettle',OGA+'pava.blend','pava.blend',None,.26,None,'DREAM_SEARCH_REPEAT','CC0-1.0','https://opengameart.org/content/old-keetle'),
 ('Matchbox',OGA+'firebreathermatchbox.zip','Matchbox/matchbox.glb',None,.055,None,'the_mazander_man','CC-BY-3.0','https://opengameart.org/content/firebreather-matchbox'),
 ('Bread',OGA+'foodPackOGACC0.blend','foodPackOGACC0.blend',['bread'],.32,{'diffuse':'packed:breadD','normal':'packed:bread01N.tga'},'yd','CC0-1.0','https://opengameart.org/content/food-pack-0'),
 ('Belt',OGA+'belt_1.zip','belt_1/belt_1.blend',None,.30,{'diffuse':'belt_1/textures/belt_1_001_d.png','normal':'belt_1/textures/belt_1_001_n.png'},'Paul Wortmann','CC0-1.0','https://opengameart.org/content/belt'),
 ('TeaSet','ph:tea_set_01','tea_set_01.gltf',None,None,None,'Rico Cilliers, James Ray Cock, Jurita Burger (Poly Haven)','CC0-1.0','https://polyhaven.com/a/tea_set_01'),
 # Poly Pizza (direct GLB, no sign-in). Google Poly archive models: original author shown by Poly Pizza as "Poly by Google".
 ('JamJar',PP+'46c56d53-8640-405c-a561-ea25cc5d0b77.glb','46c56d53-8640-405c-a561-ea25cc5d0b77.glb',None,.14,None,'Poly by Google (via Poly Pizza)','CC-BY-3.0','https://poly.pizza/m/3jQoqeTyeLC'),
 ('Shawl',PP+'d81a47b9-df2a-43d3-9589-76f1b203d983.glb','d81a47b9-df2a-43d3-9589-76f1b203d983.glb',['Line002'],.45,None,'Poly by Google (via Poly Pizza)','CC-BY-3.0','https://poly.pizza/m/5CEwkJvkU9V'),
 ('Bag',PP+'2bc244a8-c709-4cb1-aba1-1f24beb6b9c5.glb','2bc244a8-c709-4cb1-aba1-1f24beb6b9c5.glb',None,.50,None,'Quaternius (via Poly Pizza)','CC0-1.0','https://poly.pizza/m/Wq1JPPqHZS'),
 ('Corvalol',PP+'a441a76e-cc50-48a7-bc0a-430a5e242ef9.glb','a441a76e-cc50-48a7-bc0a-430a5e242ef9.glb',None,.09,None,'Quaternius (via Poly Pizza)','CC0-1.0','https://poly.pizza/m/Pc8dM9Ja4V'),
 ('Cans','ph:russian_food_cans_01','russian_food_cans_01.gltf',None,None,None,'Kosan Aziz (Poly Haven)','CC0-1.0','https://polyhaven.com/a/russian_food_cans_01'),
]
# Tea set laid out for one table: teapot, sugar bowl, two cups on saucers (metres, around the bottom-centre).
TEA_LAYOUT={'teapot_01':(0,0),'teapot_01_lid':(0,0),'sugar_cup_01':(.2,-.06),'sugar_cup_01_lid':(.2,-.06),'cup_small_01':(-.2,.12),'saucer_circular_03':(-.2,.12),'cup_small_02':(.2,.16),'saucer_circular_04':(.2,.16)}

UA={'User-Agent':'Mozilla/5.0 (ataka story props)'}
def download(url,dest):
 with urllib.request.urlopen(urllib.request.Request(url,headers=UA),timeout=60) as r:Path(dest).write_bytes(r.read())
def fetch(url,name):
 if url.startswith('ph:'):
  asset=url[3:];folder=cache/('polyhaven_'+asset);info=json.load(urllib.request.urlopen(urllib.request.Request('https://api.polyhaven.com/files/'+asset,headers=UA)))['gltf']['1k']['gltf']
  folder.mkdir(parents=True,exist_ok=True);main=folder/(asset+'.gltf');download(info['url'],main)
  for rel,e in info.get('include',{}).items():t=folder/rel;t.parent.mkdir(parents=True,exist_ok=True);download(e['url'],t)
  return folder
 dest=cache/url.rsplit('/',1)[1]
 if not dest.exists():download(url,dest)
 if dest.suffix=='.zip':
  folder=cache/dest.stem
  if not folder.exists():zipfile.ZipFile(dest).extractall(folder)
  return folder
 return cache
def load(path,only):
 bpy.ops.wm.read_factory_settings(use_empty=True);ext=path.suffix.lower()
 if ext=='.blend':
  with bpy.data.libraries.load(str(path)) as (src,dst):dst.objects=list(src.objects);dst.images=list(src.images)
  for o in dst.objects:
   if o and o.type=='MESH' and (only is None or o.name in only):bpy.context.scene.collection.objects.link(o)
 elif ext in('.glb','.gltf'):
  bpy.ops.import_scene.gltf(filepath=str(path))
  for o in [o for o in bpy.context.scene.objects if o.type=='MESH' and only and o.name not in only]:bpy.data.objects.remove(o,do_unlink=True)
 return [o for o in bpy.context.scene.objects if o.type=='MESH']
def image(spec,base):
 if spec.startswith('packed:'):return bpy.data.images.get(spec[7:])
 return bpy.data.images.load(str(base/spec))
def wire(meshes,tex,base):
 m=bpy.data.materials.new('wired');m.use_nodes=True;nt=m.node_tree;p=nt.nodes['Principled BSDF'];p.inputs['Roughness'].default_value=.6
 d=nt.nodes.new('ShaderNodeTexImage');d.image=image(tex['diffuse'],base);nt.links.new(d.outputs['Color'],p.inputs['Base Color'])
 if tex.get('normal'):
  n=nt.nodes.new('ShaderNodeTexImage');n.image=image(tex['normal'],base);n.image.colorspace_settings.name='Non-Color';nm=nt.nodes.new('ShaderNodeNormalMap');nt.links.new(n.outputs['Color'],nm.inputs['Color']);nt.links.new(nm.outputs['Normal'],p.inputs['Normal'])
 for o in meshes:o.data.materials.clear();o.data.materials.append(m)
def layout_tea(meshes):
 for o in meshes:
  key=next((k for k in sorted(TEA_LAYOUT,key=len,reverse=True) if o.name.endswith(k)),None)
  if key is None:bpy.data.objects.remove(o,do_unlink=True);continue
  x,y=TEA_LAYOUT[key];o.location.x,o.location.y=x,y
 bpy.context.view_layer.update();return [o for o in bpy.context.scene.objects if o.type=='MESH']
def save_materials(label,objs,tex):
 mats=[];seen=set()
 for o in objs:
  for slot in o.material_slots:
   m=slot.material
   if not m or m.as_pointer() in seen or not m.use_nodes:continue
   seen.add(m.as_pointer());m.name=label+'_Material_'+str(len(mats));p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
   if not p:continue
   e={'name':m.name,'color':list(p.inputs['Base Color'].default_value),'roughness':p.inputs['Roughness'].default_value,'texture':'','normal':''}
   for n in m.node_tree.nodes:
    if n.type!='TEX_IMAGE' or not n.image:continue
    img=n.image;fn=re.sub('[^a-zA-Z0-9_]+','_',img.name)+'.png'
    if max(img.size)>1024:img.scale(*(max(1,v*1024//max(img.size)) for v in img.size))
    st=bpy.context.scene.render.image_settings;st.file_format='PNG';st.color_mode='RGBA' if img.channels==4 else 'RGB';st.color_depth='8';img.save_render(str(tex/fn))
    for l in n.outputs['Color'].links:
     if l.to_node==p and l.to_socket.name=='Base Color':e['texture']='Textures/'+fn
     if l.to_node.type=='NORMAL_MAP':e['normal']='Textures/'+fn
   mats.append(e)
 return mats

for label,url,inner,only,size,tex,author,license,page in SOURCES:
 if ONLY and label not in ONLY:continue
 base=fetch(url,inner);path=base/inner
 if not path.exists():print('MISSING',path);continue
 meshes=load(path,only)
 for o in meshes:
  for sl in o.material_slots:
   t=TINT.get(label,{}).get(sl.material.name if sl.material else '')
   if t:bp=sl.material.node_tree.nodes.get('Principled BSDF');bp.inputs['Base Color'].default_value=t[0];bp.inputs['Roughness'].default_value=t[1]
 if label=='TeaSet':meshes=layout_tea(meshes)
 if tex:wire(meshes,tex,base)
 if label=='Bread':  # the author's loaf stands on its end (Y-up source); lay it on the table
  for o in meshes:o.matrix_world=Matrix.Rotation(1.5708,4,'X')@o.matrix_world
  bpy.context.view_layer.update()
 pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
 lo=Vector(tuple(min(p[i] for p in pts) for i in range(3)));hi=Vector(tuple(max(p[i] for p in pts) for i in range(3)))
 scale=size/max(hi-lo) if size else 1.0;center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
 for o in meshes:
  mw=o.matrix_world.copy();o.parent=None
  if o.data.users>1:o.data=o.data.copy()
  for v in o.data.vertices:v.co=(mw@v.co-center)*scale
  o.matrix_world=Matrix.Identity(4)
 out=outroot/label;texdir=out/'Textures';texdir.mkdir(parents=True,exist_ok=True);mats=save_materials(label,meshes,texdir)
 bpy.ops.object.select_all(action='DESELECT')
 for o in meshes:o.select_set(True)
 bpy.context.view_layer.objects.active=meshes[0]
 bpy.ops.export_scene.fbx(filepath=str(out/(label+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
 dims=[round(x,3) for x in (hi-lo)*scale]
 (out/'manifest.json').write_text(json.dumps({'id':label,'size':size or max(dims),'dimensions':dims,'source':page,'author':author,'license':license,'materials':mats},indent=1,ensure_ascii=False),encoding='utf-8')
 (out/'SOURCE.md').write_text('# '+label+'\n\nAuthor: '+author+'\nSource: '+page+'\nLicense: '+license+'\nPrepared by tools/art/prepare_story_props_open.py (uniform scale, bottom-centre pivot'+(', author textures wired into a Principled material' if tex else '')+(', four pieces laid out for one table' if label=='TeaSet' else '')+').\n',encoding='utf-8')
 print('PREPARED',label,'dims',dims,'faces',sum(len(o.data.polygons) for o in meshes),'materials',[m['name'] for m in mats])
