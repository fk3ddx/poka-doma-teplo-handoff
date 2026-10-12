using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Ataka.Scenario {
 // QA only (~20 s):  -door-qa <dir>
 // Door states at the two places where the story and the loop hand the flat to each other, measured on the real scene:
 // 1) "Continue" into the evening (grandmother already in the kitchen): kitchen and bedroom open, and a walking route
 //    from the flat door into the kitchen and the bedroom that crosses no closed door;
 // 2) the real story→loop hand-off (EnterLoop): kitchen and bathroom closed AND locked, carpet door locked, flat door shut,
 //    no route into the kitchen; 3) back into the story (night 1): kitchen open again. Writes checks.txt, exit 1 on FAIL.
 public sealed class DoorStateQA:MonoBehaviour {
  string output;readonly List<string> checks=new List<string>();int errors;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-door-qa");if(i<0||i+1>=args.Length)return;
   new GameObject("Door state QA").AddComponent<DoorStateQA>().output=Path.GetFullPath(args[i+1]);}
  void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;checks.Add("ERROR "+message.Split('\n')[0]);}}
  void Check(bool ok,string what){checks.Add((ok?"PASS ":"FAIL ")+what);}
  static string State(ScenarioDoor d)=>d?(d.open?"open":"closed")+(d.locked?" locked":""):"missing";
  IEnumerator Start(){
   Directory.CreateDirectory(output);Application.logMessageReceived+=Log;Application.runInBackground=true;
   yield return new WaitForSecondsRealtime(2);
   var loop=FindFirstObjectByType<LoopDirector>();var story=loop.story;var flat=loop.flat;var p=flat.player;
   loop.testing=true;p.automating=true;loop.Pause(false);
   checks.Add("MODE door-state QA; core test buttons in the menu: "+(LoopDirector.QaMenuShown?"shown (-qa given)":"hidden"));
   var from=new Vector3(8.0f,0,5.0f);
   // 1) continue into the evening
   story.QaBegin("Evening");yield return new WaitForSeconds(1.5f);
   checks.Add("EVENING kitchen "+State(flat.kitchen)+", bedroom "+State(flat.bedroom)+", bathroom "+State(flat.bath)+", grandmother "+(flat.grandmother.activeSelf?flat.grandmother.transform.position.ToString("F2"):"hidden"));
   Check(flat.kitchen.open&&!flat.kitchen.locked,"continued evening: kitchen door open");
   Check(flat.bedroom.open&&!flat.bedroom.locked,"continued evening: bedroom door open");
   Check(story.QaWalkableNow(from,StoryDirector.QaNode("Kitchen")),"continued evening: walking route hall → kitchen crosses no closed door");
   Check(story.QaWalkableNow(from,StoryDirector.QaNode("Bedroom")),"continued evening: walking route hall → bedroom crosses no closed door");
   // 2) the real hand-off into the loop
   story.QaEnterLoop();yield return new WaitForSeconds(1.5f);
   checks.Add("LOOP lap "+loop.Progress.Lap+": kitchen "+State(flat.kitchen)+", bathroom "+State(flat.bath)+", carpet "+State(flat.carpet)+", flat door "+State(flat.entrance));
   Check(!flat.kitchen.open&&flat.kitchen.locked,"loop entry: kitchen door closed and locked (SCENARIO 9.2)");
   Check(!flat.bath.open&&flat.bath.locked,"loop entry: bathroom door closed and locked");
   Check(flat.carpet.locked&&!flat.carpet.open,"loop entry: carpet door locked until the lap's gate");
   Check(flat.entrance.locked&&!flat.entrance.open,"loop entry: flat door shut");
   Check(!story.QaWalkableNow(from,StoryDirector.QaNode("Kitchen")),"loop entry: no walking route into the kitchen");
   Check(story.QaWalkableNow(from,StoryDirector.QaNode("Bedroom")),"loop entry: the lap route hall → bedroom is open");
   // 3) back into the story
   story.QaBegin("Night1");yield return new WaitForSeconds(1.5f);
   checks.Add("NIGHT1 kitchen "+State(flat.kitchen)+", bedroom "+State(flat.bedroom));
   Check(flat.kitchen.open&&!flat.kitchen.locked,"story after the loop: kitchen open again");
   Check(errors==0,"no runtime errors ("+errors+")");
   File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);yield return new WaitForSecondsRealtime(.5f);Application.Quit(checks.Exists(c=>c.StartsWith("FAIL "))?1:0);
  }
 }
}
