import bpy,json
from pathlib import Path
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='U:/unity-game/Assets/_Project/Art/ScenarioProxies/PolyHaven/signal_flashlight/signal_flashlight.fbx')
for o in bpy.context.scene.objects:
 if o.type!='MESH':continue
 print('TORCH_OBJECT',o.name,[(i,m.name) for i,m in enumerate(o.data.materials)])
 for i,m in enumerate(o.data.materials):
  verts=[o.matrix_world@o.data.vertices[v].co for f in o.data.polygons if f.material_index==i for v in f.vertices]
  if verts:print('SUBMESH',i,m.name,'min',[min(v[j] for v in verts) for j in range(3)],'max',[max(v[j] for v in verts) for j in range(3)],'faces',sum(f.material_index==i for f in o.data.polygons))
