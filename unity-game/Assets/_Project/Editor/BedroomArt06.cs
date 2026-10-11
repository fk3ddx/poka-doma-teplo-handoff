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
   var tabletop=Measure(table.gameObject);
   Remove(flat.apartment,"Ready Poly Haven bedside clock06");
   var clock=Ready(Art+"CC0/alarm_clock_01/alarm_clock_01.fbx",flat.apartment,"Ready Poly Haven bedside clock06");clock.transform.rotation=Quaternion.Euler(0,90,0);
   PrefabUtility.UnpackPrefabInstance(clock,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   // Preserve the measured author scale: 13.2 cm wide, 17.4 cm tall.
   PlaceOnTable(clock,new Vector2(1.18f,.88f),tabletop);
   var mat=Lit("ReadyAlarmClock",Color.white,.35f);string dir=Art+"CC0/alarm_clock_01/";
   mat.SetTexture("_BaseMap",Texture(dir+"alarm_clock_01_diff_1k.jpg"));mat.SetTexture("_BumpMap",Texture(dir+"alarm_clock_01_nor_gl_1k.jpg",true));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_BumpScale",.35f);
   mat.SetTexture("_MetallicGlossMap",Texture(dir+"UnityMetalSmooth.png",false,true));mat.SetFloat("_Smoothness",1);mat.EnableKeyword("_METALLICSPECGLOSSMAP");
   var clear=Lit("ReadyClockGlass",new Color(.8f,.87f,.91f,.05f),.8f);Transparent(clear);
   foreach(var r in clock.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>r.name.Contains("glass")?clear:mat).ToArray();
   var dial=clock.AddComponent<ClockFace>();dial.timeSource=flat.clock;dial.Show(flat.clock.seconds);
   var parts=clock.GetComponentsInChildren<Transform>();
   Transform Hand(string partName){
    var model=parts.Single(t=>t.name==partName);var pivot=new GameObject(partName+" normalized axis").transform;
    pivot.SetParent(clock.transform,false);pivot.position=model.position;model.SetParent(pivot,true);
    var mesh=model.GetComponent<MeshFilter>();if(!mesh)throw new Exception("Clock hand has no mesh: "+partName);
    Vector3 tip=Vector3.zero;foreach(var v in mesh.sharedMesh.vertices){var p=pivot.InverseTransformPoint(model.TransformPoint(v));if(p.x*p.x+p.y*p.y>tip.x*tip.x+tip.y*tip.y)tip=p;}
    if(new Vector2(tip.x,tip.y).magnitude<.003f)throw new Exception("Invalid clock hand pivot: "+partName);
    model.RotateAround(pivot.position,pivot.forward,Mathf.Atan2(tip.x,tip.y)*Mathf.Rad2Deg);return pivot;
   }
   dial.hour=Hand("alarm_clock_01_hour_hand");dial.minute=Hand("alarm_clock_01_minute_hand");dial.second=Hand("alarm_clock_01_second_hand");
   EditorUtility.SetDirty(dial);

  }
  static void PlaceOnTable(GameObject prop,Vector2 center,Bounds tabletop){
   var b=Measure(prop);prop.transform.position+=new Vector3(center.x-b.center.x,tabletop.max.y+.002f-b.min.y,center.y-b.center.z);
   b=Measure(prop);const float margin=.01f;
   if(b.min.x<tabletop.min.x+margin||b.max.x>tabletop.max.x-margin||b.min.z<tabletop.min.z+margin||b.max.z>tabletop.max.z-margin)
    throw new Exception(prop.name+" does not fit the bedside surface with a 1cm edge margin; do not squash the authored mesh");
   if(Mathf.Abs(b.min.y-tabletop.max.y-.002f)>.001f)throw new Exception(prop.name+" has an invalid support height");
  }
  static void InstallSpectacles(FlatBindings flat){
   var table=flat.apartment.GetComponentsInChildren<Transform>().Single(t=>t.name=="TEMP Kenney nightstand at headboard");
   Remove(flat.apartment,"Ready CC0 bedside spectacles06");
   string dir=Art+"ReadyModels/Glasses/";
   var glasses=Ready(dir+"BedsideGlasses.fbx",flat.apartment,"Ready CC0 bedside spectacles06");glasses.transform.rotation=Quaternion.Euler(0,90,0);
   PlaceOnTable(glasses,new Vector2(1.22f,1.105f),Measure(table.gameObject));
   var m=Lit("ReadyBedsideSpectacles",Color.white,.35f);m.SetTexture("_BaseMap",Texture(dir+"Glasses-leopard.png"));
   m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.4f);m.SetFloat("_Cull",0);m.EnableKeyword("_ALPHATEST_ON");m.SetOverrideTag("RenderType","TransparentCutout");m.renderQueue=2450;
   foreach(var r in glasses.GetComponentsInChildren<Renderer>())r.sharedMaterial=m;
   var clock=flat.apartment.Find("Ready Poly Haven bedside clock06");
   if(clock&&Measure(clock.gameObject).Intersects(Measure(glasses)))throw new Exception("Clock and spectacles overlap; keep both at their authored scale");
   EditorUtility.SetDirty(m);
  }
  // Explicit staging operation only: no InitializeOnLoad, no automatic build or player launch.
  [MenuItem("Ataka/Bedroom06/Apply prepared corner and bedside clock")]
  public static void ApplyPreparedArt(){
   System.IO.Directory.CreateDirectory(Output);AssetDatabase.Refresh();
   var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();
   InstallCorner(flat);InstallClock(flat);InstallSpectacles(flat);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Debug.Log("BEDROOM06 Prepared corner, clock and spectacles installed; visual review, shader compilation and gameplay checks still required. Water glass not installed.");
  }

 }
}
