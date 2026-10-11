import bpy,collections
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='U:/unity-game/Assets/_Project/Art/CC0/old_bed_frame/old_bed_frame.fbx')
z=[]
for o in bpy.context.scene.objects:
 if o.type!='MESH':continue
 for v in o.data.vertices:
  p=o.matrix_world@v.co
  if abs(p.x)<.40 and abs(p.y)<.7:z.append(p.z)
print('BED_CENTRE_HEIGHTS',collections.Counter(round(v,2) for v in z).most_common(20))
