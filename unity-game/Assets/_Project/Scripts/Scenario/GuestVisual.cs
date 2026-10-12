using UnityEngine;
namespace Ataka.Scenario {
 // The Guest keeps her logical proxy skeleton (poses, HeadAt, chase) but is SEEN as a ready authored creature
 // installed as a child by GuestInstaller. This plays the author's clips by context.
 public sealed class GuestVisual:MonoBehaviour {
  public Animation player;public string idle,walk,run,crawl;
  public Renderer[] proxyRenderers;   // hidden Quaternius proxy surfaces
  public Transform model;             // authored creature, pivot at the feet
  [System.NonSerialized] public float groundY=float.NaN; // set while crawling/running so the creature stays on the floor
  string current;
  Transform restModel;Vector3 restLocalPosition;
  void RememberPlacement(){if(model&&restModel!=model){restModel=model;restLocalPosition=model.localPosition;}}
  void Awake(){RememberPlacement();}
  public void Mode(string mode){
   if(!player)return;string clip=mode=="run"?First(run,crawl,walk,idle):mode=="crawl"?First(crawl,run,walk,idle):mode=="walk"?First(walk,run,idle):idle;
   if(string.IsNullOrEmpty(clip)||clip==current||player[clip]==null)return;current=clip;player[clip].wrapMode=WrapMode.Loop;player.CrossFade(clip,.2f);
  }
  static string First(params string[] names){foreach(var n in names)if(!string.IsNullOrEmpty(n))return n;return null;}
  void LateUpdate(){
   RememberPlacement();if(!model)return;
   // Ground clamping changes the ready model's local offset. Restore the installed offset after the hunt;
   // clearing the clamp alone leaves the next close-up looking at its feet or torso.
   if(float.IsNaN(groundY))model.localPosition=restLocalPosition;
   else model.position=new Vector3(model.position.x,groundY,model.position.z);
  }
  void OnEnable(){RememberPlacement();if(proxyRenderers!=null)foreach(var r in proxyRenderers)if(r)r.enabled=false;current=null;Mode("idle");}
 }
}
