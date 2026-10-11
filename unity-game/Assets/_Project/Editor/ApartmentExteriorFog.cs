using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Ataka.Editor {
 public static partial class ApartmentArtBuilder {
  public static void InstallExteriorFog(){
   EditorSceneManager.OpenScene(ScenePath);art=GameObject.Find("M1 authored apartment").transform;
   ExteriorFog();PlayerSettings.bundleVersion="0.2.9-dev";
   EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
   Debug.Log("M1_EXTERIOR_FOG_INSTALLED");
  }
  static void ExteriorFog(){
   foreach(var prior in art.GetComponentsInChildren<Ataka.LocalLightFog>(true))Object.DestroyImmediate(prior.gameObject);
   const string materialPath=Root+"/Art/M1Materials/CourtyardLightFog.mat";
   var shader=Shader.Find("Ataka/CourtyardLightFog");if(shader==null)throw new System.InvalidOperationException("CourtyardLightFog shader missing");
   var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
   if(material==null){material=new Material(shader){name="CourtyardLightFog"};AssetDatabase.CreateAsset(material,materialPath);}
   material.shader=shader;material.enableInstancing=false;EditorUtility.SetDirty(material);
   foreach(var light in art.GetComponentsInChildren<Light>().Where(x=>x.name=="Courtyard pool"||x.name=="Entrance warm pool")){
    bool entrance=light.name=="Entrance warm pool";
    var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Local sodium scattering "+light.name;go.transform.SetParent(art,false);go.layer=29;
    Object.DestroyImmediate(go.GetComponent<Collider>());
    var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
    var fog=go.AddComponent<Ataka.LocalLightFog>();fog.sourceLight=light;fog.radius=entrance?2.4f:5.8f;fog.density=entrance?.13f:.12f;fog.gain=entrance?1.1f:1.6f;fog.referenceIntensity=light.intensity;fog.Refresh();
   }
   var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"/Settings/M0_URP.asset");pipeline.supportsCameraDepthTexture=true;EditorUtility.SetDirty(pipeline);
   // Baked room cubemaps do not have the depth texture needed by local integration.
   // Dynamic planar mirrors use their own depth texture and include these volumes.
   foreach(var probe in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None))probe.cullingMask&=~(1<<29);
  }
 }
}
