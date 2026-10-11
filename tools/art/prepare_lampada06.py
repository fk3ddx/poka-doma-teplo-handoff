"""Small lampada explicitly authorized in Claude art-review-05 (not a general prop generator).
Run with Blender --background --factory-startup --disable-autoexec --python.
"""
import bpy, math, json
from pathlib import Path
from mathutils import Vector
root=Path('U:/unity-game/Assets/_Project/Art')
bpy.ops.wm.read_factory_settings(use_empty=True)
# Measure downloaded authors' meshes before any changes; do not alter them.
measurements=[]
for filename in [root/'CC0/alarm_clock_01/alarm_clock_01.fbx',root/'CC0/fancy_picture_frame_01/fancy_picture_frame_01.fbx']:
 bpy.ops.import_scene.fbx(filepath=str(filename))
 points=[o.matrix_world@v.co for o in bpy.context.scene.objects if o.type=='MESH' for v in o.data.vertices]
 lo=[min(v[i] for v in points) for i in range(3)];hi=[max(v[i] for v in points) for i in range(3)]
 measurements.append({'file':filename.name,'axes':'Blender X/Y horizontal, Z up','min':lo,'max':hi,'size':[hi[i]-lo[i] for i in range(3)],'parts':[o.name for o in bpy.context.scene.objects if o.type=='MESH']})
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
out=root/'BedroomDecor/Lampada06';out.mkdir(parents=True,exist_ok=True)
(out/'ready-asset-measurements.json').write_text(json.dumps(measurements,indent=2),encoding='utf-8')
red=bpy.data.materials.new('Lampada_RedGlass');red.diffuse_color=(.18,.006,.002,.55)
brass=bpy.data.materials.new('Lampada_AgedBrass');brass.diffuse_color=(.28,.17,.055,1)
def lathe(name,profile,material):
 n=32;vs=[];faces=[]
 for r,z in profile:
  for i in range(n):
   a=i*math.tau/n;vs.append((r*math.cos(a),r*math.sin(a),z))
 for j in range(len(profile)-1):
  for i in range(n):
   a=j*n+i;b=j*n+(i+1)%n;faces.append((a,b,b+n,a+n))
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(vs,[],faces);mesh.update()
 o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);mesh.materials.append(material)
 for poly in mesh.polygons:poly.use_smooth=True
 return o
# Closed-bottom cup, open top: 6 cm diameter, 7 cm height. Pivot is flame position.
lathe('Lampada red glass cup',[(0,-.08),(.015,-.08),(.022,-.074),(.029,-.045),(.03,-.01),(.028,-.01),(.027,-.045),(.020,-.072),(.014,-.078),(0,-.078)],red)
lathe('Lampada brass rim',[(.029,-.013),(.032,-.013),(.032,-.009),(.029,-.009),(.029,-.013)],brass)
for i in range(3):
 a=i*math.tau/3;start=Vector((.031*math.cos(a),.031*math.sin(a),-.01));end=Vector((.008*math.cos(a),.008*math.sin(a),.20))
 delta=end-start;bpy.ops.mesh.primitive_cylinder_add(vertices=8,radius=.00075,depth=delta.length,location=(start+end)*.5)
 o=bpy.context.object;o.name='Lampada suspension '+str(i+1);o.rotation_euler=delta.to_track_quat('Z','Y').to_euler();o.data.materials.append(brass)
lathe('Lampada suspension ring',[(.006,.195),(.010,.195),(.010,.201),(.006,.201),(.006,.195)],brass)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(out/'Lampada06.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,bake_space_transform=False,axis_forward='-Z',axis_up='Y',add_leaf_bones=False)
manifest={'authority':'docs/reviews/2026-10-11/claude-art-review-05/README.md, package06 item1','scope':'Approved small lampada geometry only; no Unity import or visual verification','units':'meters; pivot at flame; world flame target Y=1.75, shelf top=1.62','cup_diameter':.06,'cup_height':.07,'parts':[]}
for o in bpy.context.scene.objects:
 if o.type=='MESH':manifest['parts'].append({'name':o.name,'triangles':sum(len(poly.vertices)-2 for poly in o.data.polygons)})
(out/'geometry.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(json.dumps({'measurements':measurements,'lampada':manifest},indent=2))
