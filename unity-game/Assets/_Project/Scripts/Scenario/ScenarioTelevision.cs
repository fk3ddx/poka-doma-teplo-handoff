using UnityEngine;
namespace Ataka.Scenario {
 // Characters on layer 31 exist only in the television's render, never behind the real player.
 public sealed class ScenarioTelevision:MonoBehaviour {
  public LoopDirector loop;public Camera feed;public Transform misha,visitor;public Renderer screen;public Material screenMaterial;
  RenderTexture texture;Material material;bool watched,turned;int lap;
  void Start(){texture=new RenderTexture(320,240,16){name="Rubin live feed",filterMode=FilterMode.Bilinear};feed.targetTexture=texture;material=new Material(screenMaterial);screen.sharedMaterial=material;material.SetTexture("_BaseMap",texture);feed.cullingMask=(1<<0)|(1<<31);feed.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;}
  void LateUpdate(){
   int n=loop.Progress.Lap;if(n!=lap){lap=n;watched=turned=false;}
   bool on=n==4&&!loop.Progress.EventComplete&&!loop.chase.Active;feed.enabled=on;misha.gameObject.SetActive(on);visitor.gameObject.SetActive(on&&!turned);material.SetColor("_BaseColor",on?new Color(.65f,.72f,.8f):Color.black);if(!on)return;
   var player=loop.flat.player;var direction=loop.flat.television.position-player.transform.position;direction.y=0;direction.Normalize();
   misha.position=player.transform.position;misha.rotation=Quaternion.LookRotation(direction);
   visitor.position=player.transform.position-direction;visitor.rotation=Quaternion.LookRotation(direction);
   feed.transform.position=player.transform.position-direction*2.5f+Vector3.up*1.55f;feed.transform.LookAt(player.transform.position+Vector3.up*1.3f+direction*.7f);
   float dot=Vector3.Dot(player.head.forward,(loop.flat.television.position-player.head.position).normalized);
   if(dot>.85f)watched=true;else if(watched&&dot<0)turned=true;
   if(turned)misha.rotation=Quaternion.LookRotation(-direction);
  }
  void OnDestroy(){if(texture){texture.Release();Destroy(texture);}if(material)Destroy(material);}
 }
}
