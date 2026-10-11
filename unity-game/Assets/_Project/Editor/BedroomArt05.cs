using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Ataka.Scenario;
namespace Ataka.Editor {
 public static class BedroomArt05 {
  const string Art="Assets/_Project/Art",Folder="Assets/_Project/Scenario/Bedroom05";
  static Material Mat(string name,string texture,Color tint,float smooth=.3f,string normal=null){
   var path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
   if(texture!=null){var ti=AssetImporter.GetAtPath(texture) as TextureImporter;if(ti){ti.maxTextureSize=512;ti.filterMode=FilterMode.Bilinear;ti.SaveAndReimport();}m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));}
   if(normal!=null){var ti=(TextureImporter)AssetImporter.GetAtPath(normal);ti.textureType=TextureImporterType.NormalMap;ti.maxTextureSize=512;ti.SaveAndReimport();m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));m.SetFloat("_BumpScale",.3f);m.EnableKeyword("_NORMALMAP");}
   m.SetColor("_BaseColor",tint);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
  }
  static GameObject Box(string name,Transform parent,Vector3 p,Vector3 size,Material mat,bool collision=false){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=mat;if(!collision)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o;}
  static Transform Group(string name,Transform parent){var old=parent.Find(name);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
  static GameObject Ready(string file,Transform parent){var o=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(file));o.transform.SetParent(parent,false);return o;}
  public static void ConfigureIcon(FlatBindings flat){
   var l=flat.look.icon;l.type=LightType.Spot;l.transform.position=new Vector3(.24f,1.62f,.24f);l.transform.rotation=Quaternion.LookRotation(new Vector3(1.4f,1,2.2f)-l.transform.position);l.spotAngle=150;l.innerSpotAngle=90;l.range=3.5f;l.shadows=LightShadows.Soft;
   EditorUtility.SetDirty(l);
  }
  static void Icons(FlatBindings flat){
   ConfigureIcon(flat);var root=Group("Bedroom05 icon corner",flat.apartment);
   var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/LightLab/Wood.mat");
   Box("Corner icon shelf",root,new Vector3(.27f,1.48f,.27f),new Vector3(.48f,.035f,.48f),wood);
   string frame=Art+"/CC0/fancy_picture_frame_01/";
   var fm=Mat("IconFrame",frame+"diffuse.jpg",Color.white,.3f,frame+"normal.png");
   int[] ids={205079,831188};
   for(int i=0;i<2;i++){
    var g=Ready(frame+"fancy_picture_frame_01.fbx",root);g.name="Ready framed Met icon "+ids[i];g.transform.localScale=Vector3.one*.3f;
    g.transform.localPosition=i==0?new Vector3(.24f,1.88f,.04f):new Vector3(.04f,1.78f,.24f);g.transform.localRotation=Quaternion.Euler(0,i==0?0:90,90);
    var icon=Mat("MetIcon"+ids[i],Art+"/OpenAccessMet/Icon"+ids[i]+".jpg",Color.white,.1f);
    foreach(var r in g.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>r.name.Contains("canvas")?icon:fm).ToArray();
   }
   // Reuse authored CC0 vase silhouette for the small glass vessel; no invented prop mesh.
   var vessel=Ready(Art+"/CC0/ceramic_vase_01/fbx.fbx",root);vessel.name="CC0 vase reused as lampada vessel";
   var rs=vessel.GetComponentsInChildren<Renderer>();Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);vessel.transform.localScale*=.13f/b.size.y;
   b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);vessel.transform.position+=new Vector3(.24f-b.center.x,1.499f-b.min.y,.24f-b.center.z);
   var glass=Mat("LampadaRedGlass",Art+"/BedroomDecor/VesselDiffuse.jpg",new Color(.17f,.012f,.004f),.9f,Art+"/BedroomDecor/VesselNormal.jpg");glass.EnableKeyword("_EMISSION");glass.SetColor("_EmissionColor",new Color(.10f,.004f,.001f));foreach(var r in rs){r.sharedMaterials=r.sharedMaterials.Select(_=>glass).ToArray();r.shadowCastingMode=ShadowCastingMode.Off;}
   var halo=new GameObject("Icon glass halo").AddComponent<Light>();halo.transform.SetParent(root,false);halo.transform.position=new Vector3(.24f,1.59f,.24f);halo.type=LightType.Point;halo.color=flat.look.icon.color;halo.intensity=.012f;halo.range=.35f;halo.shadows=LightShadows.None;
  }
  static void Door(ScenarioDoor d,float w,Material enamel,Material brass){
   var leaf=d.leaf;var old=leaf.Find("Architectural door leaf");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
   var root=Group("White panelled architectural leaf",leaf);
   // One unchanged-size collider and relieved enamel panels. Original hinge and interaction remain.
   Box("Door structure",root,new Vector3(w/2,1,0),new Vector3(w,2,.039f),enamel,true);
   for(int side=-1;side<=1;side+=2){float z=side*.025f;
    foreach(float cy in new[]{.47f,1.44f}){
     float h=cy<1?.68f:.94f,pw=w-.23f;
     Box("Recessed enamel panel",root,new Vector3(w/2,cy,z),new Vector3(pw,h,.01f),enamel);
     foreach(float x in new[]{(w-pw)/2-.012f,(w+pw)/2+.012f})Box("Panel stile moulding",root,new Vector3(x,cy,z+side*.009f),new Vector3(.022f,h+.04f,.012f),enamel);
     foreach(float y in new[]{cy-h/2-.01f,cy+h/2+.01f})Box("Panel rail moulding",root,new Vector3(w/2,y,z+side*.009f),new Vector3(pw+.044f,.022f,.012f),enamel);
    }
    Box("Brass handle backplate",root,new Vector3(w-.065f,.95f,z),new Vector3(.027f,.15f,.017f),brass);
    Box("Brass handle spindle",root,new Vector3(w-.065f,.99f,z+side*.027f),new Vector3(.014f,.014f,.045f),brass);
    Box("Brass lever",root,new Vector3(w-.1f,.99f,z+side*.05f),new Vector3(.085f,.018f,.018f),brass);
   }
   var trim=Group("White architectural casing",d.transform);
   foreach(float side in new[]{-1f,1f}){
    foreach(float x in new[]{-w/2-.035f,w/2+.035f})Box("White door casing",trim,new Vector3(x,0,side*.069f),new Vector3(.065f,2.07f,.023f),enamel);
    Box("White door casing lintel",trim,new Vector3(0,1.038f,side*.069f),new Vector3(w+.13f,.065f,.023f),enamel);
   }
  }
  static void Wardrobe(FlatBindings flat){
   var old=flat.apartment.Find("Wardrobe side-wall placement");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
   var root=Group("READY authored walnut wardrobe",flat.apartment);root.position=ScenarioLayout.Wardrobe;root.rotation=Quaternion.Euler(0,180,0);root.localScale=new Vector3(.55f/.95246f,1,1/1.71380f);
   const string folder=Art+"/ReadyModels/Sketchfab/Wardrobe/";
   var model=Ready(folder+"WardrobeSeparated.fbx",root);
   PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   var mat=Mat("WalnutWardrobe",folder+"Textures/Image_0.png",Color.white,.3f,folder+"Textures/Image_3.png");
   mat.SetFloat("_Cull",0);
   foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>mat).ToArray();
   var parts=model.GetComponentsInChildren<Transform>();var leaves=new Transform[2];
   Func<Transform,Bounds> localBounds=t=>{var filter=t.GetComponent<MeshFilter>();var vertices=filter.sharedMesh.vertices.Select(v=>root.InverseTransformPoint(t.TransformPoint(v))).ToArray();var b=new Bounds(vertices[0],Vector3.zero);foreach(var v in vertices)b.Encapsulate(v);return b;};
   float frontSign=Mathf.Sign(localBounds(parts.First(t=>t.name=="LeafLeft")).center.x);root.rotation=Quaternion.Euler(0,frontSign>0?180:0,0);
   for(int i=0;i<2;i++){
    var part=parts.First(t=>t.name==(i==0?"LeafLeft":"LeafRight"));
    var hingeParent=new GameObject("Wardrobe authored hinge basis "+i).transform;hingeParent.SetParent(root,false);var bounds=localBounds(part);float hingeZ=bounds.center.z<0?bounds.min.z:bounds.max.z;hingeParent.localPosition=new Vector3(frontSign>0?bounds.min.x:bounds.max.x,0,hingeZ);
    Debug.Log("WARDROBE_LEAF "+i+" local bounds "+bounds+" pivot "+hingeParent.localPosition+" front "+frontSign);
    // Right hinge reverses its up axis so the existing +95-degree event opens both leaves outward.
    if(frontSign*Mathf.Sign(bounds.center.z-hingeZ)<0)hingeParent.localRotation=Quaternion.Euler(180,0,0);
    var hinge=new GameObject("Wardrobe animated leaf "+i).transform;hinge.SetParent(hingeParent,false);part.SetParent(hinge,true);leaves[i]=hinge;
    var col=part.gameObject.AddComponent<BoxCollider>();var mf=part.GetComponent<MeshFilter>();col.center=mf.sharedMesh.bounds.center;col.size=mf.sharedMesh.bounds.size;
   }
   // Thin author-shaped shell collision, leaving an actual interior instead of a solid door blocker.
   var invisible=new GameObject("Wardrobe shell collision").transform;invisible.SetParent(root,false);
   foreach(var sizeAndPos in new[]{new[]{new Vector3(-.44f,1,0),new Vector3(.065f,1.70f,1.70f)},new[]{new Vector3(-.02f,1,-.80f),new Vector3(.85f,1.70f,.07f)},new[]{new Vector3(-.02f,1,.80f),new Vector3(.85f,1.70f,.07f)}}){var c=new GameObject("Wardrobe carcass collider").AddComponent<BoxCollider>();c.transform.SetParent(invisible,false);c.center=new Vector3(sizeAndPos[0].x*frontSign,sizeAndPos[0].y,sizeAndPos[0].z);c.size=sizeAndPos[1];}
   // Gameplay observes the near visible corner, independently of the imported model pivot.
   // The old proxy pivot was offset; using the new carcass centre broke lap1 visibility/range.
   var interaction=new GameObject("Wardrobe near-front interaction").transform;interaction.SetParent(root,false);interaction.position=new Vector3(2.16f,0,2.42f);flat.wardrobe=interaction;flat.wardrobeLeaves=leaves;
   Debug.Log("BEDROOM05 Wardrobe author geometry .55m depth x 1.00m width x 1.90m height; horizontal fit only, front visual acceptance pending.");
  }
  static void FinishSurfaces(FlatBindings flat,Material enamel){
   var root=Group("Bedroom05 architectural finishes",flat.apartment);
   var wallpaper=Mat("BedroomFloralWallpaper",Art+"/BedroomDecor/wallpaper-v2.png",Color.white,.025f);
   var walls=new System.Collections.Generic.List<Renderer>();int count=0;
   Action<bool,float,float,float,float,float> panel=(isX,fixedAxis,a0,a1,y0,y1)=>{
    var m=new Mesh{name="Bedroom continuous wallpaper UV "+count};var v=isX?new[]{new Vector3(fixedAxis,y0,a0),new Vector3(fixedAxis,y0,a1),new Vector3(fixedAxis,y1,a1),new Vector3(fixedAxis,y1,a0)}:new[]{new Vector3(a0,y0,fixedAxis),new Vector3(a1,y0,fixedAxis),new Vector3(a1,y1,fixedAxis),new Vector3(a0,y1,fixedAxis)};
    m.vertices=v;m.uv=new[]{new Vector2(a0,y0),new Vector2(a1,y0),new Vector2(a1,y1),new Vector2(a0,y1)};m.triangles=new[]{0,1,2,0,2,3};m.RecalculateNormals();
    string path=Folder+"/WallpaperPanel"+count+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(m,existing);UnityEngine.Object.DestroyImmediate(m);m=existing;EditorUtility.SetDirty(m);}else AssetDatabase.CreateAsset(m,path);
    var o=new GameObject("Continuous bedroom wallpaper "+count++);o.transform.SetParent(root,false);o.AddComponent<MeshFilter>().sharedMesh=m;var r=o.AddComponent<MeshRenderer>();r.sharedMaterial=wallpaper;r.shadowCastingMode=ShadowCastingMode.Off;walls.Add(r);
   };
   wallpaper.SetFloat("_Cull",0);
   panel(true,.004f,0,4.4f,0,2.5f);
   panel(true,2.794f,0,3.35f,0,2.5f);panel(true,2.794f,4.15f,4.4f,0,2.5f);panel(true,2.794f,3.35f,4.15f,2,2.5f);
   panel(false,.004f,0,.75f,0,2.5f);panel(false,.004f,2.05f,2.8f,0,2.5f);panel(false,.004f,.75f,2.05f,0,.85f);panel(false,.004f,.75f,2.05f,2.3f,2.5f);
   panel(false,4.394f,0,.45f,0,2.5f);panel(false,4.394f,1.35f,2.8f,0,2.5f);panel(false,4.394f,.45f,1.35f,2.05f,2.5f);
   flat.dressing.bedroomWalls=walls.ToArray();flat.dressing.bedroomNormal=wallpaper.mainTexture;
   // Trim stops at the actual openings, so no collision or visual bar crosses a doorway.
   foreach(float y in new[]{.045f,2.46f}){
    Box("West bedroom skirting/cove",root,new Vector3(.018f,y,2.2f),new Vector3(.03f,.07f,4.4f),enamel);
    Box("North bedroom skirting/cove",root,new Vector3(1.4f,y,.018f),new Vector3(2.8f,.07f,.03f),enamel);
    foreach(var span in y<1?new[]{new Vector2(0,3.35f),new Vector2(4.15f,4.4f)}:new[]{new Vector2(0,4.4f)})Box("East bedroom skirting/cove",root,new Vector3(2.782f,y,(span.x+span.y)/2),new Vector3(.03f,.07f,span.y-span.x),enamel);
    foreach(var span in y<1?new[]{new Vector2(0,.45f),new Vector2(1.35f,2.8f)}:new[]{new Vector2(0,2.8f)})Box("South bedroom skirting/cove",root,new Vector3((span.x+span.y)/2,y,4.382f),new Vector3(span.y-span.x,.07f,.03f),enamel);
   }
   var rugRoot=Group("Bedroom05 textured hanging rug",flat.carpet.leaf);var rug=GameObject.CreatePrimitive(PrimitiveType.Quad);rug.name="Textured rug 1.4 x 2.0";rug.transform.SetParent(rugRoot,false);rug.transform.localPosition=new Vector3(.45f,1.025f,-.028f);rug.transform.localScale=new Vector3(1.4f,2,1);UnityEngine.Object.DestroyImmediate(rug.GetComponent<Collider>());var rugmat=Mat("BedroomRug",Art+"/BedroomDecor/rug-v2.png",Color.white,.02f);rugmat.SetFloat("_Cull",0);rug.GetComponent<Renderer>().sharedMaterial=rugmat;
   EditorUtility.SetDirty(flat.dressing);
  }
  static void CeilingFixture(FlatBindings flat){
   var root=Group("Bedroom05 ready ceiling socket",flat.apartment);
   string socket=Art+"/CC0/pull_chain_light_socket/",bulb=Art+"/CC0/lightbulb_01/";
   var holder=Ready(socket+"pull_chain_light_socket.fbx",root);holder.transform.localPosition=new Vector3(1.70f,2.5f,2.3f);
   var sm=Mat("CeilingSocket",socket+"diffuse.jpg",Color.white,.35f,socket+"normal.png");foreach(var r in holder.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>sm).ToArray();
   var lightbulb=Ready(bulb+"lightbulb_01.fbx",root);lightbulb.transform.localPosition=new Vector3(1.70f,2.433f,2.3f);lightbulb.transform.localRotation=Quaternion.Euler(180,0,0);
   var bm=Mat("UnlitBulb",bulb+"diffuse.jpg",new Color(.5f,.5f,.5f),.75f,bulb+"normal.png");foreach(var r in lightbulb.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>bm).ToArray();
  }
  public static void ApplyAndBuild(){
   System.IO.Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();
   Icons(flat);Wardrobe(flat);
   var enamel=Mat("WhiteEnamel",Art+"/BedroomDecor/WhiteEnamel.jpg",Color.white,.48f);enamel.mainTextureScale=new Vector2(.3f,.3f);
   var brass=Mat("AgedBrass",Art+"/BedroomDecor/BrassWear.jpg",new Color(.72f,.51f,.18f),.58f);brass.SetFloat("_Metallic",.75f);
   Door(flat.bedroom,.8f,enamel,brass);Door(flat.bath,.7f,enamel,brass);FinishSurfaces(flat,enamel);CeilingFixture(flat);
   foreach(var t in flat.apartment.GetComponentsInChildren<Transform>(true).ToArray())if(t&&t.name=="READY Poly Haven iron bed with textured linen")UnityEngine.Object.DestroyImmediate(t.gameObject);
   BedroomBedInstaller.Create(flat.apartment);
   // Rebuild serialized chase references after replacing the bathroom door geometry.
   // Keeping deleted proxy renderers here caused a lap6 NullReferenceException.
   Physics.SyncTransforms();var overlap=new Bounds(new Vector3(2.1725f,1.25f,5.25f),new Vector3(5.045f,3,1.49f));
   flat.chaseOverlapRenderers=flat.apartment.GetComponentsInChildren<Renderer>(true).Where(r=>r.bounds.Intersects(overlap)).ToArray();
   flat.chaseOverlapColliders=flat.apartment.GetComponentsInChildren<Collider>(true).Where(c=>c.bounds.Intersects(overlap)).ToArray();
   if(flat.chaseOverlapRenderers.Any(r=>!r)||flat.chaseOverlapColliders.Any(c=>!c))throw new Exception("Missing chase geometry after art installation");
   Debug.Log("BEDROOM05_CHASE_REFERENCES renderers="+flat.chaseOverlapRenderers.Length+" colliders="+flat.chaseOverlapColliders.Length);
   var savedDoor=flat.bedroom.leaf.localRotation;var savedLeaves=flat.wardrobeLeaves.Select(t=>t.localRotation).ToArray();flat.bedroom.leaf.localRotation=Quaternion.Euler(0,flat.bedroom.direction*95,0);
   foreach(float angle in new[]{0f,95f}){foreach(var h in flat.wardrobeLeaves)h.localRotation=Quaternion.Euler(0,angle,0);Physics.SyncTransforms();for(int i=0;i<=12;i++){var point=Vector3.Lerp(new Vector3(3.3f,0,3.6f),new Vector3(1.9f,0,3.6f),i/12f);var hits=Physics.OverlapCapsule(point+Vector3.up*.3f,point+Vector3.up*1.4f,.22f,~0,QueryTriggerInteraction.Ignore).Where(c=>!c.transform.IsChildOf(flat.player.transform)).ToArray();if(hits.Length>0)throw new Exception("Bedroom05 entry blocked: "+string.Join(",",hits.Select(c=>c.name)));}}
   flat.bedroom.leaf.localRotation=savedDoor;for(int i=0;i<savedLeaves.Length;i++)flat.wardrobeLeaves[i].localRotation=savedLeaves[i];Debug.Log("BEDROOM05_ENTRY_AUDIT_PASS diameter .44 m at z3.6, wardrobe closed/open.");
   EditorUtility.SetDirty(flat);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);ScenarioBuilder.Build();
  }
 }
}
