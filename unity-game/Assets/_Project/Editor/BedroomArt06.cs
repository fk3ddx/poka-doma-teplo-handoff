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
  static GameObject ReadyMeasured(string path,Transform parent,string name,float authorHeight){
   var importer=AssetImporter.GetAtPath(path) as ModelImporter;if(!importer)throw new Exception("Missing ModelImporter "+path);
   // Preserve proportions. Convert file units to the independent Blender measurement, not to a furniture footprint.
   importer.globalScale=1;importer.useFileScale=true;importer.SaveAndReimport();
   var obj=Ready(path,parent,name);float observed=Measure(obj).size.y;
   if(observed<.000001f)throw new Exception("Invalid imported mesh height "+name);
   if(Mathf.Abs(observed-authorHeight)>.0001f){
    UnityEngine.Object.DestroyImmediate(obj);importer.globalScale=authorHeight/observed;importer.SaveAndReimport();obj=Ready(path,parent,name);
   }
   float actual=Measure(obj).size.y;if(Mathf.Abs(actual-authorHeight)>.001f)throw new Exception("Unit conversion failed for "+name+": "+actual);
   Debug.Log("BEDROOM06_IMPORT_UNITS "+name+" before="+observed+" after="+actual+" globalScale="+importer.globalScale);return obj;
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
   var lamp=ReadyMeasured(Art+"BedroomDecor/Lampada06/Lampada06.fbx",root,"Approved lampada06",.281f);lamp.transform.position=new Vector3(.36f,1.75f,.36f);
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
   var clock=ReadyMeasured(Art+"CC0/alarm_clock_01/alarm_clock_01_rigid.fbx",flat.apartment,"Ready Poly Haven bedside clock06",.1741554f);clock.transform.rotation=Quaternion.Euler(0,90,0)*clock.transform.rotation;
   PrefabUtility.UnpackPrefabInstance(clock,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   // Preserve the measured author scale: 13.2 cm wide, 17.4 cm tall.
   PlaceOnTable(clock,new Vector2(1.18f,.88f),tabletop);
   if(Mathf.Abs(Measure(clock).size.y-.1741554f)>.01f)throw new Exception("Ready clock is not upright or has an unexpected import scale");
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
  static void PlaceOnTable(GameObject prop,Vector2 center,Bounds tabletop,float supportGap=.002f){
   var b=Measure(prop);prop.transform.position+=new Vector3(center.x-b.center.x,tabletop.max.y+supportGap-b.min.y,center.y-b.center.z);
   b=Measure(prop);const float margin=.01f;
   if(b.min.x<tabletop.min.x+margin||b.max.x>tabletop.max.x-margin||b.min.z<tabletop.min.z+margin||b.max.z>tabletop.max.z-margin)
    throw new Exception(prop.name+" does not fit the bedside surface with a 1cm edge margin; prop="+b+" table="+tabletop+"; do not squash the authored mesh");
   if(Mathf.Abs(b.min.y-tabletop.max.y-supportGap)>.001f)throw new Exception(prop.name+" has an invalid support height");
  }
  static void InstallSpectacles(FlatBindings flat){
   var table=flat.apartment.GetComponentsInChildren<Transform>().Single(t=>t.name=="TEMP Kenney nightstand at headboard");
   Remove(flat.apartment,"Ready CC0 bedside spectacles06");
   string dir=Art+"ReadyModels/Glasses/";
   var glasses=ReadyMeasured(dir+"BedsideGlasses.fbx",flat.apartment,"Ready CC0 bedside spectacles06",.0411893f);glasses.transform.rotation=Quaternion.Euler(0,90,0)*glasses.transform.rotation;
   PlaceOnTable(glasses,new Vector2(1.22f,1.105f),Measure(table.gameObject));
   if(Mathf.Abs(Measure(glasses).size.y-.0411893f)>.005f)throw new Exception("Ready spectacles have unexpected orientation/scale: "+Measure(glasses));
   var m=Lit("ReadyBedsideSpectacles",Color.white,.35f);m.SetTexture("_BaseMap",Texture(dir+"Glasses-leopard.png"));
   m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.4f);m.SetFloat("_Cull",0);m.EnableKeyword("_ALPHATEST_ON");m.SetOverrideTag("RenderType","TransparentCutout");m.renderQueue=2450;
   foreach(var r in glasses.GetComponentsInChildren<Renderer>())r.sharedMaterial=m;
   var clock=flat.apartment.Find("Ready Poly Haven bedside clock06");
   if(clock&&Measure(clock.gameObject).Intersects(Measure(glasses)))throw new Exception("Clock and spectacles overlap; keep both at their authored scale");
   EditorUtility.SetDirty(m);
  }
  static void InstallSmallProps(FlatBindings flat){
   var table=flat.apartment.GetComponentsInChildren<Transform>().Single(t=>t.name=="TEMP Kenney nightstand at headboard");var surface=Measure(table.gameObject);
   const string glassName="Ready CC0 water glass06";Remove(flat.apartment,glassName);
   var glass=ReadyMeasured(Art+"ReadyModels/WaterGlass/BedsideWaterGlass.fbx",flat.apartment,glassName,.1f);
   PrefabUtility.UnpackPrefabInstance(glass,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   PlaceOnTable(glass,new Vector2(1.32f,.89f),surface,.0005f);
   var gm=Lit("ReadyDrinkingGlass",new Color(.80f,.88f,.91f,.045f),.72f);Transparent(gm);gm.SetFloat("_Metallic",0);
   var wm=Lit("ReadyDrinkingWater",new Color(.78f,.87f,.91f,.02f),.88f);Transparent(wm);wm.SetFloat("_Metallic",0);
   foreach(var renderer in glass.GetComponentsInChildren<Renderer>()){
    renderer.sharedMaterial=renderer.name.Contains("Water volume")?wm:gm;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   }
   var cream=Lit("ReadyCapsuleCream",new Color(.80f,.74f,.65f),.28f);var red=Lit("ReadyCapsuleRed",new Color(.55f,.035f,.025f),.30f);
   var props=new System.Collections.Generic.List<GameObject>{glass};
   for(int i=0;i<2;i++){
    string name="Ready CC0 bedside capsule06 "+i;Remove(flat.apartment,name);
    var pill=ReadyMeasured(Art+"ReadyModels/BedsidePills/ReadyCapsule.fbx",flat.apartment,name,.00671369f);
    // A second import of this same source must not revert the first instance's name/material overrides.
    PrefabUtility.UnpackPrefabInstance(pill,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
    pill.transform.rotation=Quaternion.Euler(0,i==0?18:-27,0)*pill.transform.rotation;
    PlaceOnTable(pill,i==0?new Vector2(1.313f,1.07f):new Vector2(1.345f,1.099f),surface,.0001f);
    foreach(var renderer in pill.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m&&m.name.Contains("002")?red:cream).ToArray();
    props.Add(pill);
   }
   foreach(string name in new[]{"Ready Poly Haven bedside clock06","Ready CC0 bedside spectacles06"}){
    var other=flat.apartment.Find(name);if(!other)throw new Exception("Expected existing bedside prop "+name);props.Add(other.gameObject);
   }
   for(int i=0;i<props.Count;i++)for(int j=i+1;j<props.Count;j++)if(Measure(props[i]).Intersects(Measure(props[j])))throw new Exception("Bedside overlap: "+props[i].name+" / "+props[j].name);
   foreach(var mat in new[]{gm,wm,cream,red})EditorUtility.SetDirty(mat);
  }
  [MenuItem("Ataka/Bedroom06/Apply ready water glass and capsules only")]
  public static void ApplyBedsideSmallProps(){
   System.IO.Directory.CreateDirectory(Output);AssetDatabase.Refresh();
   var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();
   InstallSmallProps(flat);WriteGeometryReport(flat);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Debug.Log("BEDROOM06 Ready glass and capsules installed; measured support/overlap only, URP appearance still unverified.");
  }
  [Serializable] class PlacedProp {public string name;public Vector3 min,max,size;}
  [Serializable] class PreparedArtReport {public string scope="Scene geometry only; not rendered or visually approved";public PlacedProp[] props;public float iconIntensity,iconInnerAngle,haloIntensity;}
  static void WriteGeometryReport(FlatBindings flat){
   string[] names={"TEMP Kenney nightstand at headboard","Ready Poly Haven bedside clock06","Ready CC0 bedside spectacles06","Corner icon shelf","Approved lampada06","Ready CC0 water glass06","Ready CC0 bedside capsule06 0","Ready CC0 bedside capsule06 1"};
   var all=flat.apartment.GetComponentsInChildren<Transform>();var report=new PreparedArtReport();
   report.props=names.Select(name=>{var matches=all.Where(v=>v.name==name).ToArray();if(matches.Length!=1)throw new Exception("Geometry report expected one "+name+", found "+matches.Length+". Bedside names: "+string.Join(" | ",all.Select(v=>v.name).Where(n=>n.Contains("capsule")||n.Contains("Capsule")||n.Contains("glass"))));var t=matches[0];var b=Measure(t.gameObject);return new PlacedProp{name=name,min=b.min,max=b.max,size=b.size};}).ToArray();
   report.iconIntensity=flat.look.icon.intensity;report.iconInnerAngle=flat.look.icon.innerSpotAngle;report.haloIntensity=all.Single(v=>v.name=="Icon glass halo").GetComponent<Light>().intensity;
   System.IO.Directory.CreateDirectory("QA/Bedroom06Placement");System.IO.File.WriteAllText("QA/Bedroom06Placement/geometry.json",JsonUtility.ToJson(report,true));
  }
  // Explicit staging operation only: no InitializeOnLoad, no automatic build or player launch.
  [MenuItem("Ataka/Bedroom06/Apply prepared corner and bedside clock")]
  public static void ApplyPreparedArt(){
   System.IO.Directory.CreateDirectory(Output);AssetDatabase.Refresh();
   var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();
   InstallCorner(flat);InstallClock(flat);InstallSpectacles(flat);InstallSmallProps(flat);WriteGeometryReport(flat);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Debug.Log("BEDROOM06 Prepared corner, clock and spectacles installed; visual review, shader compilation and gameplay checks still required. Ready glass/capsules included; no visual acceptance.");
  }

 }
}
