import bpy,json
from pathlib import Path
from mathutils import Vector
root=Path('U:/ImportInbox/SketchfabSources')
report=[]
for name,source in [('SovietSink',root/'SovietSink/original/source/Sink.fbx'),('WoodenWallCabinet',root/'WoodenWallCabinet/wooden_wall_cabinet_-_pbr__game-ready.glb')]:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 if source.suffix=='.fbx':bpy.ops.import_scene.fbx(filepath=str(source))
 else:bpy.ops.import_scene.gltf(filepath=str(source))
 obs=[o for o in bpy.context.scene.objects if o.type=='MESH']
 points=[o.matrix_world@v.co for o in obs for v in o.data.vertices]
 lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
 mats=[{'name':m.name,'color':list(m.diffuse_color),'images':[n.image.name for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image] if m.use_nodes else []} for m in bpy.data.materials]
 report.append({'id':name,'size':list(hi-lo),'min':list(lo),'vertices':sum(len(o.data.vertices) for o in obs),'uv_layers':{o.name:len(o.data.uv_layers) for o in obs},'materials':mats})
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.device='CPU';scene.render.resolution_x=960;scene.render.resolution_y=720;scene.render.resolution_percentage=100
 scene.world=bpy.data.worlds.new('Preview');scene.world.color=(.25,.25,.25)
 center=(lo+hi)/2;sz=max(hi-lo)
 bpy.ops.object.camera_add(location=center+Vector((1.5,-2.3,1.35))*sz);camera=bpy.context.object;camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=sz*1.7;scene.camera=camera
 for offset,power in [((1,-2,3),1000),((-2,0,2),600)]:
  bpy.ops.object.light_add(type='AREA',location=center+Vector(offset)*sz);light=bpy.context.object;light.data.energy=power*sz*sz;light.data.shape='DISK';light.data.size=sz*2;light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
 scene.render.filepath=str(root/name/'preview.png');bpy.ops.render.render(write_still=True)
 (root/name/'inspection.json').write_text(json.dumps(report[-1],indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
