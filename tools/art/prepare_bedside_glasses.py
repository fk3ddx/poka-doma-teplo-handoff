"""Export an existing CC0 spectacles mesh; do not remodel or change the character."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
root=Path('U:/unity-game/Assets/_Project/Art/ReadyModels/Glasses')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.obj_import(filepath=str(root/'roundglasses_leopard.obj'),forward_axis='NEGATIVE_Z',up_axis='Y')
objects=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert objects
pts=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
lo=Vector([min(v[i] for v in pts) for i in range(3)]);hi=Vector([max(v[i] for v in pts) for i in range(3)])
scale=.14/(hi.x-lo.x);origin=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
mat=bpy.data.materials.new('Ready_Toigo_RoundGlasses');mat.use_nodes=True
bsdf=mat.node_tree.nodes.get('Principled BSDF');image=mat.node_tree.nodes.new('ShaderNodeTexImage');image.image=bpy.data.images.load(str(root/'Glasses-leopard.png'))
mat.node_tree.links.new(image.outputs['Color'],bsdf.inputs['Base Color']);mat.node_tree.links.new(image.outputs['Alpha'],bsdf.inputs['Alpha'])
for o in objects:
 for v in o.data.vertices:v.co=(o.matrix_world@v.co-origin)*scale
 o.matrix_world.identity();o.data.materials.clear();o.data.materials.append(mat)
 for face in o.data.polygons:face.use_smooth=True
 bpy.context.view_layer.objects.active=o;o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(root/'BedsideGlasses.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
pts=[o.matrix_world@v.co for o in objects for v in o.data.vertices];lo=[min(v[i] for v in pts) for i in range(3)];hi=[max(v[i] for v in pts) for i in range(3)]
result={'scope':'FBX export and dimensions; no Unity import/render acceptance','source_width_units':.14/scale,'uniform_scale':scale,'blender_axes':'X/Y horizontal; Z up','min':lo,'max':hi,'size':[hi[i]-lo[i] for i in range(3)],'triangles':sum(len(p.vertices)-2 for o in objects for p in o.data.polygons)}
(root/'geometry.json').write_bytes(json.dumps(result,indent=2).encode('utf-8'));print(json.dumps(result,indent=2))
