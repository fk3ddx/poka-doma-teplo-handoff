"""Author apartment props in Blender; export evaluated meshes with exact Unity axes.
Run blender -b -P tools/art/build_furniture.py -- <project directory>.
Coordinates used by helpers: Unity x/y/z; Blender stores x/z/y.
"""
import bpy, math, random, json, pathlib, sys, gzip
from mathutils import Vector
random.seed(731)
ROOT=pathlib.Path(sys.argv[sys.argv.index('--')+1])
OUT=ROOT/'Assets/_Project/Art/Authored'; OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
materials={}; groups={}; current='Living'; collision=[]
def xyz(p):return (p[0],p[2],p[1])
def mat(name):
 if name not in materials: materials[name]=bpy.data.materials.new(name)
 return materials[name]
def register(o,name,material):
 o.name=name; o.data.materials.append(mat(material)); groups.setdefault(current,[]).append(o);return o
def box(name,p,s,m='Wood',bevel=.006):
 bpy.ops.mesh.primitive_cube_add(size=1,location=xyz(p));o=bpy.context.object;o.dimensions=xyz(s)
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('Crafted edges','BEVEL');mod.width=bevel;mod.segments=3
  mod=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');mod.keep_sharp=True
 return register(o,name,m)
def ellipsoid(name,p,s,m):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,location=xyz(p));o=bpy.context.object;o.scale=xyz(s)
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 for f in o.data.polygons:f.use_smooth=True
 return register(o,name,m)
def rod(name,a,b,r,m='Brass',r2=None,vertices=16):
 a,b=Vector(xyz(a)),Vector(xyz(b));v=b-a
 bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r,radius2=r if r2 is None else r2,depth=v.length,location=(a+b)*.5)
 o=bpy.context.object;o.rotation_euler=v.to_track_quat('Z','Y').to_euler()
 for f in o.data.polygons:f.use_smooth=True
 return register(o,name,m)
def curve(name,points,r,m,handles='AUTO',resolution=10):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.resolution_u=resolution;c.bevel_depth=r;c.bevel_resolution=2
 s=c.splines.new('BEZIER');s.bezier_points.add(len(points)-1)
 for q,p in zip(s.bezier_points,points):q.co=xyz(p);q.handle_left_type=handles;q.handle_right_type=handles
 o=bpy.data.objects.new(name,c);bpy.context.collection.objects.link(o);bpy.ops.object.select_all(action='DESELECT');bpy.context.view_layer.objects.active=o;o.select_set(True)
 bpy.ops.object.convert(target='MESH');o=bpy.context.object;o.select_set(False);return register(o,name,m)
def quad(name,verts,m,uv=None):
 mesh=bpy.data.meshes.new(name);mesh.from_pydata([xyz(v) for v in verts],[],[(0,1,2,3) if m in ('Carpet','FamilyPhoto','Mirror') else (3,2,1,0)]);mesh.update()
 layer=mesh.uv_layers.new();uv=uv or [(0,0),(1,0),(1,1),(0,1)]
 for p in mesh.polygons:
  for li in p.loop_indices:layer.data[li].uv=uv[mesh.loops[li].vertex_index]
 o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);return register(o,name,m)
exec(compile((ROOT/'tools/art/soft_textiles.py').read_text(encoding='utf-8-sig'), 'soft_textiles.py', 'exec'), globals())

def pillow(p,s,m='Fabric'):
 if m=='Linen':return soft_bed_pillow(p,s,m)
 o=box('Soft cushion',p,s,m,.08);o.rotation_euler.z=random.uniform(-.025,.025)
 mod=o.modifiers.new('Soft upholstery','SUBSURF');mod.levels=1
 return o
def turn(objects,center,yaw,position):
 # Blender Z corresponds to Unity Y; mirror handedness, use negative angle.
 from mathutils import Matrix
 rot=Matrix.Rotation(-math.radians(yaw),4,'Z')
 shift=Vector(xyz(position));c=Vector(xyz(center))
 for o in objects:o.matrix_world=Matrix.Translation(shift)@rot@Matrix.Translation(-c)@o.matrix_world

def unit(name,func,pos,yaw=0):
 before=set(bpy.data.objects);func();objects=[o for o in bpy.data.objects if o not in before];turn(objects,(0,0,0),yaw,pos)
 for o in objects:o['furniture_unit']=name

