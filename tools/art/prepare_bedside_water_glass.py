"""Adapt DREAM_SEARCH_REPEAT's CC0 VasoChico without changing its proportions/topology.
Only the water effect is derived from the author's inner wall cross-section.
"""
import bpy,json,math,hashlib
from mathutils import Vector,Matrix
from pathlib import Path
source=Path('U:/art-sources/BedsideGlass/glasses.blend')
assert hashlib.sha256(source.read_bytes()).hexdigest()=='236915e459ca99e1aad9e13f0b89197c8ff2b210efaa6d8944504622d4db0a1d'
out=Path('U:/unity-game/Assets/_Project/Art/ReadyModels/WaterGlass');out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source),load_ui=False)
glass=bpy.data.objects['VasoChico'];points=[glass.matrix_world@v.co for v in glass.data.vertices]
lo=Vector(tuple(min(p[i] for p in points) for i in range(3)));hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
origin=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));scale=.1/(hi.z-lo.z)
for v,p in zip(glass.data.vertices,points):v.co=(p-origin)*scale
glass.matrix_world=Matrix.Identity(4);glass.name='Authored 8-sided drinking glass'
# Smooth normals averaged across the thin inner/outer lip turn the preview into a metal-like shell.
# The ready glass is genuinely faceted; keep each authored flat pane normal.
for face in glass.data.polygons:face.use_smooth=False
for o in list(bpy.data.objects):
 if o!=glass:bpy.data.objects.remove(o,do_unlink=True)
# This version of the author mesh has paired inner rings24..31 and32..39.
# Keep the glass itself intact; obtain the liquid boundary from those inner walls.
inner_top=[glass.data.vertices[i].co.copy() for i in range(24,32)]
inner_bottom=[glass.data.vertices[i].co.copy() for i in range(32,40)]
assert len(glass.data.vertices)==40
pairs=sorted(zip(inner_bottom,inner_top),key=lambda pair:math.atan2(pair[0].y,pair[0].x))
level=.064;upper=[];lower=[]
for bottom,top in pairs:
 assert bottom.z<level<top.z
 p=bottom.lerp(top,(level-bottom.z)/(top.z-bottom.z));p.x*=.99;p.y*=.99;upper.append(p)
 q=bottom.copy();q.x*=.99;q.y*=.99;q.z+=.0002;lower.append(q)
vertices=[tuple(v) for v in lower+upper];n=len(lower)
faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
mesh=bpy.data.meshes.new('Water inside author inner wall');mesh.from_pydata(vertices,[],faces);mesh.update()
water=bpy.data.objects.new('Water volume from inner profile',mesh);bpy.context.collection.objects.link(water)
# Neutral physically transparent source materials; Unity installer creates the URP equivalents.
for obj,name,color,rough in [(glass,'BedsideGlass',(.90,.95,1,1),.18),(water,'BedsideWater',(.80,.90,.95,1),.05)]:
 material=bpy.data.materials.new(name);material.use_nodes=True
 bsdf=material.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Base Color'].default_value=color;bsdf.inputs['Roughness'].default_value=rough;bsdf.inputs['Transmission Weight'].default_value=1;bsdf.inputs['IOR'].default_value=1.45 if obj==glass else 1.333
 obj.data.materials.clear();obj.data.materials.append(material);obj.select_set(True)
bpy.context.view_layer.objects.active=glass
bpy.ops.export_scene.fbx(filepath=str(out/'BedsideWaterGlass.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
report={'source_object':'VasoChico','author':'DREAM_SEARCH_REPEAT','source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'uniform_scale':scale,'glass_size_m':[hi[i]*scale-lo[i]*scale for i in range(3)],'glass_triangles':sum(len(f.vertices)-2 for f in glass.data.polygons),'water_height_m':level,'water_triangles':sum(len(f.vertices)-2 for f in water.data.polygons),'adaptation':'Recentered and uniformly scaled original glass to10cm; source40vertices/topology retained; flat pane normals instead of smoothing across the rim. Liquid effect from original inner-wall profile. No authored glass geometry remodelled.','limits':'Blender/source geometry only. URP transparency and actual game framing not verified.'}
(out/'preparation.json').write_bytes((json.dumps(report,indent=2)+'\n').encode())
# CPU-only source preview, not a Unity screenshot or acceptance of the game's rendering.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=24;scene.render.threads_mode='FIXED';scene.render.threads=2
scene.render.resolution_x=512;scene.render.resolution_y=512;scene.render.resolution_percentage=100
world=bpy.data.worlds.new('Neutral inspection world');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs['Color'].default_value=(.3,.3,.3,1);world.node_tree.nodes['Background'].inputs['Strength'].default_value=.5
bpy.ops.mesh.primitive_plane_add(size=2,location=(0,0,-.001));floor=bpy.context.object
mat=bpy.data.materials.new('Preview floor');mat.use_nodes=True
nodes=mat.node_tree.nodes;checker=nodes.new('ShaderNodeTexChecker');checker.inputs['Color1'].default_value=(.055,.035,.02,1);checker.inputs['Color2'].default_value=(.4,.28,.15,1);checker.inputs['Scale'].default_value=40
mat.node_tree.links.new(checker.outputs['Color'],nodes.get('Principled BSDF').inputs['Base Color']);floor.data.materials.append(mat)
for at,power,size in [((.2,-.2,.35),12,.2),((-.2,.15,.25),8,.2)]:
 bpy.ops.object.light_add(type='AREA',location=at);light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=size;light.rotation_euler=(Vector((0,0,.05))-light.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(.20,-.28,.21));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,.05))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=.19;scene.camera=camera
scene.render.image_settings.file_format='PNG';scene.render.filepath='U:/art-sources/BedsideGlass/source-preview.png';bpy.ops.render.render(write_still=True)
print(json.dumps(report,indent=2))
