using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ataka.Scenario;
namespace Ataka.Editor {
 public static class BedroomArt06 {
  public static void MoveNightstandAndBuild(){
   var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();
   var table=flat.apartment.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="PROXY Kenney cabinetBed"||t.name=="TEMP Kenney nightstand at headboard");
   if(!table)throw new Exception("Missing existing nightstand");
   if(table.name=="PROXY Kenney cabinetBed")table.rotation=Quaternion.Euler(0,90,0)*table.rotation;
   table.name="TEMP Kenney nightstand at headboard";
   var rs=table.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
   table.position+=new Vector3(1.22f,0,1)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
   Physics.SyncTransforms();Debug.Log("BEDROOM06_NIGHTSTAND headboard x1.22 z1.00, original proxy retained pending art06");
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);ScenarioBuilder.Build();
  }
  const string Art="Assets/_Project/Art/", Output="Assets/_Project/Scenario/Bedroom06/";
  static Bounds Measure(GameObject obj){
   var rs=obj.GetComponentsInChildren<Renderer>();if(rs.Length==0)throw new Exception("No renderers: "+obj.name);
   var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;
  }
  static Material MaterialCopy(string name,Material source){
   var path=Output+name+".mat";var result=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!result){result=new Material(source);AssetDatabase.CreateAsset(result,path);}return result;
  }
  static Material Lit(string name,Color color,float smooth){
   var path=Output+name+".mat";var result=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!result){result=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(result,path);}
   result.SetColor("_BaseColor",color);result.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(result);return result;
  }
  static Texture2D Texture(string path,bool normal=false,bool data=false){
   var importer=AssetImporter.GetAtPath(path) as TextureImporter;
   if(!importer)throw new Exception("Missing texture: "+path);
   importer.maxTextureSize=512;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
   importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
   if(!normal)importer.sRGBTexture=!data;importer.alphaIsTransparency=!data&&!normal;importer.SaveAndReimport();
   return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  }
  static void Transparent(Material m){
   m.SetFloat("_Surface",1);m.SetFloat("_ZWrite",0);m.SetFloat("_SrcBlend",5);m.SetFloat("_DstBlend",10);
   m.SetOverrideTag("RenderType","Transparent");m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;m.SetShaderPassEnabled("ShadowCaster",false);
  }
  static GameObject Ready(string path,Transform parent,string name){
   var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset)throw new Exception("Missing authored mesh "+path);
   var obj=(GameObject)PrefabUtility.InstantiatePrefab(asset);obj.transform.SetParent(parent,false);obj.name=name;return obj;
  }
  static void Remove(Transform parent,string name){var obj=parent.Find(name);if(obj)UnityEngine.Object.DestroyImmediate(obj.gameObject);}
  static void Rod(Transform parent,string name,Vector3 a,Vector3 b,float radius,Material mat){
   var obj=GameObject.CreatePrimitive(PrimitiveType.Cylinder);obj.name=name;obj.transform.SetParent(parent,false);
   obj.transform.position=(a+b)*.5f;obj.transform.up=(b-a).normalized;obj.transform.localScale=new Vector3(radius*2,(b-a).magnitude*.5f,radius*2);
   UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());obj.GetComponent<Renderer>().sharedMaterial=mat;
  }
  static void InstallCorner(FlatBindings flat){
   var root=flat.apartment.Find("Bedroom05 icon corner");if(!root)throw new Exception("Install bedroom05 first");
   var shelf=root.Find("Corner icon shelf");if(!shelf)throw new Exception("Missing icon shelf");
   var wood=MaterialCopy("DarkIconShelf",shelf.GetComponent<Renderer>().sharedMaterial);wood.SetColor("_BaseColor",new Color(.12f,.09f,.065f));wood.SetFloat("_Smoothness",.22f);EditorUtility.SetDirty(wood);
   shelf.GetComponent<Renderer>().sharedMaterial=wood;shelf.position=new Vector3(.33f,1.6025f,.33f);shelf.localScale=new Vector3(.6f,.035f,.6f);
   var icons=root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Ready framed Met icon ")).OrderBy(t=>t.name).ToArray();
   if(icons.Length!=2)throw new Exception("Expected two existing Met icons");
   for(int i=0;i<2;i++){
    var t=icons[i];t.rotation=Quaternion.Euler(0,45,90);t.localScale=Vector3.one*.55f;
    var b=Measure(t.gameObject);var center=i==0?new Vector3(.24f,0,.42f):new Vector3(.42f,0,.24f);
    t.position+=new Vector3(center.x-b.center.x,1.62f-b.min.y,center.z-b.center.z);
   }
   Remove(root,"CC0 vase reused as lampada vessel");Remove(root,"Approved lampada06");
   var lamp=Ready(Art+"BedroomDecor/Lampada06/Lampada06.fbx",root,"Approved lampada06");lamp.transform.position=new Vector3(.36f,1.75f,.36f);
   var glass=Lit("LampadaRedGlass",new Color(.18f,.006f,.002f,.55f),.8f);Transparent(glass);glass.EnableKeyword("_EMISSION");glass.SetColor("_EmissionColor",new Color(.035f,.001f,0));
   var brass=Lit("LampadaBrass",new Color(.28f,.17f,.055f),.5f);brass.SetFloat("_Metallic",.65f);
   foreach(var r in lamp.GetComponentsInChildren<Renderer>()){r.sharedMaterial=r.name.Contains("glass")?glass:brass;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
   // Bracket really reaches the wall and the authored suspension ring; no floating attachment.
   Rod(lamp.transform,"Lampada wall bracket",new Vector3(.36f,1.95f,.012f),new Vector3(.36f,1.95f,.36f),.003f,brass);
   Rod(lamp.transform,"Lampada bracket wall plate",new Vector3(.36f,1.91f,.012f),new Vector3(.36f,1.99f,.012f),.005f,brass);
   var flame=GameObject.CreatePrimitive(PrimitiveType.Quad);flame.name="Lampada flame 1 x 2 cm";flame.transform.SetParent(lamp.transform,false);flame.transform.localPosition=Vector3.zero;flame.transform.rotation=Quaternion.Euler(0,45,0);flame.transform.localScale=new Vector3(.01f,.02f,1);UnityEngine.Object.DestroyImmediate(flame.GetComponent<Collider>());
   var flameSource=new Material(Shader.Find("Universal Render Pipeline/Unlit"));var fm=MaterialCopy("LampadaFlame",flameSource);UnityEngine.Object.DestroyImmediate(flameSource);fm.SetTexture("_BaseMap",Texture(Art+"BedroomDecor/Lampada06/Flame.png"));fm.SetColor("_BaseColor",new Color(1,.68f,.3f));fm.SetFloat("_Cull",0);Transparent(fm);EditorUtility.SetDirty(fm);flame.GetComponent<Renderer>().sharedMaterial=fm;flame.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   var halo=root.GetComponentsInChildren<Light>().Single(l=>l.name=="Icon glass halo");halo.transform.position=new Vector3(.36f,1.75f,.36f);halo.range=.5f;halo.intensity=.018f;EditorUtility.SetDirty(halo);
   // Accepted spot color and intensity deliberately unchanged.
   flat.look.icon.innerSpotAngle=40;EditorUtility.SetDirty(flat.look.icon);
  }
  static void InstallClock(FlatBindings flat){
   var table=flat.apartment.GetComponentsInChildren<Transform>().Single(t=>t.name=="TEMP Kenney nightstand at headboard");
   var rs=table.GetComponentsInChildren<Renderer>();int n=0;foreach(var r in rs){r.sharedMaterials=r.sharedMaterials.Select(m=>{var dark=MaterialCopy("NightstandWalnut"+n++,m);dark.SetColor("_BaseColor",new Color(.12f,.08f,.045f));EditorUtility.SetDirty(dark);return dark;}).ToArray();}
   var top=Measure(table.gameObject).max.y;
   Remove(flat.apartment,"Ready Poly Haven bedside clock06");
   var clock=Ready(Art+"CC0/alarm_clock_01/alarm_clock_01.fbx",flat.apartment,"Ready Poly Haven bedside clock06");clock.transform.rotation=Quaternion.Euler(0,90,0);
   // Preserve the measured author scale: 13.2 cm wide, 17.4 cm tall.
   var b=Measure(clock);clock.transform.position+=new Vector3(1.22f-b.center.x,top+.002f-b.min.y,1.00f-b.center.z);
   var mat=Lit("ReadyAlarmClock",Color.white,.35f);string dir=Art+"CC0/alarm_clock_01/";
   mat.SetTexture("_BaseMap",Texture(dir+"alarm_clock_01_diff_1k.jpg"));mat.SetTexture("_BumpMap",Texture(dir+"alarm_clock_01_nor_gl_1k.jpg",true));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_BumpScale",.35f);
   mat.SetTexture("_MetallicGlossMap",Texture(dir+"UnityMetalSmooth.png",false,true));mat.SetFloat("_Smoothness",1);mat.EnableKeyword("_METALLICSPECGLOSSMAP");
   var clear=Lit("ReadyClockGlass",new Color(.8f,.87f,.91f,.05f),.8f);Transparent(clear);
   foreach(var r in clock.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>r.name.Contains("glass")?clear:mat).ToArray();
  }
  // Explicit staging operation only: no InitializeOnLoad, no automatic build or player launch.
  [MenuItem("Ataka/Bedroom06/Apply prepared corner and bedside clock")]
  public static void ApplyPreparedArt(){
   System.IO.Directory.CreateDirectory(Output);AssetDatabase.Refresh();
   var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();
   InstallCorner(flat);InstallClock(flat);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Debug.Log("BEDROOM06 Prepared corner and clock installed; visual review, shader compilation and gameplay checks still required. Water glass not installed.");
  }

 }
}