def sofa():
 for x in [-.84,.84]:
  for z in [-.27,.27]:rod('Tapered sofa foot',(x,0,z),(x,.22,z),.034,'Wood',.025)
 box('Sofa timber frame',(0,.24,0),(2.05,.2,.74),'Wood',.04)
 for x in [-.48,.48]:
  pillow((x,.43,.1),(.94,.2,.7),'Fabric')
  o=pillow((x,.72,-.25),(.94,.62,.22),'Fabric');o.rotation_euler.x=math.radians(-8)
 for x in [-1.01,1.01]:
  pillow((x,.51,0),(.17,.31,.8),'Fabric')
  box('Polished arm cap',(x,.695,0),(.19,.045,.76),'Wood',.02)
 for x in [-.8,-.25,.25,.8]:ellipsoid('Button tuft',(x,.75,-.113),(.014,.014,.007),'DarkFabric')
 # Throw over one arm, with a curved drop and visible hem.
 verts=[];faces=[];N=30;M=20
 for j in range(M+1):
  v=j/M
  for i in range(N+1):
   u=i/N;x=.35+u*.8;z=-.1+v*.53;y=.56 if x<.94 else .56-(x-.94)*2.0
   verts.append(xyz((x,y+.012*math.sin(u*35+v*3),z)))
 for j in range(M):
  for i in range(N):a=j*(N+1)+i;faces.append((a,a+1,a+N+2,a+N+1))
 mesh=bpy.data.meshes.new('Plaid drape');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Folded throw',mesh);bpy.context.collection.objects.link(o);register(o,'Folded throw','Plaid')
 mod=o.modifiers.new('Textile thickness','SOLIDIFY');mod.thickness=.003

def wallunit():
 # Three asymmetric sections, rear panels, shelves and actual inset fronts.
 for x,w in [(-.95,.85),(0,1.0),(.95,.8)]:
  box('Cabinet back',(x,1.05,-.205),(w,1.95,.025),'Wood',.002)
  for dx in [-w/2,w/2]:box('Cabinet side',(x+dx,1.07,0),(.035,1.98,.45))
  for y in [.1,.65,1.22,1.68,2.05]:box('Shelf',(x,y,0),(w,.025,.45))
  for dx in [-w/4,w/4]:
   box('Lower paneled door',(x+dx,.37,.24),(w/2-.022,.5,.025),'Wood',.008)
   box('Door inset',(x+dx,.37,.256),(w/2-.08,.4,.01),'WoodLight',.002)
   rod('Brass handle',(x+dx+.09,.32,.275),(x+dx+.09,.43,.275),.009)
 # Wardrobe paneled upper doors on right section.
 for dx in [.75,1.15]:
  box('Wardrobe door',(dx,1.37,.245),(.385,1.37,.028),'Wood',.004)
  box('Raised veneer',(dx,1.37,.263),(.3,1.26,.012),'WoodLight',.003)
  rod('Wardrobe handle',(dx-.1,1.23,.29),(dx-.1,1.36,.29),.008)
 # Servery glazing with slender brass timber edges, no opaque fake glass.
 for x in [-1.16,-.74]:
  for dx in [-.19,.19]:box('Glass stile',(x+dx,1.36,.246),(.016,1.33,.02),'Wood')
  for y in [.7,2.02]:box('Glass rail',(x,y,.246),(.4,.016,.02),'Wood')
  quad('Cabinet glazing',[(x-.18,.7,.25),(x+.18,.7,.25),(x+.18,2,.25),(x-.18,2,.25)],'Glass')
 for level in [.67,1.24,1.70]:
  for x in [-1.25,-1.07,-.89,-.71]:
   rod('Crystal stem',(x,level,-.01),(x,level+.12,-.01),.008,'Crystal')
   rod('Crystal bowl',(x,level+.12,-.01),(x,level+.24,-.01),.025,'Crystal',.055,16)
   rod('Crystal foot',(x,level,-.01),(x,level+.014,-.01),.04,'Crystal')
 # Books with paper blocks, spines, bands and nonuniform heights.
 for i in range(14):
  x=-.43+i*.061;h=random.uniform(.21,.32);m=['BookRed','BookGreen','BookTan'][i%3]
  box('Book paper',(x,1.7+h/2,0),(.045,h,.17),'Paper',.001)
  for dx in [-.026,.026]:box('Book cover',(x+dx,1.7+h/2,0),(.003,h+.008,.18),m,.001)
  box('Book spine',(x,1.7+h/2,.091),(.05,h+.006,.012),m,.003)
  for y in [1.74,1.7+h-.045]:box('Gilt book band',(x,y,.098),(.04,.004,.001),'Brass',0)

def tv():
 box('CRT case',(0,.22,0),(.61,.43,.36),'TVBrown',.044)
 box('CRT bezel',(-.035,.235,.19),(.465,.33,.048),'Black',.039)
 # A convex rounded rectangle, not an oval screen.
 verts=[xyz((-.035,.235,.239))];faces=[];N=64;R=7
 for ring in range(1,R+1):
  t=ring/R
  for i in range(N):
   a=i*2*math.pi/N;cx=math.cos(a);cy=math.sin(a)
   x=-.035+.222*t*math.copysign(abs(cx)**.34,cx);y=.235+.153*t*math.copysign(abs(cy)**.34,cy);z=.217+.022*(1-t*t)
   verts.append(xyz((x,y,z)))
 for i in range(N):faces.append((0,1+(i+1)%N,1+i))
 for j in range(R-1):
  for i in range(N):a=1+j*N+i;b=1+j*N+(i+1)%N;faces.append((a,b,b+N,a+N))
 mesh=bpy.data.meshes.new('Curved CRT rectangle');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Convex CRT glass',mesh);bpy.context.collection.objects.link(o);register(o,'Convex CRT glass','TVScreen')
 for f in mesh.polygons:f.use_smooth=True
 for y in [.31,.22]:rod('Channel dial',(.25,y,.195),(.25,y,.226),.029,'Black')
 for y in [.1,.115,.13,.145,.16]:box('Speaker slot',(.25,y,.205),(.08,.005,.006),'Black',.001)
 for x in [-.22,.22]:box('TV foot',(x,.011,0),(.08,.022,.2),'Black')

