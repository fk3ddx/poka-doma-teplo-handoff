"""Normalize an existing authored CC0 Poly Haven bulb, without network or new geometry.
Blender --background --python tools/art/prepare_story_bulb_local.py
"""
import bpy,json,shutil
from pathlib import Path
from mathutils import Vector,Matrix
root=Path(__file__).resolve().parents[2]
source=root/'unity-game/Assets/_Project/Art/CC0/lightbulb_01'
out=root/'unity-game/Assets/_Project/Art/ReadyModels/Story/Bulb'
out.mkdir(parents=True,exist_ok=True);(out/'Textures').mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(source/'lightbulb_01.fbx'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
lo=Vector([min(v[i] for v in points) for i in range(3)]);hi=Vector([max(v[i] for v in points) for i in range(3)])
scale=.11/max(hi-lo);pivot=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
for o in meshes:
 mw=o.matrix_world.copy();o.parent=None
 for v in o.data.vertices:v.co=(mw@v.co-pivot)*scale
 o.matrix_world=Matrix.Identity(4)
materials=[]
for i,m in enumerate({m.name:m for o in meshes for m in o.data.materials if m}.values()):
 m.name='Bulb_Material_'+str(i)
 materials.append({'name':m.name,'color':[1,1,1,1],'roughness':.45,'texture':'Textures/diffuse.jpg','normal':'Textures/normal.png'})
for f in ('diffuse.jpg','normal.png'):shutil.copy2(source/f,out/'Textures'/f)
bpy.ops.object.select_all(action='DESELECT')
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=meshes[0]
bpy.ops.export_scene.fbx(filepath=str(out/'Bulb.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
(out/'manifest.json').write_text(json.dumps({'id':'Bulb','size':.11,'source':'https://polyhaven.com/a/lightbulb_01','author':'Josh Dean / Poly Haven','license':'CC0-1.0','materials':materials},indent=2),encoding='utf-8')
(out/'SOURCE.md').write_text('# Bulb\n\nAuthor: Josh Dean / Poly Haven. CC0.\nSource: https://polyhaven.com/a/lightbulb_01\nUses already verified local FBX and textures. Uniform scale to 11 cm and bottom-centre pivot; geometry unchanged.\nGlass is opaque in this small pickup representation; not a new luminous fixture.\n',encoding='utf-8')
print('READY_BULB',len(meshes),len(materials),'max_dimension_m',.11)
