using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Ataka.Scenario {
 public sealed class ChaseCorridor:MonoBehaviour {
  public FlatBindings flat;public LoopDirector loop;public ScenarioSound sound;public ScenarioTimeline scenes;public CorridorModule[] pool;
  public bool Active{get;private set;}public ChaseProgress Progress{get;private set;}
  readonly List<CorridorModule> live=new List<CorridorModule>();readonly List<Vector3> trail=new List<Vector3>();
  int next,deathCount,currentSlot;bool ending,prepared,detached,relocate;float breathTimer;Vector3 previous;Renderer[] apartmentRenderers;Collider[] apartmentColliders;bool[] rendererStates,colliderStates;bool apartmentHidden;
  public static readonly Vector3 EntryOrigin=new Vector3(4.7f,0,5.25f);
  public CorridorModule Current=>live.Count>currentSlot?live[currentSlot]:null;public int DeathCount=>deathCount;public bool Ending=>ending;
  void Start(){apartmentRenderers=flat.apartment.GetComponentsInChildren<Renderer>(true);apartmentColliders=flat.apartment.GetComponentsInChildren<Collider>(true);entrancePosition=flat.entrance.leaf.localPosition;foreach(var m in pool)m.Visible(false);}
  void FlatVisible(bool value){
   if(!value){if(apartmentHidden)return;rendererStates=new bool[apartmentRenderers.Length];colliderStates=new bool[apartmentColliders.Length];
    for(int i=0;i<apartmentRenderers.Length;i++){rendererStates[i]=apartmentRenderers[i].enabled;apartmentRenderers[i].enabled=false;}
    for(int i=0;i<apartmentColliders.Length;i++){colliderStates[i]=apartmentColliders[i].enabled;apartmentColliders[i].enabled=false;}apartmentHidden=true;
   }else if(apartmentHidden){for(int i=0;i<apartmentRenderers.Length;i++)if(apartmentRenderers[i])apartmentRenderers[i].enabled=rendererStates[i];for(int i=0;i<apartmentColliders.Length;i++)if(apartmentColliders[i])apartmentColliders[i].enabled=colliderStates[i];apartmentHidden=false;}
  }
  bool[] overlapRendererStates,overlapColliderStates;bool overlapHidden;Vector3 entrancePosition;
  readonly Dictionary<Light,(float intensity,Color color)> savedLights=new Dictionary<Light,(float,Color)>();
  void OverlapVisible(bool value){
   if(!value&&!overlapHidden){overlapRendererStates=new bool[flat.chaseOverlapRenderers.Length];overlapColliderStates=new bool[flat.chaseOverlapColliders.Length];
    for(int i=0;i<overlapRendererStates.Length;i++){overlapRendererStates[i]=flat.chaseOverlapRenderers[i].enabled;flat.chaseOverlapRenderers[i].enabled=false;}
    for(int i=0;i<overlapColliderStates.Length;i++){overlapColliderStates[i]=flat.chaseOverlapColliders[i].enabled;flat.chaseOverlapColliders[i].enabled=false;}overlapHidden=true;
   }else if(value&&overlapHidden){for(int i=0;i<overlapRendererStates.Length;i++)flat.chaseOverlapRenderers[i].enabled=overlapRendererStates[i];for(int i=0;i<overlapColliderStates.Length;i++)flat.chaseOverlapColliders[i].enabled=overlapColliderStates[i];overlapHidden=false;}
  }
  void RestoreApartment(){
   if(apartmentRenderers!=null)FlatVisible(true);OverlapVisible(true);
   foreach(var pair in savedLights)if(pair.Key){pair.Key.intensity=pair.Value.intensity;pair.Key.color=pair.Value.color;}savedLights.Clear();
   flat.entrance.scripted=false;flat.entrance.open=false;flat.entrance.leaf.localPosition=entrancePosition;flat.entrance.leaf.localRotation=Quaternion.identity;
   flat.living.open=true;flat.living.locked=false;
  }
  public void Stop(){StopAllCoroutines();foreach(var m in pool)m.Visible(false);RestoreApartment();Active=false;ending=false;prepared=false;relocate=false;flat.player.chasing=false;flat.player.control=true;flat.guest.SetActive(false);}
  public void PrepareEntry(){
   foreach(var m in pool)m.Visible(false);RestoreApartment();Active=false;flat.player.chasing=false;prepared=true;detached=false;relocate=false;
   foreach(var l in flat.look.warm)savedLights[l]=(l.intensity,l.color);foreach(var l in flat.look.cold)savedLights[l]=(l.intensity,l.color);savedLights[flat.look.icon]=(flat.look.icon.intensity,flat.look.icon.color);
   savedLights[flat.televisionLight]=(flat.televisionLight.intensity,flat.televisionLight.color);savedLights[scenes.landingLight]=(scenes.landingLight.intensity,scenes.landingLight.color);
   OverlapVisible(false);flat.living.open=false;flat.living.locked=true;flat.living.leaf.localRotation=Quaternion.identity;
   flat.entrance.scripted=true;live.Clear();var pose=new Pose(EntryOrigin,Quaternion.Euler(0,-90,0));next=0;currentSlot=0;
   foreach(var m in pool){m.transform.SetPositionAndRotation(pose.position,pose.rotation);m.transform.localScale=Vector3.one;m.Visible(true);m.Configure(next++,false);live.Add(m);pose=m.Next();}
   foreach(var l in flat.look.cold)l.intensity=0;for(int i=0;i<flat.look.warm.Length;i++){var l=flat.look.warm[i];l.color=new Color(1,.025f,.01f);l.intensity=i<2?.7f:0;}
  }
  public void Begin(){
   if(!prepared)PrepareEntry();Active=true;ending=false;Progress=new ChaseProgress();flat.player.chasing=true;flat.player.control=true;
   previous=flat.player.transform.position;flat.player.routeForward=Vector3.left;
   trail.Clear();trail.Add(new Vector3(8.05f,0,6.3f));trail.Add(previous);flat.guest.SetActive(true);loop.SaveCheckpoint(true);
   if(deathCount>0)loop.Thought("Часы… Пока часы идут — дом помнит");
  }
  public void ShiftIntoIsolatedSpace(Vector3 shift){
   // A rigid shift preserves every visible relative pose; the first corner hides the apartment behind us.
   FlatVisible(false);foreach(var l in savedLights.Keys)if(l)l.intensity=0;
   foreach(var m in live)m.transform.position+=shift;
   var player=flat.player;var body=player.GetComponent<CharacterController>();body.enabled=false;player.transform.position+=shift;body.enabled=true;
   flat.guest.transform.position+=shift;for(int i=0;i<trail.Count;i++)trail[i]+=shift;previous+=shift;detached=true;
   sound.ShiftWorld(shift);scenes.cine.PreviousStateIsValid=false;
  }
  Vector3 TrailAtDistance(float remaining){
   Vector3 target=trail[0];for(int i=trail.Count-1;i>0;i--){float d=Vector3.Distance(trail[i],trail[i-1]);if(remaining<=d)return Vector3.Lerp(trail[i],trail[i-1],remaining/Mathf.Max(.001f,d));remaining-=d;}return target;
  }
  void Update(){
   if(!Active||ending||flat.player.paused)return;var player=flat.player;var at=player.transform.position;
   float moved=Vector3.Distance(at,previous);previous=at;
   if(trail.Count==0||Vector3.Distance(at,trail[trail.Count-1])>.12f){trail.Add(at);if(trail.Count>150)trail.RemoveAt(0);}
   var current=Current;player.routeForward=current.transform.forward;float distance=Vector3.Distance(player.head.position,current.clock.transform.position);
   bool nearWarm=current.warm&&distance<1.6f;bool e=Keyboard.current?.eKey.isPressed==true;
   bool winding=nearWarm&&(e||Progress.Wound);
   Progress.Tick(Time.deltaTime,false,moved>Time.deltaTime*.1f,player.LookingBack,winding);
   foreach(var m in live)m.clock.Show(m.warm?22*3600:Progress.ClockSeconds);
   loop.prompt.text=nearWarm?(Progress.Wound?"[E] Толкнуть маятник":"[E удерживать] Завести часы"):current.warm&&deathCount>=2&&distance<4?"[E] Завести часы":"";
   if(nearWarm&&Progress.Wound&&Keyboard.current?.eKey.wasPressedThisFrame==true&&Progress.PushPendulum()){StartCoroutine(Complete());return;}
   if(Progress.Dead){StartCoroutine(Death());return;}
   if(current.Crossed(at)){
    if(current.warm)Progress.PassedWarmModule();if(current.turn)Progress.Corner();
    if(currentSlot<2){currentSlot++;}else{
    var recycled=live[0];live.RemoveAt(0);var pose=live[live.Count-1].Next();recycled.transform.SetPositionAndRotation(pose.position,pose.rotation);recycled.Configure(next++,Progress.AllocateNextModule());live.Add(recycled);}
    if(current.turn&&!detached)relocate=true;
   }
   Vector3 target=TrailAtDistance(Progress.GuestDistance);
   flat.guest.transform.position=target;var facing=at-target;facing.y=0;if(facing.sqrMagnitude>.01f)flat.guest.transform.rotation=Quaternion.LookRotation(facing);
   flat.guest.GetComponent<ProxyPose>().Crawl(target);
   breathTimer+=Time.deltaTime;if(breathTimer>.7f){breathTimer=0;sound.Play("palms",target,flat.guest.transform);}
   if(relocate){relocate=false;ShiftIntoIsolatedSpace(Vector3.down*50);}
  }
  IEnumerator Death(){ending=true;deathCount++;yield return scenes.Play("DeathShort");flat.look.disruption=1;yield return new WaitForSeconds(1);
   flat.look.disruption=0;flat.player.Place(new Vector3(8.05f,.02f,5.35f),0);loop.sound.EndDeathRecovery();PrepareEntry();yield return scenes.Play("ChaseStart");Begin();
  }
  IEnumerator Complete(){
   ending=true;flat.player.control=false;var warm=Current;var collapsible=new List<CorridorModule>(live);collapsible.Remove(warm);collapsible.Sort((a,b)=>Vector3.Distance(b.transform.position,warm.transform.position).CompareTo(Vector3.Distance(a.transform.position,warm.transform.position)));
   for(int i=0;i<10;i++){
    sound.Play("chime",warm.clock.transform.position,warm.clock.transform);foreach(var m in live)if(m!=warm)m.lamp.intensity*=.65f;
    if(i>=4&&i-4<collapsible.Count)collapsible[i-4].transform.localScale=new Vector3(1,1,.01f);
    float until=Time.time+1;while(Time.time<until){var target=TrailAtDistance(1.5f+(i+1-(until-Time.time))*.5f);flat.guest.transform.position=target;flat.guest.GetComponent<ProxyPose>().Crawl(target);yield return null;}
   }
   flat.guest.SetActive(false);scenes.screenWhite=1;if(Application.isBatchMode)yield return null;else yield return new WaitForEndOfFrame();
   foreach(var m in pool)m.Visible(false);RestoreApartment();Active=false;prepared=false;flat.player.control=true;loop.FinishCore();
   for(float t=0;t<.3f;t+=Time.unscaledDeltaTime){scenes.screenWhite=1-t/.3f;yield return null;}scenes.screenWhite=0;
  }
 }
}