def table():
 box('Rounded polished table',(0,.727,0),(.78,.045,.91),'WoodLight',.04)
 for x in [-.30,.30]:
  for z in [-.365,.365]:rod('Tapered table leg',(x,.02,z),(x,.705,z),.021,'Wood',.034)
 box('Table lower shelf',(0,.20,0),(.67,.024,.76),'Wood')

def mug(p):
 x,y,z=p
 rod('Cup foot',(x,y,z),(x,y+.012,z),.043,'Porcelain')
 rod('Cup body',(x,y+.012,z),(x,y+.095,z),.037,'Porcelain',.048,32)
 rod('Coffee',(x,y+.096,z),(x,y+.097,z),.042,'Coffee',vertices=32)
 curve('Cup handle',[(x+.042,y+.077,z),(x+.074,y+.08,z),(x+.074,y+.024,z),(x+.043,y+.026,z)],.007,'Porcelain')
 rod('Saucer',(x,y-.006,z),(x,y,z),.073,'Porcelain',vertices=32)

def floorlamp():
 rod('Lamp base',(0,0,0),(0,.04,0),.17,'Brass',.14,32)
 rod('Lamp pole',(0,.04,0),(0,1.40,0),.016,'Brass')
 # Open conical shade, pleated mesh.
 verts=[];faces=[];N=96
 for y,r in [(1.27,.28),(1.63,.16)]:
  for i in range(N):a=i*2*math.pi/N;rr=r+(i%2)*.006;verts.append(xyz((rr*math.cos(a),y,rr*math.sin(a))))
 for i in range(N):j=(i+1)%N;faces.append((i,j,j+N,i+N))
 mesh=bpy.data.meshes.new('Pleated shade');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Pleated shade',mesh);bpy.context.collection.objects.link(o);register(o,'Pleated shade','Shade')
 mod=o.modifiers.new('Shade thickness','SOLIDIFY');mod.thickness=.002
 for y,r in [(1.27,.28),(1.63,.16)]:curve('Shade binding',[(r*math.cos(i*math.pi/8),y,r*math.sin(i*math.pi/8)) for i in range(17)],.006,'ShadeBinding')
 ellipsoid('Bulb',(0,1.4,0),(.037,.057,.037),'Bulb')

def radiator(x,z,width=1):
 for i in range(int(width/.095)):
  xx=x-width/2+i*.095
  box('Cast iron section',(xx,.46,z),(.07,.56,.15),'Enamel',.03)
 for y in [.22,.71]:rod('Radiator header',(x-width/2-.04,y,z),(x+width/2,y,z),.025,'Enamel')
 rod('Heating riser',(x+width/2+.06,.06,z),(x+width/2+.06,2.5,z),.016,'Enamel')

def curtain(x,width,z,top=2.39,bottom=.12,material='Curtain'):
 verts=[];faces=[];N=96;M=26
 for j in range(M+1):
  v=j/M
  for i in range(N+1):
   u=i/N;xx=x+(u-.5)*width; yy=bottom+(top-bottom)*v+.016*math.cos(u*35)*(1-v)
   zz=z+.045*math.cos(u*math.pi*12+.12*math.sin(v*4))+.007*math.sin(v*7+u*13)
   verts.append(xyz((xx,yy,zz)))
 for j in range(M):
  for i in range(N):a=j*(N+1)+i;faces.append((a,a+1,a+N+2,a+N+1))
 if material=='Tulle':faces=[tuple(reversed(face)) for face in faces]
 mesh=bpy.data.meshes.new('Curtain fabric');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Curtain folds',mesh);bpy.context.collection.objects.link(o);register(o,'Curtain folds',material)
 for f in mesh.polygons:f.use_smooth=True
 if material!='Tulle':
  mod=o.modifiers.new('Woven fabric thickness','SOLIDIFY');mod.thickness=.002

exec(compile((ROOT/'tools/art/domestic_plants.py').read_text(encoding='utf-8-sig'), 'domestic_plants.py', 'exec'), globals())

def plant(p):
 domestic_geranium(p)

