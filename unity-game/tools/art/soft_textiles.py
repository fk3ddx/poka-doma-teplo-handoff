"""Bed linen with pinched sewn edges and inflated cloth surfaces (metres)."""
def soft_bed_pillow(p,s,material):
 nx,nz=28,24;verts=[];faces=[]
 def edge(u,v):return (u*s[0]*.5*(1-.06*v**8),v*s[2]*.5*(1-.06*u**8))
 for side in [1,-1]:
  for j in range(nz+1):
   v=j/nz*2-1
   for i in range(nx+1):
    u=i/nx*2-1;x,z=edge(u,v)
    dome=max(0,(1-u*u)*(1-v*v))**.46
    # Small edge puckers dissolve before the broad centre; no uniform corrugation.
    gathers=.0035*math.sin(v*31+u*5)*abs(u)**5*(1-v*v)+.0025*math.sin(u*27-v*3)*abs(v)**5*(1-u*u)
    y=side*s[1]*.5*(.13+.87*dome)+gathers*dome
    verts.append(xyz((x,y,z)))
 stride=(nx+1)*(nz+1)
 for layer in range(2):
  for j in range(nz):
   for i in range(nx):
    a=layer*stride+j*(nx+1)+i;face=(a,a+1,a+nx+2,a+nx+1)
    faces.append(face if layer==0 else tuple(reversed(face)))
 boundary=list(range(nx+1))+[j*(nx+1)+nx for j in range(1,nz+1)]+[nz*(nx+1)+i for i in range(nx-1,-1,-1)]+[j*(nx+1) for j in range(nz-1,0,-1)]
 for k,a in enumerate(boundary):
  b=boundary[(k+1)%len(boundary)];faces.append((a,a+stride,b+stride,b))
 mesh=bpy.data.meshes.new('Sewn cotton pillow');mesh.from_pydata(verts,[],faces);mesh.update()
 import bmesh
 bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
 for f in mesh.polygons:f.use_smooth=True
 obj=bpy.data.objects.new('Soft cushion',mesh);bpy.context.collection.objects.link(obj);obj.location=xyz(p);register(obj,'Soft cushion',material)
 # Fine seam follows the same rounded rectangle as the inflated surfaces.
 points=[]
 for k in boundary[::4]:
  bx,by,bz=verts[k];points.append((p[0]+bx,p[1],p[2]+by))
 points.append(points[0]);curve('Pillow sewn edge',points,.0017,material,resolution=2)
 return obj
