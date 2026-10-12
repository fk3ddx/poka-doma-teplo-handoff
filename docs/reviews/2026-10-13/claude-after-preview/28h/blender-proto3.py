import bpy,sys,math,time
from mathutils import Vector,Matrix,Quaternion
bpy.ops.wm.read_factory_settings(use_empty=True)
A="/home/claude/ap/unity-game/Assets/_Project/Art/"
args=sys.argv[sys.argv.index("--")+1:];t=float(args[0]);cam_kind=args[1];reveal=float(args[2]) if len(args)>2 else 1.0
def load(path):
    before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=path);new=[o for o in bpy.data.objects if o not in before]
    arm=[o for o in new if o.type=='ARMATURE'][0];body=max([o for o in new if o.type=='MESH' and len(o.vertex_groups)>0],key=lambda o:len(o.data.vertices))
    for o in new:
        if o.type=='MESH' and o!=body:o.hide_render=True
    return arm,body
ka,kb=load(A+"Characters/KatchoGranny/Granny.fbx");ga,gb=load(A+"ReadyModels/Sketchfab/GuestBody/GuestBody.fbx");gb.hide_render=True
def mat_tex(name,img):
    m=bpy.data.materials.new(name);m.use_nodes=True;nt=m.node_tree;bs=nt.nodes["Principled BSDF"];tx=nt.nodes.new("ShaderNodeTexImage");tx.image=bpy.data.images.load(img);tx.interpolation='Closest';nt.links.new(tx.outputs[0],bs.inputs[0])
    # Unity culls back faces: emulate with Backfacing -> transparent
    geo=nt.nodes.new("ShaderNodeNewGeometry");tr=nt.nodes.new("ShaderNodeBsdfTransparent");mix=nt.nodes.new("ShaderNodeMixShader");out=nt.nodes["Material Output"]
    nt.links.new(geo.outputs["Backfacing"],mix.inputs[0]);nt.links.new(bs.outputs[0],mix.inputs[1]);nt.links.new(tr.outputs[0],mix.inputs[2]);nt.links.new(mix.outputs[0],out.inputs[0]);return m
KM=mat_tex("granny",A+"Characters/KatchoGranny/granny.png");GM=mat_tex("guest",A+"ReadyModels/Sketchfab/GuestBody/Textures/albedo.png")
UNLIT=len(args)>6 and args[6]=='unlit'
if UNLIT:
    for m in (KM,GM):
        nt=m.node_tree;bs=nt.nodes["Principled BSDF"];tx=[n for n in nt.nodes if n.type=='TEX_IMAGE'][0];em=nt.nodes.new("ShaderNodeEmission");nt.links.new(tx.outputs[0],em.inputs[0]);mix=[n for n in nt.nodes if n.type=='MIX_SHADER'][0];nt.links.new(em.outputs[0],mix.inputs[1])
for i in range(len(kb.data.materials)):kb.data.materials[i]=KM
def dom(o,v):
    best=None;m=-1
    for g in v.groups:
        if g.weight>m:m=g.weight;best=o.vertex_groups[g.group].name
    return best
# ---------- her head in the Head bone's BIND frame
hbone=ka.data.bones['Head'];B=hbone.matrix_local;Bi=B.inverted()   # armature space rest
kd=[dom(kb,v) for v in kb.data.vertices]
class _U:
    def __init__(s,uv):s.uv=uv
uvl=[_U(d.uv.copy()) for d in kb.data.uv_layers.active.data]
tris=[]
for p in kb.data.polygons:
    vs=list(p.vertices);ls=list(p.loop_indices)
    for k in range(1,len(vs)-1):tris.append(((vs[0],vs[k],vs[k+1]),(ls[0],ls[k],ls[k+1]),0))
head=[x for x in tris if all(kd[i]=='Head' for i in x[0])]
pb={i:Bi@kb.data.vertices[i].co for x in head for i in x[0]}       # bone space (rest)
fwdB=(Bi.to_3x3()@Vector((0,-1,0))).normalized();upB=(Bi.to_3x3()@Vector((0,0,1)));upB=(upB-fwdB*upB.dot(fwdB)).normalized();rightB=upB.cross(fwdB)
def F(p):return Vector((p.dot(rightB),p.dot(upB),p.dot(fwdB)))
def Finv(q):return rightB*q.x+upB*q.y+fwdB*q.z
loc={i:F(pb[i]) for i in pb}
zs=[loc[i].z for x in head for i in x[0]];zf=min(zs)+(max(zs)-min(zs))*.55
uvraw=kb.data.uv_layers.active.data
def inface(l):u=uvraw[l].uv;return u.x<=76/256 and u.y>=1-66/256
skin=[x for x in tris if all(inface(l) for l in x[1])];scalp=[]
NECKLINE=len(args)>5 and args[5]=='fix'
pool=[x for x in tris if all(kd[i] in (('Head','Neck') if NECKLINE else ('Head',)) for i in x[0])]
shell=[((x[0][0],x[0][2],x[0][1]),(x[1][0],x[1][2],x[1][1]),0) for x in pool if not all(inface(l) for l in x[1])]
for x in pool:
    for i in x[0]:
        if i not in loc:loc[i]=F(Bi@kb.data.vertices[i].co)