def clock():
 box('Clock case',(0,1.57,0),(.34,.81,.16),'Wood',.02)
 rod('Clock brass bezel',(0,1.79,.087),(0,1.79,.10),.143,'Brass',vertices=48)
 rod('Clock face',(0,1.79,.102),(0,1.79,.104),.13,'Paper',vertices=48)
 for i in range(12):
  a=i*math.pi/6;rod('Hour mark',(.109*math.sin(a),1.79+.109*math.cos(a),.106),(.122*math.sin(a),1.79+.122*math.cos(a),.106),.004,'Black')
 rod('Clock minute',(0,1.79,.11),(.081,1.74,.11),.004,'Black')
 rod('Clock hour',(0,1.79,.113),(-.047,1.84,.113),.006,'Black')
 box('Pendulum recess',(0,1.4,.085),(.235,.33,.02),'Black',.01)
 rod('Pendulum',(0,1.57,.11),(0,1.3,.11),.008,'Brass')
 ellipsoid('Pendulum bob',(0,1.3,.11),(.06,.06,.012),'Brass')

# LIVING: actual dimensioned furniture, original passing clearances retained.
unit('Sofa',sofa,(3.35,0,1.73),90)
unit('Wall unit',wallunit,(6.24,0,1.92),-90)
unit('CRT',tv,(6.14,.66,1.92),-90)
unit('Coffee table',table,(4.3,0,2.1))
mug((4.42,.76,2.30))
unit('Floor lamp',floorlamp,(5.78,0,.39))
unit('Pendulum clock',clock,(5.03,0,4.385),180)
quad('Wall carpet',[(2.912,.99,.48),(2.912,.99,2.94),(2.912,2.25,2.94),(2.912,2.25,.48)],'Carpet')
quad('Floor carpet',[(3.9,.012,.55),(5.75,.012,.55),(5.75,.012,3.5),(3.9,.012,3.5)],'Carpet')
curtain(3.25,.38,.09);curtain(4.47,.38,.09)
curtain(3.85,1.10,-.035,2.35,.79,'Tulle')
rod('Curtain rail',(3.02,2.42,.07),(4.74,2.42,.07),.017,'Wood')
radiator(3.85,.02,1.12);plant((3.415,.885,.025))
# Glass vase and dried twigs.
rod('Vase body',(4.12,.76,1.87),(4.12,.94,1.87),.047,'Crystal',.032,24)
# Small framed pictures as physical frames; image inserts added in Unity.
for x,y,z in [(6.07,1.38,1.57),(6.07,1.37,2.21)]:
 box('Photo frame',(x,y,z),(.024,.24,.18),'Brass',.004)
 box('Photo insert',(x-.014,y,z),(.001,.205,.145),'Photo',0)
# Chandelier 3 bent arms and opal shades.
rod('Ceiling rose',(4.8,2.48,2.4),(4.8,2.5,2.4),.1,'Brass')
rod('Pendant chain',(4.8,2.14,2.4),(4.8,2.49,2.4),.011,'Brass')
for i in range(3):
 a=i*math.pi*2/3;dx=.3*math.cos(a);dz=.3*math.sin(a)
 curve('Chandelier arm',[(4.8,2.17,2.4),(4.8+dx*.6,2.02,2.4+dz*.6),(4.8+dx,2.1,2.4+dz)],.013,'Brass')
 ellipsoid('Opal shade',(4.8+dx,2.14,2.4+dz),(.105,.08,.105),'Shade')

# KITCHEN: rounded enamel fridge, range, cupboards, worktop, hardware.
current='Kitchen'
box('Zil enamel carcass',(8.39,.65,.7),(.65,1.3,.65),'Enamel',.065)
box('Zil rounded door',(8.39,.67,1.042),(.62,1.23,.06),'Enamel',.057)
curve('Zil handle',[(8.12,.8,1.09),(8.12,.97,1.12),(8.12,1.1,1.09)],.013,'Chrome')
box('Range enamel',(8.45,.42,1.69),(.58,.84,.59),'Enamel',.018)
box('Oven dark window',(8.45,.42,1.99),(.42,.31,.025),'Black',.025)
rod('Oven handle',(8.23,.64,2.03),(8.68,.64,2.03),.015,'Chrome')
for x in [8.23,8.37,8.52,8.67]:rod('Range control',(x,.765,1.99),(x,.765,2.018),.022,'Black')
for x in [8.30,8.59]:
 for z in [1.54,1.83]:
  rod('Gas burner',(x,.847,z),(x,.86,z),.065,'Black')
  for dx in [-.065,.065]:rod('Stove grate',(x+dx,.885,z-.085),(x+dx,.885,z+.085),.007,'Black')
