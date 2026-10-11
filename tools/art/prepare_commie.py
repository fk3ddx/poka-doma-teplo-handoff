import bpy,json,re,shutil,math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(Path('U:/ImportInbox/CommieBlock/OldRussianApartmentPropsLite.blend')),load_ui=False)
out=Path('U:/unity-game/Assets/_Project/Art/ReadyModels/CommieBlock');out.mkdir(parents=True,exist_ok=True)
tex=out/'Textures';tex.mkdir(exist_ok=True)
for mat in bpy.data.materials:
 if mat.use_nodes:
  for node in mat.node_tree.nodes:
   if node.type=='TEX_IMAGE' and node.image:
    fn=node.image.filepath.replace(chr(92),'/').split('/')[-1]
    fp=Path('U:/ImportInbox/CommieBlock/textures')/fn
    if fp.is_file(): node.image=bpy.data.images.load(str(fp),check_existing=False)
groups={'SovietSofa':(['Sofa'],.85),'SovietArmchair':(['Chair 3 Seat'],.85),'SovietDiningChair':(['Chair'],.88),'SovietDesk':(['Table','Shelf'],.80),'SovietCoffeeTable':(['Coffee Table Top'],.45),'SovietShelf':(['Shelf.001'],1.9),'SovietTV':(['TV'],.45)}
original=list(bpy.data.objects)
manifest=[]
for label,(roots,h) in groups.items():
 objects=[]
 for name in roots:
  ob=bpy.data.objects[name];objects+=[ob]+list(ob.children_recursive)
 objects=list(set(objects))
 copies=[]
 for ob in objects:
  n=ob.copy();n.data=ob.data.copy();bpy.context.scene.collection.objects.link(n);n.parent=None;n.matrix_world=ob.matrix_world.copy();copies.append(n)
 points=[o.matrix_world@Vector(v) for o in copies for v in o.bound_box]
 lo=Vector(tuple(min(p[i] for p in points) for i in range(3)));hi=Vector(tuple(max(p[i] for p in points) for i in range(3)));center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));scale=h/(hi.z-lo.z)
 materials=[]
 for n in copies:
  for v in n.data.vertices:v.co=(n.matrix_world@v.co-center)*scale
  n.matrix_world.identity()
  for idx,m in enumerate(n.data.materials):
   if not m:continue
   originalMat=m
   # Keep the artist's image texture; standardise only names/paths for URP import.
   p=next((x for x in m.node_tree.nodes if x.type=='BSDF_PRINCIPLED'),None)
   if not p or not p.inputs['Base Color'].links:raise RuntimeError('Missing textured material '+m.name)
   src=p.inputs['Base Color'].links[0].from_node
   if src.type!='TEX_IMAGE':raise RuntimeError('Unsupported texture chain '+m.name)
   srcpath=Path(src.image.filepath); print('COPY',m.name,src.image.name,str(srcpath),flush=True);mid=re.sub('[^a-zA-Z0-9_]+','_',m.name)
   filename=mid+'.png';shutil.copyfile(srcpath,tex/filename)
   nm=m.copy();nm.name='CB_'+mid;n.data.materials[idx]=nm
   materials.append({'name':nm.name,'texture':'Textures/'+filename})
 for o in bpy.context.selected_objects:o.select_set(False)
 for o in copies:o.select_set(True)
 bpy.context.view_layer.objects.active=copies[0]
 bpy.ops.export_scene.fbx(filepath=str(out/(label+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,path_mode='AUTO')
 manifest.append({'id':label,'height':h,'dimensions':list((hi-lo)*scale),'materials':materials,'sourceObjects':[o.name for o in objects]})
 # Neutral asset QA only, not an in-game screenshot.
 for o in original:o.hide_render=True
 for o in copies:o.hide_render=False
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.render.resolution_x=600;scene.render.resolution_y=500;scene.render.resolution_percentage=100
 world=bpy.data.worlds.new('QA neutral');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.25,.25,.25,1);world.node_tree.nodes['Background'].inputs[1].default_value=.7;scene.world=world
 cam=bpy.data.cameras.new('QA camera');cg=bpy.data.objects.new('QA camera',cam);scene.collection.objects.link(cg);scene.camera=cg
 span=max((hi-lo)*scale);cg.location=Vector((span*1.4,-span*1.9,h+span*.7));target=Vector((0,0,h*.5));cg.rotation_euler=(target-cg.location).to_track_quat('-Z','Y').to_euler();cam.type='ORTHO';cam.ortho_scale=span*1.4
 ld=bpy.data.lights.new('QA key','AREA');lg=bpy.data.objects.new('QA key',ld);scene.collection.objects.link(lg);lg.location=(1,-2,3);ld.energy=200;ld.size=3;lg.rotation_euler=(target-lg.location).to_track_quat('-Z','Y').to_euler()
 scene.render.filepath=str(root/(label+'.png'));bpy.ops.render.render(write_still=True)
 for o in copies+[cg,lg]:bpy.data.objects.remove(o,do_unlink=True)
(out/'manifest.json').write_text(json.dumps({'assets':manifest},indent=2))
(out/'SOURCE.md').write_text('# Commie Block Apartment Props\n\nAuthor: GrawlixMcLaren. Source: https://grawlixmclaren.itch.io/low-poly-apartment-props-ps1ps2\nLicense: CC0, explicitly confirmed by the author in https://itch.io/post/15507819 (read in Chrome 2026-10-11: "cc0, go crazy").\nArchive: Soviet Props.rar, downloaded from the official free download. Exported selected existing meshes, retained artist UVs and texture images, normalised metre scale. No replacement meshes generated.\nThese are an art candidate set, not a claim of final acceptance.\n',encoding='utf-8')
print('EXPORTED',len(manifest))
