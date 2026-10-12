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
# Review06: one continuous drape. No head-end height step or repeated raised ribs.
# Mattress top is .59 before the shared .06 lift; cloth underside stays above it.
v=[];f=[];nx=36;ny=64
for j in range(ny+1):
 y=-.945+j/ny*1.89
 for i in range(nx+1):
  x=-.56+i/nx*1.12
  topx=min(1,abs(x)/.43)
  sag=.010*(1-topx*topx)
  side=max(0,abs(x)-.405)
  drop=(math.sqrt(side*side+.012*.012)-.012)*1.08
  end=max(0,abs(y)-.915)
  drop+=end*end*9
  waves=.0012*math.sin(y*8+x*5)+.0006*math.sin(y*17-x*8)
  z=.609-sag-drop+waves
  v.append((x,y,z))
for j in range(ny):
 for i in range(nx):
  a=j*(nx+1)+i;f.append((a,a+1,a+nx+2,a+nx+1))
o=obj('Textile coverlet',v,f,cover)
mod=o.modifiers.new('Fabric thickness','SOLIDIFY');mod.thickness=.002
bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
# Review06: separate cushions, 67 degrees from the mattress, below the top rail.
# Widths .70/.60/.50; slightly compressed vertical cloth dimension, not rigid squares.
tilt=math.radians(67);sn=math.sin(tilt);cs=math.cos(tilt)
pillow_specs=[(.70,-.72),(.60,-.565),(.50,-.41)]
def pillow_point(w,base_y,u,vv,side):
 radius=.04;h=w/(2*math.sqrt(2))+radius*(1-1/math.sqrt(2))
 a=(u*2-1)*h;b=(vv*2-1)*h;inner=h-radius
 da=max(0,abs(a)-inner);db=max(0,abs(b)-inner);length=math.hypot(da,db)
 if length>radius:
  a=math.copysign(inner+da*radius/length,a)
  b=math.copysign(inner+db*radius/length,b)
 px=(a-b)/math.sqrt(2);pv=(a+b)/math.sqrt(2)
 vertical=(pv+w*.5)*.76
 bulge=max(0,math.sin(math.pi*u)*math.sin(math.pi*vv))**.48
 thickness=side*(.003+.05*bulge)
 return (px,base_y-vertical*cs+thickness*sn,.605+vertical*sn+thickness*cs)
for num,(w,base_y) in enumerate(pillow_specs):
 v=[];f=[];nx=24;ny=24
 for side in [1,-1]:
  for j in range(ny+1):
   for i in range(nx+1):v.append(pillow_point(w,base_y,i/nx,j/ny,side))
 layer=(nx+1)*(ny+1)
 for side in range(2):
  for j in range(ny):
   for i in range(nx):
    a=side*layer+j*(nx+1)+i;q=(a,a+1,a+nx+2,a+nx+1)
    f.append(tuple(reversed(q)) if side==0 else q)
 edge=list(range(nx+1))+[j*(nx+1)+nx for j in range(1,ny+1)]+[ny*(nx+1)+i for i in range(nx-1,-1,-1)]+[j*(nx+1) for j in range(ny-1,0,-1)]
 for k,a in enumerate(edge):b=edge[(k+1)%len(edge)];f.append((a,b,b+layer,a+layer))
 o=obj('Textile upright pillow '+str(num+1),v,f,linen)
 for poly in o.data.polygons:
  for li in poly.loop_indices:
   co=o.data.vertices[o.data.loops[li].vertex_index].co
   o.data.uv_layers.active.data[li].uv=(co.x*2,co.z*2)
# Veil follows the actual front surface of the large back cushion only.
lace=bpy.data.materials.new('Bed_Lace_CC0');lace.diffuse_color=(1,1,1,1)
v=[];f=[];nx=24;ny=16
for j in range(ny+1):
 for i in range(nx+1):
  u=.50+.46*i/nx;vv=.50+.46*j/ny
  x,y,z=pillow_point(.70,-.72,u,vv,1)
  v.append((x,y+.004*sn,z+.004*cs))
for j in range(ny):
 for i in range(nx):
  a=j*(nx+1)+i;f.append((a,a+1,a+nx+2,a+nx+1))
# Same outward normal as the pillow front, so white threads receive the flashlight.
o=obj('Textile lace veil',v,[tuple(reversed(face)) for face in f],lace)
for poly in o.data.polygons:
 for li in poly.loop_indices:
  index=o.data.loops[li].vertex_index
  o.data.uv_layers.active.data[li].uv=((index%(nx+1))/nx,(index//(nx+1))/ny)
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
(out/'BedTextiles-geometry.json').write_text(json.dumps({'scope':'Blender textile mesh dimensions only; no Unity import or visual approval','review':'06','pillow_tilt_degrees':67,'pillow_parallel_plane_gap_m':.155*sn-.106,'meshes':report},indent=2),encoding='utf-8')
print('TEXTILES_EXPORTED',len(bpy.context.scene.objects),'cloth meshes')
