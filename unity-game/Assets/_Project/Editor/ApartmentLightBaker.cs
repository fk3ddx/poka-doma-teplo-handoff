using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace Ataka.Editor {
 public static class ApartmentLightBaker {
  const string Root="Assets/_Project";
  public static void Bake(){
   EditorSceneManager.OpenScene(ApartmentArtBuilder.ScenePath);
   var settings=new LightingSettings();settings.name="M1 apartment GI";settings.bakedGI=true;settings.realtimeGI=false;settings.lightmapper=LightingSettings.Lightmapper.ProgressiveGPU;settings.lightmapResolution=20;settings.lightmapMaxSize=1024;settings.directSampleCount=32;settings.indirectSampleCount=128;settings.environmentSampleCount=64;settings.maxBounces=3;settings.mixedBakeMode=MixedLightingMode.IndirectOnly;settings.directionalityMode=LightmapsMode.NonDirectional;
   string settingsPath=Root+"/Settings/M1_Lighting.asset";var old=AssetDatabase.LoadAssetAtPath<LightingSettings>(settingsPath);if(old==null)AssetDatabase.CreateAsset(settings,settingsPath);else {EditorUtility.CopySerialized(settings,old);UnityEngine.Object.DestroyImmediate(settings);settings=old;}Lightmapping.lightingSettings=settings;
   var controller=UnityEngine.Object.FindFirstObjectByType<ApartmentLightingStates>();if(controller==null)controller=new GameObject("Baked apartment power states").AddComponent<ApartmentLightingStates>();
   controller.surfaces=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.InstanceID).Where(x=>x.gameObject.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene() && GameObjectUtility.AreStaticEditorFlagsSet(x.gameObject,StaticEditorFlags.ContributeGI)).Cast<Renderer>().ToArray();
   foreach(var r in controller.surfaces)GameObjectUtility.SetStaticEditorFlags(r.gameObject,GameObjectUtility.GetStaticEditorFlags(r.gameObject)&~StaticEditorFlags.BatchingStatic);controller.switches=UnityEngine.Object.FindObjectsByType<LampSwitch>(FindObjectsSortMode.None);
   controller.supplementary=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(x=>x.name=="Floor lamp warm pool"||x.name=="CRT bounce").ToArray();
   controller.glowingSurfaces=controller.surfaces.Where(x=>x.name=="Living_Shade"||x.name=="Living_Bulb"||x.name=="Living_TVScreen").ToArray();controller.reflection=UnityEngine.Object.FindFirstObjectByType<ReflectionProbe>();
   foreach(var l in controller.supplementary)l.lightmapBakeType=LightmapBakeType.Mixed;
   // Glowing glass must not illuminate walls independently of its electrical source.
   foreach(var r in controller.glowingSurfaces)r.sharedMaterial.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;
   EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
   if(!Lightmapping.Bake())throw new Exception("Warm light bake failed");controller.warm=Capture("Warm",controller.surfaces);
   float[] power=controller.switches.Select(x=>x.controlledLight.intensity).ToArray(),extra=controller.supplementary.Select(x=>x.intensity).ToArray();
   foreach(var s in controller.switches)s.controlledLight.intensity=0;foreach(var l in controller.supplementary)l.intensity=0;
   if(!Lightmapping.Bake())throw new Exception("Blackout light bake failed");controller.blackout=Capture("Blackout",controller.surfaces);
   for(int i=0;i<power.Length;i++)controller.switches[i].controlledLight.intensity=power[i];for(int i=0;i<extra.Length;i++)controller.supplementary[i].intensity=extra[i];
   var warm=controller.warm;LightmapSettings.lightmaps=warm.colors.Select((tex,i)=>new LightmapData{lightmapColor=tex}).ToArray();for(int i=0;i<controller.surfaces.Length;i++){controller.surfaces[i].lightmapIndex=warm.indices[i];controller.surfaces[i].lightmapScaleOffset=warm.offsets[i];}
   EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();Debug.Log("M1_DUAL_BAKE_COMPLETE warm="+warm.colors.Length+" blackout="+controller.blackout.colors.Length);
  }
  static ApartmentLightingStates.State Capture(string name,Renderer[] surfaces){
   string dir=Root+"/Art/Lighting/"+name;Directory.CreateDirectory(dir);AssetDatabase.Refresh();var maps=LightmapSettings.lightmaps;if(maps.Length==0)throw new Exception("Bake returned no lightmaps");
   var state=new ApartmentLightingStates.State{colors=new Texture2D[maps.Length],directions=new Texture2D[maps.Length],indices=surfaces.Select(x=>x.lightmapIndex).ToArray(),offsets=surfaces.Select(x=>x.lightmapScaleOffset).ToArray()};
   for(int i=0;i<maps.Length;i++){string source=AssetDatabase.GetAssetPath(maps[i].lightmapColor),target=dir+"/Lightmap-"+i+Path.GetExtension(source);if(File.Exists(target))File.Copy(source,target,true);else if(!AssetDatabase.CopyAsset(source,target))throw new Exception("Cannot preserve lightmap "+source);AssetDatabase.ImportAsset(target);state.colors[i]=AssetDatabase.LoadAssetAtPath<Texture2D>(target);}
   return state;
  }
 }
}
