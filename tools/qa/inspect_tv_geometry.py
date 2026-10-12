import bpy,json
from pathlib import Path
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='U:/unity-game/Assets/_Project/Art/ReadyModels/CommieBlock/SovietTV.fbx')
result=[]
for obj in bpy.data.objects:
 if obj.type!='MESH':continue
 mesh=obj.data; uv=mesh.uv_layers.active
 faces=[]
 for p in mesh.polygons:
  faces.append({'index':p.index,'normal':[round(x,4) for x in p.normal],'vertices':[[round(x,5) for x in obj.matrix_world@mesh.vertices[mesh.loops[k].vertex_index].co] for k in p.loop_indices],'uv':[[round(x,5) for x in uv.data[k].uv] for k in p.loop_indices]})
 result.append({'name':obj.name,'faces':faces})
Path('U:/diagnostics-tv-geometry.json').write_text(json.dumps(result),encoding='utf-8')
print('TV_GEOMETRY',len(result),sum(len(x['faces']) for x in result))
