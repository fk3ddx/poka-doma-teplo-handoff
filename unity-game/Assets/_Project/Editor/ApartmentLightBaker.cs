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
   var settings=new LightingSettings{name="M1 apartment GI",bakedGI=true,realtimeGI=false,lightmapper=LightingSettings.Lightmapper.ProgressiveGPU,lightmapResolution=20,lightmapMaxSize=1024,directSampleCount=32,indirectSampleCount=128,environmentSampleCount=64,maxBounces=3,mixedBakeMode=MixedLightingMode.Shadowmask,directionalityMode=LightmapsMode.NonDirectional};
   string settingsPath=Root+"/Settings/M1_Lighting.asset";var old=AssetDatabase.LoadAssetAtPath<LightingSettings>(settingsPath);
   if(old==null)AssetDatabase.CreateAsset(settings,settingsPath);else {EditorUtility.CopySerialized(settings,old);UnityEngine.Object.DestroyImmediate(settings);settings=old;}
   Lightmapping.lightingSettings=settings;QualitySettings.shadowmaskMode=ShadowmaskMode.DistanceShadowmask;
   var controller=UnityEngine.Object.FindFirstObjectByType<ApartmentLightingStates>();if(controller==null)controller=new GameObject("Baked apartment power states").AddComponent<ApartmentLightingStates>();
   controller.surfaces=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.InstanceID).Where(x=>x.gameObject.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene()&&GameObjectUtility.AreStaticEditorFlagsSet(x.gameObject,StaticEditorFlags.ContributeGI)).Cast<Renderer>().ToArray();
   foreach(var r in controller.surfaces)GameObjectUtility.SetStaticEditorFlags(r.gameObject,GameObjectUtility.GetStaticEditorFlags(r.gameObject)&~StaticEditorFlags.BatchingStatic);
   controller.switches=UnityEngine.Object.FindObjectsByType<LampSwitch>(FindObjectsSortMode.None);
   controller.supplementary=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(x=>x.name=="Floor lamp warm pool"||x.name=="CRT bounce").ToArray();
   controller.glowingSurfaces=controller.surfaces.Where(x=>x.name=="Living_Shade"||x.name=="Living_Bulb"||x.name=="Living_TVScreen").ToArray();
   controller.roomReflections=UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None).OrderBy(x=>x.name).ToArray();
   controller.reflectionSwitches=controller.roomReflections.Select(x=>controller.switches.Single(s=>s.persistentId=="lamp-"+(x.name.StartsWith("hall")?"hall":x.name.Split(' ')[0]))).ToArray();
   foreach(var probe in controller.roomReflections)probe.mode=ReflectionProbeMode.Baked;
   foreach(var l in controller.supplementary)l.lightmapBakeType=LightmapBakeType.Mixed;
   // URP validation removes _EMISSION when neither emissive GI flag is set.
   // Realtime GI is disabled: these surfaces glow visually; fixture lights provide baked transport.
   foreach(var r in controller.glowingSurfaces){
    r.sharedMaterial.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;
    r.sharedMaterial.EnableKeyword("_EMISSION");EditorUtility.SetDirty(r.sharedMaterial);
   }
   EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
   if(!Lightmapping.Bake())throw new Exception("Warm light bake failed");
   controller.warm=Capture("Warm",controller);
   float[] power=controller.switches.Select(x=>x.controlledLight.intensity).ToArray(),extra=controller.supplementary.Select(x=>x.intensity).ToArray();
   foreach(var s in controller.switches)s.controlledLight.intensity=0;foreach(var l in controller.supplementary)l.intensity=0;
   var off=new MaterialPropertyBlock();off.SetColor("_EmissionColor",Color.black);off.SetFloat("_Power",0);foreach(var r in controller.glowingSurfaces)r.SetPropertyBlock(off);
   if(!Lightmapping.Bake())throw new Exception("Blackout light bake failed");
   controller.blackout=Capture("Blackout",controller);
   for(int i=0;i<power.Length;i++)controller.switches[i].controlledLight.intensity=power[i];for(int i=0;i<extra.Length;i++)controller.supplementary[i].intensity=extra[i];
   foreach(var r in controller.glowingSurfaces)r.SetPropertyBlock(null);
   var warm=controller.warm;warm.Prepare();LightmapSettings.lightmaps=warm.runtime;
   if(LightmapSettings.lightProbes==null||LightmapSettings.lightProbes.count!=warm.probes.Length||warm.probes.Length<24)throw new Exception("Missing or inconsistent dynamic probe bake");
   LightmapSettings.lightProbes.bakedProbes=warm.probes;
   for(int i=0;i<controller.surfaces.Length;i++){controller.surfaces[i].lightmapIndex=warm.indices[i];controller.surfaces[i].lightmapScaleOffset=warm.offsets[i];}
   for(int i=0;i<controller.roomReflections.Length;i++){controller.roomReflections[i].mode=ReflectionProbeMode.Custom;controller.roomReflections[i].customBakedTexture=warm.reflections[i];}
   EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
   Debug.Log("M1_DUAL_BAKE_COMPLETE warm="+warm.colors.Length+" blackout="+controller.blackout.colors.Length+" probes="+warm.probes.Length+" reflections="+warm.reflections.Length);
  }
  static ApartmentLightingStates.State Capture(string name,ApartmentLightingStates controller){
   string dir=Root+"/Art/Lighting/"+name;Directory.CreateDirectory(dir);AssetDatabase.Refresh();var maps=LightmapSettings.lightmaps;if(maps.Length==0)throw new Exception("Bake returned no lightmaps");
   var surfaces=controller.surfaces;
   var state=new ApartmentLightingStates.State{colors=new Texture2D[maps.Length],directions=new Texture2D[maps.Length],shadowMasks=new Texture2D[maps.Length],indices=surfaces.Select(x=>x.lightmapIndex).ToArray(),offsets=surfaces.Select(x=>x.lightmapScaleOffset).ToArray(),reflections=new Texture[controller.roomReflections.Length]};
   Texture2D Preserve(Texture2D source,string stem){
    if(source==null)return null;string original=AssetDatabase.GetAssetPath(source),target=dir+"/"+stem+Path.GetExtension(original);
    if(File.Exists(target))File.Copy(original,target,true);else if(!AssetDatabase.CopyAsset(original,target))throw new Exception("Cannot preserve "+original);
    AssetDatabase.ImportAsset(target);return AssetDatabase.LoadAssetAtPath<Texture2D>(target);
   }
   for(int i=0;i<maps.Length;i++){state.colors[i]=Preserve(maps[i].lightmapColor,"Lightmap-"+i);state.directions[i]=Preserve(maps[i].lightmapDir,"Direction-"+i);state.shadowMasks[i]=Preserve(maps[i].shadowMask,"Shadowmask-"+i);}
   var sh=LightmapSettings.lightProbes.bakedProbes;state.probeCoefficients=new float[sh.Length*27];
   for(int p=0;p<sh.Length;p++)for(int rgb=0;rgb<3;rgb++)for(int c=0;c<9;c++)state.probeCoefficients[p*27+rgb*9+c]=sh[p][rgb,c];
   for(int i=0;i<controller.roomReflections.Length;i++){
    var probe=controller.roomReflections[i];string path=dir+"/Reflection-"+i+".exr";
    if(!Lightmapping.BakeReflectionProbe(probe,path))throw new Exception("Cannot bake reflection "+probe.name);
    AssetDatabase.ImportAsset(path);state.reflections[i]=AssetDatabase.LoadAssetAtPath<Texture>(path);if(state.reflections[i]==null)throw new Exception("Reflection asset missing "+path);
   }
   return state;
  }
 }
}
