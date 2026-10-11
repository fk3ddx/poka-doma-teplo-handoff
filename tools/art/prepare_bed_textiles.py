# Textiles explicitly authorized in Claude review02 G2; furniture frame is an authored CC0 asset.
import bpy,math,json
from pathlib import Path
bpy.ops.wm.read_factory_settings(use_empty=True)
out=Path('U:/unity-game/Assets/_Project/Art/CC0/old_bed_frame')
linen=bpy.data.materials.new('Bed_Linen_CC0');linen.diffuse_color=(.72,.68,.57,1)
cover=bpy.data.materials.new('Bed_Cover_CC0');cover.diffuse_color=(.23,.29,.25,1)
def obj(name,verts,faces,mat):
 m=bpy.data.meshes.new(name);m.from_pydata(verts,[],faces);m.update();o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);m.materials.append(mat)
 uv=m.uv_layers.new(name='UVMap')
 for poly in m.polygons:
  poly.use_smooth=True
  for li in poly.loop_indices:
   v=m.vertices[m.loops[li].vertex_index].co;uv.data[li].uv=(v.x*2,v.y*2)
 return o
# Mattress: rounded textile block, not newly invented bed furniture.
bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,.52));m=bpy.context.object;m.name='Textile mattress';m.dimensions=(.86,1.88,.14);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);m.data.materials.append(linen);b=m.modifiers.new('Rounded fabric edges','BEVEL');b.width=.045;b.segments=3;bpy.ops.object.modifier_apply(modifier=b.name)
for f in m.data.polygons:f.use_smooth=True
# Coverlet: continuous grid, folds fall over both sides, open at pillow end.
v=[];f=[];nx=26;ny=34
for j in range(ny+1):
 y=-.30+j/ny*1.20
 for i in range(nx+1):
  x=-.56+i/nx*1.12;drop=max(0,abs(x)-.43)*1.05+max(0,y-.84)*2.0
  z=.612-drop+.007*math.sin(y*27+x*9)+.003*math.sin(y*61-x*14)
  v.append((x,y,z))
for j in range(ny):
 for i in range(nx):a=j*(nx+1)+i;f.append((a,a+1,a+nx+2,a+nx+1))
o=obj('Textile coverlet',v,f,cover);mod=o.modifiers.new('Fabric thickness','SOLIDIFY');mod.thickness=.003;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
# Upright, plump diamond pillows leaning into the headboard; authored textile exception.
# Widths are measured horizontally after the 45 degree turn, not long rectangular slabs.
for num,(w,cy,angle) in enumerate([(.70,-.72,-.035),(.60,-.59,.03),(.50,-.45,-.02)]):
 v=[];f=[];nx=22;ny=22
 for side in [1,-1]:
  for j in range(ny+1):
   for i in range(nx+1):
    u=i/nx;vv=j/ny
    # Review05: rounded 4cm corners, not pointed diamond fins.
    radius=.04;h=w/(2*math.sqrt(2))+radius*(1-1/math.sqrt(2))
    a=(u*2-1)*h;b=(vv*2-1)*h;inner=h-radius
    da=max(0,abs(a)-inner);db=max(0,abs(b)-inner);length=math.hypot(da,db)
    if length>radius:
     a=math.copysign(inner+da*radius/length,a);b=math.copysign(inner+db*radius/length,b)
    px=(a-b)/math.sqrt(2);pz=(a+b)/math.sqrt(2)
    bulge=max(0,math.sin(math.pi*u)*math.sin(math.pi*vv))**.48
    # All three lower corners touch the mattress; tops lean back, seams remain thin.
    z=.595+w*.5+pz
    y=cy-pz*.22+side*(.003+.05*bulge)
    v.append((px*math.cos(angle)-pz*math.sin(angle),y,z+px*math.sin(angle)))
 layer=(nx+1)*(ny+1)
 for side in range(2):
  for j in range(ny):
   for i in range(nx):
    a=side*layer+j*(nx+1)+i;q=(a,a+1,a+nx+2,a+nx+1);f.append(tuple(reversed(q)) if side==0 else q)
 edge=list(range(nx+1))+[j*(nx+1)+nx for j in range(1,ny+1)]+[ny*(nx+1)+i for i in range(nx-1,-1,-1)]+[j*(nx+1) for j in range(ny-1,0,-1)]
 for k,a in enumerate(edge):b=edge[(k+1)%len(edge)];f.append((a,b,b+layer,a+layer))
 o=obj('Textile upright pillow '+str(num+1),v,f,linen)
 # Cloth UVs in the actual cushion plane, avoiding the squashed former top-projection.
 for poly in o.data.polygons:
  for li in poly.loop_indices:
   co=o.data.vertices[o.data.loops[li].vertex_index].co;o.data.uv_layers.active.data[li].uv=(co.x*2,co.z*2)
# Review05: small veil on the upper part of the large back pillow only.
# It follows that cushion surface rather than crossing all three pillows.
lace=bpy.data.materials.new('Bed_Lace_CC0');lace.diffuse_color=(1,1,1,1)
v=[];f=[];nx=28;ny=22
for j in range(ny+1):
 t=j/ny
 for i in range(nx+1):
  x=-.22+.44*i/nx;z=(1.265-abs(x))*(1-t)+1.005*t
  pz=z-(.595+.35);h=.70/(2*math.sqrt(2))+.04*(1-1/math.sqrt(2))
  a=(x+pz)/math.sqrt(2);b=(pz-x)/math.sqrt(2)
  u=max(0,min(1,(a/h+1)/2));vv=max(0,min(1,(b/h+1)/2))
  bulge=max(0,math.sin(math.pi*u)*math.sin(math.pi*vv))**.48
  y=-.72-pz*.22+.003+.05*bulge+.009+.002*math.sin(x*58+t*12)
  v.append((x,y,z))
for j in range(ny):
 for i in range(nx):a=j*(nx+1)+i;f.append((a,a+1,a+nx+2,a+nx+1))
o=obj('Textile lace veil',v,f,lace)
for poly in o.data.polygons:
 for li in poly.loop_indices:
  index=o.data.loops[li].vertex_index;o.data.uv_layers.active.data[li].uv=((index%(nx+1))/nx,(index//(nx+1))/ny)
# Author frame spring deck measured at .49-.50m; mattress underside at .51m.
for o in bpy.context.scene.objects:o.location.z+=.06
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(out/'BedTextiles.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
report=[]
for o in bpy.context.scene.objects:
 points=[o.matrix_world @ vertex.co for vertex in o.data.vertices]
 lo=[min(v[i] for v in points) for i in range(3)];hi=[max(v[i] for v in points) for i in range(3)]
 assert all(math.isfinite(x) for x in lo+hi)
 report.append(dict(name=o.name,vertices=len(points),min=lo,max=hi,size=[hi[i]-lo[i] for i in range(3)]))
(out/'BedTextiles-geometry.json').write_text(json.dumps({'scope':'Blender textile mesh dimensions only; no Unity import or visual approval','meshes':report},indent=2),encoding='utf-8')
print('TEXTILES_EXPORTED',len(bpy.context.scene.objects),'cloth meshes')
