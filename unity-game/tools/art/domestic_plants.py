"""Original curved botanical surfaces; executed in the furniture Blender namespace.
Pelargonium silhouette reference: https://www.rhs.org.uk/plants/12407/pelargonium-zonale/details
No external photographs/textures are copied into these meshes.
"""
def plant_surface(name,points,faces,material,normal=(0,1,0),thickness=.00045):
 mesh=bpy.data.meshes.new(name);mesh.from_pydata([xyz(p) for p in points],[],faces);mesh.update()
 reference=Vector(xyz(normal))
 if sum(p.normal.dot(reference) for p in mesh.polygons)<0:
  mesh.clear_geometry();mesh.from_pydata([xyz(p) for p in points],[],[tuple(reversed(f)) for f in faces]);mesh.update()
 o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);register(o,name,material)
 for face in mesh.polygons:face.use_smooth=True
 if thickness:
  mod=o.modifiers.new('Thin organic surface','SOLIDIFY');mod.thickness=thickness;mod.offset=0
 return o

def plant_grid(name,fn,cols,rows,material,normal=(0,1,0),thickness=.00045):
 points=[fn(i/cols,j/rows) for j in range(rows+1) for i in range(cols+1)];faces=[]
 for j in range(rows):
  for i in range(cols):
   a=j*(cols+1)+i;faces.append((a,a+1,a+cols+2,a+cols+1))
 return plant_surface(name,points,faces,material,normal,thickness)

def tulip_flower(center,angle,index):
 x,y,z=center
 for layer in range(2):
  for k in range(3):
   phi=angle+k*math.tau/3+layer*math.pi/3
   def point(u,v,phi=phi,layer=layer):
    t=.02+.98*v;across=u*2-1;theta=phi+across*1.13
    radius=(.004+.024*math.sin(t*math.pi*.79))*(1-.13*layer)
    radius+=.0025*math.cos(across*math.pi*.5)*math.sin(t*math.pi)
    height=.068*t*(1-.09*layer)-.008*abs(across)**3*t**5
    return(x+radius*math.cos(theta),y+height,z+radius*math.sin(theta))
   plant_grid('Tulip cupped petal',point,10,10,'PetalRose' if layer==0 else 'PetalDeep',(math.cos(phi),0,math.sin(phi)))
 # A small dark throat visible through the open petals, not a sealed ellipsoid bud.
 rod('Tulip throat',(x,y+.008,z),(x,y+.014,z),.004,'BookTan',vertices=8)

def tulip_blade(base,angle,length=.12):
 x,y,z=base;direction=Vector((math.cos(angle),0,math.sin(angle)));side=Vector((-math.sin(angle),0,math.cos(angle)))
 def point(u,v):
  t=.015+.97*v;across=u*2-1;w=.018*math.sin(math.pi*t)**.7
  pos=Vector((x,y,z))+direction*(length*t*.64)+side*(across*w)
  pos.y+=length*(.83*t-.34*t*t)+.0035*(1-across*across)*math.sin(math.pi*t)
  return tuple(pos)
 plant_grid('Tulip lanceolate leaf',point,6,16,'LeafSoft')
 # Thin central vein follows the fold, without a thick sausage-like leaf body.
 def vein(u,v):
  p=list(point(.48+.04*u,v));p[1]+=.00065;return tuple(p)
 plant_grid('Tulip leaf midrib',vein,1,16,'LeafVein',thickness=0)

def geranium_leaf(center,angle,tilt,index):
 x,y,z=center;rx=.059*(.92+.04*(index%3));rz=rx*.82
 def point(t,r):
  radius=r*(1+.105*math.cos(7*t)+.026*math.cos(21*t))
  dx=rx*radius*math.cos(t);dz=rz*radius*math.sin(t)
  xx=dx*math.cos(angle)-dz*math.sin(angle);zz=dx*math.sin(angle)+dz*math.cos(angle)
  yy=-.010*r*r+.006*math.cos(t*7)*r*r+dx*tilt
  return (x+xx,y+yy,z+zz)
 cols=56;rows=3;points=[(x,y,z)];faces=[]
 for j in range(1,rows+1):
  for i in range(cols):points.append(point(i*math.tau/cols,j/rows))
 for i in range(cols):faces.append((0,1+i,1+(i+1)%cols))
 for j in range(rows-1):
  for i in range(cols):
   a=1+j*cols+i;b=1+j*cols+(i+1)%cols;faces.append((a,a+cols,b+cols,b))
 plant_surface('Pelargonium scalloped leaf',points,faces,'LeafSoft')
 for k in range(7):
  theta=k*math.tau/7
  def vein(u,v,theta=theta):
   p=list(point(theta+(u-.5)*.018,.10+.78*v));p[1]+=.00065;return tuple(p)
  plant_grid('Pelargonium palmate vein',vein,1,6,'LeafVein',thickness=0)

def geranium_flower_head(center):
 x,y,z=center
 for i in range(7):
  angle=i*2.4;r=.029*math.sqrt(i/6);cx=x+r*math.cos(angle);cz=z+r*math.sin(angle);cy=y+.012*(1-i/7)
  rod('Pelargonium flower pedicel',(x,y-.027,z),(cx,cy,cz),.0014,'Leaf',vertices=6)
  for k in range(5):
   phi=k*math.tau/5+.23*i
   def point(u,v,phi=phi):
    a=(u-.5)*math.pi*1.2;t=.008*(.15+.85*v)
    radius=.004+t*math.cos(a);cross=t*math.sin(a)*.72
    return(cx+radius*math.cos(phi)-cross*math.sin(phi),cy+.002*math.sin(v*math.pi),cz+radius*math.sin(phi)+cross*math.cos(phi))
   plant_grid('Pelargonium flower petal',point,6,3,'PetalRose' if k<3 else 'PetalDeep',thickness=.0003)

def domestic_geranium(p):
 x,y,z=p
 rod('Clay flower pot',(x,y,z),(x,y+.14,z),.062,'Terracotta',.088,24)
 rod('Earth',(x,y+.137,z),(x,y+.14,z),.08,'Coffee')
 for i in range(9):
  angle=i*2.4;h=random.uniform(.22,.39);dx=.12*math.cos(angle);dz=.12*math.sin(angle);tilt=random.uniform(-.5,.5)
  curve('Geranium stem',[(x,y+.13,z),(x+dx*.35,y+h,z+dz*.35),(x+dx,y+h-.04,z+dz)],.003,'Leaf',resolution=4)
  geranium_leaf((x+dx,y+h-.04,z+dz),angle,tilt,i)
 for i in range(2):
  dx=(-.037 if i==0 else .039);dz=.013 if i==0 else -.016;top=y+.425-i*.031
  curve('Pelargonium flowering stalk',[(x,y+.14,z),(x+dx*.6,y+.32,z+dz*.7),(x+dx,top-.027,z+dz)],.0027,'Leaf',resolution=4)
  geranium_flower_head((x+dx,top,z+dz))