rod('Kettle body',(8.59,.89,1.57),(8.59,1.055,1.57),.095,'Enamel',.066,32)
rod('Kettle lid',(8.59,1.055,1.57),(8.59,1.07,1.57),.069,'Chrome')
curve('Kettle handle',[(8.5,1,1.57),(8.48,1.16,1.57),(8.67,1.16,1.57),(8.68,1,1.57)],.013,'Black')
rod('Kettle spout',(8.65,.96,1.6),(8.76,1.06,1.62),.035,'Enamel',.022)
for y in [.85,1.95]:
 box('Kitchen cupboard',(6.84,y/2 if y<1 else 1.7,1.3),(.46,y if y<1 else .6,1.5),'KitchenPaint',.012)
 for z in [.91,1.67]:
  box('Cupboard door',(7.083,y/2 if y<1 else 1.7,z),(.035,y-.035 if y<1 else .56,.70),'KitchenPaint',.008)
  rod('Cabinet pull',(7.115,y*.5 if y<1 else 1.7,z-.2),(7.115,y*.5+.09 if y<1 else 1.79,z-.2),.007,'Chrome')
box('Kitchen worktop',(6.86,.86,1.3),(.54,.035,1.56),'Enamel',.012)
# Sink inset bowl represented by four rims and lowered base.
box('Sink dark well',(6.86,.88,.88),(.38,.016,.43),'Metal',.08)
for z in [.65,1.11]:box('Sink rim',(6.86,.89,z),(.45,.025,.024),'Chrome',.01)
for x in [6.64,7.08]:box('Sink rim',(x,.89,.88),(.024,.025,.45),'Chrome',.01)
curve('Curved faucet',[(6.66,.89,.64),(6.66,1.15,.64),(6.85,1.15,.71),(6.85,1.09,.77)],.012,'Chrome')
rod('Yellow gas pipe',(8.76,.02,2.22),(8.76,2.5,2.22),.018,'GasYellow')
rod('Gas elbow',(8.76,1.23,2.22),(8.48,1.23,2.22),.018,'GasYellow')
box('Gas valve',(8.62,1.25,2.23),(.085,.022,.025),'BookRed')
unit('Kitchen table',table,(7.72,0,.51))
mug((7.60,.76,.62));plant((7.315,.89,0))
for stoolX,z in [(7.72,1.12),(8.48,2.59)]:
 box('Stool seat',(stoolX,.45,z),(.34,.038,.34),'WoodLight',.04)
 for dx in [-.13,.13]:
  for dz in [-.13,.13]:rod('Stool leg',(stoolX+dx,.02,z+dz),(stoolX+dx,.43,z+dz),.022,'Wood')
curtain(7.7,1.14,-.07,1.67,1.0,'Tulle')
radiator(7.65,.025,.9)
# BEDROOM
current='Bedroom'
box('Mattress',(1.06,.45,1.71),(1.45,.21,2.1),'Linen',.085)
for x in [.31,1.81]:
 for z in [.62,2.79]:
  rod('Iron bedpost',(x,.03,z),(x,1.1,z),.023,'Enamel');ellipsoid('Bedpost finial',(x,1.1,z),(.035,.035,.035),'Brass')
for z in [.62,2.79]:
 curve('Bed top rail',[(.31,.94,z),(.65,1.02,z),(1.06,1.06,z),(1.47,1.02,z),(1.81,.94,z)],.02,'Enamel')
 rod('Bed lower rail',(.31,.57,z),(1.81,.57,z),.016,'Enamel')
 for x in [.52,.73,.94,1.15,1.36,1.57]:rod('Bed spindle',(x,.57,z),(x,1.0,z),.01,'Enamel')
for y,s in [(.62,(.64,.18,.51)),(.76,(.59,.17,.45)),(.90,(.52,.16,.4))]:pillow((1.05,y,.99),s,'Linen')
box('Bedside table',(2.12,.31,.89),(.44,.62,.45),'Wood',.015)
for y in [.2,.43]:
 box('Drawer',(2.12,y,1.128),(.39,.18,.025),'WoodLight');ellipsoid('Drawer knob',(2.12,y,1.155),(.021,.021,.02),'Brass')
# Bedside objects are authored in add_domestic_detail.py.
box('Wardrobe',(1.22,1.0,4.13),(1.67,2.0,.45),'Wood',.016)
for x in [.82,1.61]:
 box('Wardrobe panel',(x,1,3.89),(.75,1.86,.025),'WoodLight',.006)
 rod('Wardrobe pull',(x,1,3.86),(x,1.13,3.86),.009,'Brass')
curtain(.52,.38,.05);curtain(2.06,.38,.05);radiator(1.28,.02,1.1)
curtain(1.28,1.12,-.035,2.35,.79,'Tulle')
# HALL
current='Hall'
box('Telephone dresser',(6.91,.43,5.52),(.53,.86,.75),'Wood',.02)
box('Mirror backing',(6.61,1.54,5.54),(.042,.85,.64),'WoodLight',.025)
quad('Mirror',[(6.635,1.16,5.27),(6.635,1.16,5.81),(6.635,1.91,5.81),(6.635,1.91,5.27)],'Mirror')
box('Telephone base',(6.98,.90,5.44),(.25,.09,.24),'BookRed',.03)
rod('Rotary dial',(6.98,.945,5.46),(6.98,.951,5.46),.075,'Black',vertices=32)
for i in range(10):
 a=i*math.pi*.16;rod('Dial hole',(6.98+.053*math.cos(a),.952,5.46+.053*math.sin(a)),(6.98+.053*math.cos(a),.954,5.46+.053*math.sin(a)),.01,'Paper',vertices=12)
