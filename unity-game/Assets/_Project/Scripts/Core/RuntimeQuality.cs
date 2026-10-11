using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Ataka {
 // High=0 preserves older settings files which have no quality field.
 public enum VisualQuality { High=0, Medium=1, Low=2 }
 public sealed class RuntimeQuality : MonoBehaviour {
  UniversalRenderPipelineAsset source,pipeline;
  RenderPipelineAsset previous;
  PlanarMirror[] mirrors;
  LocalLightFog[] fog;
  int applied=-1;
  int outputHeight=-1;
  RasterStyle rasterStyle=RasterStyle.Standard;
  public RasterStyle Raster=>rasterStyle;
  public int Revision {get;private set;}
  public UniversalRenderPipelineAsset Pipeline=>pipeline;
  public VisualQuality Current=>(VisualQuality)Mathf.Max(0,applied);
  public static string Label(VisualQuality q)=>q==VisualQuality.Low?"Низкое":q==VisualQuality.Medium?"Среднее":"Высокое";
  public void Apply(VisualQuality quality){ Apply(quality, rasterStyle); }
  public void Apply(VisualQuality quality, RasterStyle style){
   int value=Mathf.Clamp((int)quality,0,2);
   rasterStyle=(RasterStyle)Mathf.Clamp((int)style,0,2);
   if(value==applied){ApplyRaster();return;}
   if(pipeline==null){
    source=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
    if(source==null)return;
    previous=QualitySettings.renderPipeline;pipeline=Instantiate(source);pipeline.name="Ataka runtime quality";pipeline.hideFlags=HideFlags.DontSave;
    QualitySettings.renderPipeline=pipeline;
    mirrors=FindObjectsByType<PlanarMirror>(FindObjectsInactive.Include,FindObjectsSortMode.None);
    fog=FindObjectsByType<LocalLightFog>(FindObjectsInactive.Include,FindObjectsSortMode.None);
   }
   bool low=value==2,medium=value==1;
   ApplyRaster();
   pipeline.msaaSampleCount=1;
   pipeline.shadowDistance=low?12:medium?18:25;
   pipeline.mainLightShadowmapResolution=low?1024:medium?2048:4096;
   pipeline.additionalLightsShadowmapResolution=low?1024:medium?2048:4096;
   foreach(var mirror in mirrors)if(mirror!=null)mirror.Configure(low?256:medium?384:512,low?5:medium?8:10);
   foreach(var volume in fog)if(volume!=null)volume.enabled=!low;
   applied=value;Revision++;
  }
  void ApplyRaster(){
   if(pipeline==null)return;
   float scale=RetroRaster.Scale(rasterStyle,Screen.height);
   if(!Mathf.Approximately(pipeline.renderScale,scale))pipeline.renderScale=scale;
   pipeline.upscalingFilter=UpscalingFilterSelection.Point;
   outputHeight=Screen.height;
  }
  void LateUpdate(){if(pipeline!=null&&outputHeight!=Screen.height)ApplyRaster();}
  void OnDestroy(){
   if(pipeline==null)return;
   if(QualitySettings.renderPipeline==pipeline)QualitySettings.renderPipeline=previous;
   Destroy(pipeline);
  }
 }
}