for x in skin:
    for i in x[0]:
        if i not in loc:loc[i]=F(Bi@kb.data.vertices[i].co)
allv=list(loc.values());hmin=Vector([min(v[a] for v in allv) for a in range(3)]);hmax=Vector([max(v[a] for v in allv) for a in range(3)])
# ---------- guest head/jaw (bind, mesh space), face = model forward (-Y), up Z
gd=[dom(gb,v) for v in gb.data.vertices];jawset={b.name for b in ga.data.bones['Jaw'].children_recursive}|{'Jaw'}
under=({b.name for b in ga.data.bones['joint4'].children_recursive}|{'joint4'})-jawset
gw=[gb.matrix_world@v.co for v in gb.data.vertices];guvl=gb.data.uv_layers.active.data
gtris=[]
for p in gb.data.polygons:
    vs=list(p.vertices);ls=list(p.loop_indices)
    for k in range(1,len(vs)-1):gtris.append(((vs[0],vs[k],vs[k+1]),(ls[0],ls[k],ls[k+1]),0))
ghead=[];gjaw=[]
for x in gtris:
    d=[gd[i] for i in x[0]];inj=sum(a in jawset for a in d)
    if not all((a in jawset or a in under) for a in d):continue
    (gjaw if inj>=2 else ghead).append(x)
cen=Vector();n=0
for i,v in enumerate(gw):
    if gd[i] in under or gd[i] in jawset:cen+=v;n+=1
cen/=n
GF=lambda p:Vector((p.x,p.z,-p.y))
gp={i:GF(gw[i]-cen) for x in ghead+gjaw for i in x[0]}
gmin=Vector([min(v[a] for v in gp.values()) for a in range(3)]);gmax=Vector([max(v[a] for v in gp.values()) for a in range(3)])
hs=hmax-hmin;gs=gmax-gmin;k2=.92*min(hs.x/gs.x,hs.y/gs.y,hs.z/gs.z)
shift=Vector(((hmin.x+hmax.x)/2-(gmin.x+gmax.x)/2*k2,hmin.y+.01-gmin.y*k2,(hmin.z+hmax.z)/2-(gmin.z+gmax.z)/2*k2-.005))
if len(args)>5 and args[5]=='fix':
    sk0=[F(Bi@kb.data.vertices[i].co) for x in tris if all(inface(l) for l in x[1]) for i in x[0]]
    fb=min(v.y for v in sk0);ft=max(v.y for v in sk0);fz=max(v.z for v in sk0)
    k2=min(.92*hs.x/gs.x,.95*hs.z/gs.z,1.02*(ft-fb)/gs.y)
    shift=Vector(((hmin.x+hmax.x)/2-(gmin.x+gmax.x)/2*k2,fb-gmin.y*k2,fz-.02-gmax.z*k2))
    print('FIT face y',round(fb,3),round(ft,3),'k2',round(k2,3))
