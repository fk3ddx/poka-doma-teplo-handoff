# Blender: blender -b --python tools/art/merge_oga_monster.py -- <Poses dir> <out.glb> <dir with albedo.png, normal.png>
# Merge OGA "3D Horror Game Monster" (CC0): Creature1 + 70-bone rig from Idle.fbx, actions from Idle/Walk/Run/Jump.fbx.
import bpy,sys,os
src,out,tex=sys.argv[sys.argv.index('--')+1:][:3]
def rig_of(objs):return max((o for o in objs if o.type=='ARMATURE'),key=lambda o:len(o.data.bones))
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(src,'Idle.fbx'))
arm=rig_of(bpy.context.scene.objects);mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
keep={arm,mesh}
for o in list(bpy.context.scene.objects):
  if o not in keep:bpy.data.objects.remove(o,do_unlink=True)
idle=arm.animation_data.action;idle.name='Idle';idle.use_fake_user=True
for a in list(bpy.data.actions):
  if a!=idle:bpy.data.actions.remove(a)
for name in ['Walk','Run','Jump']:
  before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=os.path.join(src,name+'.fbx'))
  new=[o for o in bpy.data.objects if o not in before];r=rig_of(new);act=r.animation_data.action;act.name=name;act.use_fake_user=True
  for o in new:bpy.data.objects.remove(o,do_unlink=True)
  for a in list(bpy.data.actions):
    if a.users==0 and not a.use_fake_user:bpy.data.actions.remove(a)
arm.animation_data.action=idle
for a in list(bpy.data.actions):
  if a.name not in('Idle','Walk','Run','Jump'):bpy.data.actions.remove(a)
# Material: Blue skin variant, downscaled to 1024 for the game (PSX-like distance)
m=mesh.data.materials[0];m.use_nodes=True;nt=m.node_tree
for n in list(nt.nodes):nt.nodes.remove(n)
p=nt.nodes.new('ShaderNodeBsdfPrincipled');o=nt.nodes.new('ShaderNodeOutputMaterial');nt.links.new(p.outputs[0],o.inputs[0])
al=bpy.data.images.load(os.path.join(tex,'albedo.png'));al.name='Guest_albedo';al.scale(1024,1024)
nm=bpy.data.images.load(os.path.join(tex,'normal.png'));nm.name='Guest_normal';nm.scale(1024,1024);nm.colorspace_settings.name='Non-Color'
ta=nt.nodes.new('ShaderNodeTexImage');ta.image=al;nt.links.new(ta.outputs['Color'],p.inputs['Base Color'])
tn=nt.nodes.new('ShaderNodeTexImage');tn.image=nm;nmap=nt.nodes.new('ShaderNodeNormalMap');nt.links.new(tn.outputs['Color'],nmap.inputs['Color']);nt.links.new(nmap.outputs['Normal'],p.inputs['Normal'])
p.inputs['Roughness'].default_value=.62;m.name='Guest_Skin'
print('ACTIONS',[(a.name,tuple(a.frame_range)) for a in bpy.data.actions])
bpy.ops.export_scene.gltf(filepath=out,export_format='GLB',export_animations=True,export_animation_mode='ACTIONS',export_image_format='AUTO')
print('WROTE',out,os.path.getsize(out))
