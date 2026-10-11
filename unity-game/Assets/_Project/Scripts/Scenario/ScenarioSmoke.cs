using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Ataka.Scenario {
 public sealed class ScenarioSmoke:MonoBehaviour {
  LoopDirector loop;ScenarioPlayer player;Keyboard keyboard;string output;List<string> checks=new List<string>();int errors;bool failed;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-scenario-smoke");if(i>=0&&i+1<args.Length){var s=new GameObject("Scenario smoke").AddComponent<ScenarioSmoke>();s.output=args[i+1];}}
  IEnumerator Start(){
   Directory.CreateDirectory(output);Application.logMessageReceived+=Log;Application.runInBackground=true;Application.targetFrameRate=60;
   yield return new WaitForSecondsRealtime(2);loop=FindFirstObjectByType<LoopDirector>();player=loop.flat.player;player.automating=true;loop.testing=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;keyboard=InputSystem.AddDevice<Keyboard>();
   yield return Shot("00-menu");loop.Pause(false);checks.Add("menu-to-play");
   StartCoroutine(CaptureScenes());StartCoroutine(CaptureVestibules());
   if(Array.IndexOf(Environment.GetCommandLineArgs(),"-scenario-chase-only")>=0){loop.chase.Begin();yield return ChaseRun();Finish();yield break;}
   yield return Walk(new Vector3(7.65f,0,3.8f));Look(loop.flat.torchPickup.position);yield return Press(Key.E);yield return new WaitForSeconds(.2f);Check(player.hasFlashlight,"flashlight pickup");yield return Shot("01-hall");
   for(int lap=1;lap<=6&&!failed;lap++){
    Check(loop.Progress.Lap==lap,"lap "+lap+" entered");
    if(lap==5){Check(loop.flat.bedroom.locked&&!loop.flat.bedroom.open,"fifth bedroom locked before phone");Check(Mathf.Abs(loop.flat.bedroom.closedAngle-Mathf.Asin(.2f/.8f)*Mathf.Rad2Deg)<.01f,"fifth bedroom has 20cm ajar gap");yield return Walk(new Vector3(7.65f,0,3.8f));Look(loop.flat.phone.position);yield return Press(Key.E);yield return WaitBusy();}
    yield return Walk(new Vector3(7.9f,0,5.35f));yield return Walk(new Vector3(5.65f,0,5.35f));yield return Shot("lap"+lap+"-clock");yield return Walk(new Vector3(5.65f,0,3.6f));
    if(lap==3){yield return Walk(new Vector3(5.25f,0,1.15f));yield return WaitBusy();yield return Walk(new Vector3(5.65f,0,3.6f));}
    if(lap==4){Look(loop.flat.television.position);yield return new WaitForSeconds(3.5f);yield return WaitBusy();}
    yield return Walk(new Vector3(3.25f,0,3.6f));
    if(lap==5){Look(loop.flat.bedroom.transform.position);yield return Press(Key.E);yield return WaitBusy();}
    yield return Walk(new Vector3(2.3f,0,3.6f));yield return WaitBusy();
    if(lap==2){yield return Walk(new Vector3(1.4f,0,3.1f));yield return Press(Key.LeftCtrl);Look(loop.flat.bed.position);yield return null;yield return null;checks.Add("UNDERBED crouch="+player.crouched+" distance="+Vector3.Distance(player.head.position,loop.flat.bed.position)+" dot="+Vector3.Dot(player.head.forward,(loop.flat.bed.position-player.head.position).normalized)+" prompt="+loop.prompt.text);yield return Shot("underbed-before");yield return Press(Key.E);yield return WaitBusy();yield return Press(Key.LeftCtrl);}
    yield return Walk(new Vector3(.9f,0,3.75f));Look(loop.flat.carpet.transform.position);yield return Shot("lap"+lap+"-gate");
    if(lap==6){Check(!loop.Progress.CarpetDoorUnlocked,"sixth carpet stays locked");break;}
    Check(loop.Progress.CarpetDoorUnlocked,"gate "+lap+" released by event");yield return Press(Key.E);yield return new WaitForSeconds(.7f);yield return Walk(new Vector3(.9f,0,5.1f));yield return new WaitForSeconds(1);
    Look(loop.flat.entrance.transform.position);yield return Press(Key.E);yield return new WaitForSeconds(.7f);yield return Walk(new Vector3(8.05f,0,5.55f));
   }
   if(!failed){yield return Walk(new Vector3(2.25f,0,3.6f));yield return Walk(new Vector3(5.65f,0,3.6f));yield return Walk(new Vector3(5.65f,0,5.35f));yield return Walk(new Vector3(8.05f,0,5.45f));Look(loop.flat.entrance.transform.position);yield return Press(Key.E);yield return WaitBusy();float chaseDeadline=Time.realtimeSinceStartup+5;while(!loop.chase.Active&&Time.realtimeSinceStartup<chaseDeadline)yield return null;Check(loop.chase.Active,"chase entered");Check(player.transform.position.z<7&&player.transform.position.z>4,"chase starts in original hallway without visible teleport");yield return Shot("chase-start");}
   if(!failed)yield return ChaseRun();
   Finish();
  }
  IEnumerator CaptureVestibules(){
   int captured=0;
   while(!loop.Finished){var local=loop.flat.sourceVestibule.InverseTransformPoint(player.transform.position);
    if(loop.Busy&&loop.Progress.Lap>captured&&string.IsNullOrEmpty(loop.scenes.PlayingScene)&&Mathf.Abs(local.z)<.18f&&Mathf.Abs(local.x)<.2f){
     int lap=loop.Progress.Lap;captured=lap;
     while(loop.Progress.Lap==lap&&!loop.flat.carpet.IsClosed)yield return null;
     yield return Shot("seam"+lap+"-closed");
     while(loop.Progress.Lap==lap&&loop.Busy)yield return null;
     yield return Shot("seam"+lap+"-mapped");
    }yield return null;
   }
  }
  IEnumerator CaptureScenes(){var captured=new HashSet<string>();while(!loop.Finished){var scene=loop.scenes.PlayingScene;float t=loop.scenes.FrameTime;if(!string.IsNullOrEmpty(scene)){foreach(float at in new[]{.4f,1.1f,1.7f,2.1f,2.4f,2.6f,2.9f,3.6f,4.6f,5.1f}){string key=scene+"-"+at.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture);if(t>=at&&t<at+.1f&&!captured.Contains(key)){captured.Add(key);yield return Shot("scene-"+key);break;}}}yield return null;}}
  IEnumerator ChaseRun(){
   InputSystem.QueueStateEvent(keyboard,new KeyboardState());
   float elapsed=loop.chase.Progress.Elapsed;loop.Pause(true);yield return new WaitForSecondsRealtime(.5f);Check(Mathf.Abs(loop.chase.Progress.Elapsed-elapsed)<.01f,"pause freezes chase clock");loop.Pause(false);
   int deaths=loop.chase.DeathCount;float deadline=Time.realtimeSinceStartup+18;
   while(loop.chase.DeathCount==deaths||loop.chase.Ending){if(Time.realtimeSinceStartup>deadline){Check(false,"stall death restart timeout");yield break;}yield return null;}
   Check(loop.chase.Active&&loop.chase.Progress.Elapsed<.5f,"stall death restarts chase");
   if(Array.IndexOf(Environment.GetCommandLineArgs(),"-scenario-death-skip")>=0){
    int beforeSkip=loop.chase.DeathCount;float waitDeadline=Time.realtimeSinceStartup+18;
    while(loop.scenes.PlayingScene!="DeathShort"&&Time.realtimeSinceStartup<waitDeadline)yield return null;
    Check(loop.scenes.PlayingScene=="DeathShort","repeat death available for skip");
    yield return new WaitForSeconds(.2f);float skipStarted=Time.time;
    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));
    while(loop.scenes.PlayingScene=="DeathShort"&&Time.time-skipStarted<2)yield return null;
    InputSystem.QueueStateEvent(keyboard,new KeyboardState());
    Check(Time.time-skipStarted<1,"held E skips repeat death");
    while(loop.chase.Ending&&Time.realtimeSinceStartup<waitDeadline)yield return null;
    Check(loop.chase.DeathCount==beforeSkip+1&&!loop.chase.Ending,"skipped death restarts chase");
   }
   deadline=Time.realtimeSinceStartup+85;int startDeaths=loop.chase.DeathCount;bool cornerCaptured=false;yield return Shot("chase-hallway-entry");
   while(!loop.Finished&&!failed){
    if(Time.realtimeSinceStartup>deadline||loop.chase.DeathCount!=startDeaths){Check(false,"chase route failed at "+player.transform.position+" elapsed="+loop.chase.Progress.Elapsed);yield return Shot("FAILED-chase");yield break;}
    if(!cornerCaptured&&player.transform.position.y < -30){cornerCaptured=true;yield return Shot("chase-first-corner");Check(Vector3.Distance(player.view.transform.position,player.head.position)<.03f,"camera follows rigid shift without a frame of lag");}
    var module=loop.chase.Current;var local=module.transform.InverseTransformPoint(player.transform.position);
    if(module.warm&&local.z>2.2f&&local.z<3.8f){
     InputSystem.QueueStateEvent(keyboard,new KeyboardState());Look(module.clock.transform.position);yield return Shot("warm-clock");Check(loop.chase.Progress.Elapsed>=42&&loop.chase.Progress.Elapsed<=50,"warm clock reached within 42вЂ“50s");
     InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));yield return new WaitForSeconds(2.15f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
     Check(loop.chase.Progress.Wound,"clock wound by holding E two seconds");yield return Press(Key.E);float endDeadline=Time.realtimeSinceStartup+13;while(!loop.Finished&&Time.realtimeSinceStartup<endDeadline){if(module.transform.localScale!=Vector3.one){Check(false,"warm module collapsed under player");break;}yield return null;}Check(loop.Finished,"ten strikes return to warm apartment");yield return new WaitForSecondsRealtime(.4f);yield return Shot("core-end");yield break;
    }
    var localTarget=module.turn&&local.z>CorridorModule.CornerZ-.15f?new Vector3(-1.1f,0,CorridorModule.CornerZ):new Vector3(0,0,module.turn?CorridorModule.CornerZ+.03f:6.35f);
    var direction=module.transform.TransformPoint(localTarget)-player.transform.position;direction.y=0;player.transform.rotation=Quaternion.LookRotation(direction);player.pitch=0;
    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.F));yield return null;
   }
  }
  IEnumerator Walk(Vector3 target){if(failed)yield break;float start=Time.realtimeSinceStartup;int startLap=loop.Progress.Lap;bool startChase=loop.chase.Active;
   while(Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(target.x,target.z))>.12f){
    if(loop.Progress.Lap!=startLap||loop.chase.Active!=startChase)break;
    if(Time.realtimeSinceStartup-start>12){Check(false,"walk blocked toward "+target+" at "+player.transform.position+" yaw="+player.transform.eulerAngles.y+" W="+Keyboard.current.wKey.isPressed+" control="+player.control+" speed="+player.ActualSpeed+" paused="+player.paused);yield return Shot("FAILED-walk");break;}
    var dir=target-player.transform.position;dir.y=0;player.transform.rotation=Quaternion.LookRotation(dir);player.pitch=5;
    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return null;
   }InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;checks.Add("WALK "+player.transform.position+" target="+target);if(player.transform.position.y < -.2f){Check(false,"fell below floor");yield return Shot("FAILED-floor");}
  }
  void Look(Vector3 point){var dir=point-player.head.position;var q=Quaternion.LookRotation(dir);player.transform.rotation=Quaternion.Euler(0,q.eulerAngles.y,0);player.pitch=Mathf.DeltaAngle(0,q.eulerAngles.x);player.head.localRotation=Quaternion.Euler(player.pitch,0,0);}
  IEnumerator Press(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;}
  IEnumerator WaitBusy(){yield return null;float start=Time.realtimeSinceStartup;while(loop.Busy){if(Time.realtimeSinceStartup-start>15){Check(false,"event timeout "+loop.LastEvent);break;}yield return null;}}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());Destroy(image);}
  void Check(bool ok,string message){checks.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);if(!ok)failed=true;}
  void Log(string s,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;checks.Add("ERROR "+s);}}
  void Finish(){File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(new Result{passed=!failed&&errors==0,errors=errors,lap=loop.Progress.Lap,chase=loop.chase.Active},true));Application.Quit(failed||errors>0?1:0);}
  [Serializable]class Result{public bool passed,chase;public int errors,lap;}
 }
}
