"""Blender: export Poly Haven Television_01 with original UVs and curved screen.
Usage: blender -b --python export-cc0-tv.py -- <Unity project>.
Source is preserved under tools/art/sources/Television_01/source.blend (CC0).
"""
import bpy, pathlib, sys, json
root=pathlib.Path(sys.argv[sys.argv.index('--')+1])
bpy.ops.wm.open_mainfile(filepath=str(root/'tools/art/sources/Television_01/source.blend'),load_ui=False,use_scripts=False)
o=bpy.data.objects['Television_01'];mesh=o.data
# The original glass is a disconnected 10x10 vertex patch. Select by its
# measured bounds, then traverse connectivity, rather than UV or face indices.
seed=min(mesh.vertices,key=lambda v:(v.co.x+.0654)**2+(v.co.y+.19236)**2+(v.co.z-.25919)**2).index
adj={i:set() for i in range(len(mesh.vertices))}
for e in mesh.edges:
 a,b=e.vertices;adj[a].add(b);adj[b].add(a)
glass=set();queue=[seed]
while queue:
 i=queue.pop()
 if i in glass:continue
 glass.add(i);queue.extend(adj[i]-glass)
assert len(glass)==100, f'Unexpected glass topology: {len(glass)}'
mesh.calc_loop_triangles()
records=[]
for screen in (False,True):
 d=dict(name='Living_TVScreen' if screen else 'Living_Television_01',material='TVScreen' if screen else 'Television_01',vertices=[],normals=[],uv=[],triangles=[])
 for tri in mesh.loop_triangles:
  if (tri.vertices[0] in glass)!=screen:continue
  start=len(d['vertices'])//3
  for li in tri.loops:
   loop=mesh.loops[li];p=mesh.vertices[loop.vertex_index].co;n=loop.normal
   d['vertices'].extend([round(6.14+p.y,6),round(.66+p.z+.0000155,6),round(1.92-p.x,6)])
   d['normals'].extend([round(n.y,6),round(n.z,6),round(-n.x,6)])
   if screen:
    # CRT shader mirrors U; compensate here, keeping readable programme text.
    uv=((.126331925-p.x)/.383467137,(p.z-.114147052)/.290085807)
   else:uv=mesh.uv_layers.active.data[li].uv
   d['uv'].extend([round(float(v),6) for v in uv])
  d['triangles'].extend([start+2,start+1,start])
 records.append(d)
out=root/'Assets/_Project/Art/CC0/Television_01/Television.meshdata.json'
out.write_text(json.dumps(dict(meshes=records),separators=(',',':')),encoding='utf8')
print('CC0_TV_EXPORTED',[(d['name'],len(d['triangles'])//3) for d in records])
