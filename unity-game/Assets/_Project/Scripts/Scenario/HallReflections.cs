using UnityEngine;
namespace Ataka.Scenario {
 // Refresh only when the existing room lighting changes; spread cubemap work across frames.
 public sealed class HallReflections:MonoBehaviour {
  public ReflectionProbe probe;public ScenarioLook look;int job=-1,signature=int.MinValue;float nextCheck;bool previousRealtime;
  void Awake(){previousRealtime=QualitySettings.realtimeReflectionProbes;QualitySettings.realtimeReflectionProbes=true;}
  void OnDestroy(){if(Application.isPlaying)QualitySettings.realtimeReflectionProbes=previousRealtime;}
  public int CompletedCaptures{get;private set;}public bool Ready=>CompletedCaptures>0&&probe.texture;
  int Signature(){unchecked{int h=17;foreach(var l in look.warm)h=h*31+l.intensity.GetHashCode()+l.color.GetHashCode();foreach(var l in look.cold)h=h*31+l.intensity.GetHashCode()+l.color.GetHashCode();return h;}}
  void LateUpdate(){
   if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
   if(job>=0){if(!probe.IsFinishedRendering(job))return;job=-1;CompletedCaptures++;
    // Unity UUM-129405: 6.3's probe atlas otherwise keeps the first cubemap after RenderProbe.
    if(probe.texture)probe.texture.IncrementUpdateCount();
   }
   if(Time.unscaledTime<nextCheck)return;nextCheck=Time.unscaledTime+.15f;
   int current=Signature();if(current==signature)return;signature=current;job=probe.RenderProbe();
  }
 }
}
