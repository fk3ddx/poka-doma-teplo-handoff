from pathlib import Path
import bpy,shutil,json
root=Path(__file__).resolve().parents[2]
src=root/'art-sources/Katcho-Granny'
out=root/'unity-game/Assets/_Project/Art/Characters/KatchoGranny';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(src/'Granny.blend'))
# Preserve original topology, UVs, normals, costume and author animation. Only convert the material for FBX.
mat=bpy.data.materials.new('KatchoGranny');mat.use_nodes=True
bsdf=mat.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=1
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(src/'granny.png'));mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
body=bpy.data.objects['Granny'];rig=bpy.data.objects['Armature'];cane=bpy.data.objects['CaneDummy']
rig.animation_data.use_nla=False
rig.animation_data.action=bpy.data.actions['Oidle']
rig.animation_data.action_slot=rig.animation_data.action.slots[0]
bpy.context.scene.frame_set(0)
for ob in [body,cane]:ob.data.materials.clear();ob.data.materials.append(mat)
# The source contains a second, unparented reference cane. Export only the cane bound to the hand.
bpy.ops.object.select_all(action='DESELECT')
for ob in [body,rig,cane]:ob.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(out/'Granny.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_force_startend_keying=True,bake_anim_simplify_factor=0)
shutil.copy2(src/'granny.png',out/'granny.png')
report={'source':'https://katcho.itch.io/free-characters','license':'CC0','bodyTriangles':sum(len(p.vertices)-2 for p in body.data.polygons),'texture':[256,256],'clips':{a.name:list(a.frame_range) for a in bpy.data.actions},'changes':['FBX conversion and standard material only','Excluded duplicate unparented reference cane'],'appearance':'User accepted author preview 2026-10-11; appearance preserved; separate Unity preview, gameplay integration pending'}
(out/'source.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('KATCHO_EXPORT_OK',report)