gq={i:shift+gp[i]*k2 for i in gp}
jb=ga.data.bones['Jaw'];jawpivot=shift+GF(ga.matrix_world@jb.head_local-cen)*k2
print("her head in bind frame",tuple(round(a,3) for a in hmin),tuple(round(a,3) for a in hmax),"k2",round(k2,3),"jawpivot",tuple(round(a,3) for a in jawpivot))
# ---------- animation of 11.9 at time t
sm=lambda a,b,x:0 if x<=a else 1 if x>=b else (lambda u:u*u*(3-2*u))((x-a)/(b-a))
slide=sm(1.5,2.2,t);jaw=38*sm(2.0,2.25,t);u=sm(1.6,3.5,t);grow=1+(2.4/1.55-1)*u;bow=12*sm(1.5,2.1,t)+35*min(1,max(0,u*1.0))
show_guest=t>=1.3;rev=1+(reveal-1)*slide
# reveal: guest grows about its TOP centre (top unchanged), covering the neck
topc=Vector(((hmin.x+hmax.x)/2,max(v.y for v in gq.values()),(hmin.z+hmax.z)/2))
def GR(q):return topc+(q-topc)*rev
# pose: root growth, neck bow (about armature X), head collapsed
ka.scale=(grow,grow,grow)
bpy.context.scene.frame_set(1)
nb=ka.pose.bones['Neck'];nb.rotation_mode='QUATERNION';nb.rotation_quaternion=Quaternion((1,0,0),math.radians(-bow))@nb.rotation_quaternion.copy()
ka.pose.bones['Neck'].keyframe_insert('rotation_quaternion',frame=1)
ORIG=len(args)>3 and args[3]=='orig'
import bmesh
if ORIG:hp_skip=True
else:hp_skip=False
bm=bmesh.new();bm.from_mesh(kb.data);bm.faces.ensure_lookup_table();hp=set(p.index for p in kb.data.polygons if all(inface(l) for l in p.loop_indices));bmesh.ops.delete(bm,geom=[] if ORIG else [f for f in bm.faces if f.index in hp],context='FACES_ONLY');bm.to_mesh(kb.data);bm.free()
bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
dgx=bpy.context.evaluated_depsgraph_get();ex=kb.evaluated_get(dgx);mx=ex.to_mesh();print('BODY TOP',round(max(v.co.z for v in mx.vertices),3));ex.to_mesh_clear()
# head world matrix WITHOUT the collapse: posed matrix with scale removed, times armature scale
W=ka.matrix_world@ka.pose.bones['Head'].matrix
def world_from_face(q):return W@Finv(q)
def mk(name,trs,posf,uvsrc,mat):
    me=bpy.data.meshes.new(name);vs=[];fs=[];uvs=[]
    for x in trs:
        b=len(vs);vs+=[posf(i) for i in x[0]];fs.append((b,b+1,b+2));uvs+=[uvsrc[l].uv.copy() for l in x[1]]
    me.from_pydata(vs,[],fs);me.update();ul=me.uv_layers.new()
    for k,l in enumerate(me.loops):ul.data[k].uv=uvs[k]
    me.materials.append(mat);o=bpy.data.objects.new(name,me);bpy.context.scene.collection.objects.link(o);return o
sk=[loc[i] for x in skin for i in x[0]];sbot=min(v.y for v in sk);stop=max(v.y for v in sk);hgt=stop-sbot
def skinpos(i):
    q=loc[i].copy();yn=(q.y-sbot)/hgt
    q+=Vector((0,-1,0))*slide*hgt*(.55+.4*yn)+Vector((0,0,1))*slide*(.03+.03*(1-yn))+Vector((q.x*.25*slide*yn,0,0))
    return world_from_face(q)
if not ORIG:
    mk("inner lining of her head",shell,lambda i:world_from_face(loc[i]),uvl,KM)
if ORIG:pass
elif t<2.6:mk("face skin",skin,skinpos,uvl,KM)
if False:mk("scalp",scalp,lambda i:world_from_face(loc[i]),uvl,KM)
if show_guest:
    mk("guest head",ghead,lambda i:world_from_face(GR(gq[i])),guvl,GM)
    R=Matrix.Rotation(math.radians(jaw),3,Vector((1,0,0)))
    mk("guest jaw",gjaw,lambda i:world_from_face(GR(jawpivot+R@(gq[i]-jawpivot))),guvl,GM)
headw=W@Vector((0,0,0));print('HEAD PIVOT',tuple(round(a,3) for a in headw))
cam=bpy.data.objects.new("cam",bpy.data.cameras.new("cam"));bpy.context.scene.collection.objects.link(cam)
if cam_kind=="near":cam.location=headw+Vector((.3,-1.15,.1))
elif cam_kind=="below":cam.location=Vector((headw.x+.25,headw.y-.95,1.5))
else:cam.location=headw+Vector((-1.0,-.2,0))
tg=headw+Vector((0,0,.12*grow));cam.rotation_euler=(tg-cam.location).to_track_quat('-Z','Z').to_euler();cam.data.lens=28 if cam_kind=="below" else 40;bpy.context.scene.camera=cam
KEY=len(args)>4 and args[4]=='key'
l=bpy.data.objects.new("l",bpy.data.lights.new("l","POINT"));bpy.context.scene.collection.objects.link(l);l.location=headw+Vector((0,.35,1.0));l.data.energy=120
if KEY:
    k=bpy.data.objects.new("k",bpy.data.lights.new("k","SPOT"));bpy.context.scene.collection.objects.link(k);k.location=headw+Vector((.2,-1.1,.45));k.rotation_euler=(headw+Vector((0,0,.12))-k.location).to_track_quat('-Z','Y').to_euler();k.data.energy=60;k.data.spot_size=math.radians(34);k.data.color=(1,.85,.7)
w=bpy.data.worlds.new("w");bpy.context.scene.world=w;w.color=(.03,.03,.035)
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.device='CPU';s.cycles.samples=10;s.render.resolution_x=360;s.render.resolution_y=360;s.render.film_transparent=False
s.render.filepath="/tmp/rp/p3_%s_%s_%s_%d.png"%(args[0],cam_kind,"_".join(args[3:]),int(time.time()*1000)%100000);bpy.ops.render.render(write_still=True)
