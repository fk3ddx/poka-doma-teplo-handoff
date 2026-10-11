"""Final domestic geometry pass. Executed inside build_furniture.py, after earlier detail passes."""
from mathutils.bvhtree import BVHTree
import bmesh

def remove_named(room, prefixes, condition=lambda o:True):
 for obj in list(groups[room]):
  if obj.name.startswith(prefixes) and condition(obj):
   groups[room].remove(obj);bpy.data.objects.remove(obj,do_unlink=True)

def world_center(obj):
 return sum((obj.matrix_world@Vector(corner) for corner in obj.bound_box),Vector())/8

# Draped openwork cloth follows the actual evaluated pillow surfaces.
current='Bedroom';deps=bpy.context.evaluated_depsgraph_get()
pillow_trees=[(o,BVHTree.FromObject(o,deps)) for o in groups['Bedroom'] if o.name.startswith('Soft cushion')]
cols=42;rows=40;cloth=[]
for j in range(rows+1):
 row=[]
 for i in range(cols+1):
  x=.60+i/cols*.90;z=.675+j/rows*.78;top=.585
  for obj,tree in pillow_trees:
   inv=obj.matrix_world.inverted();origin=inv@Vector(xyz((x,1.5,z)));direction=(inv.to_3x3()@Vector((0,0,-1))).normalized()
   hit,normal,index,distance=tree.ray_cast(origin,direction,2)
   if hit is not None:top=max(top,(obj.matrix_world@hit).z+.009)
  row.append(top)
 cloth.append(row)
# Tension bridges steps between pillows without pushing the lace into their surfaces.
for step in range(14):
 updated=[r[:] for r in cloth]
 for j in range(1,rows):
  for i in range(1,cols):updated[j][i]=max(cloth[j][i],max(cloth[j-1][i],cloth[j+1][i],cloth[j][i-1],cloth[j][i+1])-.023)
 cloth=updated
textile('Pillow crochet canopy',lambda u,v:(.60+u*.90,cloth[round(v*rows)][round(u*cols)],.675+v*.78),rows=rows,cols=cols,material='Tulle')
for side,idx in [(.60,0),(1.50,cols)]:
 for j in range(20):
  z=.675+j/20*.78;h=cloth[round(j/20*rows)][idx]
  curve('Pillow lace scallop',[(side,h,z),(side+.005,h-.015,z+.01),(side+.005,h-.018,z+.028),(side,h,z+.039)],.0015,'Lace')

# Rounded ceramic/steel vessels have an outer wall, rolled rim, inner wall and real bottom.
def open_bowl(name,center,rings,material):
 verts=[];faces=[];steps=12
 for y,w,d,r in rings:
  for cx,cz,start in [(w/2-r,d/2-r,0),(-w/2+r,d/2-r,90),(-w/2+r,-d/2+r,180),(w/2-r,-d/2+r,270)]:
   for k in range(steps):
    a=math.radians(start+k*90/steps)
    verts.append(xyz((center[0]+cx+r*math.cos(a),y,center[1]+cz+r*math.sin(a))))
 n=steps*4
 for layer in range(len(rings)-1):
  for i in range(n):a=layer*n+i;b=layer*n+(i+1)%n;faces.append((a,b,b+n,a+n))
 faces.append(tuple(reversed(range(n))));faces.append(tuple((len(rings)-1)*n+i for i in range(n)))
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
 bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(mesh);bm.free()
 obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj);register(obj,name,material)
 for face in mesh.polygons:face.use_smooth=len(face.vertices)==4
 return obj

current='Bathroom'
remove_named('Bathroom',('Bath enamel body','Bath inner well','Washbasin','Basin inset'))
open_bowl('Sitting bath enamel shell',(3.48,5.01),[(.085,1.00,.51,.12),(.53,1.14,.67,.12),(.571,1.14,.67,.12),(.578,1.055,.575,.11),(.22,.89,.40,.13),(.18,.81,.33,.12)],'Enamel')
open_bowl('Washbasin ceramic shell',(4.10,5.79),[(.69,.37,.24,.07),(.822,.53,.36,.075),(.847,.53,.36,.075),(.847,.425,.255,.065),(.728,.30,.16,.045)],'Enamel')
for x,y,z in [(3.76,.187,5.01),(4.1,.736,5.79)]:
 rod('Basin drain ring',(x,y,z),(x,y+.004,z),.023,'Chrome',vertices=24)
 for i in range(5):
  a=i*math.tau/5;rod('Drain perforation',(x+.012*math.cos(a),y+.005,z+.012*math.sin(a)),(x+.012*math.cos(a),y+.006,z+.012*math.sin(a)),.003,'Black',vertices=8)

