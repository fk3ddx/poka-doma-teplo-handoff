using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Ataka.Scenario {
 // QA only, ~1 minute instead of the 20-minute evening:  -death-qa <dir> [-death-qa-torch on|off]
 // Starts night 1 (real story state, blackout look), stands the player at two real spots with the torch on/off and
 // plays DeathFull and DeathShort, capturing PNGs at fixed FrameTime marks: <dir>/<scene>-<spot>-<t>.png + checks.txt.
 // It proves staging and framing only; sound, feel and art acceptance stay with people.
 [DefaultExecutionOrder(10001)]
 public sealed class DeathCloseupQA:MonoBehaviour {
  static readonly float[] FullMarks={.2f,.45f,.7f,1.0f,1.3f,1.7f,2.1f,2.5f,3.0f,3.6f,4.3f,5.0f,6.0f};
  static readonly float[] ShortMarks={.2f,.45f,.7f,1.0f,1.3f,1.7f,2.1f,2.6f,3.2f};
  static readonly (string name,Vector3 feet,float yaw)[] Spots={("hall",new Vector3(7.9f,.02f,4.4f),0),("living",new Vector3(4.6f,.02f,3.2f),200)};
  string output;bool torch=true;readonly List<string> checks=new List<string>();int errors;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-death-qa");if(i<0||i+1>=args.Length)return;
   var qa=new GameObject("Death close-up QA").AddComponent<DeathCloseupQA>();qa.output=Path.GetFullPath(args[i+1]);
   int t=Array.IndexOf(args,"-death-qa-torch");if(t>=0&&t+1<args.Length)qa.torch=args[t+1]!="off";}
  void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;checks.Add("ERROR "+message.Split('\n')[0]);}}
  IEnumerator Start(){
   Directory.CreateDirectory(output);Application.logMessageReceived+=Log;Application.runInBackground=true;Application.targetFrameRate=60;
   yield return new WaitForSecondsRealtime(2);
   var loop=FindFirstObjectByType<LoopDirector>();var story=loop.story;var flat=loop.flat;var p=flat.player;var scenes=loop.scenes;
   loop.testing=true;p.automating=true;loop.Pause(false);story.QaBegin("Night1");yield return new WaitForSecondsRealtime(1.5f);
   flat.look.Apply(FlatCondition.Blackout);if(flat.look.exterior)flat.look.exterior.Blackout(true);
   p.hasFlashlight=torch;
   var fd=scenes.faceTear;checks.Add("MODE death close-up QA, torch "+(torch?"on":"off")+", claw source "+(fd&&fd.clawSource?fd.clawSource.name:"none (fallback)")+
    (fd?", fingers "+fd.clawFingers.ToString("F3")+" palm "+fd.clawPalm.ToString("F3")+" roll "+fd.clawRoll+" euler "+fd.clawEuler+" length "+fd.clawLength+" fill "+fd.fillIntensity:""));
   checks.Add((fd&&fd.clawSource?"PASS ":"FAIL ")+"ready claw source assigned (fallback is not accepted)");
   foreach(var scene in new[]{"DeathFull","DeathShort"})foreach(var spot in Spots){
    p.Place(spot.feet,spot.yaw,0);yield return null;yield return null;yield return new WaitForSeconds(.3f);
    var marks=scene=="DeathFull"?FullMarks:ShortMarks;int k=0;StartCoroutine(scenes.Play(scene));yield return null;
    while(scenes.PlayingScene==scene){
     if(k<marks.Length&&scenes.FrameTime>=marks[k]){
      yield return new WaitForEndOfFrame();string file=scene+"-"+spot.name+"-"+marks[k].ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png";
      if(!Application.isBatchMode)ScreenCapture.CaptureScreenshot(Path.Combine(output,file));
      checks.Add("SHOT "+file+" FrameTime "+scenes.FrameTime.ToString("F2")+" torch light "+(p.flashlight.enabled?"on":"off")+" guest "+(flat.guest.activeSelf?flat.guest.transform.position.ToString("F2"):"hidden"));k++;continue;}
     yield return null;}
    checks.Add((k==marks.Length?"PASS ":"FAIL ")+scene+" at "+spot.name+": "+k+"/"+marks.Length+" marks captured");
    loop.sound.EndDeathRecovery();yield return new WaitForSeconds(.5f);
   }
   checks.Add((errors==0?"PASS":"FAIL")+" no runtime errors ("+errors+")");
   File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);yield return new WaitForSecondsRealtime(1);Application.Quit(errors>0||checks.Exists(c=>c.StartsWith("FAIL "))?1:0);
  }
 }
}
