using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ataka.Scenario;
namespace Ataka.Editor {
 public static class HallArt09 {
  const string Art="Assets/_Project/Art/", Output="Assets/_Project/Scenario/Hall09/";
  [Serializable]class Link{public string name,texture,normal;}
  [Serializable]class Manifest{public Link[] materials;}
  static Bounds BoundsOf(Transform t){var rs=t.GetComponentsInChildren<Renderer>();if(rs.Length==0)throw new Exception("No geometry "+t.name);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
  static Material Lit(string name,Color color,float smooth){var path=Output+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}
  static Texture2D Texture(string path,bool normal){var imp=AssetImporter.GetAtPath(path) as TextureImporter;if(!imp)throw new Exception("Missing texture "+path);imp.maxTextureSize=512;imp.filterMode=FilterMode.Bilinear;imp.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;imp.SaveAndReimport();var loaded=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(!loaded)throw new Exception("Texture decoding failed: "+path);return loaded;}
  static GameObject Ready(string folder,string file,string name,float height,Transform parent){
   var src=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/"+file);if(!src)throw new Exception("Missing ready asset "+file);
   var root=new GameObject(name);root.transform.SetParent(parent,false);
   var model=(GameObject)PrefabUtility.InstantiatePrefab(src);model.transform.SetParent(root.transform,false);
   var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(folder+"/manifest.json"));
   foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>{
    var link=manifest.materials.SingleOrDefault(x=>x.name==m.name);if(link==null&&manifest.materials.Length==1){link=manifest.materials[0];Debug.Log("HALL09 single author material mapping "+file+" "+m.name+" -> "+link.name);}if(link==null)throw new Exception("Unmapped "+file+" material "+m.name);var mat=Lit(link.name,Color.white,.22f);
    if(!string.IsNullOrEmpty(link.texture))mat.SetTexture("_BaseMap",Texture(folder+"/"+link.texture,false));
    if(!string.IsNullOrEmpty(link.normal)){mat.SetTexture("_BumpMap",Texture(folder+"/"+link.normal,true));mat.SetFloat("_BumpScale",.6f);mat.EnableKeyword("_NORMALMAP");}EditorUtility.SetDirty(mat);return mat;
   }).ToArray();
   var b=BoundsOf(root.transform);model.transform.localScale*=height/b.size.y;b=BoundsOf(root.transform);model.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
   PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   return root;
  }
  static void Place(Transform t,Vector3 bottom){var b=BoundsOf(t);t.position+=bottom-new Vector3(b.center.x,b.min.y,b.center.z);}
  static Transform Point(Transform parent,string name,Vector3 world){var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=world;return t;}
  static void BoxFor(Renderer r,Transform parent){var t=Point(parent,"Collision for "+r.name,r.bounds.center);var b=t.gameObject.AddComponent<BoxCollider>();b.size=r.bounds.size;}
  [MenuItem("Ataka/Hall09/Install ready hallway assets")]
  public static void Install(){
   Directory.CreateDirectory(Output);AssetDatabase.Refresh();var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();
   string lookBefore=EditorJsonUtility.ToJson(flat.look);
   if(flat.hallCupboard)UnityEngine.Object.DestroyImmediate(flat.hallCupboard.gameObject);
   foreach(var t in flat.apartment.GetComponentsInChildren<Transform>(true).ToArray())if(t&&(t.name=="READY hallway HookRack09"||t.name=="READY hallway VEF09"||t.name=="READY hallway boots09"||t.name=="READY hallway slipper09 L"||t.name=="READY hallway slipper09 R"))UnityEngine.Object.DestroyImmediate(t.gameObject);
   // Imported prefab pivots are not their placement centres. Select the obsolete hallway props by rendered bounds.
   foreach(var t in flat.apartment.GetComponentsInChildren<Transform>(true).ToArray())if(t&&(t.name=="PROXY Kenney cabinetBedDrawer"||t.name=="PROXY Kenney bathroomMirror"||t.name=="PROXY Kenney radio"||t.name=="PROXY Kenney coatRack"||t.name=="PROXY Kenney bench")){
    var centre=BoundsOf(t).center;if(centre.x>6.6f&&centre.z>3&&centre.z<6){Debug.Log("HALL09 remove obsolete "+t.name+" centre="+centre);UnityEngine.Object.DestroyImmediate(t.gameObject);}
   }
   var dresser=Ready(Art+"ReadyModels/Sketchfab/Dresser","Dresser.fbx","READY hallway Dresser09",1.8f,flat.apartment);
   var front=dresser.GetComponentsInChildren<Renderer>().Single(r=>r.name.EndsWith(".001_wood_0"));
   if(front.bounds.center.x<BoundsOf(dresser.transform).center.x)dresser.transform.Rotate(0,180,0,Space.World);
   Place(dresser.transform,new Vector3(6.90f,0,3.80f));var all=dresser.GetComponentsInChildren<Renderer>();var total=BoundsOf(dresser.transform);
   if(Mathf.Abs(total.size.z-1.2f)>.002f||Mathf.Abs(total.size.x-.4472726f)>.002f)throw new Exception("Dresser proportions changed "+total.size);
   var leaves=all.Where(r=>r.name.EndsWith("_wood_0")&&r.bounds.size.x<.025f&&r.bounds.size.z>.5f&&r.bounds.size.y<.4f).OrderBy(r=>r.bounds.center.z).ToArray();
   if(leaves.Length!=2)throw new Exception("Expected exactly two authored leaves, found "+leaves.Length);
   var right=leaves[1];var rightBounds=right.bounds;
   var handle=all.Where(r=>r.name.Contains("black_wood")&&r.bounds.center.x>rightBounds.max.x&&r.bounds.center.y>.4f&&r.bounds.center.z>3.8f).Single();
   var doorRoot=Point(dresser.transform,"Right south cupboard door",new Vector3(rightBounds.min.x,rightBounds.min.y,rightBounds.max.z));
   var pivot=Point(doorRoot,"Capsule safe leaf",doorRoot.position);right.transform.SetParent(pivot,true);handle.transform.SetParent(pivot,true);
   // Convex leaf boxes only; shell uses authored mesh surfaces so the compartment remains accessible.
   BoxFor(right,pivot);BoxFor(handle,pivot);var door=doorRoot.gameObject.AddComponent<ScenarioDoor>();door.leaf=pivot;door.direction=-1;
   foreach(var r in all)if(r!=right&&r!=handle){var mesh=r.GetComponent<MeshFilter>();if(mesh){var collider=r.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh.sharedMesh;}}
   var mirror=all.Single(r=>r.name.Contains("mirror_0"));mirror.gameObject.layer=30;mirror.sharedMaterial=Lit("MirrorFallback",new Color(.12f,.13f,.12f),.95f);
   var reflection=mirror.gameObject.AddComponent<Ataka.PlanarMirror>();reflection.normal=Vector3.right;reflection.reflectionShader=Shader.Find("Ataka/PlanarMirror");reflection.Configure(256,8);
   if(!reflection.reflectionShader)throw new Exception("Missing mirror shader");
   var cupboard=dresser.AddComponent<HallCupboard>();cupboard.door=door;cupboard.interaction=Point(dresser.transform,"Cupboard handle interaction",new Vector3(rightBounds.max.x+.055f,.43f,4.05f));cupboard.pickup=flat.torchPickup;flat.hallCupboard=cupboard;
   // The actual author compartment starts at23.45cm, not10cm; keep the ready flashlight above it.
   if(BoundsOf(flat.torchPickup).size.y>BoundsOf(flat.torchPickup).size.x)flat.torchPickup.Rotate(0,0,-90,Space.World);
   Place(flat.torchPickup,new Vector3(6.88f,.242f,4.05f));
   var torchBounds=BoundsOf(flat.torchPickup);if(torchBounds.max.y>.52f||torchBounds.min.y<.234f)throw new Exception("Torch outside author compartment");
   var phone=Ready(Art+"ReadyModels/Sketchfab/VEFPhone","VEFPhone.fbx","READY hallway VEF09",.15f,flat.apartment);
   phone.transform.Rotate(0,90,0,Space.World);Place(phone.transform,new Vector3(6.88f,.546f,3.50f));var phoneBounds=BoundsOf(phone.transform);flat.phone.position=phoneBounds.center;
   var boots=Ready(Art+"CC0/rubber_boots","rubber_boots.fbx","READY hallway boots09",.37f,flat.apartment);boots.transform.Rotate(0,-90,0,Space.World);Place(boots.transform,new Vector3(8.60f,0,5.50f));BoxFor(boots.GetComponentInChildren<Renderer>(),boots.transform);
   for(int i=0;i<2;i++){var slipper=BedroomReadyProps.CreateSlipper(flat.apartment,Vector3.zero);slipper.name="READY hallway slipper09 "+(i==0?"L":"R");slipper.transform.Rotate(0,-90,0,Space.World);Place(slipper.transform,new Vector3(8.55f,0,5.15f+(i==0?-.075f:.075f)));}
   var rack=Ready(Art+"ReadyModels/Sketchfab/HookRack","HookRack.fbx","READY hallway HookRack09",.12594955f,flat.apartment);rack.transform.Rotate(0,-90,0,Space.World);
   Place(rack.transform,new Vector3(8.76f,1.60f,5.10f));var rackBounds=BoundsOf(rack.transform);rack.transform.position+=Vector3.right*(8.796f-rackBounds.max.x);
   Physics.SyncTransforms();foreach(var hit in Physics.RaycastAll(new Vector3(8,1.65f,5.1f),Vector3.right,1.5f))Debug.Log("HALL09 hook wall ray "+hit.collider.name+" at "+hit.point);
   // Inspect the maximum swept volume, not only the final open angle.
   float frontMost=0;for(int a=0;a<=95;a++){pivot.localRotation=Quaternion.Euler(0,-a,0);frontMost=Mathf.Max(frontMost,BoundsOf(pivot).max.x);}pivot.localRotation=Quaternion.identity;
   if(8.8f-frontMost<1.05f)throw new Exception("Cupboard leaves insufficient passage: "+(8.8f-frontMost));
   if(lookBefore!=EditorJsonUtility.ToJson(flat.look))throw new Exception("Accepted lighting bindings changed");
   EditorUtility.SetDirty(flat);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Directory.CreateDirectory("QA/Hall09");File.WriteAllText("QA/Hall09/placement.txt","Dresser "+total+"\nRight leaf "+rightBounds+"\nTorch "+torchBounds+"\nPhone "+phoneBounds+"\nClear passage worst sweep="+(8.8f-frontMost)+"\nMirror256px8Hz, existing PlanarMirror shader.\nReady boots and slippers installed. Ready Sousinho CC-BY hook rack installed; hooks about1.65m.\n");
   File.AppendAllText("QA/Hall09/placement.txt", "\n"+string.Join("\n",all.Select(r=>r.name+" bounds="+r.bounds+" material="+r.sharedMaterial.name))+"\nPhone "+phoneBounds+"\nRack "+BoundsOf(rack.transform)+"\nBoots "+BoundsOf(boots.transform));
   Debug.Log("HALL09_INSTALLED passage="+(8.8f-frontMost)+" torchFloor="+torchBounds.min.y+" ready models; visual QA pending.");
  }
 }
}