curve('Phone receiver',[(6.87,.995,5.37),(6.9,1.015,5.36),(7.06,1.015,5.36),(7.09,.995,5.37)],.022,'BookRed')
curve('Phone cable',[(7.1+.02*math.sin(i*.7),.93-i*.004,5.42+.02*math.cos(i*.7)) for i in range(90)],.003,'Black')
box('Coat rack',(8.70,1.65,4.6),(.055,.18,1.0),'Wood')
for z in [4.25,4.56,4.86]:curve('Coat hook',[(8.66,1.64,z),(8.57,1.69,z),(8.59,1.73,z)],.008,'Brass')
for x,z in [(7.95,5.6),(8.16,5.6)]:
 ellipsoid('Slipper sole',(x,.035,z),(.07,.025,.15),'DarkFabric');ellipsoid('Slipper upper',(x,.08,z-.06),(.07,.045,.095),'Fabric')
# BATHROOM (metric rounded fixtures)
current='Bathroom'
box('Bath enamel body',(3.54,.29,5.01),(1.19,.54,.69),'Enamel',.13)
box('Bath inner well',(3.54,.555,5.01),(1.01,.035,.52),'Metal',.14)
for x in [3.09,3.99]:
 for z in [4.78,5.23]:ellipsoid('Bath foot',(x,.04,z),(.045,.055,.045),'Chrome')
rod('Shower rail',(3.04,2.2,5.40),(4.14,2.2,5.40),.016,'Chrome')
curtain(3.18,.35,5.41,2.15,.43,'Linen')

# Second furnishing pass: personal traces and period-specific small props.
current='Living'
def armchair():
 for x in [-.29,.29]:
  for z in [-.24,.28]:rod('Armchair splayed leg',(x*1.1,.01,z*1.15),(x,.39,z),.025,'Wood',.035)
 box('Chair seat frame',(0,.34,.02),(.65,.12,.68),'Wood',.02)
 pillow((0,.45,.06),(.60,.19,.61),'Fabric')
 back=pillow((0,.75,-.25),(.61,.60,.19),'Fabric');back.rotation_euler.x=-.15
 for x in [-.36,.36]:
  rod('Armchair front post',(x,.2,.26),(x,.64,.26),.024,'Wood')
  rod('Armchair rear post',(x,.2,-.27),(x,.76,-.28),.024,'Wood')
  box('Rounded wooden armrest',(x,.66,.01),(.105,.045,.69),'WoodLight',.022)
unit('Soviet armchair',armchair,(4.36,0,.85),0)
# Eight-bit console and controller, deliberately fictional casing and game.
unit('Console',lambda:box('Eight bit console',(0,.035,0),(.27,.07,.18),'Enamel',.016),(6.11,1.25,1.91),-90)
box('Console cartridge',(6.11,1.36,1.90),(.13,.12,.045),'Enamel',.006)
box('Cartridge label',(6.11,1.36,1.926),(.09,.065,.002),'BookRed',.001)
box('Controller',(4.24,.785,2.42),(.15,.028,.064),'Enamel',.014)
box('Controller cross',(4.194,.802,2.42),(.029,.009,.012),'Black',.002)
box('Controller cross',(4.194,.802,2.42),(.011,.009,.030),'Black',.002)
for x in [4.267,4.29]:rod('Red action button',(x,.799,2.42),(x,.809,2.42),.009,'BookRed')
curve('Controller cable',[(4.24,.805,2.389),(4.36,.78,2.48),(4.44,.767,2.545),(4.46,.73,2.585),(4.48,.43,2.61),(4.50,.06,2.64),(4.57,.019,2.68),(4.9,.019,2.73),(5.35,.019,2.76),(5.77,.019,2.67),(5.94,.07,2.54),(5.95,.64,2.39),(5.97,1.19,2.30),(6.05,1.25,2.20),(6.10,1.25,2.10)],.004,'Black','AUTO')
# A book and reading spectacles at the chair.
box('Closed poetry book',(5.19,.07,.58),(.19,.043,.26),'BookGreen',.005)
# Properly mapped framed photograph on wall, separate image insert.
box('Family photograph frame',(3.91,1.62,4.385),(.34,.43,.027),'WoodLight',.009)
quad('Family photo',[(3.76,1.43,4.369),(4.06,1.43,4.369),(4.06,1.81,4.369),(3.76,1.81,4.369)],'FamilyPhoto')
# Cream lace runner on cabinet top.
box('Cabinet runner',(6.17,2.07,1.9),(.38,.005,2.53),'Linen',.002)
current='Kitchen'
# Real tiled backsplash with narrow grout, not a tiled image stretched over a wall.
for x in [6.612,8.785]:
 for j in range(4):
  for k in range(10):box('Glazed kitchen tile',(x,.94+j*.15,.60+k*.15),(.01,.146,.146),'Tile',.002)
