# Blender 4.x: prepares the story props (SCENARIO 4 / 8) as static FBX + manifest for StoryPropsInstaller.
# Run: blender --background --python tools/art/prepare_story_props.py -- <downloads folder>
# Poly Haven (CC0) files are fetched by this script; Sketchfab (CC-BY 4.0) glb files must be downloaded
# while signed in, into <downloads folder> with the file names below. Credit every CC-BY author in CREDITS.md.
import bpy,json,re,sys,urllib.request
from pathlib import Path
from mathutils import Vector,Matrix
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
downloads=Path(args[0] if args else 'C:/Users/User/Downloads')
outroot=Path(__file__).resolve().parents[2]/'unity-game/Assets/_Project/Art/ReadyModels/Story'
# id, source ('ph:<asset>' or glb file name), target longest size (m), uid/author for credits
SOURCES=[
 ('Kettle','classic_enamel_kettle.glb',.26,'a419d3acf51c41b89137e2c2187c0efa','Yevhenii3d'),
 ('Belt','leather_belt.glb',.95,'8cba1fb2d0b6442cb0b7aa903025676c','Cyril43'),
 ('JamJar','casual_jar_of_raspberry_jam.glb',.14,'61bc4a8ef3184b169ab368946fcf2135','incg5764'),
 ('Matchbox','matchbox.glb',.055,'28007022e93d44848c67623ecfcc0737','poems_obj'),
 ('Bread','bread_loaf.glb',.30,'daf4801f72ae4f9eba9c338afbbb2926','Igrodev'),
 ('Shawl','cozy_winter_shawl.glb',.75,'9ab54de829bb4800b80992e76af96ddc','AwesomeAmbz'),
 ('Bulb','ph:lightbulb_01',.11,'lightbulb_01','Poly Haven'),
 ('Cans','ph:russian_food_cans_01',.16,'russian_food_cans_01','Poly Haven'),
]
def fetch_polyhaven(asset,folder):
 info=json.load(urllib.request.urlopen('https://api.polyhaven.com/files/'+asset))['gltf']['1k']['gltf']
 folder.mkdir(parents=True,exist_ok=True);main=folder/(asset+'.gltf');urllib.request.urlretrieve(info['url'],main)
 for rel,entry in info.get('include',{}).items():
  target=folder/rel;target.parent.mkdir(parents=True,exist_ok=True);urllib.request.urlretrieve(entry['url'],target)
 return main
for label,source,size,uid,author in SOURCES:
 if source.startswith('ph:'):
  path=fetch_polyhaven(source[3:],downloads/('polyhaven_'+source[3:]));link='https://polyhaven.com/a/'+source[3:];license='CC0-1.0'
 else:
  path=downloads/source;link='https://sketchfab.com/3d-models/'+uid;license='CC-BY-4.0'
  if not path.exists():print('MISSING',path);continue
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(path))
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
 pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
 lo=Vector(tuple(min(p[i] for p in pts) for i in range(3)));hi=Vector(tuple(max(p[i] for p in pts) for i in range(3)))
 scale=size/max(hi-lo);center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z)) # pivot: bottom centre
 for o in meshes:
  mw=o.matrix_world.copy();o.parent=None
  for v in o.data.vertices:v.co=(mw@v.co-center)*scale
  o.matrix_world=Matrix.Identity(4)
 out=outroot/label;tex=out/'Textures';tex.mkdir(parents=True,exist_ok=True);mats=[]
 for i,m in enumerate([m for m in bpy.data.materials if m.users]):
  if not m.use_nodes:continue
  m.name=label+'_Material_'+str(i);p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
  if not p:continue
  entry={'name':m.name,'color':list(p.inputs['Base Color'].default_value),'roughness':p.inputs['Roughness'].default_value,'texture':'','normal':''}
  for n in m.node_tree.nodes:
   if n.type!='TEX_IMAGE' or not n.image:continue
   fn=re.sub('[^a-zA-Z0-9_]+','_',n.image.name)+'.png';n.image.filepath_raw=str(tex/fn);n.image.file_format='PNG';n.image.save()
   for l in n.outputs['Color'].links:
    if l.to_node==p and l.to_socket.name=='Base Color':entry['texture']='Textures/'+fn
    if l.to_node.type=='NORMAL_MAP':entry['normal']='Textures/'+fn
  mats.append(entry)
 bpy.ops.object.select_all(action='DESELECT')
 for o in meshes:o.select_set(True)
 bpy.context.view_layer.objects.active=meshes[0]
 bpy.ops.export_scene.fbx(filepath=str(out/(label+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
 (out/'manifest.json').write_text(json.dumps({'id':label,'size':size,'source':link,'author':author,'license':license,'materials':mats},indent=1,ensure_ascii=False),encoding='utf-8')
 (out/'SOURCE.md').write_text('# '+label+'\n\nAuthor: '+author+'\nSource: '+link+'\nLicense: '+license+'\nPrepared by tools/art/prepare_story_props.py (uniform scale, bottom-centre pivot).\n',encoding='utf-8')
 print('PREPARED',label)
