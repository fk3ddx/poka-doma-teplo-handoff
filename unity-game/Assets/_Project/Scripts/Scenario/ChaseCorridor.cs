using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Ataka.Scenario {
 public sealed class ChaseCorridor:MonoBehaviour {
  public FlatBindings flat;public LoopDirector loop;public ScenarioSound sound;public ScenarioTimeline scenes;public CorridorModule[] pool;
  public bool Active{get;private set;}public ChaseProgress Progress{get;private set;}
  readonly List<CorridorModule> live=new List<CorridorModule>();readonly List<Vector3> trail=new List<Vector3>();
  int next,deathCount;bool ending;float breathTimer;Vector3 previous;Renderer[] apartmentRenderers;Collider[] apartmentColliders;
  public CorridorModule Current=>live.Count>2?live[2]:null;public int DeathCount=>deathCount;public bool Ending=>ending;
  void Start(){apartmentRenderers=flat.apartment.GetComponentsInChildren<Renderer>();apartmentColliders=flat.apartment.GetComponentsInChildren<Collider>();foreach(var m in pool)m.Visible(false);}
  void FlatVisible(bool value){foreach(var r in apartmentRenderers)r.enabled=value;foreach(var c in apartmentColliders)c.enabled=value;}
  public void Stop(){StopAllCoroutines();foreach(var m in pool)m.Visible(false);if(apartmentRenderers!=null)FlatVisible(true);Active=false;ending=false;flat.player.chasing=false;flat.player.control=true;flat.guest.SetActive(false);}
  public void Begin(){
   Active=true;ending=false;Progress=new ChaseProgress();flat.player.chasing=true;FlatVisible(false);
   foreach(var l in flat.look.warm)l.intensity=0;foreach(var l in flat.look.cold)l.intensity=0;flat.look.icon.intensity=0;
   live.Clear();var pose=new Pose(new Vector3(0,0,30),Quaternion.identity);next=0;
   foreach(var m in pool){m.transform.SetPositionAndRotation(pose.position,pose.rotation);m.transform.localScale=Vector3.one;m.Visible(true);m.Configure(next++,false);live.Add(m);pose=m.Next();}
   var start=live[2];flat.player.Place(start.transform.TransformPoint(new Vector3(0,.02f,.25f)),start.transform.eulerAngles.y);previous=flat.player.transform.position;
   trail.Clear();trail.Add(previous-start.transform.forward*4);trail.Add(previous);flat.guest.SetActive(true);loop.SaveCheckpoint(true);
   if(deathCount>0)loop.Thought("Часы… Пока часы идут — дом помнит");
  }
  void Update(){
   if(!Active||ending||flat.player.paused)return;var player=flat.player;var at=player.transform.position;
   float moved=Vector3.Distance(at,previous);previous=at;
   if(trail.Count==0||Vector3.Distance(at,trail[trail.Count-1])>.12f){trail.Add(at);if(trail.Count>150)trail.RemoveAt(0);}
   var current=live[2];player.routeForward=current.transform.forward;float distance=Vector3.Distance(player.head.position,current.clock.transform.position);
   bool nearWarm=current.warm&&distance<1.6f;bool e=Keyboard.current?.eKey.isPressed==true;
   bool winding=nearWarm&&(e||Progress.Wound);
   Progress.Tick(Time.deltaTime,false,moved>Time.deltaTime*.1f,player.LookingBack,winding);
   foreach(var m in live)m.clock.Show(m.warm?22*3600:Progress.ClockSeconds);
   loop.prompt.text=nearWarm?(Progress.Wound?"[E] Толкнуть маятник":"[E удерживать] Завести часы"):current.warm&&deathCount>=2&&distance<4?"[E] Завести часы":"";
   if(nearWarm&&Progress.Wound&&Keyboard.current?.eKey.wasPressedThisFrame==true&&Progress.PushPendulum()){StartCoroutine(Complete());return;}
   if(Progress.Dead){StartCoroutine(Death());return;}
   if(current.Crossed(at)){
    if(current.warm)Progress.PassedWarmModule();if(current.turn)Progress.Corner();
    var recycled=live[0];live.RemoveAt(0);var pose=live[live.Count-1].Next();recycled.transform.SetPositionAndRotation(pose.position,pose.rotation);recycled.Configure(next++,Progress.AllocateNextModule());live.Add(recycled);
   }
   float remaining=Progress.GuestDistance;Vector3 target=trail[0];for(int i=trail.Count-1;i>0;i--){float d=Vector3.Distance(trail[i],trail[i-1]);if(remaining<=d){target=Vector3.Lerp(trail[i],trail[i-1],remaining/Mathf.Max(.001f,d));break;}remaining-=d;}
   flat.guest.transform.position=target;var facing=at-target;facing.y=0;if(facing.sqrMagnitude>.01f)flat.guest.transform.rotation=Quaternion.LookRotation(facing);
   flat.guest.GetComponent<ProxyPose>().Crawl(target);
   breathTimer+=Time.deltaTime;if(breathTimer>.7f){breathTimer=0;sound.Play("breath",at-player.head.forward*2,.35f);sound.Play("impact",target,.17f);}
  }
  IEnumerator Death(){Debug.Log("CHASE_DEATH elapsed="+Progress.Elapsed+" stalled="+Progress.Stalled+" distance="+Progress.GuestDistance+" back="+flat.player.LookingBack+" missed="+Progress.WarmMissed+" module="+Current.index+" local="+Current.transform.InverseTransformPoint(flat.player.transform.position));ending=true;deathCount++;yield return scenes.Play("DeathShort");flat.look.disruption=1;yield return new WaitForSeconds(1);flat.look.disruption=0;Begin();}
  IEnumerator Complete(){ending=true;flat.player.control=false;for(int i=0;i<10;i++){sound.Play("impact",live[2].clock.transform.position,.5f);foreach(var m in live)if(!m.warm)m.lamp.intensity*=.65f;if(i>=4)live[Mathf.Min(5,i-4)].transform.localScale=new Vector3(1,1,.01f);yield return new WaitForSeconds(1);}
   foreach(var m in pool)m.Visible(false);flat.guest.SetActive(false);FlatVisible(true);Active=false;flat.player.control=true;loop.FinishCore();}
 }
}