# Cups, jars, breadboard and towel create domestic clusters instead of evenly scattered clutter.
for z,h in [(1.20,.18),(1.33,.13),(1.46,.17)]:
 rod('Preserve jar',(6.8,.89,z),(6.8,.89+h,z),.055,'Crystal',vertices=24)
 rod('Jar metal lid',(6.8,.89+h,z),(6.8,.905+h,z),.057,'Brass',vertices=24)
box('Breadboard',(7.87,.761,.37),(.23,.013,.25),'WoodLight',.025)
ellipsoid('Rye loaf',(7.87,.808,.37),(.105,.055,.105),'Bread')
# Oilcloth drapes past the tabletop edges, with actual uneven edges.
verts=[];faces=[];nx=32;nz=36
for j in range(nz+1):
 for i in range(nx+1):
  u=i/nx;v=j/nz;x=7.72+(u-.5)*1.06;z=.51+(v-.5)*1.20
  drop=max(0,abs(x-7.72)-.388,abs(z-.51)-.451)
  y=.755-drop*.95+.008*math.sin(u*34+v*3)*(drop/.14)
  verts.append(xyz((x,y,z)))
for j in range(nz):
 for i in range(nx):a=j*(nx+1)+i;faces.append((a,a+1,a+nx+2,a+nx+1))
mesh=bpy.data.meshes.new('Oilcloth drape');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Kitchen oilcloth',mesh);bpy.context.collection.objects.link(o);register(o,'Kitchen oilcloth','Oilcloth')
# Radio point with perforated grille and two controls.
box('Wired radio',(8.743,1.78,2.42),(.072,.23,.34),'TVBrown',.013)
for y in [1.72+i*.017 for i in range(8)]:box('Radio grille',(8.702,y,2.40),(.005,.006,.22),'Black',.002)
for z in [2.53,2.56]:rod('Radio dial',(8.693,1.75,z),(8.675,1.75,z),.016,'Enamel')
curve('Radio wire',[(8.75,1.65,2.42),(8.76,1.49,2.43),(8.76,1.42,2.55),(8.77,.25,2.55)],.003,'Black')
current='Hall'
def coat():
 # Closed rings, fabric folds around the body, visible separate sleeves and collar.
 verts=[];faces=[];rings=[(.84,.245,.045),(1.0,.24,.06),(1.3,.22,.07),(1.55,.23,.075),(1.68,.28,.055),(1.73,.11,.045)]
 N=48
 for j,(y,rx,rz) in enumerate(rings):
  for i in range(N):
   a=i*2*math.pi/N;fold=.008*math.sin(a*9+j*.7)
   verts.append(xyz((rx*math.cos(a)+fold,y,rz*math.sin(a)+fold)))
 for j in range(len(rings)-1):
  for i in range(N):a=j*N+i;b=j*N+(i+1)%N;faces.append((a,b,b+N,a+N))
 mesh=bpy.data.meshes.new('Coat folds');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Hanging wool coat',mesh);bpy.context.collection.objects.link(o);register(o,'Hanging wool coat','Coat')
 for f in mesh.polygons:f.use_smooth=True
 mod=o.modifiers.new('Soft cloth','SUBSURF');mod.levels=1
 for sign in [-1,1]:
  curve('Hanging sleeve',[(sign*.255,1.64,0),(sign*.36,1.42,.01),(sign*.39,1.07,.035)],.058,'Coat')
  collar=box('Turned collar',(sign*.072,1.66,.075),(.13,.16,.024),'Coat',.014);collar.rotation_euler.y=sign*.2
 for y in [1.05,1.2,1.35,1.5]:ellipsoid('Coat button',(.025,y,.078),(.01,.01,.005),'Black')
unit('Grandma coat',coat,(8.57,0,4.6),-90)
# Shower fixtures, basin, mirror and plumbing.
current='Bathroom'
box('Washbasin',(4.10,.77,5.79),(.53,.14,.36),'Enamel',.10)
box('Basin inset',(4.10,.84,5.76),(.41,.011,.23),'Metal',.06)
curve('Basin tap',[(4.10,.82,5.92),(4.10,1.00,5.92),(4.10,1.00,5.78)],.012,'Chrome')
box('Bath mirror backing',(4.10,1.47,5.97),(.47,.58,.028),'Enamel',.025)
quad('Bath mirror',[(3.89,1.21,5.951),(4.31,1.21,5.951),(4.31,1.73,5.951),(3.89,1.73,5.951)],'Mirror')
curve('Sink drain',[(4.1,.72,5.79),(4.1,.46,5.79),(4.1,.4,5.90),(4.1,.05,5.93)],.027,'Enamel')

exec(compile((ROOT/'tools/art/add_domestic_detail.py').read_text(encoding='utf-8-sig'), 'add_domestic_detail.py', 'exec'), globals())

