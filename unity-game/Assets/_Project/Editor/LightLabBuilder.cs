using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.ProBuilder;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using Ataka.LightLab;
namespace Ataka.Editor {
 public static class LightLabBuilder {
  const string Root="Assets/_Project/LightLab", Art="Assets/_Project/Art",Scene=Root+"/LightLab.unity";
  static Material wall,wood,floor,linen;static Transform shell;
  static T Save<T>(T obj,string name) where T:UnityEngine.Object {string p=Root+"/"+name;var old=AssetDatabase.LoadAssetAtPath<T>(p);if(old){EditorUtility.CopySerialized(obj,old);UnityEngine.Object.DestroyImmediate(obj);EditorUtility.SetDirty(old);return old;}AssetDatabase.CreateAsset(obj,p);return obj;}
  static Material Mat(string name,string set,float smooth=.18f){
   var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/CC0/"+set+"/Diffuse.jpg"));m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/CC0/"+set+"/nor_gl.jpg"));m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.45f);m.SetFloat("_Smoothness",smooth);return Save(m,name+".mat");
  }
  static GameObject Box(string name,Vector3 p,Vector3 size,Material m){var pb=ShapeGenerator.GenerateCube(PivotLocation.Center,size);var g=pb.gameObject;g.name=name;g.transform.SetParent(shell);g.transform.position=p;g.GetComponent<Renderer>().sharedMaterial=m;if(!g.GetComponent<Collider>())g.AddComponent<BoxCollider>();return g;}
  static GameObject Cylinder(string n,Vector3 p,Vector3 s,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=n;g.transform.position=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=m;return g;}
  static Light Lamp(string name,Vector3 pos,LightType type,Color c,float range,bool shadows){var g=new GameObject(name);g.transform.position=pos;var l=g.AddComponent<Light>();l.type=type;l.color=c;l.range=range;l.intensity=0;l.shadows=shadows?LightShadows.Soft:LightShadows.None;l.shadowBias=.035f;l.shadowNormalBias=.15f;l.shadowResolution=LightShadowResolution.High;return l;}
  static GameObject Prop(string id,Vector3 position,float height,float yaw){
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/CC0/"+id+"/fbx.fbx");if(!source)throw new Exception("Missing CC0 "+id);
   var obj=(GameObject)PrefabUtility.InstantiatePrefab(source);obj.name="CC0 "+id;obj.transform.rotation=Quaternion.Euler(0,yaw,0)*obj.transform.rotation;
   var rs=obj.GetComponentsInChildren<Renderer>();Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   obj.transform.localScale*=height/b.size.y;
   b=rs[0].bounds;foreach(var r in rs){b.Encapsulate(r.bounds);r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/M1Materials/"+id+".mat");}
   obj.transform.position+=position-new Vector3(b.center.x,b.min.y,b.center.z);
   foreach(var mf in obj.GetComponentsInChildren<MeshFilter>()){var col=mf.gameObject.AddComponent<MeshCollider>();col.sharedMesh=mf.sharedMesh;}
   return obj;
  }
  [MenuItem("Ataka/LightLab/Generate and build")]
  public static void GenerateAndBuild(){Generate();Build();}
  public static void Generate(){
   Directory.CreateDirectory(Root);AssetDatabase.Refresh();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var renderer=Save(UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/_Project/Settings/M0_Renderer.asset")),"LightLab_Renderer.asset");
   // Clone each renderer feature so neither settings nor material affect the apartment renderer.
   var originals=renderer.rendererFeatures.ToArray();renderer.rendererFeatures.Clear();
   var vhs=Save(new Material(Shader.Find("Ataka/LightLabVHS")),"LightLab_VHS.mat");
   foreach(var original in originals){var f=UnityEngine.Object.Instantiate(original);AssetDatabase.AddObjectToAsset(f,renderer);
    if(f is FullScreenPassRendererFeature full){full.passMaterial=vhs;full.injectionPoint=FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;full.requirements=ScriptableRenderPassInput.Color;}
    else {var settings=new SerializedObject(f);settings.FindProperty("m_Settings.Intensity").floatValue=1;settings.FindProperty("m_Settings.Radius").floatValue=.35f;settings.FindProperty("m_Settings.DirectLightingStrength").floatValue=.25f;settings.ApplyModifiedPropertiesWithoutUndo();}
    renderer.rendererFeatures.Add(f);f.Create();}
   var rd=new SerializedObject(renderer);rd.FindProperty("m_RenderingMode").intValue=2;rd.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(renderer);
   var pipe=Save(UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/_Project/Settings/M0_URP.asset")),"LightLab_URP.asset");
   var ps=new SerializedObject(pipe);ps.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;ps.ApplyModifiedPropertiesWithoutUndo();
   pipe.renderScale=2f/3;pipe.upscalingFilter=UpscalingFilterSelection.Linear;pipe.msaaSampleCount=1;pipe.supportsHDR=true;pipe.shadowDistance=22;pipe.additionalLightsShadowmapResolution=4096;EditorUtility.SetDirty(pipe);
   wall=Mat("Wall_plaster","plastered_wall_02");wood=Mat("Wood","dark_wood");floor=Mat("Parquet","herringbone_parquet");linen=Mat("Linen","waffle_pique_cotton");
   shell=new GameObject("ProBuilder shell — 4 x 5 m, height 2.5 m").transform;
   Box("Floor",new Vector3(2.6f,-.1f,2.5f),new Vector3(5.6f,.2f,5.4f),floor);Box("Ceiling",new Vector3(2.6f,2.6f,2.5f),new Vector3(5.6f,.2f,5.4f),wall);
   Box("West",new Vector3(-.1f,1.25f,2.5f),new Vector3(.2f,2.7f,5.4f),wall);Box("South",new Vector3(2.6f,1.25f,-.1f),new Vector3(5.6f,2.7f,.2f),wall);
   Box("Hall east",new Vector3(5.3f,1.25f,2.5f),new Vector3(.2f,2.7f,5.4f),wall);Box("Hall end",new Vector3(4.65f,1.25f,5.1f),new Vector3(1.5f,2.7f,.2f),wall);
   Box("Partition front",new Vector3(4.1f,1.25f,.45f),new Vector3(.2f,2.7f,1.1f),wall);Box("Partition rear",new Vector3(4.1f,1.25f,3.55f),new Vector3(.2f,2.7f,3.1f),wall);
   Box("Door lintel",new Vector3(4.1f,2.32f,1.5f),new Vector3(.2f,.56f,1.03f),wood);
   Box("Window left",new Vector3(.45f,1.25f,5.1f),new Vector3(1.1f,2.7f,.2f),wall);Box("Window right",new Vector3(3.55f,1.25f,5.1f),new Vector3(1.1f,2.7f,.2f),wall);
   Box("Window below",new Vector3(2,.44f,5.1f),new Vector3(2.1f,.88f,.2f),wall);Box("Window above",new Vector3(2,2.43f,5.1f),new Vector3(2.1f,.34f,.2f),wall);
   foreach(float x in new[]{1f,2f,3f})Box("Window vertical frame",new Vector3(x,1.58f,5),new Vector3(.06f,1.45f,.16f),wood);
   foreach(float y in new[]{.89f,1.85f,2.29f})Box("Window horizontal frame",new Vector3(2,y,5),new Vector3(2.06f,.05f,.16f),wood);
   Box("Sill",new Vector3(2,.86f,4.94f),new Vector3(2.2f,.08f,.42f),wood);
   var windowBlock=new GameObject("Window collision");windowBlock.transform.position=new Vector3(2,1.6f,5);windowBlock.AddComponent<BoxCollider>().size=new Vector3(2,1.5f,.04f);
   Box("Table lighting proxy",new Vector3(1.8f,.7f,2.5f),new Vector3(.95f,.09f,.65f),wood);
   foreach(float x in new[]{1.4f,2.2f})foreach(float z in new[]{2.24f,2.76f})Box("Table leg",new Vector3(x,.34f,z),new Vector3(.05f,.68f,.05f),wood);
   Box("TV stand proxy",new Vector3(.55f,.3f,.85f),new Vector3(.9f,.6f,.6f),wood);
   Prop("mid_century_lounge_chair",new Vector3(.9f,0,3.65f),.85f,155);Prop("mid_century_lounge_chair",new Vector3(2.8f,0,3.75f),.85f,205);
   Prop("ceramic_vase_01",new Vector3(1.5f,.746f,2.5f),.25f,0);Prop("ceramic_vase_01",new Vector3(2.65f,.903f,4.91f),.21f,35);
   var tv=new GameObject("CC0 Television_01");tv.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/M1Meshes/Living_Television_01.asset");tv.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/M1Materials/Television_01.mat");
   var bounds=tv.GetComponent<Renderer>().bounds;tv.transform.position=new Vector3(.55f,.6f,.85f)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);tv.AddComponent<BoxCollider>();
   var warm=new Color(1,180/255f,107/255f);var bounce=new Color(.64f,.39f,.22f);
   var main=Lamp("Ceiling practical",new Vector3(2,2.18f,2.6f),LightType.Point,warm,4.5f,true);
   var mainBounce=Lamp("Ceiling bounce",new Vector3(2,1.2f,2.6f),LightType.Point,bounce,8,false);
   var spot=Lamp("Floor lamp down",new Vector3(.65f,1.35f,3.2f),LightType.Spot,warm,4.5f,true);spot.transform.rotation=Quaternion.Euler(90,0,0);spot.spotAngle=110;spot.innerSpotAngle=55;
   var glow=Lamp("Floor shade glow",new Vector3(.65f,1.62f,3.2f),LightType.Point,warm,2.5f,true);
   var lampBounce=Lamp("Floor bounce",new Vector3(.65f,.75f,3.2f),LightType.Point,bounce,6,false);
   var shadeMat=new Material(linen);shadeMat.EnableKeyword("_EMISSION");shadeMat=Save(shadeMat,"Shade.mat");
   var ceilingShade=Cylinder("Ceiling diffuser",new Vector3(2,2.41f,2.6f),new Vector3(.38f,.065f,.38f),shadeMat);ceilingShade.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
   Cylinder("Floor lamp base",new Vector3(.65f,.025f,3.2f),new Vector3(.3f,.025f,.3f),wood);Cylinder("Floor lamp stem",new Vector3(.65f,.78f,3.2f),new Vector3(.025f,.78f,.025f),wood);
   var shade=Cylinder("Floor lampshade",new Vector3(.65f,1.64f,3.2f),new Vector3(.42f,.16f,.42f),shadeMat);shade.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
   var cold=Lamp("Window cold cookie",new Vector3(2,2.05f,5.55f),LightType.Spot,new Color(.56f,.65f,.78f),11,true);cold.transform.LookAt(new Vector3(1.5f,.5f,1));cold.spotAngle=96;cold.innerSpotAngle=75;
   var cookie=new Texture2D(256,256,TextureFormat.RGBA32,false);cookie.wrapMode=TextureWrapMode.Clamp;cookie.filterMode=FilterMode.Bilinear;
   for(int y=0;y<256;y++)for(int x=0;x<256;x++){float a=(x<8||x>247||y<8||y>247||Mathf.Abs(x-128)<4||Mathf.Abs(y-158)<4)?0:1;cookie.SetPixel(x,y,new Color(a,a,a,a));}cookie.Apply();cold.cookie=Save(cookie,"WindowCookie.asset");
   var tvlight=Lamp("Television glow",new Vector3(.55f,.95f,1.25f),LightType.Point,new Color(.4f,.57f,.85f),2.8f,false);
   var profile=ScriptableObject.CreateInstance<VolumeProfile>();
   profile.Add<Tonemapping>(true).mode.value=TonemappingMode.Neutral;
   var ca=profile.Add<ColorAdjustments>(true);ca.postExposure.value=-1;ca.saturation.value=-15;ca.contrast.value=7;
   profile.Add<WhiteBalance>(true).temperature.value=10;
   var bloom=profile.Add<Bloom>(true);bloom.threshold.value=1;bloom.intensity.value=.55f;bloom.scatter.value=.65f;
   var vig=profile.Add<Vignette>(true);vig.intensity.value=.28f;vig.smoothness.value=.4f;
   var volume=new GameObject("Global grading — no grain or CA").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=Save(profile,"LightLabVolume.asset");
   foreach(var component in volume.sharedProfile.components)if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,volume.sharedProfile);
   RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(12/255f,14/255f,20/255f);RenderSettings.reflectionIntensity=.25f;RenderSettings.fog=false;
   var probes=new ReflectionProbe[2];for(int i=0;i<2;i++){var probe=new GameObject("Room reflection "+i).AddComponent<ReflectionProbe>();probe.transform.position=new Vector3(i==0?2:4.65f,1.2f,2.5f);probe.size=new Vector3(i==0?4:1.3f,2.5f,5);probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.AllFacesAtOnce;probe.resolution=128;probe.boxProjection=true;probe.intensity=.25f;probes[i]=probe;}
   var actor=new GameObject("Player");actor.layer=2;actor.transform.position=new Vector3(3.4f,.02f,.7f);actor.transform.rotation=Quaternion.Euler(0,-35,0);
   var body=actor.AddComponent<CharacterController>();body.height=1.7f;body.radius=.23f;body.center=Vector3.up*.85f;body.skinWidth=.02f;body.stepOffset=.1f;
   actor.AddComponent<PlayerControls>();var player=actor.AddComponent<FirstPersonController>();
   var eye=new GameObject("Camera");eye.tag="MainCamera";eye.transform.SetParent(actor.transform,false);eye.transform.localPosition=Vector3.up*1.55f;var cam=eye.AddComponent<Camera>();cam.nearClipPlane=.05f;cam.farClipPlane=80;cam.fieldOfView=65;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.018f,.024f,.038f);cam.allowHDR=true;cam.GetUniversalAdditionalCameraData().renderPostProcessing=true;cam.GetUniversalAdditionalCameraData().antialiasing=AntialiasingMode.None;eye.AddComponent<AudioListener>();player.view=cam;
   var lab=new GameObject("LightLab session").AddComponent<LightLabSession>();lab.pipeline=pipe;lab.player=player;lab.vhs=vhs;lab.volume=volume;lab.chandelier=new[]{main,mainBounce};lab.floorLamp=new[]{spot,glow,lampBounce};lab.windowLight=cold;lab.tvLight=tvlight;lab.chandelierShade=ceilingShade.GetComponent<Renderer>();lab.lampShade=shade.GetComponent<Renderer>();lab.probes=probes;
   lab.profiles=new VhsProfile[4];string[] profileNames={"Normal","Tension","Scare","Rewind"};for(int i=0;i<4;i++){var p=ScriptableObject.CreateInstance<VhsProfile>();if(i>0){p.ghostWeight=.14f;p.chromaBlur=12;p.jitter=.8f;p.grain=.05f;}if(i>1)p.disruption=i==2?1:.7f;lab.profiles[i]=Save(p,"VhsProfile_"+profileNames[i]+".asset");}
   Vector3[] positions={new Vector3(3.4f,1.55f,.7f),new Vector3(2.8f,1.55f,2.3f),new Vector3(1.6f,1.55f,3.1f),new Vector3(4.65f,1.55f,.5f),new Vector3(3.6f,1.55f,4.25f),new Vector3(.6f,1.55f,1.9f)};
   Vector3[] angles={new Vector3(4,-35,0),new Vector3(10,-85,0),new Vector3(2,145,0),new Vector3(1,0,0),new Vector3(5,205,0),new Vector3(2,42,0)};
   lab.viewpoints=new Transform[6];for(int i=0;i<6;i++){var pose=new GameObject("Capture view "+(i+1)).transform;pose.position=positions[i];pose.eulerAngles=angles[i];lab.viewpoints[i]=pose;}
   var variants=new ShaderVariantCollection();variants.Add(new ShaderVariantCollection.ShaderVariant(vhs.shader,PassType.Normal));lab.variants=Save(variants,"LightLabWarmup.shadervariants");
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Scene);Debug.Log("LIGHTLAB_GENERATED");
  }
  public static void Build(){
   var old=GraphicsSettings.defaultRenderPipeline;var oldQ=QualitySettings.renderPipeline;var oldAPI=PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);bool oldAuto=PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64);
   var oldName=PlayerSettings.productName;var oldVersion=PlayerSettings.bundleVersion;var oldMode=PlayerSettings.fullScreenMode;int oldW=PlayerSettings.defaultScreenWidth,oldH=PlayerSettings.defaultScreenHeight;
   try{var pipe=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"/LightLab_URP.asset");GraphicsSettings.defaultRenderPipeline=pipe;QualitySettings.renderPipeline=pipe;PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
    PlayerSettings.productName="LightLab 0.1.1 — A-B";PlayerSettings.bundleVersion="0.1.1";PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
    Directory.CreateDirectory("Builds/LightLab011");var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/LightLab011/LightLab.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});if(result.summary.result!=BuildResult.Succeeded)throw new Exception("LightLab build failed");Debug.Log("LIGHTLAB_BUILD_SUCCESS");
   }finally{GraphicsSettings.defaultRenderPipeline=old;QualitySettings.renderPipeline=oldQ;PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,oldAPI);PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,oldAuto);PlayerSettings.productName=oldName;PlayerSettings.bundleVersion=oldVersion;PlayerSettings.fullScreenMode=oldMode;PlayerSettings.defaultScreenWidth=oldW;PlayerSettings.defaultScreenHeight=oldH;AssetDatabase.SaveAssets();}
  }
 }
}
