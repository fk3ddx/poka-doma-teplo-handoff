using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace Ataka.Editor {
 public static partial class ApartmentArtBuilder {
  public static void RefineCourtyardScene(){
   EditorSceneManager.OpenScene(ScenePath);ImportTextures();mats=AssetDatabase.FindAssets("t:Material",new[]{Root+"/Art/M1Materials"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>).ToDictionary(x=>x.name,x=>x);art=GameObject.Find("M1 authored apartment").transform;CourtyardFinish();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();Debug.Log("M1_COURTYARD_REFINED");
  }
  static void CourtyardFinish(){
   var priorPrefix=shellPrefix;int priorIndex=shellIndex;shellPrefix="courtyard_";shellIndex=0;
   foreach(var t in art.GetComponentsInChildren<Transform>().Where(x=>x.name=="Wet courtyard"||x.name=="Dirty snowbank"||x.name=="Courtyard paving tile"||x.name=="Weathered pavement"||x.name=="Kerb"||x.name=="Soft snow edge").ToArray())Object.DestroyImmediate(t.gameObject);
   Pbr(M("Road",C(.42f,.45f,.47f),.4f),"asphalt_02",.55f);Pbr(M("Snow",C(.64f,.67f,.67f),.06f),"asphalt_snow",1);
   for(int x=0;x<14;x++)for(int z=0;z<11;z++){
    var tile=Box("Courtyard paving tile",new Vector3(-61+x*10,-Floor-.1f,-81+z*10),new Vector3(10,.2f,10),"Road",false);Reproject(tile.GetComponent<MeshRenderer>());
   }
   var pavement=Box("Weathered pavement",new Vector3(-6,-Floor+.06f,-27.2f),new Vector3(42,.12f,2.2f),"Concrete",false);Reproject(pavement.GetComponent<MeshRenderer>());
   for(int i=0;i<35;i++)Box("Kerb",new Vector3(-26.4f+i*1.2f,-Floor+.09f,-26.1f),new Vector3(1.18f,.18f,.15f),"Concrete",false);
   var rng=new System.Random(79);
   for(int i=0;i<20;i++){
    var snow=GameObject.CreatePrimitive(PrimitiveType.Sphere);snow.name="Soft snow edge";snow.transform.SetParent(art,false);snow.transform.localPosition=new Vector3(-27+i*2.3f,-Floor+.03f,-25.7f+(float)rng.NextDouble()*.4f);snow.transform.localScale=new Vector3(1.6f+(float)rng.NextDouble()*1.4f,.18f+(float)rng.NextDouble()*.13f,.6f+(float)rng.NextDouble()*.5f);snow.GetComponent<Renderer>().sharedMaterial=mats["Snow"];Object.DestroyImmediate(snow.GetComponent<Collider>());
   }
   foreach(var light in art.GetComponentsInChildren<Light>().Where(x=>x.name=="Courtyard pool")){light.type=LightType.Spot;light.transform.localRotation=Quaternion.Euler(90,0,0);light.spotAngle=125;light.innerSpotAngle=65;light.intensity=38;light.range=22;}
   var window=mats["WindowWarm"];window.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;window.SetColor("_EmissionColor",C(1.8f,1.0f,.35f));window.EnableKeyword("_EMISSION");EditorUtility.SetDirty(window);
   var sodium=M("SodiumGlass",C(.58f,.39f,.15f));sodium.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;sodium.SetColor("_EmissionColor",C(4.0f,2.1f,.6f));sodium.EnableKeyword("_EMISSION");EditorUtility.SetDirty(sodium);
   foreach(var fixture in art.GetComponentsInChildren<Renderer>().Where(x=>x.name=="Sodium lamp"))fixture.sharedMaterial=sodium;
   foreach(var old in art.GetComponentsInChildren<Transform>().Where(x=>x.name=="Entrance warm pool"||x.name=="Entrance light glass"||x.name.StartsWith("Entrance door")).ToArray())Object.DestroyImmediate(old.gameObject);
   foreach(float x in new[]{-16f,-4f,8f}){
    Box("Entrance door frame",new Vector3(x,1.1f-Floor,-28.44f),new Vector3(1.45f,2.2f,.10f),"Concrete",false);
    Box("Entrance door leaf",new Vector3(x,1.03f-Floor,-28.37f),new Vector3(1.18f,2.06f,.06f),"KitchenPaint",false);
    Box("Entrance door inset",new Vector3(x,1.03f-Floor,-28.326f),new Vector3(1.01f,1.84f,.028f),"Metal",false);
    Box("Entrance door handle",new Vector3(x+.43f,1.1f-Floor,-28.285f),new Vector3(.035f,.27f,.04f),"Black",false);
    var entrance=Point("Entrance warm pool",new Vector3(x,2.7f-Floor,-27.3f),C(1,.65f,.35f),8,7,false);entrance.type=LightType.Spot;entrance.spotAngle=115;entrance.innerSpotAngle=50;entrance.transform.localRotation=Quaternion.Euler(78,0,0);
    Box("Entrance light glass",new Vector3(x,2.8f-Floor,-27.6f),new Vector3(.34f,.12f,.12f),"SodiumGlass",false);
   }
   var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"/Settings/M0_URP.asset");var p=new SerializedObject(pipeline);p.FindProperty("m_AdditionalLightShadowsSupported").boolValue=true;p.FindProperty("m_SoftShadowsSupported").boolValue=true;p.FindProperty("m_AdditionalLightsShadowmapResolution").intValue=4096;p.FindProperty("m_AdditionalLightsPerObjectLimit").intValue=8;p.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);
   var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root+"/Settings/M0_Renderer.asset");var r=new SerializedObject(renderer);r.FindProperty("m_RenderingMode").intValue=(int)RenderingMode.ForwardPlus;r.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(renderer);
   foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(x=>x.type==LightType.Point&&x.shadows!=LightShadows.None)){
    var data=light.GetComponent<UniversalAdditionalLightData>();if(data==null)data=light.gameObject.AddComponent<UniversalAdditionalLightData>();var so=new SerializedObject(data);so.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=1;so.ApplyModifiedPropertiesWithoutUndo();
   }
   foreach(var shade in art.GetComponentsInChildren<Renderer>().Where(x=>x.name=="Living_Shade"||x.name=="Living_Bulb"||x.name=="Living_ShadeBinding"))shade.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   shellPrefix=priorPrefix;shellIndex=priorIndex;
  }
 }
}