exec(compile((ROOT/'tools/art/add_kitchen_and_yard_detail.py').read_text(encoding='utf-8-sig'), 'add_kitchen_and_yard_detail.py', 'exec'), globals())

exec(compile((ROOT/'tools/art/finish_domestic.py').read_text(encoding='utf-8-sig'), 'finish_domestic.py', 'exec'), globals())

exec(compile((ROOT/'tools/art/personalize_materials.py').read_text(encoding='utf-8-sig'), 'personalize_materials.py', 'exec'), globals())

# Bare courtyard trees: tapered, curved growth and secondary forks.
current='Yard';rng=random.Random(39)
def taper_branch(points,r):
 c=bpy.data.curves.new('Tapered branch','CURVE');c.dimensions='3D';c.resolution_u=3;c.bevel_depth=r;c.bevel_resolution=1
 spline=c.splines.new('BEZIER');spline.bezier_points.add(len(points)-1)
 for i,(q,p) in enumerate(zip(spline.bezier_points,points)):
  q.co=xyz(p);q.handle_left_type='AUTO';q.handle_right_type='AUTO';q.radius=max(.05,1-i/(len(points)-.8))
 o=bpy.data.objects.new('Bare deciduous branch',c);bpy.context.collection.objects.link(o);bpy.ops.object.select_all(action='DESELECT');bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o=bpy.context.object;o.select_set(False);register(o,'Bare deciduous branch','Bark')
for tree in range(14):
 x=-21+rng.random()*56;z=-7-rng.random()*16;h=rng.uniform(7,12.6);y=-11.7;bend=rng.uniform(-.65,.65)
 taper_branch([(x,y,z),(x+bend*.2,y+h*.34,z+.12),(x+bend*.65,y+h*.7,z-.1),(x+bend,y+h,z+.25)],.14)
 for j in range(10):
  a=j*2.4+rng.random()*.6;sy=y+h*(.26+j*.058);length=rng.uniform(1.5,3.2)*(1-j*.035);dx=math.cos(a)*length;dz=math.sin(a)*length
  start=(x+bend*.35,sy,z);mid=(x+dx*.65,sy+length*.52,z+dz*.7);end=(x+dx,sy+length*1.3,z+dz)
  taper_branch([start,(x+dx*.3,sy+.23,z+dz*.3),mid,end],.042*(1-j*.045))
  for sign in [-1,1]:
   branch_angle=a+sign*.7;tx=math.cos(branch_angle)*length*.6;tz=math.sin(branch_angle)*length*.6
   taper_branch([mid,(mid[0]+tx*.6,mid[1]+.5,mid[2]+tz*.7),(mid[0]+tx,mid[1]+1.4,mid[2]+tz)],.014)

# Geometry export: preserve explicit UVs; otherwise world/object-local box projection at real scale.
# Triangulate evaluated bevel/subdivision geometry, group by room and material to minimize draws.
source=ROOT/'tools/art/sources';source.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(source/'ApartmentFurniture.blend'))
result=[];deps=bpy.context.evaluated_depsgraph_get()
for room,objects in groups.items():
 chunks={}
 for obj in objects:
  ev=obj.evaluated_get(deps);mesh=ev.to_mesh();mesh.calc_loop_triangles();mw=obj.matrix_world;norm=mw.to_3x3().inverted().transposed()
  key=obj.data.materials[0].name;chunk=chunks.setdefault(key,{'name':room+'_'+key,'material':key,'vertices':[],'normals':[],'uv':[],'triangles':[]})
  explicit=obj.name.startswith(('Wall carpet','Floor carpet','Cabinet glazing','Mirror','Family photo','Bath mirror'))
  for tri in mesh.loop_triangles:
   start=len(chunk['vertices'])//3
   for li in tri.loops:
    loop=mesh.loops[li];v=mesh.vertices[loop.vertex_index];p=mw@v.co;n=(norm@loop.normal).normalized()
    chunk['vertices'].extend([round(p.x,5),round(p.z,5),round(p.y,5)])
    chunk['normals'].extend([round(n.x,5),round(n.z,5),round(n.y,5)])
    if key=='TVScreen':u,vv=(v.co.x+.035)/.444+.5,(v.co.z-.235)/.306+.5
    elif explicit and mesh.uv_layers.active:u,vv=mesh.uv_layers.active.data[li].uv
    else:
     local=v.co;axis=max(range(3),key=lambda a:abs(loop.normal[a]));axes=[a for a in range(3) if a!=axis];u,vv=local[axes[0]],local[axes[1]]
    chunk['uv'].extend([round(u,5),round(vv,5)])
   chunk['triangles'].extend([start+2,start+1,start])
  ev.to_mesh_clear()
 result.extend(chunks.values())
(OUT/'ApartmentFurniture.meshdata.json.gz').write_bytes(gzip.compress(json.dumps({'meshes':result},separators=(',',':')).encode('utf8'),compresslevel=9,mtime=0))
print('AUTHORED_MESHES',len(result),'TRIANGLES',sum(len(x['triangles'])//3 for x in result))
