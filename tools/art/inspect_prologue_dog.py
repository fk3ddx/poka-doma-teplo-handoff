import bpy,json
from pathlib import Path
r=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(r/'art-source/prologue/QuaterniusDog/Dog.blend'))
report={'objects':[], 'images':[], 'actions':[]}
for o in bpy.context.scene.objects:
 if o.type in ('MESH','ARMATURE'):
  d={'name':o.name,'type':o.type,'size':list(o.dimensions),'scale':list(o.scale)}
  if o.type=='MESH':d.update(vertices=len(o.data.vertices),triangles=sum(len(p.vertices)-2 for p in o.data.polygons),uv=[u.name for u in o.data.uv_layers],colors=[c.name for c in o.data.color_attributes],materials=[{'name':m.name,'diffuse':list(m.diffuse_color),'nodes':[(n.type,n.image.name if n.type=='TEX_IMAGE' and n.image else '') for n in m.node_tree.nodes] if m.use_nodes else []} for m in o.data.materials])
  report['objects'].append(d)
for i in bpy.data.images:report['images'].append({'name':i.name,'path':i.filepath,'size':list(i.size),'packed':bool(i.packed_file)})
for a in bpy.data.actions:report['actions'].append({'name':a.name,'range':list(a.frame_range)})
(r/'art-source/prologue/QuaterniusDog/inspection.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
