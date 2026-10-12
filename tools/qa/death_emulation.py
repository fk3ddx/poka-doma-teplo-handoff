# Blender emulation of FaceTearDeath framing (same maths: claw orientation from the Claw material, clawRoll, clawLength,
# head-space keys, FOV 55, guest head 0.62 m ahead). blender -b --python tools/qa/death_emulation.py -- <out prefix>
# Keys/params in tools/qa/death_keys.json (paths below expect the repo at /home/claude/pc — adjust root).
import bpy,sys,math,json
from mathutils import Vector,Matrix
out=sys.argv[sys.argv.index('--')+1]
K=json.load(open(__import__('os').path.join(__import__('os').path.dirname(__file__),'death_keys.json')))
root='/home/claude/pc/unity-game/Assets/_Project/Art/ReadyModels/Sketchfab/'
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=root+'GuestClaw/GuestClaw.fbx')
src=next(o for o in bpy.context.scene.objects if o.type=='MESH')
U=lambda b:Vector((-b.x,b.z,-b.y))          # Blender -> Unity (FBX default axes)
B=lambda h:Vector((h.x,h.z,h.y))            # Unity head space -> Blender render space (camera looks +Y)
verts=[U(src.matrix_world@v.co) for v in src.data.vertices]
claw_mat={i for i,s in enumerate(src.material_slots) if s.material and s.material.name.endswith('_Material_1')}
clawv={v for f in src.data.polygons if f.material_index in claw_mat for v in f.vertices}
allc=sum(verts,Vector())/len(verts);clawc=sum((verts[i] for i in clawv),Vector())/len(clawv)
f=(clawc-allc).normalized();tipi=max(clawv,key=lambda i:(verts[i]-allc).dot(f));off=verts[tipi]-allc;palm=(off-f*off.dot(f)).normalized()
def look(fw,up):
  fw=fw.normalized();r=up.cross(fw).normalized();u=fw.cross(r);return Matrix((r,u,fw)).transposed()   # columns r,u,f
R0=Matrix.Rotation(math.radians(K.get('roll',0)),3,'Z')@look(f,-palm).inverted()
w=[R0@v for v in verts];lo=Vector([min(p[k] for p in w) for k in range(3)]);hi=Vector([max(p[k] for p in w) for k in range(3)]);s=K.get('len',.46)/max(hi-lo)
w=[(p*s) for p in w];lo=lo*s;hi=hi*s;shift=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));w=[p-shift for p in w]
print('ORIENT fingers',tuple(round(x,2) for x in f),'palm',tuple(round(x,2) for x in palm))
def pose(keys,t):
  i=0
  while i<len(keys)-2 and t>keys[i+1][0]:i+=1
  a,b=keys[i],keys[i+1];u=max(0,min(1,(t-a[0])/max(1e-4,b[0]-a[0])));u=u*u*(3-2*u)
  O=Vector(a[1]).lerp(Vector(b[1]),u);F=Vector(a[2]).normalized().slerp(Vector(b[2]).normalized(),u);P=Vector(a[3]).normalized().slerp(Vector(b[3]).normalized(),u)
  up=-(P-F*P.dot(F));up=up if up.length>1e-3 else Vector((0,1,0));return O,look(F,up)
def place(name,keys,t,mirror):
  O,R=pose(keys,t);me=src.data.copy();me.name=name
  for v,p in zip(me.vertices,w):
    q=Vector((-p.x if mirror else p.x,p.y,p.z));v.co=B(R@q+O)
  if mirror:me.flip_normals()
  ob=bpy.data.objects.new(name,me);bpy.context.scene.collection.objects.link(ob);return ob
src.hide_render=True
# body: idle pose, head at K['body'] (head space), facing the camera
bpy.ops.import_scene.fbx(filepath=root+'GuestBody/GuestBody.fbx')
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');body=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o.name!=src.name)
act=next(a for a in bpy.data.actions if 'Idle' in a.name);arm.animation_data.action=act;bpy.context.scene.frame_set(20)
holder=bpy.data.objects.new('holder',None);bpy.context.scene.collection.objects.link(holder);arm.parent=holder
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=24;sc.render.resolution_x=640;sc.render.resolution_y=360
alb=bpy.data.images.load(root+'GuestBody/Textures/albedo.png')
def skin(ob):
  for sl in ob.material_slots:
    m=sl.material
    if not m:continue
    nt=m.node_tree;p=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED')
    if m.name.endswith('_Material_1'):p.inputs['Base Color'].default_value=(.012,.011,.01,1);p.inputs['Roughness'].default_value=.25
    else:t=nt.nodes.new('ShaderNodeTexImage');t.image=alb;nt.links.new(t.outputs[0],p.inputs['Base Color'])
skin(body);skin(src)
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam'));cam.data.clip_start=.02;cam.data.sensor_fit='VERTICAL';cam.data.angle=math.radians(55);sc.collection.objects.link(cam);sc.camera=cam
cam.location=(0,0,0);cam.rotation_euler=(math.radians(90),0,0)
lt=bpy.data.objects.new('fill',bpy.data.lights.new('fill','POINT'));lt.data.energy=K.get('fillW',6);lt.data.color=(.75,.8,.92);lt.data.shadow_soft_size=.2;lt.location=B(Vector(K.get('fill',[0,.3,.05])));sc.collection.objects.link(lt)
sc.world=bpy.data.worlds.new('w');sc.world.color=tuple([K.get('world',.004)]*3)
bpy.context.view_layer.update()
mw=body.matrix_world;pts=[mw@Vector(c) for c in body.bound_box];top=max(p.z for p in pts);cx=sum(p.x for p in pts)/8;cy=sum(p.y for p in pts)/8
for t in K['times']:
  for o in [o for o in bpy.context.scene.objects if o.name.startswith('hand')]:bpy.data.objects.remove(o,do_unlink=True)
  vis=K['hands_from']<=t<K['hands_to']
  if vis:place('handR',K['right'],t,False)
  if vis and t<K['left_to']:place('handL',K['left'],t,True)
  bd=Vector(K['body']);lift=K.get('lift',0) if K['lift_from']<=t<K['lift_to'] else 0
  target=B(Vector((bd.x,bd.y+lift,bd.z)));holder.location=Vector((0,0,0));bpy.context.view_layer.update()
  head=Vector((cx,cy,top-.12));d=target-head;holder.location=d;body.hide_render=not(t<K['body_to'])
  pitch=K['pitch'] if K['lift_from']<=t<K['lift_to'] else 0;cam.rotation_euler=(math.radians(90+pitch),0,0)
  print('VIS',body.hide_render,body.hide_get(),body.visible_get(),len(body.data.polygons),[s.material.name if s.material else None for s in body.material_slots],[(m.type,m.show_render) for m in body.modifiers]);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();ev=body.evaluated_get(dg);vs=[ev.matrix_world@v.co for v in ev.data.vertices];print('BODYBOX',body.name,body.parent.name if body.parent else None,[round(min(v[k] for v in vs),2) for k in range(3)],[round(max(v[k] for v in vs),2) for k in range(3)],'mw',arm.matrix_world.to_scale());sc.render.filepath=out+'-%04.2f.png'%t;bpy.ops.render.render(write_still=True)
print('DONE')
