using UnityEngine;
namespace Ataka {
 [ExecuteAlways,RequireComponent(typeof(MeshRenderer))]
 public sealed class LocalLightFog:MonoBehaviour {
  public Light sourceLight;
  [Min(.1f)] public float radius=5.5f;
  [Min(0)] public float density=.12f, gain=5;
  [Min(.01f)] public float referenceIntensity=38;
  Renderer surface;MaterialPropertyBlock block;
  static readonly int Center=Shader.PropertyToID("_CenterRadius"),ColorId=Shader.PropertyToID("_SourceColor"),Direction=Shader.PropertyToID("_LightDirection"),Cone=Shader.PropertyToID("_Cone"),Density=Shader.PropertyToID("_Density"),Gain=Shader.PropertyToID("_Gain");
  void OnEnable(){Refresh();}
  void LateUpdate(){Refresh();}
  public void Refresh(){
   if(surface==null)surface=GetComponent<Renderer>();
   bool lit=sourceLight!=null&&sourceLight.isActiveAndEnabled&&sourceLight.intensity>0;
   surface.enabled=lit;if(!lit)return;
   if(block==null)block=new MaterialPropertyBlock();
   transform.position=sourceLight.transform.position;
   // Parent scale is identity in the apartment recipe. The sphere has unit diameter.
   transform.localScale=Vector3.one*(radius*2);
   var p=transform.position;block.SetVector(Center,new Vector4(p.x,p.y,p.z,radius));
   var colour=sourceLight.color.linear*(sourceLight.intensity/Mathf.Max(.01f,referenceIntensity));
   // SetVector preserves linear HDR radiance; SetColor would convert gamma a second time.
   block.SetVector(ColorId,new Vector4(colour.r,colour.g,colour.b,1));block.SetVector(Direction,sourceLight.transform.forward);
   block.SetVector(Cone,new Vector4(Mathf.Cos(sourceLight.innerSpotAngle*.5f*Mathf.Deg2Rad),Mathf.Cos(sourceLight.spotAngle*.5f*Mathf.Deg2Rad),RenderSettings.fog?RenderSettings.fogDensity:0,0));
   block.SetFloat(Density,density);block.SetFloat(Gain,gain);surface.SetPropertyBlock(block);
  }
  void OnDisable(){if(surface!=null)surface.enabled=false;}
 }
}
