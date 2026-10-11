using System;
using System.Linq;
using UnityEngine;
namespace Ataka {
 public sealed class ApartmentLightingStates : MonoBehaviour {
  [Serializable] public sealed class State {
   public Texture2D[] colors, directions;
   public int[] indices;
   public Vector4[] offsets;
   [NonSerialized] public LightmapData[] runtime;
   public void Prepare(){runtime=new LightmapData[colors.Length];for(int i=0;i<runtime.Length;i++)runtime[i]=new LightmapData{lightmapColor=colors[i],lightmapDir=directions!=null&&i<directions.Length?directions[i]:null};}
  }
  public Renderer[] surfaces;
  public State warm, blackout;
  public LampSwitch[] switches;
  public Light[] supplementary;
  public Renderer[] glowingSurfaces;
  public ReflectionProbe reflection;
  float[] intensities; bool dark, livingWasOn=true;
  LampSwitch living;
  MaterialPropertyBlock off;
  void Awake(){
   if(surfaces==null||surfaces.Any(x=>x==null)||!Valid(warm)||!Valid(blackout)){
    Debug.LogError("M1 lighting state contains missing surfaces or inconsistent lightmap data",this);enabled=false;return;
   }
   warm.Prepare();blackout.Prepare();living=switches.FirstOrDefault(x=>x.persistentId=="lamp-living");
   intensities=supplementary.Select(x=>x.intensity).ToArray();off=new MaterialPropertyBlock();off.SetColor("_EmissionColor",Color.black);off.SetFloat("_Power",0);Apply(warm);
  }
  bool Valid(State s)=>s!=null&&s.colors!=null&&s.colors.Length>0&&s.colors.All(x=>x!=null)&&s.indices!=null&&s.indices.Length==surfaces.Length&&s.offsets!=null&&s.offsets.Length==surfaces.Length;
  void LateUpdate(){
   bool allOff=true;foreach(var s in switches)if(s.powered){allOff=false;break;}
   if(allOff!=dark){dark=allOff;Apply(dark?blackout:warm);}
   bool livingOn=living!=null?living.powered:!allOff;
   if(livingOn==livingWasOn)return;livingWasOn=livingOn;
   for(int i=0;i<supplementary.Length;i++)supplementary[i].intensity=livingOn?intensities[i]:0;
   foreach(var r in glowingSurfaces)r.SetPropertyBlock(livingOn?null:off);
   if(reflection!=null)reflection.intensity=livingOn?.65f:.1f;
  }
  void Apply(State s){LightmapSettings.lightmaps=s.runtime;for(int i=0;i<surfaces.Length;i++){surfaces[i].lightmapIndex=s.indices[i];surfaces[i].lightmapScaleOffset=s.offsets[i];}}
 }
}
