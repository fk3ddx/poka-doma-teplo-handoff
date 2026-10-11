using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace Ataka.Editor {
public static partial class ApartmentArtBuilder {
 const string Root="Assets/_Project";
 public const string ScenePath=Root+"/Scenes/Apartment_M1.unity";
 const float Floor=11.7f;
 static Dictionary<string,Material> mats;
 static Transform art; static int shellIndex; static string shellPrefix="shell_stable_";
 [Serializable] class MeshSet {public MeshRecord[] meshes;}
 [Serializable] class MeshRecord {public string name,material;public float[] vertices,normals,uv;public int[] triangles;}
 [MenuItem("Ataka/M1/Generate furnished apartment")]
 public static void Generate(){
  MilestoneBuilder.Generate();shellIndex=0;shellPrefix="shell_stable_";
  Directory.CreateDirectory(Root+"/Art/M1Materials");Directory.CreateDirectory(Root+"/Art/M1Meshes");AssetDatabase.Refresh();ImportTextures();mats=new Dictionary<string,Material>();CreateMaterials();MaterialRefinements();DomesticMaterials();PersonalMaterials();
  art=new GameObject("M1 authored apartment").transform;art.position=Vector3.up*Floor;
  var shell=GameObject.Find("Apartment • M0 metric shell").transform;
  foreach(var r in shell.GetComponentsInChildren<MeshRenderer>()){
   string n=r.name;if(n.Contains("proxy")||n=="Balcony railing"){Object.DestroyImmediate(r.gameObject);continue;}
   string old=r.sharedMaterial.name;
   if(old=="M0_plaster")r.sharedMaterial=mats["Wallpaper"];
   else if(old=="M0_floor")r.sharedMaterial=mats["Parquet"];
   else if(old=="M0_ceiling")r.sharedMaterial=mats["Plaster"];
   else if(old=="M0_door")r.sharedMaterial=mats["Wood"];
   else if(old=="M0_metal")r.sharedMaterial=mats["Metal"];
   if(n=="Window frame"||n=="Window sill")r.sharedMaterial=mats["Enamel"];
   if(n=="Leaf"){r.sharedMaterial=mats["WoodLight"];DoorDetails(r.transform);}
   Reproject(r);
   if(r.GetComponentInParent<Interactable>()==null)Static(r.gameObject);
  }
  ImportAuthoredMeshes();AddModel("ceramic_vase_01",new Vector3(6.13f,1.24f,2.05f),0,.23f);
  TrimAndWindows();Yard();Lighting();Atmosphere();Staircase();Finishing();WhitewashedCeilings();Seating();PersonalPhotos();
  var session=Object.FindFirstObjectByType<GameSession>();session.checkpointFile="checkpoint-m1.json";session.player.transform.position=new Vector3(5.1f,Floor+.02f,3.2f);session.player.transform.rotation=Quaternion.Euler(0,215,0);
  session.GetComponent<PrototypeMenu>().milestoneDescription="M1 · художественная квартира — рабочая версия\nЗал, кухня и бытовые детали. Сюжетный пролог ещё в разработке.";
  session.player.view.farClipPlane=240;session.player.view.backgroundColor=C(.095f,.12f,.14f);
  PlayerSettings.productName="Атака на запоминание";PlayerSettings.bundleVersion="0.2.6-dev";
  EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene(MilestoneBuilder.ScenePath,true)};AssetDatabase.SaveAssets();Debug.Log("M1_SCENE_GENERATED");
 }
 static void Static(GameObject go)=>GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic|StaticEditorFlags.ReflectionProbeStatic);
 static void ImportTextures(){
  foreach(string file in Directory.GetFiles(Root+"/Art","*",SearchOption.AllDirectories)){
   if(!file.EndsWith(".jpg")&&!file.EndsWith(".png"))continue;var t=AssetImporter.GetAtPath(file.Replace('\\','/')) as TextureImporter;if(t==null)continue;
   bool normal=file.Contains("nor_gl"),data=normal||file.Contains("Rough")||file.Contains("smoothness")||file.Contains("/AO.");t.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;t.sRGBTexture=!data;t.maxTextureSize=2048;t.anisoLevel=4;t.wrapMode=TextureWrapMode.Repeat;t.isReadable=file.Contains("Rough");t.SaveAndReimport();
  }
  foreach(string f in Directory.GetFiles(Root+"/Art/CC0","*.fbx",SearchOption.AllDirectories)){var imp=(ModelImporter)AssetImporter.GetAtPath(f.Replace('\\','/'));imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.generateSecondaryUV=true;imp.SaveAndReimport();}
 }
 static Color C(float r,float g,float b)=>new Color(r,g,b);
 static Material M(string name,Color color,float smooth=.25f,float metallic=0){
  string path=Root+"/Art/M1Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metallic);m.SetFloat("_Cull",2);mats[name]=m;return m;
 }
 static void Pbr(Material m,string asset,float scale=1){
  string path=Root+"/Art/CC0/"+asset+"/";m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path+"Diffuse.jpg");m.mainTextureScale=Vector2.one*scale;
  var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(path+"nor_gl.jpg");if(normal!=null){m.SetTexture("_BumpMap",normal);m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.65f);}
  var ao=AssetDatabase.LoadAssetAtPath<Texture2D>(path+"AO.jpg");if(ao!=null){m.SetTexture("_OcclusionMap",ao);m.SetFloat("_OcclusionStrength",.65f);m.EnableKeyword("_OCCLUSIONMAP");}
  var rough=AssetDatabase.LoadAssetAtPath<Texture2D>(path+"Rough.jpg");if(rough!=null){
   string maskPath=Root+"/Art/M1Materials/"+asset+"_smoothness.png";
   if(!File.Exists(maskPath)){var tex=new Texture2D(rough.width,rough.height,TextureFormat.RGBA32,false,true);var pixels=rough.GetPixels32();for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(0,0,0,(byte)(255-pixels[i].r));tex.SetPixels32(pixels);tex.Apply();File.WriteAllBytes(maskPath,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(maskPath);var imp=(TextureImporter)AssetImporter.GetAtPath(maskPath);imp.sRGBTexture=false;imp.alphaSource=TextureImporterAlphaSource.FromInput;imp.SaveAndReimport();}
   m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath));m.EnableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Smoothness",.4f);
  }
 }
 static void CreateMaterials(){
  Pbr(M("Wood",C(.56f,.58f,.59f),.35f),"dark_wood",1.6f);Pbr(M("WoodLight",C(.70f,.71f,.68f),.32f),"dark_wood",1.4f);Pbr(M("Parquet",C(.72f,.63f,.51f),.25f),"herringbone_parquet",.5f);Pbr(M("Plaster",C(.8f,.79f,.74f),.1f),"clay_plaster",.6f);
  var wallpaper=M("Wallpaper",C(.8f,.78f,.70f),.06f);wallpaper.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/Textures/GrandmaWallpaper.png");wallpaper.mainTextureScale=Vector2.one*.9f;
  var carpet=M("Carpet",Color.white,.05f);carpet.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/Textures/GrandmaCarpet.png");carpet.SetFloat("_Cull",0);
  Pbr(M("Fabric",C(.50f,.40f,.27f),.06f),"quatrefoil_jacquard_fabric",2);Pbr(M("DarkFabric",C(.25f,.22f,.16f),.04f),"quatrefoil_jacquard_fabric",2);Pbr(M("Curtain",C(.50f,.31f,.20f),.04f),"quatrefoil_jacquard_fabric",1.3f);Pbr(M("Plaid",C(.4f,.33f,.24f),.04f),"quatrefoil_jacquard_fabric",1.2f);Pbr(M("Linen",C(.72f,.68f,.57f),.04f),"quatrefoil_jacquard_fabric",2.2f);
  M("Lace",C(.72f,.71f,.62f),.03f);M("Enamel",C(.73f,.75f,.69f),.45f);M("Porcelain",C(.74f,.71f,.61f),.65f);M("Coffee",C(.026f,.014f,.01f),.8f);M("Black",C(.018f,.019f,.017f),.28f);M("Metal",C(.24f,.28f,.28f),.4f,.55f);M("Chrome",C(.7f,.73f,.72f),.8f,.9f);M("Brass",C(.42f,.27f,.1f),.65f,.65f);M("Crystal",C(.26f,.34f,.33f),.92f,.45f);
  var crystal=mats["Crystal"];crystal.color=new Color(.68f,.75f,.72f,.12f);crystal.SetFloat("_Metallic",0);crystal.SetFloat("_Surface",1);crystal.SetFloat("_Blend",0);
  crystal.SetFloat("_SrcBlend",(float)BlendMode.One);crystal.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);crystal.SetFloat("_ZWrite",0);crystal.SetFloat("_BlendModePreserveSpecular",1);crystal.SetOverrideTag("RenderType","Transparent");crystal.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");crystal.EnableKeyword("_ALPHAPREMULTIPLY_ON");crystal.renderQueue=3000;
  var glass=M("Glass",C(.11f,.16f,.16f),.9f);glass.SetFloat("_Surface",1);glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);glass.SetFloat("_ZWrite",0);glass.color=new Color(.2f,.26f,.24f,.13f);glass.renderQueue=3000;glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
  M("Mirror",C(.46f,.50f,.48f),.65f,.6f);M("Paper",C(.69f,.63f,.48f),.04f);M("BookRed",C(.23f,.05f,.035f),.15f);M("BookGreen",C(.06f,.13f,.10f),.15f);M("BookTan",C(.37f,.26f,.15f),.1f);M("TVBrown",C(.17f,.09f,.045f),.25f);
  var tv=M("TVScreen",C(.18f,.27f,.24f),.65f);tv.EnableKeyword("_EMISSION");tv.SetColor("_EmissionColor",C(.12f,.21f,.17f));M("Terracotta",C(.31f,.15f,.09f),.07f);M("Leaf",C(.085f,.18f,.055f),.18f);M("DriedStem",C(.29f,.20f,.09f),.1f);M("Photo",C(.26f,.22f,.16f),.15f);M("KitchenPaint",C(.39f,.49f,.39f),.25f);M("GasYellow",C(.56f,.4f,.075f),.25f);
  var shade=M("Shade",C(.73f,.57f,.36f),.1f);shade.SetFloat("_Cull",0);shade.EnableKeyword("_EMISSION");shade.SetColor("_EmissionColor",C(.25f,.13f,.055f));var bulb=M("Bulb",C(1,.75f,.45f));bulb.EnableKeyword("_EMISSION");bulb.SetColor("_EmissionColor",C(2,1.1f,.4f));
  Pbr(M("Concrete",C(.26f,.29f,.28f),.1f),"plastered_wall_02",.35f);M("Road",C(.055f,.065f,.07f),.48f);M("Snow",C(.35f,.39f,.39f),.05f);M("Bark",C(.09f,.10f,.085f),.05f);M("WindowDark",C(.015f,.027f,.036f),.55f);var win=M("WindowWarm",C(.22f,.16f,.075f));win.EnableKeyword("_EMISSION");win.SetColor("_EmissionColor",C(.25f,.16f,.07f));
 }
 static void ImportAuthoredMeshes(){
  var data=JsonUtility.FromJson<MeshSet>(File.ReadAllText(Root+"/Art/Authored/ApartmentFurniture.meshdata.json"));
  var drawerRoot=new GameObject("Working hall drawer");drawerRoot.transform.SetParent(art,false);
  var drawer=drawerRoot.AddComponent<SlidingDrawer>();drawer.persistentId="hall-drawer";drawer.label="ящик трюмо";drawer.movingPart=drawerRoot.transform;drawer.closedLocalPosition=Vector3.zero;
  drawer.envelope=drawerRoot.AddComponent<BoxCollider>();drawer.envelope.center=new Vector3(6.956f,.66f,5.71f);drawer.envelope.size=new Vector3(.51f,.20f,.34f);
  foreach(var d in data.meshes){
   string path=Root+"/Art/M1Meshes/"+d.name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();mesh.name=d.name;mesh.indexFormat=IndexFormat.UInt32;
   var v=new Vector3[d.vertices.Length/3];var n=new Vector3[v.Length];var uv=new Vector2[v.Length];for(int i=0;i<v.Length;i++){v[i]=new Vector3(d.vertices[i*3],d.vertices[i*3+1],d.vertices[i*3+2]);n[i]=new Vector3(d.normals[i*3],d.normals[i*3+1],d.normals[i*3+2]);uv[i]=new Vector2(d.uv[i*2],d.uv[i*2+1]);}
   // Project tulle across its full width/height, independent of each pleat's facing.
   if(d.material=="Tulle")for(int i=0;i<v.Length;i++)uv[i]=Mathf.Abs(n[i].y)>.5f?new Vector2(v[i].x,v[i].z):new Vector2(v[i].x,v[i].y);
   if(d.material=="WoodTeak")for(int i=0;i<v.Length;i++){
    // Vertical boards follow the height; table surfaces retain horizontal grain.
    if(Mathf.Abs(n[i].y)<.5f)uv[i]=new Vector2(uv[i].y,uv[i].x);
   }
   mesh.vertices=v;mesh.normals=n;mesh.uv=uv;mesh.triangles=d.triangles;mesh.RecalculateTangents();mesh.RecalculateBounds();Unwrapping.GenerateSecondaryUVSet(mesh);EditorUtility.SetDirty(mesh);
   bool isDrawer=d.name.StartsWith("HallDrawer_");
   var go=new GameObject(d.name);go.transform.SetParent(isDrawer?drawerRoot.transform:art,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mats[d.material];
   if(!isDrawer&&!new[]{"Glass","Curtain","Lace","Tulle","Bulb","Shade","ShadeBinding","DriedStem","Carpet"}.Contains(d.material))go.AddComponent<MeshCollider>().sharedMesh=mesh;
   if(!isDrawer&&!d.name.StartsWith("Yard_"))Static(go);
  }
 }
 static void Reproject(MeshRenderer r){
  var mf=r.GetComponent<MeshFilter>();if(mf==null)return;var mesh=Object.Instantiate(mf.sharedMesh);var v=mesh.vertices;var n=mesh.normals;var uv=new Vector2[v.Length];for(int i=0;i<v.Length;i++){Vector3 p=Vector3.Scale(v[i],r.transform.lossyScale),a=n[i];uv[i]=Mathf.Abs(a.y)>.5f?new Vector2(p.x,p.z):Mathf.Abs(a.x)>.5f?new Vector2(p.z,p.y):new Vector2(p.x,p.y);}mesh.uv=uv;mesh.RecalculateTangents();Unwrapping.GenerateSecondaryUVSet(mesh);string path=Root+"/Art/M1Meshes/"+shellPrefix+(shellIndex++)+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null)AssetDatabase.CreateAsset(mesh,path);else {EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}mf.sharedMesh=mesh;
 }
 static GameObject Box(string name,Vector3 p,Vector3 s,string material,bool collider=true){
  var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(art,false);go.transform.localPosition=p;go.transform.localScale=s;go.GetComponent<Renderer>().sharedMaterial=mats[material];if(!collider)Object.DestroyImmediate(go.GetComponent<Collider>());if(p.y>-2)Static(go);return go;
 }
 static void Rod(string name,Vector3 a,Vector3 b,float radius,string material){
  var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(art,false);go.transform.localPosition=(a+b)/2;go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);go.transform.localScale=new Vector3(radius*2,(b-a).magnitude/2,radius*2);go.GetComponent<Renderer>().sharedMaterial=mats[material];Object.DestroyImmediate(go.GetComponent<Collider>());GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ReflectionProbeStatic);
 }
 static void AddModel(string asset,Vector3 pos,float yaw,float height){
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Art/CC0/"+asset+"/fbx.fbx");if(prefab==null)throw new Exception("Missing model "+asset);var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=asset;go.transform.SetParent(art,false);go.transform.localRotation=Quaternion.Euler(0,yaw,0)*go.transform.localRotation;var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);go.transform.localScale*=height/b.size.y;b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);go.transform.position+=art.TransformPoint(pos)-new Vector3(b.center.x,b.min.y,b.center.z);var m=M(asset,Color.white);Pbr(m,asset);foreach(var r in rs){r.sharedMaterial=m;Static(r.gameObject);var mf=r.GetComponent<MeshFilter>();if(mf!=null)r.gameObject.AddComponent<MeshCollider>().sharedMesh=mf.sharedMesh;}
 }
 static void DoorDetails(Transform leaf){
  foreach(float side in new[]{-1f,1f}){var p=GameObject.CreatePrimitive(PrimitiveType.Cube);p.name="Door inset";p.transform.SetParent(leaf,false);p.transform.localPosition=new Vector3(0,.08f,side*.51f);p.transform.localScale=new Vector3(.76f,.66f,.15f);p.GetComponent<Renderer>().sharedMaterial=mats["Wood"];Object.DestroyImmediate(p.GetComponent<Collider>());var h=GameObject.CreatePrimitive(PrimitiveType.Cylinder);h.name="Door handle";h.transform.SetParent(leaf,false);h.transform.localPosition=new Vector3(.34f,-.045f,side*.95f);h.transform.localRotation=Quaternion.Euler(0,0,90);h.transform.localScale=new Vector3(.025f,.065f,.5f);h.GetComponent<Renderer>().sharedMaterial=mats["Brass"];Object.DestroyImmediate(h.GetComponent<Collider>());}
 }
 static void TrimAndWindows(){
  foreach(var r in GameObject.Find("Apartment • M0 metric shell").GetComponentsInChildren<MeshRenderer>()){
   string n=r.name;if(!(n.Contains("exterior")||n.Contains(" / ")||n=="Exterior wall")||r.bounds.min.y>Floor+.1f)continue;var b=r.bounds;var p=b.center-Vector3.up*Floor;p.y=.07f;var s=b.size;s.y=.12f;if(b.size.x>b.size.z)s.z+=.025f;else s.x+=.025f;Box("Skirting",p,s,"Wood",false);p.y=2.44f;s.y=.07f;Box("Cornice",p,s,"Plaster",false);
  }
  foreach(var pair in new[]{new Vector2(.5f,2.1f),new Vector2(3.2f,4.55f),new Vector2(7.1f,8.3f)}){
   float x=(pair.x+pair.y)/2,w=pair.y-pair.x;foreach(float y in new[]{.87f,2.29f,1.91f})Box("Window horizontal rebate",new Vector3(x,y,-.15f),new Vector3(w,.037f,.12f),"Enamel",false);Box("Window glass",new Vector3(x,1.575f,-.215f),new Vector3(w-.05f,1.4f,.005f),"Glass",false);
  }
  for(float x=3;x<6.45f;x+=.16f)Rod("Balcony vertical",new Vector3(x,.06f,-1.31f),new Vector3(x,1.06f,-1.31f),.013f,"Metal");Rod("Balcony top rail",new Vector3(2.95f,1.06f,-1.31f),new Vector3(6.45f,1.06f,-1.31f),.026f,"Metal");Box("Balcony safety collision",new Vector3(4.7f,.53f,-1.31f),new Vector3(3.5f,1.06f,.035f),"Metal").GetComponent<Renderer>().enabled=false;
 }
 static void Yard(){
  Box("Wet courtyard",new Vector3(4,-Floor-.1f,-31),new Vector3(140,.2f,110),"Road");Building(-6,-33,42,5);Building(-44,-51,19,9);Building(43,-61,30,9);var rng=new System.Random(19);
  foreach(float x in new[]{-12f,11f,27f}){Rod("Courtyard lamp pole",new Vector3(x,-Floor,-15),new Vector3(x,5-Floor,-15),.065f,"Metal");Rod("Lamp arm",new Vector3(x,5-Floor,-15),new Vector3(x+.7f,5.1f-Floor,-15),.035f,"Metal");Box("Sodium lamp",new Vector3(x+.7f,5.05f-Floor,-15),new Vector3(.45f,.12f,.2f),"Bulb",false);Point("Courtyard pool",new Vector3(x+.7f,4.8f-Floor,-15),C(1,.52f,.16f),2.6f,9,false);}
  for(int i=0;i<9;i++)Box("Dirty snowbank",new Vector3(-18+i*6,-Floor+.02f,-26),new Vector3(4,.07f,1.9f),"Snow",false);
 }
 static void Building(float x,float z,float width,int floors){
  float h=floors*2.8f;Box("Panel block",new Vector3(x,h/2-Floor,z),new Vector3(width,h,9),"Concrete",false);
  for(int f=0;f<floors;f++){
   float y=f*2.8f+1.65f-Floor;Box("Floor seam",new Vector3(x,f*2.8f-Floor,z+4.51f),new Vector3(width,.045f,.02f),"Road",false);
   for(float wx=x-width/2+1.3f;wx<x+width/2-1;wx+=2.9f){bool lit=((int)(wx*10)+f*13)%7==0;Box("Recessed window",new Vector3(wx,y,z+4.52f),new Vector3(1.45f,1.45f,.05f),lit?"WindowWarm":"WindowDark",false);Box("Mullion",new Vector3(wx,y,z+4.56f),new Vector3(.055f,1.45f,.035f),"Concrete",false);Box("Concrete sill",new Vector3(wx,y-.75f,z+4.58f),new Vector3(1.55f,.065f,.16f),"Concrete",false);if(((int)(wx*10))%3==0){Box("Balcony slab",new Vector3(wx,y-.84f,z+4.9f),new Vector3(1.8f,.12f,.75f),"Concrete",false);Box("Balcony faded panel",new Vector3(wx,y-.36f,z+5.28f),new Vector3(1.8f,.84f,.06f),"KitchenPaint",false);}}
  }
  Box("Roof cornice",new Vector3(x,h-Floor,z),new Vector3(width+.4f,.2f,9.3f),"Concrete",false);
 }
 static Light Point(string name,Vector3 p,Color color,float intensity,float range,bool shadow){var go=new GameObject(name);go.transform.SetParent(art,false);go.transform.localPosition=p;var l=go.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=shadow?LightShadows.Soft:LightShadows.None;l.shadowBias=.025f;l.shadowNormalBias=.12f;return l;}
 static void Lighting(){
  RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=C(.12f,.15f,.19f);RenderSettings.ambientEquatorColor=C(.075f,.082f,.085f);RenderSettings.ambientGroundColor=C(.03f,.025f,.022f);RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.025f;RenderSettings.fogColor=C(.095f,.12f,.14f);
  foreach(var lamp in Object.FindObjectsByType<LampSwitch>(FindObjectsSortMode.None)){lamp.intensity=lamp.persistentId=="lamp-living"?1.25f:1.1f;lamp.controlledLight.intensity=lamp.intensity;lamp.controlledLight.color=C(1,.80f,.60f);lamp.controlledLight.shadowBias=.02f;lamp.controlledLight.shadowNormalBias=.15f;if(lamp.persistentId=="lamp-living")lamp.controlledLight.transform.localPosition=new Vector3(4.8f,1.95f,2.4f);lamp.controlledLight.lightmapBakeType=LightmapBakeType.Mixed;lamp.controlledLight.bounceIntensity=1.0f;}
  Point("Floor lamp warm pool",new Vector3(5.78f,1.36f,.39f),C(1,.67f,.34f),.65f,3.5f,true);Point("CRT bounce",new Vector3(5.88f,.98f,1.92f),C(.48f,.65f,.7f),.13f,1.6f,false);
  var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root+"/Settings/M0_Renderer.asset");if(!renderer.rendererFeatures.Any(x=>x is ScreenSpaceAmbientOcclusion)){var ao=ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();ao.name="Contact ambient occlusion";AssetDatabase.AddObjectToAsset(ao,renderer);renderer.rendererFeatures.Insert(0,ao);var so=new SerializedObject(ao);so.FindProperty("m_Settings.Intensity").floatValue=.85f;so.FindProperty("m_Settings.Radius").floatValue=.22f;so.FindProperty("m_Settings.Downsample").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();ao.Create();EditorUtility.SetDirty(renderer);}
  var volume=new GameObject("Apartment color and lens").AddComponent<Volume>();volume.isGlobal=true;string path=Root+"/Settings/M1_Volume.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}volume.sharedProfile=profile;
  if(!profile.TryGet<Tonemapping>(out var tone))tone=profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);if(!profile.TryGet<ColorAdjustments>(out var color))color=profile.Add<ColorAdjustments>();color.postExposure.Override(.15f);color.saturation.Override(-8);color.contrast.Override(6);if(!profile.TryGet<Bloom>(out var bloom))bloom=profile.Add<Bloom>();bloom.intensity.Override(.08f);bloom.threshold.Override(1.2f);if(!profile.TryGet<Vignette>(out var vignette))vignette=profile.Add<Vignette>();vignette.intensity.Override(.15f);vignette.smoothness.Override(.6f);EditorUtility.SetDirty(profile);
  RoomLightingProbes();
 }
 [MenuItem("Ataka/M1/Build Windows")]
 public static void Build(){Directory.CreateDirectory("Builds/M1");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/M1/Ataka-M1.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("M1 build failed "+report.summary.result);Debug.Log("M1_BUILD_SUCCESS "+report.summary.totalSize);}
}}
