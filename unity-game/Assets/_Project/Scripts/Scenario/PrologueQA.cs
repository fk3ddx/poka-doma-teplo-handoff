using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Ataka.Scenario {
 // QA only (pc29):  -prologue-qa <dir>
 // SCENARIO 3.1–3.9 on the installed PrologueSet: the time code, the yard (snow, breath, sodium light), the roof figure
 // when he looks up, the iron entrance door and its slam, the lobby notice, the dying bulb on the third floor, the TV
 // behind 56, Tuzik behind 59, the attic hatch, the ring at 60 and her opening both doors, then the evening — and that
 // nothing of the prologue stays on (lights back to 0, snow, breath, beds). Then 4.9: Valya and Tuzik on the landing
 // when their models are supplied, and again in Night2 (SCENARIO 8.1). pc29b: he WALKS the whole route with his own
 // CharacterController (ScenarioPlayer.qaAxis: the same Move, speed and gravity as WASD) — 40 m of yard, the entrance,
 // the lobby and all nine flights to the fifth floor; there is no Place on the route. A leg that makes no progress is a
 // FAIL with his position (he is placed at the leg's end only so the later beats can still be looked at, and that is
 // logged). An action that cannot be aimed at is a FAIL and is not fired. Breath is watched over whole cycles. Every
 // PNG is written synchronously before the next step (the last one before quitting). PNGs + checks.txt
 // (PASS/FAIL/ABSENT) + result.json; exit 0 = no FAIL.
 public sealed class PrologueQA:MonoBehaviour {
  string output;readonly List<string> checks=new List<string>();int errors,failures;
  [Serializable] class Result {public bool passed,installed;public int failures,errors;public float blinkSpread,routeMetres;public int stuckLegs,yardPuffs,fifthPuffs;public string[] beats,absent;}
  readonly Result result=new Result();readonly List<string> absent=new List<string>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-prologue-qa");if(i<0||i+1>=args.Length)return;
   new GameObject("Prologue QA").AddComponent<PrologueQA>().output=Path.GetFullPath(args[i+1]);}
  void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;checks.Add("ERROR "+message.Split('\n')[0]);}}
  void Check(bool ok,string what){checks.Add((ok?"PASS ":"FAIL ")+what);if(!ok)failures++;File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();if(!Application.isBatchMode){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),t.EncodeToPNG());Destroy(t);}checks.Add("SHOT "+name+".png");File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);}
  static void Aim(ScenarioPlayer p,Vector3 at){var d=at-p.head.position;var f=d;f.y=0;if(f.sqrMagnitude<1e-4f)return;p.transform.rotation=Quaternion.LookRotation(f);p.pitch=Mathf.Clamp(-Mathf.Atan2(d.y,f.magnitude)*Mathf.Rad2Deg,-80,80);p.head.localRotation=Quaternion.Euler(p.pitch,0,0);}
  static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);
  // Walks through the points (feet positions) with the real controller. A point counts as reached within 0.2 m on the
  // ground plane AND within 0.25 m of its height (so a flight that was not climbed, or a fall, is caught). No progress of
  // 0.15 m in 1.5 s while he has control = stuck.
  IEnumerator Walk(ScenarioPlayer p,string leg,params Vector3[] points){
   bool ok=true;string why="";var from=p.transform.position;
   foreach(var q in points){float best=Flat(q-p.transform.position).magnitude,since=Time.time,give=Time.time+60;
    while(true){var d=Flat(q-p.transform.position);float dist=d.magnitude;
     if(dist<.2f)break;
     if(Time.time>give){ok=false;why="timeout at "+p.transform.position.ToString("F2")+" → "+q.ToString("F2");break;}
     if(!p.control||p.paused){p.qaAxis=Vector2.zero;since=Time.time;yield return null;continue;}
     p.transform.rotation=Quaternion.LookRotation(d);p.pitch=0;p.head.localRotation=Quaternion.identity;p.qaAxis=Vector2.up;
     if(Time.time-since>1.5f){if(best-dist<.15f){ok=false;why="stuck at "+p.transform.position.ToString("F2")+" → "+q.ToString("F2");break;}best=dist;since=Time.time;}
     yield return null;}
    p.qaAxis=Vector2.zero;
    if(ok&&Mathf.Abs(p.transform.position.y-q.y)>.25f){ok=false;why="arrived at height "+p.transform.position.y.ToString("F2")+", expected "+q.y.ToString("F2");}
    if(!ok)break;}
   p.qaAxis=Vector2.zero;result.routeMetres+=Vector3.Distance(from,p.transform.position);
   Check(ok,"walk «"+leg+"» with the CharacterController"+(ok?"":": "+why));
   if(!ok){result.stuckLegs++;foreach(var hit in Physics.OverlapSphere(p.transform.position+Vector3.up*.8f,.95f,~(1<<31),QueryTriggerInteraction.Ignore))if(!hit.transform.IsChildOf(p.transform))checks.Add("NEAR_STUCK "+GranNav.PathOf(hit.transform)+" bounds="+hit.bounds);yield return Shot("FAILED-walk-"+result.stuckLegs);var end=points[points.Length-1];p.Place(end,p.transform.eulerAngles.y);checks.Add("PLACED at the end of «"+leg+"» after the FAIL above, only to look at the later beats");yield return null;}}
  IEnumerator Fire(StoryDirector story,ScenarioPlayer p,string label,Vector3 look){StoryDirector.Act act=null;float until=Time.time+15;while(Time.time<until&&act==null){act=story.OpenActs.Find(a=>a.label==label&&!a.fired&&(a.when==null||a.when()));if(act==null)yield return null;}
   Check(act!=null,"action «"+label+"» offered");if(act==null)yield break;for(int i=0;i<40&&story.Aimed!=act;i++){Aim(p,look);yield return null;}
   bool aimed=story.Aimed==act;Check(aimed,"«"+label+"» can be aimed at from where he stands (crosshair, range, line of sight)");if(!aimed)yield break;story.QaFire(act);yield return null;}
  IEnumerator Wait(Func<bool> until,float seconds){float end=Time.time+seconds;while(Time.time<end&&!until())yield return null;}
  // Breath over whole cycles: a puff is a jump of ≥8 particles in one frame; between puffs it must fade back to none.
  int puffs;bool faded;
  IEnumerator WatchBreath(StoryDirector story,float seconds){puffs=0;faded=false;int last=0;bool seen=false;for(float t=0;t<seconds;t+=Time.deltaTime){int n=story.QaBreath?story.QaBreath.particleCount:0;if(n-last>=8){puffs++;seen=true;}if(seen&&n==0)faded=true;last=n;yield return null;}}
  IEnumerator Start(){
   Directory.CreateDirectory(output);Application.logMessageReceived+=Log;Application.runInBackground=true;Application.targetFrameRate=60;
   yield return new WaitForSecondsRealtime(2);
   var loop=FindFirstObjectByType<LoopDirector>();var story=loop.story;var flat=loop.flat;var p=flat.player;var sound=loop.sound;
   int barkCount=0;sound.CueStarted+=id=>{if(id=="dog_bark")barkCount++;};
   loop.testing=true;p.automating=true;loop.Pause(false);
   // Keep automated movement isolated from the human keyboard, in this player process only.
   InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
   var keyboard=InputSystem.AddDevice<Keyboard>();foreach(var device in InputSystem.devices)if(device is Keyboard&&device!=keyboard)InputSystem.DisableDevice(device);keyboard.MakeCurrent();
   var mouse=InputSystem.AddDevice<Mouse>();foreach(var device in InputSystem.devices)if(device is Mouse&&device!=mouse)InputSystem.DisableDevice(device);mouse.MakeCurrent();
   var set=FindFirstObjectByType<PrologueSet>(FindObjectsInactive.Include);result.installed=set;Check(set,"the prologue set is installed (Ataka.Editor.PrologueInstaller.Install)");
   if(!set){Finish();yield break;}
   foreach(var l in set.stairLights)Check(l&&l.intensity==0,"before the prologue the stair light "+(l?l.name:"?")+" is off");
   float G=set.lobby.position.y-.02f,door=set.entranceDoor.transform.position.z;
   story.QaBegin("Prologue");
   yield return Wait(()=>story.QaPrologueBeat=="black",5);yield return new WaitForSeconds(1);Check(story.QaVhsTitle=="15 ноября 1996. 19:10","3.1 black, the time code bottom left");yield return Shot("prologue-01-timecode");
   yield return Wait(()=>story.QaPrologueBeat=="yard",6);yield return new WaitForSeconds(2);
   Check(set.IsOn&&p.control,"3.2 the yard: the set's lights on, control given");yield return Shot("prologue-02-yard");
   float route=Vector3.Distance(Flat(p.transform.position),Flat(set.entranceDoor.transform.position));Check(route>38.5f&&route<41.5f,"3.2 «до подъезда 40 м»: "+route.ToString("F1")+" m from the garages to the entrance door");
   Check(story.QaYardSnow,"3.2 wet snow falls in the yard (the courtyard's own particle system, copied)");
   yield return WatchBreath(story,7.5f);result.yardPuffs=puffs;Check(puffs>=2&&faded,"3.2 his breath steams in the cold: "+puffs+" puffs in 7.5 s, fading between them="+faded);
   // 3.3 the roof edge: he walks towards the entrance first (the roof is then well above him), then looks up
   yield return Walk(p,"yard: garages → 9 m from the entrance",new Vector3(8.2f,G,door+9.4f));
   string guestBefore=story.QaGuestSignature();
   Aim(p,set.roofFigure.position+Vector3.up*1.1f);yield return Wait(()=>story.QaRoofShown,3);Check(story.QaRoofShown,"3.3 looking up at the roof edge: the stooped figure stands there");if(story.QaRoofShown)yield return Shot("prologue-03-roof-figure");
   yield return new WaitForSeconds(1.8f);yield return null;
   Check(!story.QaRoofShown&&story.QaRoofRestored&&story.QaGuestSignature()==guestBefore,"3.3 after 1.5 s it is gone and the shared Guest is handed back exactly (active, parent, every bone's position/rotation/scale, renderers, property blocks)");
   // 3.4 the entrance door
   yield return Walk(p,"yard: to the entrance door",new Vector3(8.45f,G,door+1.25f));
   yield return Fire(story,p,"Открыть дверь подъезда",set.entranceDoor.transform.position+Vector3.up*.1f);yield return new WaitForSeconds(1);Check(set.entranceDoor.open,"3.4 the iron entrance door opens");yield return Shot("prologue-04-entrance");
   yield return Walk(p,"through the entrance door",new Vector3(8.2f,G,door+.7f),new Vector3(8.2f,G,set.entranceInside.position.z));yield return Wait(()=>story.QaPrologueBeat=="slam",3);Check(story.QaPrologueBeat=="slam"&&!set.entranceDoor.open,"3.4 it slams shut behind him on its spring");
   // 3.5 the lobby notice
   yield return Walk(p,"into the lobby",new Vector3(7.9f,G,10.95f));Aim(p,set.notice.position);yield return null;yield return Shot("prologue-05-lobby");
   yield return Fire(story,p,"Прочитать объявление",set.notice.position);yield return new WaitForSeconds(.6f);var v=story.QaViewer;Check(v&&v.IsOpen,"3.5 the «ПРОПАЛ ЧЕЛОВЕК» notice can be read");yield return Shot("prologue-06-notice");
   if(v&&v.Current!=null&&!v.Current.loaded)absent.Add("notice picture Resources/"+StoryDirector.NoticeResource+".png (photo bleached white + the text) — drawn by code until then");story.QaLeaveLeisure();yield return new WaitForSeconds(.3f);
   // 3.6 the stairs, every flight walked: up the west flight to each floor, across, up the east flight to the half landing
   const float W=7.12f,E=8.27f,Land=8.6f,Half=12.0f;
   yield return Walk(p,"lobby → first floor (flight 1)",new Vector3(7.9f,G,Half),new Vector3(W,G,Half),new Vector3(W,G+1.4f,Land));
   for(int k=2;k<=5;k++){float y=-(5-k)*2.8f;
    yield return Walk(p,"floor "+(k-1)+" → half landing (flight "+(2*k-2)+")",new Vector3(E,y-2.8f,Land),new Vector3(E,y-1.4f,Half));
    yield return Walk(p,"half landing → floor "+k+" (flight "+(2*k-1)+")",new Vector3(W,y-1.4f,Half),new Vector3(W,y,Land));
    if(k==3){float y3=y;yield return Walk(p,"third-floor landing",new Vector3(7.7f,y3,8.4f));Aim(p,new Vector3(8.25f,y3+2.2f,8.35f));float lo=float.MaxValue,hi=0;for(float t=0;t<1.5f;t+=Time.deltaTime){lo=Mathf.Min(lo,set.blinkLight.intensity);hi=Mathf.Max(hi,set.blinkLight.intensity);yield return null;}
     result.blinkSpread=hi-lo;Check(hi-lo>.2f,"3.6 the third-floor bulb flickers ("+lo.ToString("F2")+"–"+hi.ToString("F2")+")");yield return Shot("prologue-07-third-floor");yield return Walk(p,"back to the stairs",new Vector3(W,y3,Land));}
    if(k==4){Aim(p,set.door56.position+Vector3.up*1.2f);yield return Wait(()=>story.QaPrologueBeats.Contains("tv56"),3);Check(story.QaPrologueBeats.Contains("tv56"),"3.6 the TV behind 56, heard on the way up: «…в квартире никто не проживал, однако на столе…»");yield return Shot("prologue-08-door56");}}
   if(!Resources.Load<AudioClip>(StoryDirector.LaughterResource))absent.Add("audience laughter Resources/"+StoryDirector.LaughterResource+" (the channel switch is a click and a subtitle until then)");
   // 3.7 the fifth floor
   Check(story.QaPrologueBeats.Contains("fifth"),"3.7 the fifth floor");Aim(p,set.door59.position+Vector3.up*1.2f);yield return null;yield return Shot("prologue-09-fifth-floor");
   int fifthBarkStart=barkCount;if(!Resources.Load<AudioClip>(StoryDirector.BarkResource))absent.Add("Tuzik's bark Resources/"+StoryDirector.BarkResource+" (silent until supplied)");
   yield return WatchBreath(story,7.5f);result.fifthPuffs=puffs;Check(puffs>=2&&faded,"3.7 breath steam on the cold landing: "+puffs+" puffs in 7.5 s, fading between them="+faded);
   if(Resources.Load<AudioClip>(StoryDirector.BarkResource))Check(barkCount>fifthBarkStart,"3.7 recorded dog_bark cue actually emitted behind59 during landing wait");
   Aim(p,set.hatch.position);yield return null;yield return Shot("prologue-10-attic-hatch");absent.Add("the torn padlock on the attic hatch: no model (the hatch itself is the M1 authored one)");
   // 3.8 the ring at 60
   var outer=story.OuterDoor();yield return Walk(p,"fifth-floor landing → in front of 60",set.landing5.position);yield return Wait(()=>story.QaPrologueBeat=="landing5",3);Check(story.QaPrologueBeat=="landing5","3.8 at flat 60's door");
   yield return Fire(story,p,"Позвонить",(outer?outer.transform.position:flat.entrance.transform.position)+Vector3.up*.3f);yield return Wait(()=>story.QaPrologueBeat=="open",8);yield return new WaitForSeconds(1.2f);
   Check(flat.entrance.open&&(!outer||(outer.open&&Quaternion.Angle(Quaternion.identity,outer.leaf.localRotation)>70))&&flat.grandmother.activeSelf,"3.8 she opens both doors (the tambour's and the flat's), warm light from the hall");yield return Shot("prologue-11-grandmother-opens");
   // into the flat once she has said her line and gone to the kitchen: the evening begins; nothing of the prologue left on
   yield return Wait(()=>story.Objective=="Войти",20);yield return new WaitForSeconds(1.5f);
   yield return Walk(p,"into the flat",new Vector3(8.05f,0,6.6f),new Vector3(7.85f,0,5.4f));yield return Wait(()=>story.QaPrologueBeat=="inside",6);yield return new WaitForSeconds(1);
   bool allOff=!set.IsOn;foreach(var l in set.stairLights)allOff&=l.intensity==0;foreach(var l in set.yardLights)allOff&=l.intensity==0;allOff&=set.blinkLight.intensity==0;
   Check(story.QaPrologueBeat=="inside"&&allOff&&!story.QaYardSnow&&!story.QaBreath&&(!outer||(!outer.open&&outer.locked))&&!sound.BedOn("wind")&&!sound.BedOn("plant_hum")&&!sound.BedOn("lamp_hum"),"3.9 inside: the doors closed, the stairwell/yard lights back to 0, no snow, no breath, no yard sounds");
   result.beats=story.QaPrologueBeats.ToArray();checks.Add("BEATS "+string.Join(" → ",result.beats));checks.Add("ROUTE walked "+result.routeMetres.ToString("F1")+" m, stuck legs "+result.stuckLegs);
   // 4.9: Valya and Tuzik on the landing
   bool models=Resources.Load<GameObject>(StoryDirector.ValyaResource)&&Resources.Load<GameObject>(StoryDirector.TuzikResource);
   story.QaValyaAtTheDoor();yield return Wait(()=>(outer&&outer.open)||story.QaNeighbours,30);yield return new WaitForSeconds(1);Check(!outer||outer.open,"4.9 both doors open for Valya (the tambour's too)");
   if(models)Check(story.QaNeighbours,"4.9 Valya and Tuzik stand on the landing at the open doors");else absent.Add("Valya and Tuzik models Resources/"+StoryDirector.ValyaResource+" / "+StoryDirector.TuzikResource+" (4.9 and 8.1 are voices only until supplied)");
   Aim(p,new Vector3(8.05f,1.3f,7.8f));yield return Shot("prologue-12-valya-at-the-door");story.QaStopAll();yield return null;Check(!story.QaNeighbours&&(!outer||!outer.open),"after 4.9 (or when cut off) the neighbours are gone, the tambour door shut");
   // 8.1: Night2 after the rewind — «ровно тот же момент, что в 4.9»
   story.QaValyaAtTheDoor(true);yield return new WaitForSeconds(1);Check(!outer||outer.open,"8.1 Night2: both doors open again, as in 4.9");if(models)Check(story.QaNeighbours,"8.1 Night2: Valya and Tuzik on the landing again");
   Aim(p,new Vector3(8.05f,1.3f,7.8f));yield return Shot("prologue-13-night2-valya");story.QaStopAll();yield return null;Check(!story.QaNeighbours&&(!outer||!outer.open),"after 8.1 (cut off) the neighbours are gone, the tambour door shut");
   Finish();
  }
  void Finish(){result.absent=absent.ToArray();foreach(var a in absent)checks.Add("ABSENT "+a);Check(errors==0,"no runtime errors ("+errors+")");result.errors=errors;result.failures=failures;result.passed=failures==0&&errors==0;
   File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(result,true));File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);Application.Quit(result.passed?0:1);}
 }
}