current='Kitchen'
remove_named('Kitchen',('Sink dark well','Sink rim','Kitchen worktop','Curved faucet'))
remove_named('Kitchen',('Kitchen cupboard',),lambda o:world_center(o).z<1)
# Lower cabinet remains hollow beneath the sink, with doors and rails retained.
for x in [6.624,7.055]:box('Sink cupboard side',(x,.425,1.30),(.028,.83,1.48),'KitchenPaint',.004)
for z in [.566,2.034]:box('Sink cupboard end',(6.84,.425,z),(.43,.83,.028),'KitchenPaint',.004)
box('Sink cupboard base',(6.84,.055,1.30),(.43,.035,1.48),'KitchenPaint',.003)
# Continuous rounded counter with a true rounded sink cutout: no coplanar overlaps or corner gaps.
verts=[];faces=[];n=48
for y,cx,cz,w,d,r in [(.8775,6.86,1.30,.54,1.56,.008),(.8775,6.86,.88,.415,.45,.055),(.8425,6.86,.88,.415,.45,.055),(.8425,6.86,1.30,.54,1.56,.008)]:
 for ox,oz,start in [(w/2-r,d/2-r,0),(-w/2+r,d/2-r,90),(-w/2+r,-d/2+r,180),(w/2-r,-d/2+r,270)]:
  for k in range(12):
   a=math.radians(start+k*90/12);verts.append(xyz((cx+ox+r*math.cos(a),y,cz+oz+r*math.sin(a))))
for layer in range(4):
 for i in range(n):a=layer*n+i;b=layer*n+(i+1)%n;c=((layer+1)%4)*n+(i+1)%n;d=((layer+1)%4)*n+i;faces.append((a,b,c,d))
mesh=bpy.data.meshes.new('Continuous counter');mesh.from_pydata(verts,[],faces);mesh.update()
bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(mesh);bm.free()
obj=bpy.data.objects.new('Kitchen worktop rounded cutout',mesh);bpy.context.collection.objects.link(obj);register(obj,'Kitchen worktop rounded cutout','Enamel')
open_bowl('Stainless sink bowl',(6.86,.88),[(.71,.32,.36,.065),(.875,.445,.48,.06),(.899,.455,.49,.06),(.899,.398,.433,.065),(.73,.30,.33,.07)],'Chrome')
rod('Sink strainer',(6.86,.735,.88),(6.86,.74,.88),.023,'Chrome',vertices=24)
for i in range(5):
 a=i*math.tau/5;rod('Sink strainer hole',(6.86+.013*math.cos(a),.741,.88+.013*math.sin(a)),(6.86+.013*math.cos(a),.742,.88+.013*math.sin(a)),.003,'Black',vertices=8)
curve('Yolochka faucet neck',[(6.656,.90,.88),(6.656,1.14,.88),(6.86,1.14,.88),(6.86,1.05,.88)],.012,'Chrome')
rod('Faucet mixer bridge',(6.65,.92,.73),(6.65,.92,1.03),.018,'Chrome')
for z,cap in [(.73,'BookRed'),(1.03,'WasherBlue')]:
 rod('Faucet valve',(6.65,.895,z),(6.65,.965,z),.019,'Chrome',vertices=24)
 for axis in [0,1]:
  a=(6.615,.971,z) if axis==0 else (6.65,.971,z-.035);b=(6.685,.971,z) if axis==0 else (6.65,.971,z+.035)
  rod('Faucet cross handle',a,b,.007,'Enamel',vertices=12)
 rod('Faucet temperature cap',(6.65,.972,z),(6.65,.976,z),.009,cap,vertices=16)

# A real open-top drawer, separated as an animatable room group.
current='Hall'
remove_named('Hall',('Telephone dresser','Pocket flashlight','Torch lens'))
remove_named('Hall',('Hall dresser inset drawer','Hall drawer recessed field','Hall drawer drop handle'),lambda o:world_center(o).z>.52 and world_center(o).y>5.53)
box('Dresser rear wall',(6.66,.44,5.52),(.025,.81,.75),'Wood',.004)
for z in [5.158,5.882]:box('Dresser side wall',(6.92,.44,z),(.50,.81,.025),'Wood',.006)
for y in [.075,.515,.835]:box('Dresser internal shelf',(6.92,y,5.52),(.50,.025,.72),'Wood',.003)
box('Dresser vertical divider',(6.92,.66,5.52),(.50,.28,.025),'Wood',.003)
current='HallDrawer'
box('Working drawer front',(7.187,.66,5.71),(.025,.20,.34),'WoodLight',.007)
box('Working drawer inset',(7.203,.66,5.71),(.008,.145,.28),'Wood',.004)
curve('Drawer handle',[(7.216,.685,5.67),(7.229,.65,5.674),(7.229,.65,5.746),(7.216,.685,5.75)],.005,'Brass')
box('Drawer plywood floor',(6.956,.570,5.71),(.45,.018,.305),'WoodLight',.003)
for z in [5.553,5.867]:box('Drawer side',(6.956,.65,z),(.45,.15,.012),'WoodLight',.003)
box('Drawer back',(6.732,.65,5.71),(.015,.15,.31),'WoodLight',.003)
# Flashlight lies near the drawer front and becomes visible after opening it.
rod('Stored pocket flashlight',(7.073,.61,5.63),(7.073,.61,5.79),.023,'Black',vertices=24)
rod('Flashlight reflector',(7.073,.61,5.788),(7.073,.61,5.815),.028,'Chrome',.026,24)
rod('Flashlight glass',(7.073,.61,5.816),(7.073,.61,5.819),.023,'Glass',vertices=24)
box('Flashlight slider',(7.092,.619,5.71),(.011,.015,.035),'Chrome',.002)
for z in [5.62,5.65]:rod('Spare battery',(6.89,.592,z),(6.97,.592,z),.012,'BookTan',vertices=16)
current='Living'
