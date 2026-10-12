using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Ataka.Scenario {
 // QA only: -story-smoke <dir> [-story-from Finale|Night1|Night2|Evening]
 // Plays the story chapters by standing ~1.1 m from each offered action, facing it, and firing it ONLY when the real
 // aim selection (StoryDirector.TickActs) picked it. Never presses R (opening the door = death branch).
 // Nights run at 6x time. Records checks.txt / result.json and screenshots of title cards and objectives.
 // This proves flow and aim reachability, not walking routes, art or feel.
 [DefaultExecutionOrder(10000)]
 public sealed class StoryAutopilot:MonoBehaviour {
  LoopDirector loop;StoryDirector story;ScenarioPlayer player;string output;readonly List<string> checks=new List<string>();int errors;bool failed;
  readonly HashSet<string> shots=new HashSet<string>();int peepholes;
  // -story-branch door-death | no-belt | save : the losing branches and checkpoints the normal run never touches.
  int routeFailures;string branch="";bool doorOpened,branchRestartSeen,saveChecked;int restartsAtBranch=-1;readonly HashSet<string> scenesSeen=new HashSet<string>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-story-smoke");if(i<0||i+1>=args.Length)return;
   var go=new GameObject("Story autopilot");var a=go.AddComponent<StoryAutopilot>();a.output=Path.GetFullPath(args[i+1]);}
  void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;checks.Add("ERROR "+message.Split('\n')[0]);}}
  void Check(bool ok,string message){checks.Add((ok?"PASS ":"FAIL ")+message);if(!ok)failed=true;Write();}
  void Write(){Directory.CreateDirectory(output);File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);}
  IEnumerator Shot(string name){if(Application.isBatchMode||!shots.Add(name))yield break;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));}
  string Slug(string s){var chars=s.Where(ch=>char.IsLetterOrDigit(ch)||ch==' ').Take(28).ToArray();return new string(chars).Trim().Replace(' ','-');}
  IEnumerator Start(){
   Directory.CreateDirectory(output);Application.logMessageReceived+=Log;Application.runInBackground=true;Application.targetFrameRate=60;
   yield return new WaitForSecondsRealtime(2);loop=FindFirstObjectByType<LoopDirector>();story=loop.story;player=loop.flat.player;loop.testing=true;player.automating=true;
   var args=Environment.GetCommandLineArgs();int f=Array.IndexOf(args,"-story-from");string from=f>=0&&f+1<args.Length?args[f+1]:"Prologue";
   int b=Array.IndexOf(args,"-story-branch");branch=b>=0&&b+1<args.Length?args[b+1]:"";
   checks.Add("MODE story autopilot from "+from+(branch!=""?" branch "+branch:"")+" (aim-verified fire, 6x nights)");
   if(branch=="save"){story.qaAllowSave=true;story.QaSavePath(Path.Combine(output,"story-checkpoint.json"));}
   loop.Pause(false);
   if(from=="ChaseEnd"){story.Abandon();loop.FinishCore();checks.Add("FinishCore called as at the end of the chase (10 strikes)");}else story.QaBegin(from);
   // Routes are checked AFTER the chapter has set the flat up (doors, stool), and once more in the finale state below.
   yield return new WaitForSecondsRealtime(.8f);ValidateRoutes("start of "+story.chapter);
   lastProgressAt=Time.realtimeSinceStartup;float deadline=Time.realtimeSinceStartup+(from=="Finale"||from=="ChaseEnd"?600:2400);if(from=="ChaseEnd")yield return null;string lastObjective=null,lastCard=null;StoryDirector.Chapter lastChapter=story.chapter;
   while(Time.realtimeSinceStartup<deadline&&!failed){
    var playing=loop.scenes.PlayingScene;if(!string.IsNullOrEmpty(playing)&&scenesSeen.Add(story.chapter+":"+playing))checks.Add("SCENE "+playing+" in "+story.chapter);
    if(branch=="door-death"&&doorOpened&&!branchRestartSeen&&story.QaRestarts>restartsAtBranch){branchRestartSeen=true;Check(scenesSeen.Contains(story.chapter+":DeathShort")&&story.chapter==StoryDirector.Chapter.Night1,"opening the door at 22:02 kills (short death) and restarts night 1 at 22:00");}
    if(branch=="no-belt"&&!branchRestartSeen&&restartsAtBranch>=0&&story.QaRestarts>restartsAtBranch){branchRestartSeen=true;Check(scenesSeen.Contains("Night2:DeathShort")&&story.chapter==StoryDirector.Chapter.Night2,"no belt at 22:05: she enters, short death, restart at 21:42 of night 2");}
    if(branch=="save"&&!saveChecked&&story.chapter==StoryDirector.Chapter.Night1&&File.Exists(story.SavePath)){
     saveChecked=true;var text=File.ReadAllText(story.SavePath);Check(text.Contains("Night1"),"checkpoint written at 21:42 of night 1: "+text);
     story.Continue();yield return new WaitForSeconds(1);Check(story.chapter==StoryDirector.Chapter.Night1&&story.Objective=="Дождаться бабушку","continue from checkpoint resumes night 1");}
    if(story.chapter==StoryDirector.Chapter.Night2&&restartsAtBranch<0&&branch=="no-belt")restartsAtBranch=story.QaRestarts;
    if(story.chapter!=lastChapter){checks.Add("CHAPTER "+lastChapter+" -> "+story.chapter);lastChapter=story.chapter;if(lastChapter==StoryDirector.Chapter.Finale&&from!="Finale"&&from!="ChaseEnd"){yield return new WaitForSecondsRealtime(.8f);ValidateRoutes("finale");}yield return Shot("chapter-"+story.chapter);}
    if(story.chapter==StoryDirector.Chapter.Loop){
     if(from=="Prologue"||from=="Night1")Check(scenesSeen.Contains("Night1:DeathFull"),"first night ends in the full face-tear death (7.4)");
     if(branch=="door-death")Check(branchRestartSeen,"door-death branch exercised");if(branch=="no-belt")Check(branchRestartSeen,"no-belt branch exercised");
     Check(true,"story reached the loop (lap "+loop.Progress.Lap+")");break;}
    if(branch=="save"&&saveChecked){Check(true,"save branch done");break;}
    if(story.chapter==StoryDirector.Chapter.None&&loop.Finished){if(from=="ChaseEnd")Check(scenesSeen.Contains("Finale:DeathFull"),"chase end → finale → face tear → epilogue");Check(true,"epilogue finished, menu shows КОНЕЦ");break;}
    if(story.CurrentCard!=lastCard){lastCard=story.CurrentCard;lastProgressAt=Time.realtimeSinceStartup;if(lastCard!=null){checks.Add("CARD "+lastCard.Replace('\n',' '));yield return Shot("card-"+Slug(lastCard));}}
    if(story.Objective!=lastObjective){lastObjective=story.Objective;lastProgressAt=Time.realtimeSinceStartup;checks.Add("TASK "+lastObjective);if(!string.IsNullOrEmpty(lastObjective))yield return Shot("task-"+Slug(lastObjective));}
    Time.timeScale=story.chapter==StoryDirector.Chapter.Night1||story.chapter==StoryDirector.Chapter.Night2||story.chapter==StoryDirector.Chapter.Finale?6:2;
    // Diagnostics: every 20 s without a new task/card/act, report where everybody is.
    if(Time.realtimeSinceStartup-lastProgressAt>20){lastProgressAt=Time.realtimeSinceStartup;checks.Add("STALL? chapter "+story.chapter+" task '"+story.Objective+"' player "+player.transform.position.ToString("F2")+" "+story.QaGranState+" scene '"+loop.scenes.PlayingScene+"' acts ["+string.Join(", ",story.OpenActs.Select(x=>x.label))+"]");Write();}
    // QA yields the way (runtime collisions unchanged): she already re-planned around the autopilot and still waits.
    if(story.QaGranWaiting>2.5f&&story.QaGranGoal!=null){var spot=YieldSpot(story.QaGranPosition,story.QaGranGoal.Value);checks.Add("QA-YIELD player "+player.transform.position.ToString("F2")+" -> "+spot.ToString("F2")+" for "+story.QaGranState);player.Place(spot,0);yield return new WaitForSeconds(.5f);continue;}
    if(story.Choosing){yield return new WaitForSeconds(.5f);story.QaChoose(1);checks.Add("CHOICE 1");continue;}
    if(branch=="door-death"&&!doorOpened&&story.chapter==StoryDirector.Chapter.Night1){var r=story.OpenActs.FirstOrDefault(a=>a.label=="Открыть дверь");
     if(r!=null){restartsAtBranch=story.QaRestarts;yield return Approach(r);var e=story.OpenActs.FirstOrDefault(a=>a.label=="Глазок"&&(r.at()-a.at()).sqrMagnitude<.05f);
      if(story.Aimed==r||story.Aimed==e){doorOpened=true;checks.Add("ACT [R] Открыть дверь (branch)");story.QaFireCompanion(r);continue;}}}
    bool skipBelt=branch=="no-belt"&&!branchRestartSeen;
    var act=story.OpenActs.FirstOrDefault(a=>!a.fired&&a.key==UnityEngine.InputSystem.Key.E&&(a.when==null||a.when())&&!(a.label=="Глазок"&&peepholes>=1)&&!(skipBelt&&(a.label.Contains("ремень")||a.label.Contains("ремнём"))));
    if(act!=null&&player.control&&string.IsNullOrEmpty(loop.scenes.PlayingScene)){
     yield return Approach(act);
     if(story.Aimed==act){lastProgressAt=Time.realtimeSinceStartup;checks.Add("ACT "+act.label+(act.hold>0?" (hold "+act.hold+"s skipped)":""));yield return Shot("act-"+Slug(act.label));if(act.label=="Глазок")peepholes++;story.QaFire(act);}
     else{checks.Add("LOS blocker: "+story.QaLosBlocker(player.head.position,act.at()));yield return Shot("FAILED-aim-"+Slug(act.label));Check(false,"aim did not select '"+act.label+"' at "+act.at()+" from "+player.head.position+" (selected: "+(story.Aimed!=null?story.Aimed.label:"none")+")");break;}
    }
    // Finale refusal path (11.6б): after one look through the peephole, step away from the door.
    if(story.chapter==StoryDirector.Chapter.Finale&&!loop.flat.entrance.open&&peepholes>=1&&story.OpenActs.Any(a=>a.label=="Глазок")&&Vector3.Distance(player.transform.position,loop.flat.entrance.transform.position)<2.4f)player.Place(new Vector3(5.65f,.02f,5.3f),90);
    // The 22:07 door into the loop needs a step into the doorway.
    if(story.chapter==StoryDirector.Chapter.Night2&&loop.flat.entrance.open&&story.OpenActs.Count==0&&story.Objective==""&&loop.flat.clock.stopped){player.Place(new Vector3(8.05f,.02f,6.0f),0);}
    // Prologue "Войти": step through the opened door.
    if(story.Objective=="Войти")player.Place(new Vector3(8.05f,.02f,5.6f),180);
    if(story.Objective=="Выйти в прихожую")player.Place(new Vector3(7.7f,.02f,3.6f),0);
    yield return null;
   }
   Time.timeScale=1;
   if(Time.realtimeSinceStartup>=deadline)Check(false,"story timeout in chapter "+story.chapter+" task '"+story.Objective+"'");
   Check(errors==0,"no runtime errors ("+errors+")");
   File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(new Result{passed=!failed&&errors==0&&routeFailures==0,chapter=story.chapter.ToString(),errors=errors,routeFailures=routeFailures}));Write();
   yield return new WaitForSecondsRealtime(.5f);Application.Quit(failed||errors>0||routeFailures>0?1:0);
  }
  float lastProgressAt;
  // Free floor spots away from her; the one farthest from her straight line to the goal.
  static readonly Vector3[] Spots={new Vector3(1.9f,.02f,3.6f),new Vector3(3.4f,.02f,3.9f),new Vector3(4.4f,.02f,3.5f),new Vector3(7.6f,.02f,1.7f),new Vector3(8.3f,.02f,4.0f),new Vector3(5.0f,.02f,5.3f),new Vector3(8.2f,.02f,5.4f)};
  static Vector3 YieldSpot(Vector3 from,Vector3 to){Vector3 best=Spots[0];float bestD=-1;from.y=to.y=0;
   foreach(var s in Spots){var p=new Vector3(s.x,0,s.z);var seg=to-from;float t=seg.sqrMagnitude<1e-4f?0:Mathf.Clamp01(Vector3.Dot(p-from,seg)/seg.sqrMagnitude);float d=Vector3.Distance(p,from+seg*t);if(d>bestD){bestD=d;best=s;}}
   return best;}
  void ValidateRoutes(string when){var report=new List<string>();var bad=story.QaValidateRoutes(report);routeFailures+=bad.Count;checks.Add("ROUTES at "+when);checks.AddRange(report);
   foreach(var r in bad)checks.Add("ROUTE-BLOCKED "+r);checks.Add((bad.Count==0?"PASS":"FAIL")+" grandmother routes around furniture at "+when+" ("+bad.Count+" failures; run continues)");Write();}
  // Stand where a player could: free floor for his capsule, within reach, the SAME line-of-sight rule as the prompt,
  // and a walking route from where he is (room doors on that route are opened as he would). Nearest such spot wins.
  // LOS is never relaxed; with no valid spot the failure is reported with the blocker path and a FAILED frame.
  IEnumerator Approach(StoryDirector.Act act){
   // The preceding action may have just started opening a door. Query after its physical pose settles,
   // not in the same frame as open=true; this changes only the explicit QA driver.
   yield return new WaitForSeconds(.65f);Physics.SyncTransforms();
   var target=act.at();var body=player.GetComponent<CharacterController>();float r=body?body.radius:.3f,h=body?body.height:1.8f,eyeH=player.crouched?.94f:1.55f;
   var from=player.transform.position;Vector3 best=Vector3.zero;float bestScore=float.MaxValue;List<ScenarioDoor> bestDoors=null;var hits=new Collider[16];
   for(float radius=.55f;radius<=Mathf.Max(.6f,act.range-.2f);radius+=.15f)for(int k=0;k<32;k++){
    float a=k*Mathf.PI*2/32;var feet=new Vector3(target.x+Mathf.Cos(a)*radius,.02f,target.z+Mathf.Sin(a)*radius);var eye=feet+Vector3.up*eyeH;
    if(Vector3.Distance(eye,target)>act.range-.05f)continue;
    int n=Physics.OverlapCapsuleNonAlloc(feet+Vector3.up*(r+.06f),feet+Vector3.up*(h-r),r,hits,~(1<<31),QueryTriggerInteraction.Ignore);bool blocked=false;
    for(int i=0;i<n;i++)if(!hits[i].transform.IsChildOf(player.transform)){blocked=true;break;}
    if(blocked||!story.QaReachable(eye,target))continue;
    float score=Vector3.Distance(new Vector3(from.x,0,from.z),new Vector3(feet.x,0,feet.z))+Mathf.Abs(radius-.85f);if(score>=bestScore)continue;
    var doors=story.QaPlayerRoute(from,feet);if(doors==null)continue;best=feet;bestScore=score;bestDoors=doors;}
   if(bestDoors==null){
    var away=from-target;away.y=0;if(away.sqrMagnitude<.01f)away=Vector3.back;var eye=target+away.normalized*.9f;eye.y=from.y+eyeH;
    checks.Add("APPROACH-FAILED '"+act.label+"' target "+target.ToString("F2")+": no free spot within "+act.range.ToString("F1")+" m with line of sight and a walking route; from "+eye.ToString("F2")+" first blocker: "+story.QaLosBlocker(eye,target));
    var look0=target-eye;player.Place(new Vector3(eye.x,.02f,eye.z),Quaternion.LookRotation(new Vector3(look0.x,0,look0.z)).eulerAngles.y);yield return null;yield return Shot("FAILED-approach-"+Slug(act.label));yield break;}
   foreach(var d in bestDoors)if(!d.open&&!d.locked){d.open=true;checks.Add("QA opens "+d.name+" on the way to '"+act.label+"'");}
   if(bestDoors.Count>0)yield return new WaitForSeconds(.8f);
   var look=target-(best+Vector3.up*eyeH);float yaw=Quaternion.LookRotation(new Vector3(look.x,0,look.z)).eulerAngles.y;float pitch=-Mathf.Atan2(look.y,new Vector2(look.x,look.z).magnitude)*Mathf.Rad2Deg;
   player.Place(best,yaw,pitch);yield return null;yield return null;
  }
  [Serializable]class Result{public bool passed;public string chapter;public int errors,routeFailures;}
 }
}
