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
 y=-.30+j/ny*1.21
 for i in range(nx+1):
  x=-.56+i/nx*1.12;drop=max(0,abs(x)-.43)*1.4
  z=.612-drop+.007*math.sin(y*27+x*9)+.003*math.sin(y*61-x*14)
  v.append((x,y,z))
for j in range(ny):
 for i in range(nx):a=j*(nx+1)+i;f.append((a,a+1,a+nx+2,a+nx+1))
o=obj('Textile coverlet',v,f,cover);mod=o.modifiers.new('Fabric thickness','SOLIDIFY');mod.thickness=.003;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
# Three sewn pillows, two shaped cloth surfaces meeting at the edge.
for num,(w,l,h,z,angle) in enumerate([(.60,.43,.14,.65,-.06),(.50,.38,.12,.747,.10),(.40,.31,.10,.829,-.09)]):
 v=[];f=[];nx=18;ny=14
 for side in [1,-1]:
  for j in range(ny+1):
   for i in range(nx+1):
    u=i/nx;vv=j/ny;x=(u-.5)*w;y=(vv-.5)*l
    bulge=(max(0,math.sin(math.pi*u)*math.sin(math.pi*vv)))**.55
    px=x*math.cos(angle)-y*math.sin(angle);py=x*math.sin(angle)+y*math.cos(angle)-.60
    v.append((px,py,z+side*(.003+bulge*h*.5)))
 layer=(nx+1)*(ny+1)
 for side in range(2):
  for j in range(ny):
   for i in range(nx):
    a=side*layer+j*(nx+1)+i;q=(a,a+1,a+nx+2,a+nx+1);f.append(q if side==0 else tuple(reversed(q)))
 edge=list(range(nx+1))+[j*(nx+1)+nx for j in range(1,ny+1)]+[ny*(nx+1)+i for i in range(nx-1,-1,-1)]+[j*(nx+1) for j in range(ny-1,0,-1)]
 for k,a in enumerate(edge):b=edge[(k+1)%len(edge)];f.append((a,b,b+layer,a+layer))
 obj('Textile pillow '+str(num+1),v,f,linen)
# Author frame spring deck measured at .49-.50m; mattress underside at .51m.
for o in bpy.context.scene.objects:o.location.z+=.06
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(out/'BedTextiles.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
print('TEXTILES_EXPORTED',len(bpy.context.scene.objects),'cloth meshes')
