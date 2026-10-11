import bpy,json
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath="U:/unity-game/Assets/_Project/Art/Characters/KatchoGranny/Granny.fbx")
print("KATCHO_PARTS",json.dumps([{"name":o.name,"vertices":len(o.data.vertices),"materials":[m.name for m in o.data.materials],"groups":[g.name for g in o.vertex_groups]} for o in bpy.context.scene.objects if o.type=="MESH"]))
