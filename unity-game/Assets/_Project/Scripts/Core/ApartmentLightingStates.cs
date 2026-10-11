using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace Ataka {
 public sealed class ApartmentLightingStates : MonoBehaviour {
  [Serializable] public sealed class State {
   public Texture2D[] colors, directions, shadowMasks;
   public Texture[] reflections;
   public float[] probeCoefficients;
   public int[] indices;
   public Vector4[] offsets;
   [NonSerialized] public LightmapData[] runtime;
   [NonSerialized] public SphericalHarmonicsL2[] probes;
   public void Prepare(){
    runtime=new LightmapData[colors.Length];
    for(int i=0;i<runtime.Length;i++)runtime[i]=new LightmapData{lightmapColor=colors[i],lightmapDir=At(directions,i),shadowMask=At(shadowMasks,i)};
    probes=new SphericalHarmonicsL2[(probeCoefficients?.Length??0)/27];
    for(int p=0;p<probes.Length;p++)for(int rgb=0;rgb<3;rgb++)for(int c=0;c<9;c++)probes[p][rgb,c]=probeCoefficients[p*27+rgb*9+c];
   }
   static Texture2D At(Texture2D[] values,int i)=>values!=null&&i<values.Length?values[i]:null;
  }
  public Renderer[] surfaces;
  public State warm, blackout;
  public LampSwitch[] switches;
  public Light[] supplementary;
  public Renderer[] glowingSurfaces;
  public ReflectionProbe[] roomReflections;
  public LampSwitch[] reflectionSwitches;
  float[] intensities;
  bool dark,livingWasOn=true;
  bool[] reflectionPower;
  LampSwitch living;
  MaterialPropertyBlock off;
  void Awake(){
   if(surfaces==null||surfaces.Any(x=>x==null)||!Valid(warm)||!Valid(blackout)){
    Debug.LogError("M1 lighting state contains missing surfaces or inconsistent bake data",this);enabled=false;return;
   }
   warm.Prepare();blackout.Prepare();living=switches.FirstOrDefault(x=>x.persistentId=="lamp-living");
   intensities=supplementary.Select(x=>x.intensity).ToArray();off=new MaterialPropertyBlock();off.SetColor("_EmissionColor",Color.black);off.SetFloat("_Power",0);
   reflectionPower=new bool[roomReflections.Length];Apply(warm);RefreshReflections(true);
  }
  bool Valid(State s)=>s!=null&&s.colors!=null&&s.colors.Length>0&&s.colors.All(x=>x!=null)&&s.indices!=null&&s.indices.Length==surfaces.Length&&s.offsets!=null&&s.offsets.Length==surfaces.Length
   &&roomReflections!=null&&roomReflections.All(x=>x!=null)&&reflectionSwitches!=null&&reflectionSwitches.Length==roomReflections.Length
   &&s.reflections!=null&&s.reflections.Length==roomReflections.Length&&s.reflections.All(x=>x!=null)
   &&s.probeCoefficients!=null&&s.probeCoefficients.Length%27==0;
  void LateUpdate(){
   bool allOff=true;foreach(var s in switches)if(s.powered){allOff=false;break;}
   if(allOff!=dark){dark=allOff;Apply(dark?blackout:warm);}
   RefreshReflections(false);
   bool livingOn=living!=null?living.powered:!allOff;
   if(livingOn==livingWasOn)return;livingWasOn=livingOn;
   for(int i=0;i<supplementary.Length;i++)supplementary[i].intensity=livingOn?intensities[i]:0;
   foreach(var r in glowingSurfaces)r.SetPropertyBlock(livingOn?null:off);
  }
  void RefreshReflections(bool force){
   for(int i=0;i<roomReflections.Length;i++){
    bool powered=reflectionSwitches[i]==null?!dark:reflectionSwitches[i].powered;
    if(!force&&reflectionPower[i]==powered)continue;
    reflectionPower[i]=powered;roomReflections[i].customBakedTexture=(powered?warm:blackout).reflections[i];
   }
  }
  void Apply(State s){
   LightmapSettings.lightmaps=s.runtime;
   for(int i=0;i<surfaces.Length;i++){surfaces[i].lightmapIndex=s.indices[i];surfaces[i].lightmapScaleOffset=s.offsets[i];}
   if(LightmapSettings.lightProbes!=null&&s.probes.Length==LightmapSettings.lightProbes.count)LightmapSettings.lightProbes.bakedProbes=s.probes;
  }
 }
}
