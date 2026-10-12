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
# Review07 replaces the art director's earlier on-edge specification with a flat stack.
# Retain renderer names only to preserve Unity material overrides / file IDs.
pillow_specs=[(.70,.70,.16,-.57),(.60,.60,.15,-.60),(.50,.50,.14,-.60)]
def cover_height(x,y):
 topx=min(1,abs(x)/.43);sag=.010*(1-topx*topx);side=max(0,abs(x)-.405)
 drop=(math.sqrt(side*side+.012*.012)-.012)*1.08+max(0,abs(y)-.915)**2*9
 return .609-sag-drop+.0012*math.sin(y*8+x*5)+.0006*math.sin(y*17-x*8)
def bulge_at(x,y,w,d,cy):
 # Rounded-rectangle signed distance keeps the entire rounded seam at zero bulge.
 # A rectangular sine sampled after clipping corners produced dark pinched spikes.
 r=.026;qx=abs(x)-(w*.5-r);qy=abs(y-cy)-(d*.5-r)
 depth=r-(math.hypot(max(qx,0),max(qy,0))+min(max(qx,qy),0))
 t=max(0,min(1,depth/(min(w,d)*.5)))
 return math.sin(t*math.pi*.5)**.25

def pillow_top(i,x,y):
 w,d,t,cy=pillow_specs[i]
 return support(i,x,y)+.006+(t-.006)*bulge_at(x,y,w,d,cy)
def support(i,x,y):return cover_height(x,y) if i==0 else pillow_top(i-1,x,y)
contact=[]
for num,(w,d,t,cy) in enumerate(pillow_specs):
 v=[];f=[];nx=32;ny=32;bottom_gaps=[]
 for side in [1,-1]:
  for j in range(ny+1):
   for i in range(nx+1):
    x=(i/nx-.5)*w;y=(j/ny-.5)*d
    # Soft corners and pinched seam, convex feather filling rather than a bevelled slab.
    radius=.026;ix=w*.5-radius;iy=d*.5-radius
    dx=max(0,abs(x)-ix);dy=max(0,abs(y)-iy);length=math.hypot(dx,dy)
    if length>radius:
     x=math.copysign(ix+dx*radius/length,x);y=math.copysign(iy+dy*radius/length,y)
    y+=cy
    below=support(num,x,y)
    # Compressed underside conforms to the support across its footprint; a centre-only
    # contact left dark7cm wedges at the seams and looked like levitation at5.1.
    bottom=below+.0002
    top=pillow_top(num,x,y)
    assert bottom<top, (num,x,y,bottom,top)
    z=top if side==1 else bottom
    v.append((x,y,z))
    if side==-1:bottom_gaps.append(bottom-below)
 layer=(nx+1)*(ny+1)
 for side in range(2):
  for j in range(ny):
   for i in range(nx):
    a=side*layer+j*(nx+1)+i;q=(a,a+1,a+nx+2,a+nx+1)
    f.append(q if side==0 else tuple(reversed(q)))
 edge=list(range(nx+1))+[j*(nx+1)+nx for j in range(1,ny+1)]+[ny*(nx+1)+i for i in range(nx-1,-1,-1)]+[j*(nx+1) for j in range(ny-1,0,-1)]
 for k,a in enumerate(edge):b=edge[(k+1)%len(edge)];f.append((a,b,b+layer,a+layer))
 o=obj('Textile upright pillow '+str(num+1),v,f,linen)
 contact.append({'layer':num+1,'minimum_gap_m':min(bottom_gaps),'contact_vertices_under_1mm':sum(g<.001 for g in bottom_gaps)})
 assert min(bottom_gaps)<.001 and min(bottom_gaps)>=0
# One lace veil over the top cushion, falling about10cm down its front edge.
lace=bpy.data.materials.new('Bed_Lace_CC0');lace.diffuse_color=(1,1,1,1)
v=[];f=[];nx=32;ny=28
for j in range(ny+1):
 for i in range(nx+1):
  x=-.23+.46*i/nx
  if j<=20:
   y=-.81+.46*j/20;z=pillow_top(2,x,y)+.004
  else:
   t=(j-20)/8;y=-.346+.008*t
   z=max(pillow_top(2,x,-.35)+.004-.10*t,pillow_top(1,x,y)+.004)
  v.append((x,y,z))
for j in range(ny):
 for i in range(nx):
  a=j*(nx+1)+i;f.append((a,a+1,a+nx+2,a+nx+1))
o=obj('Textile lace veil',v,f,lace)
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
(out/'BedTextiles-geometry.json').write_text(json.dumps({'scope':'Blender textile mesh dimensions only; no Unity import or visual approval','review':'07','pillow_arrangement':'flat stacked feather cushions','support_contact':contact,'meshes':report},indent=2),encoding='utf-8')
print('TEXTILES_EXPORTED',len(bpy.context.scene.objects),'cloth meshes')
