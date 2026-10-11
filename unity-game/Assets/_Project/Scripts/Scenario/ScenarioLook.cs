using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Ataka.Scenario {
 public sealed class ScenarioLook:MonoBehaviour {
  public UniversalRenderPipelineAsset pipeline;public Material vhs;public Volume volume;
  public Light[] warm,cold;public Light icon; public float disruption,peephole;
  RenderPipelineAsset old;Material saved;
  void Awake(){old=QualitySettings.renderPipeline;QualitySettings.renderPipeline=pipeline;saved=new Material(vhs);}
  void Start(){volume.profile.TryGet<ColorAdjustments>(out var ca);ca.postExposure.value=-1.7f;Apply(FlatCondition.Familiar);}
  public void Apply(FlatCondition condition){
   for(int i=0;i<warm.Length;i++){var l=warm[i];l.color=condition==FlatCondition.RedRot?new Color(1,.04f,.015f):new Color(1,.706f,.42f);l.intensity=condition==FlatCondition.Blackout?0:condition>=FlatCondition.Rot?.3f:i==1?.08f:0;}
   foreach(var l in cold)l.intensity=condition==FlatCondition.Blackout?0:condition==FlatCondition.Frozen?3.8f:3.24f;
   if(icon)icon.intensity=.14f;
  }
  public void WarmReturn(){foreach(var l in warm){l.color=new Color(1,.706f,.42f);l.intensity=1.8f;}foreach(var l in cold)l.intensity=3.24f;}
  void Update(){
   vhs.SetFloat("_Peephole",peephole);
   vhs.SetFloat("_Lens",.08f);vhs.SetFloat("_LumaBlur",2);vhs.SetFloat("_ChromaBlur",10);vhs.SetFloat("_ChromaShift",3);vhs.SetFloat("_GhostPixels",9);vhs.SetFloat("_GhostWeight",.08f);vhs.SetFloat("_Aberration",2);vhs.SetFloat("_Grain",.036f);vhs.SetFloat("_Jitter",.4f);vhs.SetFloat("_Scanlines",.025f);vhs.SetFloat("_Wear",1);vhs.SetFloat("_Disruption",disruption);vhs.SetVector("_VhsOutputSize",new Vector4(Screen.width,Screen.height,0,0));
  }
  void OnDestroy(){QualitySettings.renderPipeline=old;if(saved){vhs.CopyPropertiesFromMaterial(saved);Destroy(saved);}}
 }
}
