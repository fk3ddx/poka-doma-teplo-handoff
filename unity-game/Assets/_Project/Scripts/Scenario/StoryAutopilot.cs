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
   checks.Add("MODE story autopilot from "+from+" (aim-verified fire, 6x nights, no R)");
   loop.Pause(false);story.QaBegin(from);
   float deadline=Time.realtimeSinceStartup+(from=="Finale"?600:2400);string lastObjective=null,lastCard=null;StoryDirector.Chapter lastChapter=story.chapter;
   while(Time.realtimeSinceStartup<deadline&&!failed){
    if(story.chapter!=lastChapter){checks.Add("CHAPTER "+lastChapter+" -> "+story.chapter);lastChapter=story.chapter;yield return Shot("chapter-"+story.chapter);}
    if(story.chapter==StoryDirector.Chapter.Loop){Check(true,"story reached the loop (lap "+loop.Progress.Lap+")");break;}
    if(story.chapter==StoryDirector.Chapter.None&&loop.Finished){Check(true,"epilogue finished, menu shows КОНЕЦ");break;}
    if(story.CurrentCard!=lastCard){lastCard=story.CurrentCard;if(lastCard!=null){checks.Add("CARD "+lastCard.Replace('\n',' '));yield return Shot("card-"+Slug(lastCard));}}
    if(story.Objective!=lastObjective){lastObjective=story.Objective;checks.Add("TASK "+lastObjective);if(!string.IsNullOrEmpty(lastObjective))yield return Shot("task-"+Slug(lastObjective));}
    Time.timeScale=story.chapter==StoryDirector.Chapter.Night1||story.chapter==StoryDirector.Chapter.Night2||story.chapter==StoryDirector.Chapter.Finale?6:2;
    if(story.Choosing){yield return new WaitForSeconds(.5f);story.QaChoose(1);checks.Add("CHOICE 1");continue;}
    var act=story.OpenActs.FirstOrDefault(a=>!a.fired&&a.key==UnityEngine.InputSystem.Key.E&&(a.when==null||a.when())&&!(a.label=="Глазок"&&peepholes>=1));
    if(act!=null&&player.control&&string.IsNullOrEmpty(loop.scenes.PlayingScene)){
     yield return Approach(act);
     if(story.Aimed==act){checks.Add("ACT "+act.label+(act.hold>0?" (hold "+act.hold+"s skipped)":""));yield return Shot("act-"+Slug(act.label));if(act.label=="Глазок")peepholes++;story.QaFire(act);}
     else{Check(false,"aim did not select '"+act.label+"' at "+act.at()+" from "+player.head.position+" (selected: "+(story.Aimed!=null?story.Aimed.label:"none")+")");break;}
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
   File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(new Result{passed=!failed&&errors==0,chapter=story.chapter.ToString(),errors=errors}));Write();
   yield return new WaitForSecondsRealtime(.5f);Application.Quit(failed||errors>0?1:0);
  }
  IEnumerator Approach(StoryDirector.Act act){
   var target=act.at();var flat=player.transform.position;var away=flat-target;away.y=0;if(away.sqrMagnitude<.01f)away=Vector3.back;away.Normalize();
   float distance=target.y<.6f?Mathf.Min(.9f,act.range*.6f):Mathf.Min(1.1f,act.range*.7f);
   // Do not stand inside or behind a wall/door: stop short of the first obstacle on the floor line.
   var origin=new Vector3(target.x,1f,target.z);
   foreach(var hit in Physics.RaycastAll(origin,away,distance+.4f,~(1<<31),QueryTriggerInteraction.Ignore))if(!hit.transform.IsChildOf(player.transform)&&hit.distance>.05f)distance=Mathf.Min(distance,Mathf.Max(.45f,hit.distance-.35f));
   var feet=new Vector3(target.x,.02f,target.z)+away*distance;
   var look=target-(feet+Vector3.up*1.55f);float yaw=Quaternion.LookRotation(new Vector3(look.x,0,look.z)).eulerAngles.y;float pitch=-Mathf.Atan2(look.y,new Vector2(look.x,look.z).magnitude)*Mathf.Rad2Deg;
   player.Place(feet,yaw,pitch);yield return null;yield return null;
  }
  [Serializable]class Result{public bool passed;public string chapter;public int errors;}
 }
}
